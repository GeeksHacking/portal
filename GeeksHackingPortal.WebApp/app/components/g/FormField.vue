<script setup lang="ts">
/**
 * GFormField: label + control + hint + error.
 *
 * Wires the control inside (e.g. <GInput>) automatically: id, aria-describedby,
 * aria-invalid, required and disabled. Drop-in for the old <UFormField label error>:
 * pass the same `error` string you get from getApiFieldError().
 */
const props = defineProps<{
  label: string
  hint?: string
  error?: string | false | null
  required?: boolean
  disabled?: boolean
  /** Hides the label visually but keeps it for screen readers. */
  hideLabel?: boolean
  id?: string
}>()

const autoId = useId()
const id = computed(() => props.id ?? `g-field-${autoId}`)
const hintId = computed(() => `${id.value}-hint`)
const errorId = computed(() => `${id.value}-error`)
const errorText = computed(() => (props.error ? props.error : ''))

const describedBy = computed(() => {
  const ids = [props.hint ? hintId.value : '', errorText.value ? errorId.value : ''].filter(Boolean)
  return ids.length ? ids.join(' ') : undefined
})

provideGFormField({
  id,
  describedBy,
  invalid: computed(() => !!errorText.value),
  required: computed(() => !!props.required),
  disabled: computed(() => !!props.disabled),
})
</script>

<template>
  <div
    class="g-field"
    :class="{ 'g-field--disabled': disabled }"
  >
    <label
      :for="id"
      class="g-field__label"
      :class="{ 'sr-only': hideLabel }"
    >
      {{ label }}<span
        v-if="required"
        class="g-field__required"
        aria-hidden="true"
      > *</span>
    </label>
    <slot />
    <p
      v-if="hint"
      :id="hintId"
      class="g-field__hint"
    >
      {{ hint }}
    </p>
    <!-- live region stays mounted so new errors are announced -->
    <div
      aria-live="polite"
      class="g-field__live"
    >
      <p
        v-if="errorText"
        :id="errorId"
        class="g-field__error"
      >
        <Icon
          name="material-symbols:error-rounded"
          class="g-field__error-icon"
          aria-hidden="true"
        />
        <span>{{ errorText }}</span>
      </p>
    </div>
  </div>
</template>

<style scoped>
.g-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-family: var(--font-body);
  letter-spacing: var(--gh-tracking);
}
.g-field__label {
  font-weight: 600;
  font-size: 14px;
  color: var(--gh-text);
}
.g-field__required {
  color: var(--gh-err);
}
.g-field--disabled .g-field__label {
  color: var(--gh-text-muted);
}
.g-field__hint {
  margin: 0;
  font-size: 13px;
  color: var(--gh-text-muted);
}
.g-field__live:empty {
  display: none;
}
.g-field__error {
  margin: 0;
  display: flex;
  gap: 6px;
  align-items: flex-start;
  font-size: 13px;
  color: var(--gh-err);
}
.g-field__error-icon {
  flex: none;
  width: 16px;
  height: 16px;
  margin-top: 1px;
}
</style>
