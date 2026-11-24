<script setup lang="ts">
import type { RouteLocationRaw } from 'vue-router'

/**
 * GCard: an outlined surface.
 *
 * - plain   (default) calm container: outline, no shadow, no motion. Use for most dashboard content.
 * - action  the whole card is one link or button: lifts onto an offset shadow on hover and presses in
 *           on click. Give it `to` (in-app), `href` (external) or listen to `@click` (renders a <button>).
 *           Disabled action cards are flat with no motion.
 * - `highlight` marks the one card per view that matters most (pale yellow in light, yellow edge in dark).
 *
 * Slots: `header` and `footer` get dividers; the default slot is the body.
 * Keep interactive elements out of action cards: the card itself is the control.
 */
const props = withDefaults(defineProps<{
  variant?: 'plain' | 'action'
  as?: string
  to?: RouteLocationRaw
  href?: string
  target?: string
  padding?: 'none' | 'sm' | 'md' | 'lg'
  highlight?: boolean
  disabled?: boolean
}>(), {
  variant: 'plain',
  padding: 'md',
})

const NuxtLink = resolveComponent('NuxtLink')

const isAction = computed(() => props.variant === 'action')
const tag = computed(() => {
  if (props.as)
    return props.as
  if (!isAction.value)
    return 'div'
  if (props.disabled)
    return 'div'
  if (props.to || props.href)
    return NuxtLink
  return 'button'
})

const bindings = computed(() => {
  if (!isAction.value)
    return {}
  if (props.disabled)
    return { 'aria-disabled': true }
  if (props.to || props.href) {
    return {
      to: props.to ?? props.href,
      target: props.target,
      external: !!props.href,
      rel: props.target === '_blank' ? 'noopener noreferrer' : undefined,
    }
  }
  return tag.value === 'button' ? { type: 'button' } : {}
})
</script>

<template>
  <component
    :is="tag"
    v-bind="bindings"
    class="g-card"
    :class="[
      `g-card--pad-${padding}`,
      {
        'g-card--action': isAction,
        'g-card--highlight': highlight,
        'g-card--disabled': disabled,
      },
    ]"
  >
    <div
      v-if="$slots.header"
      class="g-card__header"
    >
      <slot name="header" />
    </div>
    <div
      v-if="$slots.default"
      class="g-card__body"
    >
      <slot />
    </div>
    <div
      v-if="$slots.footer"
      class="g-card__footer"
    >
      <slot name="footer" />
    </div>
  </component>
</template>

<style scoped>
.g-card {
  --g-card-pad: 20px;
  display: flex;
  flex-direction: column;
  min-width: 0;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh);
  background: var(--gh-surface);
  color: var(--gh-text);
  font: inherit;
  text-align: left;
  text-decoration: none;
}
.g-card--pad-none { --g-card-pad: 0; }
.g-card--pad-sm { --g-card-pad: 12px; }
.g-card--pad-md { --g-card-pad: 20px; }
.g-card--pad-lg { --g-card-pad: 28px; }

.g-card__header,
.g-card__body,
.g-card__footer {
  padding: var(--g-card-pad);
}
.g-card__header + .g-card__body,
.g-card__header + .g-card__footer,
.g-card__body + .g-card__footer {
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}

.g-card--highlight {
  background: var(--gh-highlight);
  border-color: var(--gh-highlight-edge);
}

/* action: motion only means "you can click this" */
.g-card--action {
  width: 100%;
  cursor: pointer;
  transition:
    transform var(--gh-dur-press) var(--ease-gh),
    box-shadow var(--gh-dur-press) var(--ease-gh);
}
.g-card--action:not(.g-card--disabled):hover {
  transform: translate(-2px, -2px);
  box-shadow: 4px 4px 0 var(--gh-shadow-color);
}
.g-card--action:not(.g-card--disabled):active {
  transform: translate(0, 0);
  box-shadow: 0 0 0 var(--gh-shadow-color);
}
.g-card--action:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: 3px;
}

/* disabled: flat, grey, still */
.g-card--disabled {
  cursor: not-allowed;
  transform: none;
  box-shadow: none;
  transition: none;
  background: var(--gh-surface-sunken);
  border-color: var(--gh-line-disabled);
  color: var(--gh-text-muted);
}
</style>
