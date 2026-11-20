<script setup lang="ts">
/**
 * GCheckbox: a native checkbox with the redesign's box.
 *
 * - Pass `label` (and optionally `description`) for a self-labelled checkbox; it gets its own id.
 * - Inside <GFormField> without a `label`, the field's label names it and it picks up the
 *   field's id, hint/error, invalid, required and disabled state.
 * - Checked = solid yellow box with a dark tick (yellow is only ever a fill with dark on top).
 * - `indeterminate` shows a dash and sets the native `indeterminate` flag for screen readers.
 * - Disabled is flat and grey with no transition.
 */
defineOptions({ inheritAttrs: false })

const props = defineProps<{
  label?: string
  description?: string
  indeterminate?: boolean
  invalid?: boolean
  disabled?: boolean
  required?: boolean
  name?: string
  value?: string
  id?: string
}>()

const model = defineModel<boolean>({ default: false })

const attrs = useAttrs()
const control = useGFieldControl(props)
const autoId = useId()

// a self-labelled checkbox inside a field still needs its own id, so the field label isn't reused
const inputId = computed(() => props.id ?? (props.label ? `g-check-${autoId}` : control.id.value) ?? `g-check-${autoId}`)
const descriptionId = computed(() => (props.description ? `${inputId.value}-desc` : undefined))
const describedBy = computed(() => [descriptionId.value, control.describedBy.value].filter(Boolean).join(' ') || undefined)

const el = ref<HTMLInputElement | null>(null)
defineExpose({ focus: () => el.value?.focus(), el })

watchEffect(() => {
  if (el.value)
    el.value.indeterminate = !!props.indeterminate
})

const wrapperAttrs = computed(() => ({ class: attrs.class, style: attrs.style }))
const controlAttrs = computed(() => {
  const { class: _class, style: _style, ...rest } = attrs
  return rest
})
</script>

<template>
  <div
    class="g-check"
    :class="{
      'g-check--checked': model || indeterminate,
      'g-check--invalid': control.invalid.value,
      'g-check--disabled': control.disabled.value,
    }"
    v-bind="wrapperAttrs"
  >
    <span class="g-check__control">
      <input
        :id="inputId"
        ref="el"
        v-model="model"
        v-bind="controlAttrs"
        type="checkbox"
        class="g-check__input"
        :name="name"
        :value="value"
        :disabled="control.disabled.value"
        :required="control.required.value"
        :aria-invalid="control.invalid.value || undefined"
        :aria-describedby="describedBy"
      >
      <span
        class="g-check__box"
        aria-hidden="true"
      >
        <Icon
          v-if="indeterminate"
          name="material-symbols:remove-rounded"
          class="g-check__mark"
        />
        <Icon
          v-else-if="model"
          name="material-symbols:check-rounded"
          class="g-check__mark"
        />
      </span>
    </span>
    <span
      v-if="label || description || $slots.default"
      class="g-check__text"
    >
      <label
        :for="inputId"
        class="g-check__label"
      >
        <slot>{{ label }}</slot><span
          v-if="control.required.value && label"
          class="g-check__required"
          aria-hidden="true"
        > *</span>
      </label>
      <span
        v-if="description"
        :id="descriptionId"
        class="g-check__description"
      >{{ description }}</span>
    </span>
  </div>
</template>

<style scoped>
.g-check {
  display: inline-flex;
  align-items: flex-start;
  gap: 10px;
  font-family: var(--font-body);
  font-size: 15px;
  line-height: 1.4;
  letter-spacing: var(--gh-tracking);
  color: var(--gh-text);
}

.g-check__control {
  position: relative;
  flex: none;
  width: 20px;
  height: 20px;
  margin-top: 1px;
}
/* the real input sits on top of the box, so the hit area and focus are native */
.g-check__input {
  position: absolute;
  inset: -4px;
  width: calc(100% + 8px);
  height: calc(100% + 8px);
  margin: 0;
  opacity: 0;
  cursor: pointer;
  z-index: 1;
}
.g-check__box {
  display: grid;
  place-items: center;
  width: 100%;
  height: 100%;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-sm);
  background: var(--gh-surface);
  color: var(--gh-on-accent);
  transition:
    background-color var(--gh-dur-fast) var(--ease-gh),
    border-color var(--gh-dur-fast) var(--ease-gh);
}
.g-check__mark {
  width: 14px;
  height: 14px;
}
.g-check--checked .g-check__box {
  background: var(--gh-accent);
  border-color: var(--gh-on-accent);
}
/* dark: the dark edge disappears into the page, so the yellow fill alone carries it */
.g-check__input:focus-visible + .g-check__box {
  outline: 3px solid var(--gh-focus);
  outline-offset: 2px;
}
.g-check:not(.g-check--disabled, .g-check--checked) .g-check__input:hover + .g-check__box {
  background: var(--gh-surface-sunken);
}

.g-check--invalid .g-check__box {
  border-color: var(--gh-err);
  box-shadow: 0 0 0 1px var(--gh-err);
}

.g-check__text {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}
.g-check__label {
  cursor: pointer;
}
.g-check__required {
  color: var(--gh-err);
}
.g-check__description {
  font-size: 13px;
  color: var(--gh-text-muted);
}

/* disabled: flat, grey, still */
.g-check--disabled .g-check__input,
.g-check--disabled .g-check__label {
  cursor: not-allowed;
}
.g-check--disabled .g-check__box {
  background: var(--gh-surface-sunken);
  border-color: var(--gh-line-disabled);
  color: var(--gh-text-disabled);
  box-shadow: none;
  transition: none;
}
.g-check--disabled .g-check__label {
  color: var(--gh-text-muted);
}
</style>
