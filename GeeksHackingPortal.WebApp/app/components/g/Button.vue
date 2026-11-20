<script setup lang="ts">
import type { RouteLocationRaw } from 'vue-router'

/**
 * GButton: the redesign's button (direction A "Offset").
 *
 * - primary   yellow fill, dark outline, offset shadow. One per group.
 * - secondary surface fill, outline, offset shadow.
 * - ghost     no outline or shadow; tints on hover and never moves.
 * - danger    outlined red, for destructive actions.
 *
 * Motion only means "you can click this". Disabled buttons are flat and grey: no movement,
 * no shadow, no transition. Loading buttons keep their colours but don't move and ignore clicks.
 * Pass `to` for an in-app link or `href` for an external one; it still looks like a button.
 * Icon-only buttons must get an `aria-label`.
 */
const props = withDefaults(defineProps<{
  variant?: 'primary' | 'secondary' | 'ghost' | 'danger'
  size?: 'sm' | 'md' | 'lg'
  type?: 'button' | 'submit' | 'reset'
  label?: string
  icon?: string
  trailingIcon?: string
  loading?: boolean
  loadingLabel?: string
  disabled?: boolean
  block?: boolean
  to?: RouteLocationRaw
  href?: string
  target?: string
}>(), {
  variant: 'secondary',
  size: 'md',
  type: 'button',
  loadingLabel: 'Loading',
})

const slots = useSlots()
const NuxtLink = resolveComponent('NuxtLink')

const inactive = computed(() => props.disabled || props.loading)
const isLink = computed(() => !!(props.to || props.href))
const iconOnly = computed(() => !props.label && !slots.default && !!(props.icon || props.trailingIcon))

const tag = computed(() => {
  if (!isLink.value)
    return 'button'
  // a disabled link is not a link: render plain text that can't be followed
  if (props.disabled)
    return 'span'
  return NuxtLink
})

const bindings = computed(() => {
  if (!isLink.value) {
    return {
      'type': props.type,
      // keep loading buttons focusable (aria-disabled) so focus isn't lost mid-submit
      'disabled': props.disabled || undefined,
      'aria-disabled': props.loading || undefined,
      'aria-busy': props.loading || undefined,
    }
  }
  if (props.disabled)
    return { 'role': 'link', 'aria-disabled': true }
  return {
    to: props.to ?? props.href,
    target: props.target,
    external: !!props.href,
    rel: props.target === '_blank' ? 'noopener noreferrer' : undefined,
    'aria-busy': props.loading || undefined,
  }
})

function onClick(event: MouseEvent) {
  if (inactive.value) {
    event.preventDefault()
    event.stopImmediatePropagation()
  }
}
</script>

<template>
  <component
    :is="tag"
    v-bind="bindings"
    class="g-btn"
    :class="[
      `g-btn--${variant}`,
      `g-btn--${size}`,
      {
        'g-btn--block': block,
        'g-btn--icon-only': iconOnly,
        'g-btn--disabled': disabled,
        'g-btn--loading': loading,
      },
    ]"
    @click="onClick"
  >
    <Icon
      v-if="loading"
      name="material-symbols:progress-activity"
      class="g-btn__icon g-btn__spinner"
      aria-hidden="true"
    />
    <Icon
      v-else-if="icon"
      :name="icon"
      class="g-btn__icon"
      aria-hidden="true"
    />
    <span
      v-if="label || $slots.default"
      class="g-btn__label"
    >
      <slot>{{ label }}</slot>
    </span>
    <span
      v-if="loading"
      class="sr-only"
    >{{ loadingLabel }}</span>
    <Icon
      v-if="trailingIcon && !loading"
      :name="trailingIcon"
      class="g-btn__icon"
      aria-hidden="true"
    />
  </component>
</template>

<style scoped>
.g-btn {
  --g-btn-bg: var(--gh-surface);
  --g-btn-fg: var(--gh-text);
  --g-btn-border: var(--gh-line);
  --g-btn-shadow: var(--gh-shadow-color);

  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  border: var(--gh-border-width) solid var(--g-btn-border);
  border-radius: var(--radius-gh);
  background: var(--g-btn-bg);
  color: var(--g-btn-fg);
  font-family: var(--font-body);
  font-weight: 600;
  letter-spacing: var(--gh-tracking);
  line-height: 1.2;
  text-decoration: none;
  white-space: nowrap;
  cursor: pointer;
  user-select: none;
  box-shadow: 4px 4px 0 var(--g-btn-shadow);
  transition:
    transform var(--gh-dur-press) var(--ease-gh),
    box-shadow var(--gh-dur-press) var(--ease-gh),
    background-color var(--gh-dur-fast) var(--ease-gh);
}

/* sizes */
.g-btn--sm { padding: 6px 12px; font-size: 13px; }
.g-btn--md { padding: 10px 18px; font-size: 15px; }
.g-btn--lg { padding: 14px 22px; font-size: 17px; }
.g-btn--icon-only.g-btn--sm { padding: 6px; }
.g-btn--icon-only.g-btn--md { padding: 10px; }
.g-btn--icon-only.g-btn--lg { padding: 14px; }
.g-btn--block { display: flex; width: 100%; }

.g-btn__icon { width: 1.15em; height: 1.15em; flex: none; }
.g-btn__label { min-width: 0; overflow: hidden; text-overflow: ellipsis; }

/* variants */
.g-btn--primary {
  --g-btn-bg: var(--gh-accent);
  --g-btn-fg: var(--gh-on-accent);
  --g-btn-border: var(--gh-on-accent);
}
.g-btn--danger {
  --g-btn-fg: var(--gh-err);
  --g-btn-border: var(--gh-err);
}
.g-btn--ghost {
  --g-btn-bg: transparent;
  --g-btn-border: transparent;
  box-shadow: none;
}

/* motion: lift on hover, press into the shadow; only when actually clickable */
.g-btn:not(.g-btn--ghost, .g-btn--disabled, .g-btn--loading):hover {
  transform: translate(-1px, -1px);
  box-shadow: 5px 5px 0 var(--g-btn-shadow);
}
.g-btn:not(.g-btn--ghost, .g-btn--disabled, .g-btn--loading):active {
  transform: translate(4px, 4px);
  box-shadow: 0 0 0 var(--g-btn-shadow);
}
.g-btn--ghost:not(.g-btn--disabled, .g-btn--loading):hover {
  background: var(--gh-ghost-hover);
}

.g-btn:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: 3px;
}

/* loading: keeps its colours, but no motion and no clicks */
.g-btn--loading {
  cursor: progress;
  transition: none;
}
.g-btn__spinner { animation: g-btn-spin 0.9s linear infinite; }
@keyframes g-btn-spin { to { transform: rotate(360deg); } }
@media (prefers-reduced-motion: reduce) {
  .g-btn__spinner { animation-duration: 2.5s; }
}

/* disabled: flat, grey, still */
.g-btn--disabled {
  cursor: not-allowed;
  transform: none;
  transition: none;
  box-shadow: none;
  --g-btn-bg: var(--gh-surface-sunken);
  --g-btn-fg: var(--gh-text-disabled);
  --g-btn-border: var(--gh-line-disabled);
}
.g-btn--ghost.g-btn--disabled {
  --g-btn-bg: transparent;
  --g-btn-border: transparent;
}
</style>
