<script setup lang="ts">
import type { RouteLocationRaw } from 'vue-router'

/**
 * GStatTile: one headline number (KPI) with its label.
 *
 * - `highlight` marks the single number that matters most in a view (one per view at most).
 * - `change` is a short comparison such as "+12 since yesterday"; `trend` picks the arrow. It stays
 *   neutral grey: more isn't always good, so it doesn't borrow the green/red status colours.
 * - `loading` swaps the value for skeletons and sets aria-busy.
 * - `to` makes the whole tile a link to the detail view (an action GCard).
 */
const props = withDefaults(defineProps<{
  label: string
  value?: string | number | null
  unit?: string
  hint?: string
  icon?: string
  change?: string
  trend?: 'up' | 'down' | 'flat'
  highlight?: boolean
  loading?: boolean
  to?: RouteLocationRaw
}>(), {
  trend: 'flat',
})

const formatted = computed(() => {
  if (props.value === null || props.value === undefined || props.value === '')
    return '–'
  return typeof props.value === 'number' ? props.value.toLocaleString() : props.value
})

const trendIcon = computed(() => ({
  up: 'i-lucide-arrow-up-right',
  down: 'i-lucide-arrow-down-right',
  flat: 'i-lucide-arrow-right',
})[props.trend])
</script>

<template>
  <GCard
    :variant="to ? 'action' : 'plain'"
    :to="to"
    :highlight="highlight"
    class="g-stat"
    :aria-busy="loading || undefined"
  >
    <div class="g-stat__top">
      <span class="g-stat__label">{{ label }}</span>
      <Icon
        v-if="icon"
        :name="icon"
        class="g-stat__icon"
        aria-hidden="true"
      />
    </div>
    <template v-if="loading">
      <GSkeleton class="g-stat__skeleton-value" />
      <GSkeleton
        shape="text"
        class="g-stat__skeleton-line"
      />
      <span class="sr-only">Loading {{ label }}</span>
    </template>
    <template v-else>
      <p class="g-stat__value">
        {{ formatted }}<span
          v-if="unit"
          class="g-stat__unit"
        > {{ unit }}</span>
      </p>
      <p
        v-if="change"
        class="g-stat__change"
      >
        <Icon
          :name="trendIcon"
          class="g-stat__trend"
          aria-hidden="true"
        />
        <span>{{ change }}</span>
      </p>
      <p
        v-if="hint"
        class="g-stat__hint"
      >
        {{ hint }}
      </p>
    </template>
  </GCard>
</template>

<style scoped>
.g-stat__top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}
.g-stat__label {
  font-family: var(--font-body);
  font-size: 12px;
  font-weight: 600;
  line-height: 1.3;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.g-stat__icon {
  flex: none;
  width: 18px;
  height: 18px;
  color: var(--gh-text-muted);
}
.g-stat__value {
  margin: 8px 0 0;
  font-family: var(--font-display);
  font-size: 40px;
  font-weight: 700;
  line-height: 1.05;
  font-variant-numeric: tabular-nums;
  color: var(--gh-text);
}
.g-card--highlight .g-stat__value {
  color: var(--gh-highlight-ink);
}
.g-stat__unit {
  font-family: var(--font-body);
  font-size: 15px;
  font-weight: 600;
  color: var(--gh-text-muted);
}
.g-stat__change {
  display: flex;
  align-items: center;
  gap: 4px;
  margin: 6px 0 0;
  font-size: 13px;
  font-weight: 600;
  color: var(--gh-text-muted);
}
.g-stat__trend {
  width: 15px;
  height: 15px;
}
.g-stat__hint {
  margin: 4px 0 0;
  font-size: 13px;
  color: var(--gh-text-muted);
}
.g-stat__skeleton-value {
  width: 55%;
  height: 40px;
  margin-top: 10px;
}
.g-stat__skeleton-line {
  width: 40%;
  margin-top: 10px;
}
</style>
