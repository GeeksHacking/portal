<script setup lang="ts">
/**
 * GEmptyState: what a list or panel shows when there's nothing in it yet.
 *
 * Say what will appear here and, where possible, how to make it appear (the `actions` slot).
 * `bordered` (default) draws a dashed outline for empty panels; turn it off inside a card.
 */
withDefaults(defineProps<{
  title: string
  description?: string
  icon?: string
  size?: 'sm' | 'md'
  bordered?: boolean
}>(), {
  icon: 'material-symbols:inbox-rounded',
  size: 'md',
  bordered: true,
})
</script>

<template>
  <div
    class="g-empty"
    :class="[`g-empty--${size}`, { 'g-empty--bordered': bordered }]"
  >
    <span
      class="g-empty__icon-wrap"
      aria-hidden="true"
    >
      <Icon
        :name="icon"
        :key="icon"
        class="g-empty__icon"
      />
    </span>
    <p class="g-empty__title">
      {{ title }}
    </p>
    <p
      v-if="description"
      class="g-empty__description"
    >
      {{ description }}
    </p>
    <div
      v-if="$slots.default"
      class="g-empty__description"
    >
      <slot />
    </div>
    <div
      v-if="$slots.actions"
      class="g-empty__actions"
    >
      <slot name="actions" />
    </div>
  </div>
</template>

<style scoped>
.g-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6px;
  padding: 40px 24px;
  text-align: center;
  color: var(--gh-text);
  font-family: var(--font-body);
  letter-spacing: var(--gh-tracking);
}
.g-empty--sm { padding: 24px 16px; gap: 4px; }
.g-empty--bordered {
  border: var(--gh-border-width) dashed var(--gh-text-subtle);
  border-radius: var(--radius-gh);
}
.g-empty__icon-wrap {
  display: grid;
  place-items: center;
  width: 52px;
  height: 52px;
  margin-bottom: 8px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: 50%;
  background: var(--gh-surface-sunken);
}
.g-empty--sm .g-empty__icon-wrap { width: 40px; height: 40px; margin-bottom: 4px; }
.g-empty__icon {
  width: 24px;
  height: 24px;
  color: var(--gh-text-muted);
}
.g-empty--sm .g-empty__icon { width: 18px; height: 18px; }
.g-empty__title {
  margin: 0;
  font-size: 17px;
  font-weight: 700;
}
.g-empty--sm .g-empty__title { font-size: 15px; }
.g-empty__description {
  max-width: 46ch;
  margin: 0;
  font-size: 14px;
  color: var(--gh-text-muted);
}
.g-empty__actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 10px;
  margin-top: 12px;
}
</style>
