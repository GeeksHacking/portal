<script setup lang="ts">
/**
 * The detail side of a list/detail screen (see OrganizersListDetail): a fixed header with the
 * person's name, a scrolling body, and an optional footer pinned at the bottom for the action.
 * It fills whatever height it is given, so the footer stays in view in the side panel and in
 * the dialog used on narrow screens.
 */
defineProps<{
  title: string
  /** The heading tag; the side panel sits under the page's h1. */
  as?: string
  closeLabel?: string
}>()

const emit = defineEmits<{ close: [] }>()
</script>

<template>
  <div class="odp">
    <header class="odp__header">
      <div class="odp__heading">
        <component
          :is="as ?? 'h2'"
          class="odp__title"
        >
          {{ title }}
        </component>
        <div
          v-if="$slots.meta"
          class="odp__meta"
        >
          <slot name="meta" />
        </div>
      </div>
      <GButton
        variant="ghost"
        size="sm"
        icon="material-symbols:close-rounded"
        :aria-label="closeLabel ?? 'Close'"
        @click="emit('close')"
      />
    </header>
    <div class="odp__body">
      <slot />
    </div>
    <footer
      v-if="$slots.footer"
      class="odp__footer"
    >
      <slot name="footer" />
    </footer>
  </div>
</template>

<style scoped>
.odp {
  display: flex;
  flex-direction: column;
  min-height: 0;
  height: 100%;
  max-height: inherit;
}
.odp__header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  padding: 20px 16px 16px 24px;
  border-bottom: var(--gh-border-width) solid var(--gh-line-soft);
}
.odp__heading {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 0;
}
.odp__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 24px;
  font-weight: 700;
  line-height: 1.2;
  overflow-wrap: anywhere;
}
.odp__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px;
}
.odp__body {
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  gap: 24px;
  min-height: 0;
  overflow-y: auto;
  padding: 20px 24px 24px;
}
.odp__footer {
  flex-shrink: 0;
  padding: 16px 24px 20px;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
  background: var(--gh-surface-sunken);
}
@media (max-width: 479px) {
  .odp__header { padding: 16px 12px 12px 16px; }
  .odp__body { padding: 16px; }
  .odp__footer { padding: 12px 16px 16px; }
}
</style>
