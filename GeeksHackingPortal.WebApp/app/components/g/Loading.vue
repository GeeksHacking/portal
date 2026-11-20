<script setup lang="ts">
/**
 * GLoading: the one "please wait" state. Replaces the spinner + "Checking your session..."
 * screens that pages used to build themselves.
 *
 * - `label` says what is happening ("Checking your session…"). It is announced politely.
 * - `page` fills the window and adds the glasses mark: for screens shown before anything else
 *   exists (auth checks, redirects). Without it, the state sits centred in its container.
 * - The bar is a yellow block running along a track. With reduced motion it stands still and
 *   the label carries the meaning.
 */
withDefaults(defineProps<{
  label?: string
  page?: boolean
}>(), {
  label: 'Loading…',
})
</script>

<template>
  <div
    class="g-loading"
    :class="{ 'g-loading--page': page }"
    role="status"
    aria-live="polite"
  >
    <GLogo
      v-if="page"
      :wordmark="false"
      size="md"
      decorative
    />
    <span
      class="g-loading__bar"
      aria-hidden="true"
    >
      <span class="g-loading__run" />
    </span>
    <p class="g-loading__label">
      {{ label }}
    </p>
  </div>
</template>

<style scoped>
.g-loading {
  display: flex;
  flex: 1;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 14px;
  padding: 32px 16px;
  color: var(--gh-text);
  font-family: var(--font-body);
  letter-spacing: var(--gh-tracking);
  text-align: center;
}
.g-loading--page {
  min-height: 100dvh;
  gap: 18px;
  background: var(--gh-bg);
}

.g-loading__bar {
  position: relative;
  display: block;
  width: 120px;
  height: 8px;
  overflow: hidden;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-pill);
  background: var(--gh-surface-sunken);
}
.g-loading__run {
  position: absolute;
  top: 0;
  bottom: 0;
  left: 0;
  width: 40%;
  border-radius: inherit;
  background: var(--gh-accent);
  animation: g-loading-run 1.1s var(--ease-gh) infinite;
}
@keyframes g-loading-run {
  from { transform: translateX(-100%); }
  to { transform: translateX(250%); }
}
@media (prefers-reduced-motion: reduce) {
  .g-loading__run {
    left: 30%;
    animation: none;
  }
}

.g-loading__label {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
  color: var(--gh-text-muted);
}
</style>
