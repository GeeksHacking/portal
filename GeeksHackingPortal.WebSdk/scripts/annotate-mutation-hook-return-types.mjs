import { readdir, readFile, writeFile } from 'node:fs/promises'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'

const hooksDir = join(dirname(fileURLToPath(import.meta.url)), '../src/gen/hooks')

function extractGenerics(source, needle) {
  const start = source.indexOf(needle)
  if (start < 0) {
    return null
  }

  let i = start + needle.length
  let depth = 1
  const from = i

  while (i < source.length) {
    const char = source[i]
    if (char === '<') {
      depth++
    }
    else if (char === '>') {
      depth--
      if (depth === 0) {
        return source.slice(from, i).trim()
      }
    }
    i++
  }

  return null
}

function annotate(source) {
  if (!source.includes('return useMutation<')) {
    return source
  }

  if (source.includes('): UseMutationReturnType<')) {
    return source
  }

  const generics = extractGenerics(source, 'return useMutation<')
  if (!generics) {
    throw new Error('could not parse useMutation generics')
  }

  let next = source.replace(
    'import type { MutationObserverOptions, QueryClient } from "../../useMutation.ts";',
    'import type { MutationObserverOptions, QueryClient, UseMutationReturnType } from "../../useMutation.ts";',
  )

  if (next === source) {
    throw new Error('could not update UseMutationReturnType import')
  }

  next = next.replace(
    /(\n = \{\}) \{/,
    `\n = {}): UseMutationReturnType<${generics}> {`,
  )

  if (!next.includes('): UseMutationReturnType<')) {
    throw new Error('could not add return type annotation')
  }

  return next
}

const files = await readdir(hooksDir)
let count = 0

for (const file of files) {
  if (!file.endsWith('.ts') || file === 'index.ts') {
    continue
  }

  const path = join(hooksDir, file)
  const source = await readFile(path, 'utf8')
  const updated = annotate(source)

  if (updated !== source) {
    await writeFile(path, updated)
    count++
  }
}

console.log(`annotated ${count} mutation hooks`)
