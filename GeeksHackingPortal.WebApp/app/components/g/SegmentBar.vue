<script setup lang="ts">
/**
 * GSegmentBar: one whole split into parts (<= 6), as a thin bar with the figures listed under it.
 *
 * The list is the readable form (name, count, share, optional note) and the bar is a picture of
 * it, so the bar is hidden from screen readers. Pointing at a part of the bar or a row highlights
 * both. Tones: status tones (`ok`, `pending`, `err`) only where the part means that state; the
 * ink ramp (`ink`, `ink-2`, `ink-3`) otherwise, darkest first for ordered parts. Yellow and the
 * lightest grey are faint on the card, which is why the list always names every part.
 */
export interface GSegmentPart {
  key: string
  label: string
  value: number
  tone: 'ok' | 'pending' | 'err' | 'ink' | 'ink-2' | 'ink-3'
  icon?: string
  note?: string
}

const props = defineProps<{
  parts: GSegmentPart[]
  /** names the bar, e.g. "Participants by status" */
  label: string
  /** noun for the counts, e.g. "people" */
  unit?: string
}>()

const total = computed(() => props.parts.reduce((sum, part) => sum + Math.max(0, part.value), 0))
const shown = computed(() => props.parts.filter(part => part.value > 0))
const hovered = ref<string | null>(null)

function share(value: number) {
  if (!total.value)
    return '0%'
  const pct = (value / total.value) * 100
  return pct > 0 && pct < 1 ? '<1%' : `${Math.round(pct)}%`
}
</script>

<template>
  <div class="g-seg">
    <div
      class="g-seg__bar"
      :class="{ 'g-seg__bar--empty': !total }"
      aria-hidden="true"
    >
      <span
        v-for="part in shown"
        :key="part.key"
        class="g-seg__part"
        :class="[`g-seg__part--${part.tone}`, { 'is-dim': hovered && hovered !== part.key }]"
        :style="{ flexGrow: part.value }"
        :title="`${part.label}: ${part.value.toLocaleString()} (${share(part.value)})`"
        @pointerenter="hovered = part.key"
        @pointerleave="hovered = null"
      />
    </div>
    <ul
      class="g-seg__list"
      :aria-label="label"
    >
      <li
        v-for="part in parts"
        :key="part.key"
        class="g-seg__row"
        :class="{ 'is-on': hovered === part.key }"
        @pointerenter="hovered = part.key"
        @pointerleave="hovered = null"
      >
        <span
          class="g-seg__key"
          :class="`g-seg__part--${part.tone}`"
          aria-hidden="true"
        />
        <Icon
          v-if="part.icon"
          :name="part.icon"
          class="g-seg__icon"
          aria-hidden="true"
        />
        <span class="g-seg__name">
          {{ part.label }}
          <span
            v-if="part.note"
            class="g-seg__note"
          >{{ part.note }}</span>
        </span>
        <span class="g-seg__value">{{ part.value.toLocaleString() }}<span class="sr-only"> {{ unit }}</span></span>
        <span class="g-seg__share">{{ share(part.value) }}</span>
      </li>
    </ul>
  </div>
</template>

<style scoped>
.g-seg {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.g-seg__bar {
  display: flex;
  gap: 2px;
  height: 14px;
  padding: 2px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-pill);
  background: var(--gh-surface);
  overflow: hidden;
}
.g-seg__bar--empty { background: var(--gh-viz-track); }
.g-seg__part {
  flex: 1 1 0;
  min-width: 3px;
  height: 100%;
  border-radius: var(--radius-gh-pill);
  transition: opacity var(--gh-dur-fast) var(--ease-gh);
}
.g-seg__part.is-dim { opacity: 0.35; }
.g-seg__part--ok { background: var(--gh-ok); }
.g-seg__part--pending { background: var(--gh-accent); }
.g-seg__part--err { background: var(--gh-err); }
.g-seg__part--ink { background: var(--gh-viz-1); }
.g-seg__part--ink-2 { background: var(--gh-viz-2); }
.g-seg__part--ink-3 { background: var(--gh-viz-3); }
.g-seg__list {
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0;
  list-style: none;
}
.g-seg__row {
  display: grid;
  grid-template-columns: 12px auto minmax(0, 1fr) auto 3.25rem;
  align-items: center;
  gap: 10px;
  padding: 7px 6px;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
  border-radius: var(--radius-gh);
}
.g-seg__row:first-child { border-top: 0; }
.g-seg__row.is-on { background: var(--gh-ghost-hover); }
.g-seg__row:not(:has(.g-seg__icon)) { grid-template-columns: 12px minmax(0, 1fr) auto 3.25rem; }
.g-seg__key {
  width: 12px;
  height: 12px;
  border-radius: 3px;
  box-shadow: inset 0 0 0 1px var(--gh-line-soft);
}
.g-seg__icon {
  width: 18px;
  height: 18px;
  color: var(--gh-text-muted);
}
.g-seg__name {
  min-width: 0;
  overflow-wrap: anywhere;
}
.g-seg__note {
  margin-left: 6px;
  font-size: 13px;
  color: var(--gh-text-muted);
}
.g-seg__value {
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  text-align: right;
}
.g-seg__share {
  font-size: 13px;
  font-variant-numeric: tabular-nums;
  color: var(--gh-text-muted);
  text-align: right;
}
</style>
