<script setup lang="ts">
/**
 * GInput: single-line text field.
 *
 * Inside <GFormField> it picks up its id, description, invalid, required and disabled state.
 * On its own, pass `aria-label` (or label it some other way) plus `invalid` / `disabled` yourself.
 * Focus shows the yellow offset ring; errors show a red one. Disabled fields are flat and grey.
 */
defineOptions({ inheritAttrs: false })

const props = withDefaults(defineProps<{
  type?: 'text' | 'email' | 'url' | 'search' | 'tel' | 'password' | 'number' | 'date' | 'datetime-local' | 'time'
  placeholder?: string
  icon?: string
  size?: 'sm' | 'md' | 'lg'
  invalid?: boolean
  disabled?: boolean
  readonly?: boolean
  required?: boolean
  id?: string
}>(), {
  type: 'text',
  size: 'md',
})

const model = defineModel<string | number | null>()

const field = useGFormField()
const attrs = useAttrs()

const inputId = computed(() => props.id ?? field?.id.value)
const isInvalid = computed(() => props.invalid || !!field?.invalid.value)
const isDisabled = computed(() => props.disabled || !!field?.disabled.value)
const isRequired = computed(() => props.required || !!field?.required.value)
const describedBy = computed(() =>
  [attrs['aria-describedby'] as string | undefined, field?.describedBy.value].filter(Boolean).join(' ') || undefined,
)

const inputEl = ref<HTMLInputElement | null>(null)
defineExpose({ focus: () => inputEl.value?.focus(), el: inputEl })

// split attrs: class/style go on the wrapper, everything else on the <input>
const wrapperAttrs = computed(() => ({ class: attrs.class, style: attrs.style }))
const inputAttrs = computed(() => {
  const { class: _class, style: _style, ...rest } = attrs
  return rest
})
</script>

<template>
  <div
    class="g-input"
    :class="[
      `g-input--${size}`,
      {
        'g-input--invalid': isInvalid,
        'g-input--disabled': isDisabled,
        'g-input--readonly': readonly,
        'g-input--with-icon': icon,
      },
    ]"
    v-bind="wrapperAttrs"
  >
    <Icon
      v-if="icon"
      :name="icon"
      class="g-input__icon"
      aria-hidden="true"
    />
    <input
      :id="inputId"
      ref="inputEl"
      v-model="model"
      v-bind="inputAttrs"
      class="g-input__control"
      :type="type"
      :placeholder="placeholder"
      :disabled="isDisabled"
      :readonly="readonly"
      :required="isRequired"
      :aria-invalid="isInvalid || undefined"
      :aria-describedby="describedBy"
    >
    <slot name="trailing" />
  </div>
</template>

<style scoped>
.g-input {
  position: relative;
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh);
  background: var(--gh-surface);
  color: var(--gh-text);
  font-family: var(--font-body);
  letter-spacing: var(--gh-tracking);
  transition:
    box-shadow var(--gh-dur-fast) var(--ease-gh),
    border-color var(--gh-dur-fast) var(--ease-gh);
}
.g-input--sm { padding: 0 10px; font-size: 13px; min-height: 34px; }
.g-input--md { padding: 0 14px; font-size: 15px; min-height: 44px; }
.g-input--lg { padding: 0 16px; font-size: 17px; min-height: 52px; }

.g-input__control {
  flex: 1;
  min-width: 0;
  height: 100%;
  padding: 0;
  border: 0;
  outline: none;
  background: transparent;
  color: inherit;
  font: inherit;
  letter-spacing: inherit;
}
.g-input__control::placeholder {
  color: var(--gh-text-subtle);
  opacity: 1;
}
.g-input__icon {
  flex: none;
  width: 1.1em;
  height: 1.1em;
  color: var(--gh-text-muted);
}

/* focus: yellow offset ring (light) / yellow edge (dark) */
.g-input:focus-within {
  border-color: var(--gh-field-focus-border);
  box-shadow: var(--gh-field-focus);
}

/* error: red edge + red offset ring; focus keeps the red so the error stays visible */
.g-input--invalid,
.g-input--invalid:focus-within {
  border-color: var(--gh-err);
  box-shadow: var(--gh-field-error);
}

.g-input--readonly {
  background: var(--gh-surface-sunken);
}

/* disabled: flat, grey, still */
.g-input--disabled {
  cursor: not-allowed;
  background: var(--gh-surface-sunken);
  border-color: var(--gh-line-disabled);
  color: var(--gh-text-disabled);
  box-shadow: none;
  transition: none;
}
.g-input--disabled .g-input__control {
  cursor: not-allowed;
}
.g-input--disabled .g-input__icon {
  color: var(--gh-text-disabled);
}
</style>
