<script setup lang="ts">
/**
 * GBarList: horizontal bars, one per named row, on one baseline (teams per challenge, team
 * sizes). One series, so one ink colour and no legend: the heading says what's measured.
 * Pass rows in the order to show them: largest first for names, natural order for ordered
 * buckets. `muted` greys a row that's context rather than data (e.g. "No challenge yet").
 * The value sits at the bar's tip; `note` adds a short qualifier after the name.
 */
export interface GBarRow {
  key: string
  label: string
  value: number
  note?: string
  muted?: boolean
}

const props = defineProps<{
  rows: GBarRow[]
  label: string
  /** e.g. (n) => `${n} teams`, for the hover text */
  describe?: (value: number) => string
}>()

const max = computed(() => Math.max(1, ...props.rows.map(row => row.value)))
</script>

<template>
  <ul
    class="g-bars"
    :aria-label="label"
  >
    <li
      v-for="row in rows"
      :key="row.key"
      class="g-bar"
      :class="{ 'g-bar--muted': row.muted }"
      :title="`${row.label}: ${describe ? describe(row.value) : row.value.toLocaleString()}`"
    >
      <span class="g-bar__name">
        {{ row.label }}
        <span
          v-if="row.note"
          class="g-bar__note"
        >{{ row.note }}</span>
      </span>
      <span class="g-bar__plot">
        <span
          class="g-bar__fill"
          :style="{ width: row.value ? `max(4px, ${(row.value / max) * 100}%)` : '0' }"
          aria-hidden="true"
        />
        <span class="g-bar__value">{{ row.value.toLocaleString() }}</span>
      </span>
    </li>
  </ul>
</template>

<style scoped>
.g-bars {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.g-bar {
  display: grid;
  grid-template-columns: minmax(6rem, 11rem) minmax(0, 1fr);
  align-items: center;
  gap: 4px 12px;
  padding: 3px 4px;
  border-radius: var(--radius-gh);
}
.g-bar:hover { background: var(--gh-ghost-hover); }
.g-bar__name {
  min-width: 0;
  font-size: 14px;
  overflow-wrap: anywhere;
}
.g-bar__note {
  margin-left: 4px;
  font-size: 13px;
  color: var(--gh-text-muted);
}
.g-bar__plot {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
  /* leave room for the value at the tip of the longest bar */
  padding-right: 3rem;
}
.g-bar__fill {
  flex: none;
  height: 16px;
  border-radius: 0 4px 4px 0;
  background: var(--gh-viz-1);
}
.g-bar--muted .g-bar__fill { background: var(--gh-viz-3); }
.g-bar--muted .g-bar__name { color: var(--gh-text-muted); }
.g-bar__value {
  flex: none;
  margin-right: -3rem;
  font-size: 14px;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}
@media (max-width: 36rem) {
  .g-bar { grid-template-columns: minmax(0, 1fr); }
}
</style>
