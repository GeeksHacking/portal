<script setup lang="ts">
/**
 * One titled section of an event form (Basics, Schedule, …). `id` is what the section list in
 * <OrganizersEventFormShell> jumps to.
 */
defineProps<{
  id: string
  title: string
  description?: string
  /** marks the title, for a section whose only field hides its own label */
  required?: boolean
}>()
</script>

<template>
  <section
    :id="id"
    class="fs"
    :aria-labelledby="`${id}-title`"
  >
    <div class="fs__head">
      <h2
        :id="`${id}-title`"
        class="fs__title"
        tabindex="-1"
      >
        {{ title }}<span
          v-if="required"
          class="fs__req"
          aria-hidden="true"
        > *</span>
      </h2>
      <p
        v-if="description"
        class="fs__desc"
      >
        {{ description }}
      </p>
    </div>
    <div class="fs__body">
      <slot />
    </div>
  </section>
</template>

<style scoped>
.fs__req {
  color: var(--gh-err);
}
.fs {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 20px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
  scroll-margin-top: 16px;
}
.fs__head {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.fs__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 20px;
  font-weight: 700;
}
.fs__title:focus { outline: none; }
.fs__desc {
  max-width: 65ch;
  margin: 0;
  font-size: 14px;
  color: var(--gh-text-muted);
}
.fs__body {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
/* two fields side by side on wider screens */
.fs__body :deep(.fs-pair) {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 16px;
}
@media (min-width: 640px) {
  .fs__body :deep(.fs-pair) { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
</style>
