<script setup lang="ts">
/**
 * GLabelStrip: the wordmark's letter-spaced label bar (D accent), for section kickers and the
 * odd emphatic tag. Not a status badge (use GBadge) and not body text: keep it to a word or two.
 *
 * - ink     (default) dark bar, page-coloured text; flips in dark mode.
 * - accent  yellow bar with dark text, for one standout label per view.
 * - outline dark edge, no fill.
 */
withDefaults(defineProps<{
  label?: string
  tone?: 'ink' | 'accent' | 'outline'
  size?: 'sm' | 'md'
  as?: string
}>(), {
  tone: 'ink',
  size: 'md',
  as: 'span',
})
</script>

<template>
  <component
    :is="as"
    class="g-strip"
    :class="[`g-strip--${tone}`, `g-strip--${size}`]"
  >
    <slot>{{ label }}</slot>
  </component>
</template>

<style scoped>
.g-strip {
  display: inline-block;
  max-width: 100%;
  border: var(--gh-border-width) solid transparent;
  font-family: var(--font-display);
  font-weight: 600;
  line-height: 1.3;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  vertical-align: middle;
}
/* trailing tracking is trimmed by padding less on the right */
.g-strip--md { padding: 2px 6px 2px 10px; font-size: 13px; letter-spacing: 0.4em; }
.g-strip--sm { padding: 1px 4px 1px 7px; font-size: 11px; letter-spacing: 0.32em; }

.g-strip--ink { background: var(--gh-text); border-color: var(--gh-text); color: var(--gh-bg); }
.g-strip--accent { background: var(--gh-accent); border-color: var(--gh-on-accent); color: var(--gh-on-accent); }
.g-strip--outline { border-color: var(--gh-text); color: var(--gh-text); }
</style>
