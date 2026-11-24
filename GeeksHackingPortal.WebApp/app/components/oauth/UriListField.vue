<script setup lang="ts">
/**
 * An editable list of URIs, one per row (an OAuth app's redirect URIs). Each row has its own
 * error; pasting several lines into a row splits them into rows. Required lists keep at least
 * one row.
 */
const props = defineProps<{
  label: string
  /** "redirect URI": used in the Add button and each row's accessible name */
  itemLabel: string
  hint?: string
  placeholder?: string
  required?: boolean
  error?: string
  rowErrors?: Array<string | undefined>
}>()

const uris = defineModel<string[]>({ required: true })

const baseId = useId()
const labelId = `${baseId}-label`
const canRemove = computed(() => !props.required || uris.value.length > 1)

function inputId(index: number) {
  return `${baseId}-${index}`
}

function updateUri(index: number, value: string) {
  uris.value = uris.value.map((uri, i) => (i === index ? value : uri))
}

async function removeUri(index: number) {
  uris.value = uris.value.filter((_, i) => i !== index)
  await nextTick()
  // keep focus in the list: the row that moved up, else the one before, else the Add button
  const next = document.getElementById(inputId(Math.min(index, uris.value.length - 1)))
  ;(next ?? document.getElementById(`${baseId}-add`))?.focus()
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
  <div
    class="ul"
    role="group"
    :aria-labelledby="labelId"
  >
    <p
      :id="labelId"
      class="ul__label"
    >
      {{ label }}<span
        v-if="required"
        class="ul__req"
        aria-hidden="true"
      > *</span>
    </p>
    <p
      v-if="hint"
      class="ul__hint"
    >
      {{ hint }}
    </p>

    <ol class="ul__rows">
      <li
        v-for="(uri, index) in uris"
        :key="index"
        class="ul__row"
      >
        <div class="ul__line">
          <GInput
            :id="inputId(index)"
            :model-value="uri"
            :placeholder="placeholder"
            :aria-label="`${itemLabel} ${index + 1}`"
            :invalid="!!rowErrors?.[index]"
            :aria-describedby="rowErrors?.[index] ? `${inputId(index)}-error` : undefined"
            inputmode="url"
            autocomplete="off"
            spellcheck="false"
            autocapitalize="off"
            class="ul__input"
            @update:model-value="(value: string | number) => updateUri(index, String(value))"
            @paste="onPaste(index, $event)"
          />
          <GButton
            variant="ghost"
            size="sm"
            icon="material-symbols:close-rounded"
            :aria-label="`Remove ${itemLabel} ${index + 1}`"
            :disabled="!canRemove"
            @click="removeUri(index)"
          />
        </div>
        <p
          v-if="rowErrors?.[index]"
          :id="`${inputId(index)}-error`"
          class="ul__error"
        >
          <Icon
            name="material-symbols:error-rounded"
            aria-hidden="true"
          />
          {{ rowErrors[index] }}
        </p>
      </li>
    </ol>

    <p
      v-if="error"
      class="ul__error"
      role="alert"
    >
      <Icon
        name="material-symbols:error-rounded"
        aria-hidden="true"
      />
      {{ error }}
    </p>

    <GButton
      :id="`${baseId}-add`"
      variant="secondary"
      size="sm"
      icon="material-symbols:add-rounded"
      :label="`Add ${itemLabel}`"
      class="ul__add"
      @click="addUri"
    />
  </div>
</template>

<style scoped>
.ul {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 0;
}
.ul__label {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
}
.ul__req { color: var(--gh-err); }
.ul__hint {
  margin: 0;
  font-size: 13px;
  color: var(--gh-text-muted);
}
.ul__rows {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin: 2px 0 0;
  padding: 0;
  list-style: none;
}
.ul__line {
  display: flex;
  align-items: center;
  gap: 6px;
}
.ul__input {
  flex: 1 1 auto;
  min-width: 0;
}
.ul__error {
  display: flex;
  align-items: center;
  gap: 6px;
  margin: 4px 0 0;
  font-size: 13px;
  color: var(--gh-err);
}
.ul__add {
  align-self: flex-start;
  margin-top: 4px;
}
</style>
