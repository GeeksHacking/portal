<script setup lang="ts">
/**
 * GMeterList: several ratios, one per named row, each against its own whole
 * ("Finished the form: 31 of 40"). Bars share one width so rows compare.
 * Ink fill on a track from the same ramp; the figures are always printed, so the bar never
 * carries a value alone. A row with no whole (total 0) shows a dash and an empty track.
 */
export interface GMeterRow {
  key: string
  label: string
  value: number
  total: number
  hint?: string
}

defineProps<{
  rows: GMeterRow[]
  label?: string
}>()

function pct(row: GMeterRow) {
  return row.total > 0 ? Math.min(100, (row.value / row.total) * 100) : 0
}
function pctText(row: GMeterRow) {
  if (row.total <= 0)
    return '–'
  const p = (row.value / row.total) * 100
  return p > 0 && p < 1 ? '<1%' : `${Math.round(p)}%`
}
</script>

<template>
  <ul
    class="g-meters"
    :aria-label="label"
  >
    <li
      v-for="row in rows"
      :key="row.key"
      class="g-meter"
      :title="row.total > 0 ? `${row.label}: ${row.value.toLocaleString()} of ${row.total.toLocaleString()} (${pctText(row)})` : undefined"
    >
      <div class="g-meter__head">
        <span class="g-meter__label">{{ row.label }}</span>
        <span class="g-meter__figure">
          <strong>{{ pctText(row) }}</strong>
          <span class="g-meter__of">{{ row.value.toLocaleString() }} of {{ row.total.toLocaleString() }}</span>
        </span>
      </div>
      <div
        class="g-meter__track"
        aria-hidden="true"
      >
        <span
          class="g-meter__fill"
          :style="{ width: `${pct(row)}%` }"
        />
      </div>
      <p
        v-if="row.hint"
        class="g-meter__hint"
      >
        {{ row.hint }}
      </p>
    </li>
  </ul>
</template>

<style scoped>
.g-meters {
  display: flex;
  flex-direction: column;
  gap: 14px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.g-meter {
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.g-meter__head {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  justify-content: space-between;
  gap: 2px 12px;
}
.g-meter__label { min-width: 0; }
.g-meter__figure {
  display: inline-flex;
  align-items: baseline;
  gap: 8px;
  font-variant-numeric: tabular-nums;
}
.g-meter__of {
  font-size: 13px;
  color: var(--gh-text-muted);
}
.g-meter__track {
  height: 10px;
  border-radius: var(--radius-gh-pill);
  background: var(--gh-viz-track);
  overflow: hidden;
}
.g-meter__fill {
  display: block;
  height: 100%;
  border-radius: 0 4px 4px 0;
  background: var(--gh-viz-1);
}
.g-meter:hover .g-meter__track { outline: 1px solid var(--gh-line); outline-offset: 1px; }
.g-meter__hint {
  margin: 0;
  font-size: 13px;
  color: var(--gh-text-muted);
}
</style>
