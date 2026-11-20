<script setup lang="ts">
/**
 * GSkeleton: a placeholder block shown while content loads. Size it with classes (h-10 w-full).
 *
 * It's hidden from screen readers, so mark the loading region itself: put `aria-busy="true"` on the
 * container, or show a visually hidden "Loading…" next to the skeletons.
 * Pulses gently; stays still when the user asks for reduced motion.
 */
withDefaults(defineProps<{
  shape?: 'rect' | 'text' | 'circle'
}>(), {
  shape: 'rect',
})
</script>

<template>
  <span
    class="g-skeleton"
    :class="`g-skeleton--${shape}`"
    aria-hidden="true"
  />
</template>

<style scoped>
.g-skeleton {
  display: block;
  background: var(--gh-line-soft);
  border-radius: var(--radius-gh-sm);
  animation: g-skeleton-pulse 1.6s ease-in-out infinite;
}
.g-skeleton--text {
  height: 0.9em;
  border-radius: 4px;
}
.g-skeleton--circle {
  border-radius: 50%;
  aspect-ratio: 1;
}
@keyframes g-skeleton-pulse {
  50% { opacity: 0.45; }
}
@media (prefers-reduced-motion: reduce) {
  .g-skeleton { animation: none; opacity: 0.7; }
}
</style>
