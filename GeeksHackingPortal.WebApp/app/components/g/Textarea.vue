<script setup lang="ts">
/**
 * GTextarea: multi-line text field. Same states and wiring as <GInput>.
 *
 * Inside <GFormField> it picks up its id, description, invalid, required and disabled state.
 * `autoresize` grows the box with its content (from `rows` up to `maxRows`) instead of
 * showing a resize handle.
 */
defineOptions({ inheritAttrs: false })

const props = withDefaults(defineProps<{
  placeholder?: string
  rows?: number
  autoresize?: boolean
  maxRows?: number
  size?: 'sm' | 'md' | 'lg'
  invalid?: boolean
  disabled?: boolean
  readonly?: boolean
  required?: boolean
  id?: string
}>(), {
  rows: 4,
  size: 'md',
})

const model = defineModel<string | null>()

const attrs = useAttrs()
const { id, invalid, disabled, required, describedBy } = useGFieldControl(props)

const el = ref<HTMLTextAreaElement | null>(null)
defineExpose({ focus: () => el.value?.focus(), el })

const wrapperAttrs = computed(() => ({ class: attrs.class, style: attrs.style }))
const controlAttrs = computed(() => {
  const { class: _class, style: _style, ...rest } = attrs
  return rest
})

function resize() {
  const node = el.value
  if (!props.autoresize || !node)
    return
  node.style.height = 'auto'
  let height = node.scrollHeight
  if (props.maxRows) {
    const style = getComputedStyle(node)
    const line = Number.parseFloat(style.lineHeight) || 22
    const padding = Number.parseFloat(style.paddingTop) + Number.parseFloat(style.paddingBottom)
    height = Math.min(height, line * props.maxRows + padding)
  }
  node.style.height = `${height}px`
}

watch(model, () => nextTick(resize))
onMounted(resize)
</script>

<template>
  <div
    class="g-textarea"
    :class="[
      `g-textarea--${size}`,
      {
        'g-textarea--invalid': invalid,
        'g-textarea--disabled': disabled,
        'g-textarea--readonly': readonly,
        'g-textarea--autoresize': autoresize,
      },
    ]"
    v-bind="wrapperAttrs"
  >
    <textarea
      :id="id"
      ref="el"
      v-model="model"
      v-bind="controlAttrs"
      class="g-textarea__control"
      :rows="rows"
      :placeholder="placeholder"
      :disabled="disabled"
      :readonly="readonly"
      :required="required"
      :aria-invalid="invalid || undefined"
      :aria-describedby="describedBy"
    />
  </div>
</template>

<style scoped>
.g-textarea {
  display: flex;
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
.g-textarea--sm { font-size: 13px; }
.g-textarea--md { font-size: 15px; }
.g-textarea--lg { font-size: 17px; }

.g-textarea__control {
  flex: 1;
  min-width: 0;
  padding: 10px 14px;
  border: 0;
  border-radius: inherit;
  outline: none;
  background: transparent;
  color: inherit;
  font: inherit;
  line-height: 1.5;
  letter-spacing: inherit;
  resize: vertical;
}
.g-textarea--sm .g-textarea__control { padding: 7px 10px; }
.g-textarea--lg .g-textarea__control { padding: 12px 16px; }
.g-textarea--autoresize .g-textarea__control { resize: none; }
.g-textarea__control::placeholder {
  color: var(--gh-text-subtle);
  opacity: 1;
}

.g-textarea:focus-within {
  border-color: var(--gh-field-focus-border);
  box-shadow: var(--gh-field-focus);
}
.g-textarea--invalid,
.g-textarea--invalid:focus-within {
  border-color: var(--gh-err);
  box-shadow: var(--gh-field-error);
}
.g-textarea--readonly {
  background: var(--gh-surface-sunken);
}

.g-textarea--disabled {
  cursor: not-allowed;
  background: var(--gh-surface-sunken);
  border-color: var(--gh-line-disabled);
  color: var(--gh-text-disabled);
  box-shadow: none;
  transition: none;
}
.g-textarea--disabled .g-textarea__control {
  cursor: not-allowed;
  resize: none;
}
</style>
