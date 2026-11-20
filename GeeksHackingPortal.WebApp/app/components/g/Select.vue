<script setup lang="ts">
import {
  SelectContent,
  SelectIcon,
  SelectItem,
  SelectItemIndicator,
  SelectItemText,
  SelectPortal,
  SelectRoot,
  SelectTrigger,
  SelectValue,
  SelectViewport,
} from 'reka-ui'

/**
 * GSelect: one choice (or several, with `multiple`) from a short list (Reka UI Select underneath).
 *
 * Drop-in for the old <USelect :items value-key label-key placeholder>, and for
 * <USelectMenu multiple> without search (v-model is then an array; the menu stays open
 * while picking and the trigger lists the chosen labels):
 * `items` may be strings/numbers or objects; `valueKey` / `labelKey` default to `value` / `label`,
 * and an item with `disabled: true` can't be picked. Items with an empty-string value are skipped
 * (Reka reserves '' for "nothing selected", so the placeholder shows instead).
 *
 * Keyboard: Space/Enter/arrows open it, arrows move, typing jumps to a match, Esc closes.
 * Inside <GFormField> the field label names the trigger. Pass `name` to submit it with a native form.
 */
type Primitive = string | number
type Item = Primitive | Record<string, unknown>

defineOptions({ inheritAttrs: false })

const props = withDefaults(defineProps<{
  items: Item[]
  valueKey?: string
  labelKey?: string
  placeholder?: string
  icon?: string
  size?: 'sm' | 'md' | 'lg'
  name?: string
  multiple?: boolean
  invalid?: boolean
  disabled?: boolean
  required?: boolean
  id?: string
}>(), {
  valueKey: 'value',
  labelKey: 'label',
  placeholder: 'Select',
  size: 'md',
})

const model = defineModel<Primitive | Primitive[] | null | undefined>()

const attrs = useAttrs()
const { id, invalid, disabled, required, describedBy } = useGFieldControl(props)

interface Option { value: Primitive, label: string, disabled: boolean }
const options = computed<Option[]>(() => props.items
  .map((item): Option => {
    if (typeof item !== 'object' || item === null)
      return { value: item, label: String(item), disabled: false }
    const value = item[props.valueKey] as Primitive
    return { value, label: String(item[props.labelKey] ?? value), disabled: !!item.disabled }
  })
  .filter(o => o.value !== '' && o.value !== undefined && o.value !== null))

// Reka works with strings; map back to the original (possibly numeric) values
function toOriginal(v: string): Primitive {
  const match = options.value.find(o => String(o.value) === v)
  return match ? match.value : v
}
const innerValue = computed<string | string[]>({
  get: () => {
    if (props.multiple)
      return Array.isArray(model.value) ? model.value.map(String) : []
    return model.value === null || model.value === undefined || Array.isArray(model.value) ? '' : String(model.value)
  },
  set: (v) => {
    model.value = Array.isArray(v) ? v.map(toOriginal) : toOriginal(v)
  },
})

const open = ref(false)
const root = ref<HTMLElement | null>(null)
const { theme, sync } = useGPortalTheme(root)
watch(open, (isOpen) => {
  if (isOpen)
    sync()
})

const wrapperAttrs = computed(() => ({ class: attrs.class, style: attrs.style }))
const triggerAttrs = computed(() => {
  const { class: _class, style: _style, ...rest } = attrs
  return rest
})
</script>

<template>
  <div
    ref="root"
    class="g-select"
    v-bind="wrapperAttrs"
  >
    <SelectRoot
      v-model="innerValue"
      v-model:open="open"
      :disabled="disabled"
      :required="required"
      :name="name"
      :multiple="multiple"
    >
      <SelectTrigger
        :id="id"
        v-bind="triggerAttrs"
        class="g-select__trigger"
        :class="[
          `g-select__trigger--${size}`,
          {
            'g-select__trigger--invalid': invalid,
            'g-select__trigger--disabled': disabled,
          },
        ]"
        :aria-invalid="invalid || undefined"
        :aria-describedby="describedBy"
      >
        <Icon
          v-if="icon"
          :name="icon"
          class="g-select__icon"
          aria-hidden="true"
        />
        <SelectValue
          class="g-select__value"
          :placeholder="placeholder"
        />
        <SelectIcon as-child>
          <Icon
            name="i-lucide-chevron-down"
            class="g-select__chevron"
            aria-hidden="true"
          />
        </SelectIcon>
      </SelectTrigger>

      <SelectPortal>
        <SelectContent
          class="g-select__content"
          position="popper"
          :side-offset="6"
          :data-gh-theme="theme"
        >
          <SelectViewport class="g-select__viewport">
            <SelectItem
              v-for="option in options"
              :key="String(option.value)"
              :value="String(option.value)"
              :disabled="option.disabled"
              class="g-select__item"
              :class="{ 'g-select__item--multiple': multiple }"
            >
              <span
                v-if="multiple"
                class="g-select__box"
                aria-hidden="true"
              >
                <SelectItemIndicator as-child>
                  <Icon name="i-lucide-check" />
                </SelectItemIndicator>
              </span>
              <SelectItemText>{{ option.label }}</SelectItemText>
              <SelectItemIndicator
                v-if="!multiple"
                class="g-select__indicator"
              >
                <Icon
                  name="i-lucide-check"
                  aria-hidden="true"
                />
              </SelectItemIndicator>
            </SelectItem>
            <p
              v-if="!options.length"
              class="g-select__empty"
            >
              Nothing to choose yet.
            </p>
          </SelectViewport>
        </SelectContent>
      </SelectPortal>
    </SelectRoot>
  </div>
</template>

<!-- not scoped: the menu is teleported to <body> -->
<style>
.g-select {
  width: 100%;
}

.g-select__trigger {
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
  text-align: left;
  cursor: pointer;
  outline: none;
  transition:
    box-shadow var(--gh-dur-fast) var(--ease-gh),
    border-color var(--gh-dur-fast) var(--ease-gh);
}
.g-select__trigger--sm { padding: 0 10px; font-size: 13px; min-height: 34px; }
.g-select__trigger--md { padding: 0 14px; font-size: 15px; min-height: 44px; }
.g-select__trigger--lg { padding: 0 16px; font-size: 17px; min-height: 52px; }

.g-select__value {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.g-select__trigger[data-placeholder] .g-select__value {
  color: var(--gh-text-subtle);
}
.g-select__icon,
.g-select__chevron {
  flex: none;
  width: 1.1em;
  height: 1.1em;
  color: var(--gh-text-muted);
}
.g-select__chevron {
  transition: transform var(--gh-dur-fast) var(--ease-gh);
}
.g-select__trigger[data-state='open'] .g-select__chevron {
  transform: rotate(180deg);
}

.g-select__trigger:focus-visible,
.g-select__trigger[data-state='open'] {
  border-color: var(--gh-field-focus-border);
  box-shadow: var(--gh-field-focus);
}
.g-select__trigger--invalid,
.g-select__trigger--invalid:focus-visible,
.g-select__trigger--invalid[data-state='open'] {
  border-color: var(--gh-err);
  box-shadow: var(--gh-field-error);
}

.g-select__trigger--disabled {
  cursor: not-allowed;
  background: var(--gh-surface-sunken);
  border-color: var(--gh-line-disabled);
  color: var(--gh-text-disabled);
  box-shadow: none;
  transition: none;
}
.g-select__trigger--disabled .g-select__value,
.g-select__trigger--disabled[data-placeholder] .g-select__value,
.g-select__trigger--disabled .g-select__chevron,
.g-select__trigger--disabled .g-select__icon {
  color: var(--gh-text-disabled);
}

/* menu */
.g-select__content {
  z-index: 60;
  min-width: var(--reka-select-trigger-width);
  max-height: min(320px, var(--reka-select-content-available-height));
  overflow: hidden;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh);
  background: var(--gh-surface-raised);
  color: var(--gh-text);
  box-shadow: var(--gh-shadow-overlay);
  font-family: var(--font-body);
  font-size: 15px;
  letter-spacing: var(--gh-tracking);
  transform-origin: var(--reka-select-content-transform-origin);
}
.g-select__content[data-state='open'] {
  animation: g-select-in var(--gh-dur-fast) var(--ease-gh);
}
@keyframes g-select-in {
  from { opacity: 0; transform: translateY(-4px); }
}
@media (prefers-reduced-motion: reduce) {
  .g-select__content[data-state='open'] { animation: none; }
  .g-select__chevron { transition: none; }
}

.g-select__viewport {
  padding: 4px;
}
.g-select__item {
  position: relative;
  display: flex;
  align-items: center;
  gap: 8px;
  min-height: 38px;
  padding: 6px 32px 6px 10px;
  border-radius: var(--radius-gh-sm);
  cursor: pointer;
  outline: none;
  user-select: none;
}
.g-select__item[data-highlighted] {
  background: var(--gh-option-hover);
}
.g-select__item[data-state='checked'] {
  font-weight: 600;
}
.g-select__item[data-disabled] {
  color: var(--gh-text-disabled);
  cursor: not-allowed;
}
.g-select__indicator {
  position: absolute;
  right: 10px;
  display: inline-flex;
  width: 16px;
  height: 16px;
}
.g-select__item--multiple {
  padding-right: 10px;
}
/* multiple: a checkbox-style box, yellow fill + dark tick when picked (same as GCheckbox) */
.g-select__box {
  display: grid;
  flex: none;
  place-items: center;
  width: 18px;
  height: 18px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: 5px;
  background: var(--gh-surface);
  color: var(--gh-on-accent);
}
.g-select__box svg {
  width: 13px;
  height: 13px;
}
.g-select__item[data-state='checked'] .g-select__box {
  background: var(--gh-accent);
  border-color: var(--gh-on-accent);
}
.g-select__item[data-disabled] .g-select__box {
  background: var(--gh-surface-sunken);
  border-color: var(--gh-line-disabled);
}
.g-select__empty {
  margin: 0;
  padding: 8px 10px;
  font-size: 13px;
  color: var(--gh-text-muted);
}
</style>
