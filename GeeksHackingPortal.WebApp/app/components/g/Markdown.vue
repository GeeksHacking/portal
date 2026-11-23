<script setup lang="ts">
import type { Component } from 'vue'
import { Markdown } from '@comark/vue'
import breaks from '@comark/vue/plugins/breaks'
import html from '@comark/vue/plugins/html'
import security from '@comark/vue/plugins/security'

/**
 * GMarkdown: renders an event description (Markdown written with GMarkdownEditor) in the
 * portal's prose style (`.g-prose`, assets/css/prose.css).
 *
 * Safe for text anyone with organiser access typed:
 * - Markdown plus a short allow-list of HTML formatting tags (<em>, <strong>, <s>, <code>…).
 *   The editor writes those when bold and italic overlap partway, which Markdown can't express.
 *   Any other tag is unwrapped to its text; <script>, <style>, <iframe> and the like are removed
 *   with their content. All attributes are dropped except a link's href and an image's src/alt.
 * - Comark's component syntax isn't parsed
 * - links only to http(s), mailto or same-site paths; others render as plain text.
 *   Links to other sites open in a new tab.
 * - single line breaks are kept, as older plain-text descriptions rely on them
 * - headings are moved down to sit under the page's own heading: `#` and `##` become
 *   `<h{headingLevel}>`, `###` the level below
 *
 * `inline`: a short, single-flow version for cards. Blocks (paragraphs, headings, lists, quotes)
 * run on as one paragraph so a CSS line clamp can cut it; bold, italic and code stay, headings
 * are bold, links and images become plain text (the card has its own actions).
 */
const props = withDefaults(defineProps<{
  value?: string | null
  /** the HTML level for the description's top headings (`#` / `##`) */
  headingLevel?: 2 | 3 | 4
  inline?: boolean
}>(), {
  headingLevel: 3,
})

const SafeLink = defineComponent({
  inheritAttrs: false,
  setup(_, { attrs, slots }) {
    return () => {
      const href = String(attrs.href ?? '').trim()
      const external = /^https?:\/\//i.test(href)
      const allowed = external || /^mailto:/i.test(href) || (/^\/(?!\/)/.test(href))
      if (!allowed)
        return h('span', slots.default?.())
      return h('a', external ? { href, target: '_blank', rel: 'noopener noreferrer' } : { href }, slots.default?.())
    }
  },
})

function heading(level: number): Component {
  const tag = `h${Math.min(level, 6)}`
  return defineComponent({
    inheritAttrs: false,
    setup: (_, { slots }) => () => h(tag, slots.default?.()),
  })
}

// inline mode: every block becomes a span followed by a space
function flat(className?: string): Component {
  return defineComponent({
    inheritAttrs: false,
    setup: (_, { slots }) => () => [h('span', className ? { class: className } : {}, slots.default?.()), ' '],
  })
}
const Space = defineComponent({ setup: () => () => ' ' })
const Nothing = defineComponent({ setup: () => () => null })

const inlineComponents = (() => {
  const block = flat()
  const headingSpan = flat('g-prose__heading')
  return {
    p: block,
    li: block,
    ul: block,
    ol: block,
    blockquote: block,
    pre: block,
    h1: headingSpan,
    h2: headingSpan,
    h3: headingSpan,
    h4: headingSpan,
    h5: headingSpan,
    h6: headingSpan,
    a: flat(),
    br: Space,
    hr: Space,
    img: Nothing,
  }
})()

const components = computed(() => props.inline ? inlineComponents : ({
  a: SafeLink,
  h1: heading(props.headingLevel),
  h2: heading(props.headingLevel),
  h3: heading(props.headingLevel + 1),
  h4: heading(props.headingLevel + 2),
  h5: heading(props.headingLevel + 2),
  h6: heading(props.headingLevel + 2),
}))

const ALLOWED_TAGS = [
  'p', 'br', 'hr', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'ul', 'ol', 'li', 'blockquote', 'pre',
  'code', 'em', 'i', 'strong', 'b', 's', 'del', 'a', 'img', 'span',
]
// removed together with what's inside them
const DROPPED_TAGS = new Set(['script', 'style', 'iframe', 'object', 'embed', 'template', 'textarea', 'noscript', 'svg', 'math', 'form', 'input', 'button', 'select'])
const KEPT_ATTRIBUTES: Record<string, string[]> = { a: ['href', 'title'], img: ['src', 'alt', 'title'] }

type TreeNode = string | [string, Record<string, unknown>, ...TreeNode[]]
function stripAttributes(nodes: TreeNode[]) {
  for (const node of nodes) {
    if (!Array.isArray(node))
      continue
    const [tag, attrs, ...children] = node
    const name = String(tag).toLowerCase()
    const keep = KEPT_ATTRIBUTES[name] ?? []
    node[1] = Object.fromEntries(Object.entries(attrs ?? {}).filter(([key]) => keep.includes(key)))
    // images only from full web addresses; otherwise just the alt text
    if (name === 'img' && !/^https?:\/\//i.test(String(node[1].src ?? ''))) {
      node.splice(0, node.length, 'span', {}, String(attrs?.alt ?? ''))
      continue
    }
    stripAttributes(children)
  }
}
const onlySafeAttributes = {
  name: 'only-safe-attributes',
  post(state: { tree: { nodes: TreeNode[] } }) {
    stripAttributes(state.tree?.nodes ?? [])
  },
}

const options = { registerDefaultPlugins: false, autoClose: true, autoUnwrap: true, headingIds: false }
const plugins = [
  html(),
  breaks(),
  security({
    allowedTags: ALLOWED_TAGS,
    allowedProtocols: ['http', 'https', 'mailto'],
    // unknown tags keep their text; dangerous ones go entirely
    tagFallback: element => DROPPED_TAGS.has(String(element[0]).toLowerCase())
      ? false
      : ['span', {}, ...element.slice(2)] as unknown as typeof element,
  }),
  onlySafeAttributes,
] as ParserPlugins

type ParserPlugins = NonNullable<InstanceType<typeof Markdown>['$props']['plugins']>
</script>

<template>
  <div
    class="g-prose"
    :class="{ 'g-prose--inline': inline }"
  >
    <Suspense>
      <Markdown
        :value="value ?? ''"
        :options="options"
        :plugins="plugins"
        :components="components"
      />
    </Suspense>
  </div>
</template>
