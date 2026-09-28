<script setup lang="ts">
import { computed, nextTick, useId } from 'vue'

const props = defineProps<{
  label: string
  itemLabel: string
  description?: string
  placeholder?: string
  required?: boolean
  error?: string
  rowErrors?: Array<string | undefined>
}>()

const uris = defineModel<string[]>({ required: true })

const baseId = useId()
const canRemove = computed(() => !props.required || uris.value.length > 1)

function inputId(index: number) {
  return `${baseId}-${index}`
}

function updateUri(index: number, value: string | number) {
  uris.value = uris.value.map((uri, i) => (i === index ? String(value) : uri))
}

function removeUri(index: number) {
  uris.value = uris.value.filter((_, i) => i !== index)
}

async function addUri() {
  uris.value = [...uris.value, '']
  await nextTick()
  document.getElementById(inputId(uris.value.length - 1))?.focus()
}

// A text input drops newlines, so a pasted list would collapse into one bogus URI.
// Split it into one row per line instead.
function onPaste(index: number, event: ClipboardEvent) {
  const lines = (event.clipboardData?.getData('text') ?? '')
    .split(/\r?\n/)
    .map(line => line.trim())
    .filter(Boolean)
  if (lines.length < 2)
    return

  event.preventDefault()
  const next = [...uris.value]
  if (next[index]?.trim())
    next.splice(index + 1, 0, ...lines)
  else
    next.splice(index, 1, ...lines)
  uris.value = next
}
</script>

<template>
  <UFormField
    :label="label"
    :description="description"
    :required="required"
    :error="error"
  >
    <div class="space-y-2">
      <UFormField
        v-for="(uri, index) in uris"
        :key="index"
        :error="rowErrors?.[index]"
      >
        <div class="flex items-center gap-2">
          <UInput
            :id="inputId(index)"
            :model-value="uri"
            :placeholder="placeholder"
            :aria-label="`${itemLabel} ${index + 1}`"
            inputmode="url"
            autocomplete="off"
            spellcheck="false"
            class="min-w-0 flex-1"
            @update:model-value="value => updateUri(index, value)"
            @paste="onPaste(index, $event)"
          />
          <UButton
            icon="i-lucide-x"
            color="neutral"
            variant="ghost"
            :aria-label="`Remove ${itemLabel} ${index + 1}`"
            :disabled="!canRemove"
            @click="removeUri(index)"
          />
        </div>
      </UFormField>

      <UButton
        icon="i-lucide-plus"
        color="neutral"
        variant="outline"
        size="xs"
        @click="addUri"
      >
        Add {{ itemLabel }}
      </UButton>
    </div>
  </UFormField>
</template>
