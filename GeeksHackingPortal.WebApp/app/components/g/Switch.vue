<script setup lang="ts">
import { SwitchRoot, SwitchThumb } from 'reka-ui'

/**
 * GSwitch: an on/off setting that takes effect straight away (Reka UI Switch, role="switch").
 * Use GCheckbox instead for choices that are submitted with a form.
 *
 * Pass `label` (and optionally `description`), or put it in a <GFormField> without a label to use
 * the field's. On = yellow track with a dark thumb. Disabled is flat and grey and doesn't animate.
 */
defineOptions({ inheritAttrs: false })

const props = withDefaults(defineProps<{
  label?: string
  description?: string
  size?: 'sm' | 'md'
  disabled?: boolean
  required?: boolean
  invalid?: boolean
  name?: string
  id?: string
}>(), {
  size: 'md',
})

const model = defineModel<boolean>({ default: false })

const attrs = useAttrs()
const control = useGFieldControl(props)
const autoId = useId()
const switchId = computed(() => props.id ?? (props.label ? `g-switch-${autoId}` : control.id.value) ?? `g-switch-${autoId}`)
const descriptionId = computed(() => (props.description ? `${switchId.value}-desc` : undefined))
const describedBy = computed(() => [descriptionId.value, control.describedBy.value].filter(Boolean).join(' ') || undefined)

const wrapperAttrs = computed(() => ({ class: attrs.class, style: attrs.style }))
const controlAttrs = computed(() => {
  const { class: _class, style: _style, ...rest } = attrs
  return rest
})
</script>

<template>
  <div
    class="g-switch"
    :class="[`g-switch--${size}`, { 'g-switch--disabled': control.disabled.value }]"
    v-bind="wrapperAttrs"
  >
    <SwitchRoot
      :id="switchId"
      v-model="model"
      v-bind="controlAttrs"
      class="g-switch__track"
      :class="{ 'g-switch__track--invalid': control.invalid.value }"
      :disabled="control.disabled.value"
      :required="control.required.value"
      :name="name"
      :aria-invalid="control.invalid.value || undefined"
      :aria-describedby="describedBy"
    >
      <SwitchThumb class="g-switch__thumb" />
    </SwitchRoot>
    <span
      v-if="label || description || $slots.default"
      class="g-switch__text"
    >
      <label
        :for="switchId"
        class="g-switch__label"
      ><slot>{{ label }}</slot></label>
      <span
        v-if="description"
        :id="descriptionId"
        class="g-switch__description"
      >{{ description }}</span>
    </span>
  </div>
</template>

<style scoped>
.g-switch {
  --g-switch-w: 40px;
  --g-switch-h: 24px;
  --g-switch-thumb: 16px;
  display: inline-flex;
  align-items: flex-start;
  gap: 10px;
  font-family: var(--font-body);
  font-size: 15px;
  line-height: 1.4;
  letter-spacing: var(--gh-tracking);
  color: var(--gh-text);
}
.g-switch--sm {
  --g-switch-w: 32px;
  --g-switch-h: 20px;
  --g-switch-thumb: 12px;
  font-size: 13px;
}

.g-switch__track {
  position: relative;
  flex: none;
  width: var(--g-switch-w);
  height: var(--g-switch-h);
  margin-top: 0;
  padding: 0;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-pill);
  background: var(--gh-surface-sunken);
  cursor: pointer;
  transition: background-color var(--gh-dur-fast) var(--ease-gh);
}
.g-switch__track[data-state='checked'] {
  background: var(--gh-accent);
  border-color: var(--gh-on-accent);
}
.g-switch__track:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: 2px;
}
.g-switch__track--invalid {
  border-color: var(--gh-err);
  box-shadow: 0 0 0 1px var(--gh-err);
}

.g-switch__thumb {
  position: absolute;
  top: 50%;
  left: 3px;
  width: var(--g-switch-thumb);
  height: var(--g-switch-thumb);
  border-radius: 50%;
  background: var(--gh-line);
  transform: translateY(-50%);
  transition: transform var(--gh-dur-fast) var(--ease-gh);
}
.g-switch__thumb[data-state='checked'] {
  background: var(--gh-on-accent);
  transform: translate(calc(var(--g-switch-w) - var(--g-switch-thumb) - 9px), -50%);
}
@media (prefers-reduced-motion: reduce) {
  .g-switch__thumb,
  .g-switch__track { transition: none; }
}

.g-switch__text {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
  padding-top: 1px;
}
.g-switch--sm .g-switch__text { padding-top: 0; }
.g-switch__label {
  cursor: pointer;
}
.g-switch__description {
  font-size: 13px;
  color: var(--gh-text-muted);
}

/* disabled: flat, grey, still */
.g-switch--disabled .g-switch__track,
.g-switch--disabled .g-switch__label {
  cursor: not-allowed;
}
.g-switch--disabled .g-switch__track {
  background: var(--gh-surface-sunken);
  border-color: var(--gh-line-disabled);
  box-shadow: none;
  transition: none;
}
.g-switch--disabled .g-switch__thumb {
  background: var(--gh-line-disabled);
  transition: none;
}
/* darker thumb when on, so on/off is still readable from its position */
.g-switch--disabled .g-switch__thumb[data-state='checked'] {
  background: var(--gh-text-disabled);
}
.g-switch--disabled .g-switch__label {
  color: var(--gh-text-muted);
}
</style>
