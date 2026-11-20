<script setup lang="ts" generic="T extends Record<string, any>">
import type { GTableColumn, GTableSort } from '~/utils/g-table'
import { useElementSize, useScroll } from '@vueuse/core'

/**
 * GTable: a data table for organiser screens. Calm by design: no shadows, thin row dividers,
 * a sticky header in the label style, and a row tint on hover.
 *
 * Drop-in for the old <UTable :data :columns :virtualize>:
 * - columns: `{ accessorKey, header, id?, sortable?, align?, meta: { class: { th, td } } }`
 *   (see utils/g-table.ts). Cells render the raw value unless you fill `#<id>-cell="{ row, value }"`,
 *   where `row.original` is the item, as before. Headers can be replaced with `#<id>-header`.
 * - `virtualize` renders only the rows in view (plus `overscan`), for long lists. Rows should be
 *   roughly `estimateSize` px tall; give the table a max height (class="max-h-[42rem]") so it scrolls.
 * - Sortable columns sort on the client by `accessorKey`; bind `v-model:sort` to read or set it,
 *   or pass `manual-sort` and sort `data` yourself.
 * - `loading` shows skeleton rows and sets aria-busy; `#empty` (or `empty-title`) shows when there's no data.
 * - Pass `caption` (read by screen readers) so the table has a name.
 */
const props = withDefaults(defineProps<{
  data: T[]
  columns: GTableColumn<T>[]
  rowKey?: Extract<keyof T, string> | ((row: T, index: number) => string | number)
  caption?: string
  size?: 'sm' | 'md'
  loading?: boolean
  loadingRows?: number
  emptyTitle?: string
  emptyDescription?: string
  manualSort?: boolean
  virtualize?: boolean | { estimateSize?: number, overscan?: number }
}>(), {
  size: 'md',
  loadingRows: 5,
  emptyTitle: 'Nothing here yet',
})

const sort = defineModel<GTableSort | null>('sort', { default: null })

const sortedData = computed(() => {
  const current = sort.value
  if (!current || props.manualSort)
    return props.data
  const column = props.columns.find(c => gColumnId(c) === current.id)
  if (!column)
    return props.data
  const rows = props.data.map((row, index) => ({ row, index }))
  const isEmpty = (v: unknown) => v === null || v === undefined || v === ''
  rows.sort((a, b) => {
    const va = gCellValue(a.row, column)
    const vb = gCellValue(b.row, column)
    // empty values stay last whichever way we sort; ties keep their original order
    if (isEmpty(va) !== isEmpty(vb))
      return isEmpty(va) ? 1 : -1
    const result = gCompare(va, vb)
    return (current.desc ? -result : result) || a.index - b.index
  })
  return rows.map(r => r.row)
})

function toggleSort(column: GTableColumn<T>) {
  const id = gColumnId(column)
  if (sort.value?.id !== id)
    sort.value = { id, desc: false }
  else if (!sort.value.desc)
    sort.value = { id, desc: true }
  else
    sort.value = null
}

function ariaSort(column: GTableColumn<T>) {
  if (!column.sortable)
    return undefined
  if (sort.value?.id !== gColumnId(column))
    return 'none'
  return sort.value.desc ? 'descending' : 'ascending'
}

function sortIcon(column: GTableColumn<T>) {
  if (sort.value?.id !== gColumnId(column))
    return 'material-symbols:unfold-more-rounded'
  return sort.value.desc ? 'material-symbols:arrow-downward-rounded' : 'material-symbols:arrow-upward-rounded'
}

function keyFor(row: T, index: number) {
  if (typeof props.rowKey === 'function')
    return props.rowKey(row, index)
  if (props.rowKey)
    return row[props.rowKey] as string | number
  return index
}

// ---------- virtualisation: spacer rows above and below the rendered window ----------
const container = ref<HTMLElement | null>(null)
const { y: scrollTop } = useScroll(container)
const { height: viewportHeight } = useElementSize(container)

const virtual = computed(() => {
  if (!props.virtualize)
    return null
  const options = props.virtualize === true ? {} : props.virtualize
  return { estimateSize: options.estimateSize ?? 48, overscan: options.overscan ?? 8 }
})

const windowRange = computed(() => {
  const total = sortedData.value.length
  const v = virtual.value
  if (!v)
    return { start: 0, end: total }
  const height = viewportHeight.value || 600
  const start = Math.max(0, Math.floor(scrollTop.value / v.estimateSize) - v.overscan)
  const end = Math.min(total, Math.ceil((scrollTop.value + height) / v.estimateSize) + v.overscan)
  return { start, end }
})

const visibleRows = computed(() => {
  const { start, end } = windowRange.value
  return sortedData.value.slice(start, end).map((row, i) => ({ row, index: start + i }))
})
const padTop = computed(() => (virtual.value ? windowRange.value.start * virtual.value.estimateSize : 0))
const padBottom = computed(() => (virtual.value ? (sortedData.value.length - windowRange.value.end) * virtual.value.estimateSize : 0))

const colCount = computed(() => Math.max(1, props.columns.length))

function formatValue(value: unknown) {
  if (value === null || value === undefined || value === '')
    return '–'
  return typeof value === 'number' ? value.toLocaleString() : String(value)
}
</script>

<template>
  <div
    ref="container"
    class="g-table-wrap"
  >
    <table
      class="g-table"
      :class="`g-table--${size}`"
      :aria-busy="loading || undefined"
      :aria-rowcount="virtual ? sortedData.length + 1 : undefined"
    >
      <caption
        v-if="caption"
        class="sr-only"
      >
        {{ caption }}
      </caption>
      <thead>
        <tr :aria-rowindex="virtual ? 1 : undefined">
          <th
            v-for="column in columns"
            :key="gColumnId(column)"
            scope="col"
            class="g-table__th"
            :class="[column.meta?.class?.th, column.align && `g-table__cell--${column.align}`]"
            :aria-sort="ariaSort(column)"
          >
            <slot
              :name="`${gColumnId(column)}-header`"
              :column="column"
            >
              <button
                v-if="column.sortable"
                type="button"
                class="g-table__sort"
                @click="toggleSort(column)"
              >
                {{ column.header }}
                <Icon
                  :name="sortIcon(column)"
                  class="g-table__sort-icon"
                  :class="{ 'g-table__sort-icon--idle': sort?.id !== gColumnId(column) }"
                  aria-hidden="true"
                />
              </button>
              <template v-else>
                {{ column.header }}
              </template>
            </slot>
          </th>
        </tr>
      </thead>
      <tbody>
        <template v-if="loading">
          <tr
            v-for="n in loadingRows"
            :key="`loading-${n}`"
            class="g-table__row"
          >
            <td
              v-for="column in columns"
              :key="gColumnId(column)"
              class="g-table__td"
            >
              <GSkeleton
                shape="text"
                :style="{ width: `${50 + ((n * 17 + gColumnId(column).length * 7) % 40)}%` }"
              />
            </td>
          </tr>
        </template>
        <tr v-else-if="!sortedData.length">
          <td
            :colspan="colCount"
            class="g-table__empty"
          >
            <slot name="empty">
              <GEmptyState
                size="sm"
                :bordered="false"
                :title="emptyTitle"
                :description="emptyDescription"
              />
            </slot>
          </td>
        </tr>
        <template v-else>
          <tr
            v-if="padTop"
            aria-hidden="true"
            class="g-table__spacer"
            :style="{ height: `${padTop}px` }"
          >
            <td :colspan="colCount" />
          </tr>
          <tr
            v-for="{ row, index } in visibleRows"
            :key="keyFor(row, index)"
            class="g-table__row"
            :aria-rowindex="virtual ? index + 2 : undefined"
            :style="virtual ? { height: `${virtual.estimateSize}px` } : undefined"
          >
            <td
              v-for="column in columns"
              :key="gColumnId(column)"
              class="g-table__td"
              :class="[column.meta?.class?.td, column.align && `g-table__cell--${column.align}`]"
            >
              <slot
                :name="`${gColumnId(column)}-cell`"
                :row="{ original: row, index }"
                :value="gCellValue(row, column)"
              >
                {{ formatValue(gCellValue(row, column)) }}
              </slot>
            </td>
          </tr>
          <tr
            v-if="padBottom"
            aria-hidden="true"
            class="g-table__spacer"
            :style="{ height: `${padBottom}px` }"
          >
            <td :colspan="colCount" />
          </tr>
        </template>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.g-table-wrap {
  position: relative;
  width: 100%;
  overflow: auto;
  font-family: var(--font-body);
  letter-spacing: var(--gh-tracking);
  color: var(--gh-text);
}

.g-table {
  width: 100%;
  border-collapse: separate;
  border-spacing: 0;
  font-size: 14px;
  line-height: 1.4;
}
.g-table--sm { font-size: 13px; }

.g-table__th {
  position: sticky;
  top: 0;
  z-index: 1;
  padding: 10px 14px;
  border-bottom: var(--gh-border-width) solid var(--gh-line);
  background: var(--gh-surface-sunken);
  color: var(--gh-text-muted);
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.12em;
  line-height: 1.3;
  text-align: left;
  text-transform: uppercase;
  white-space: nowrap;
}
.g-table--sm .g-table__th { padding: 8px 10px; }

.g-table__td {
  padding: 12px 14px;
  border-bottom: 1px solid var(--gh-line-soft);
  vertical-align: middle;
}
.g-table--sm .g-table__td { padding: 7px 10px; }
.g-table__row:last-child .g-table__td { border-bottom: 0; }
.g-table__row:hover .g-table__td { background: var(--gh-ghost-hover); }

.g-table__cell--center { text-align: center; }
.g-table__cell--right { text-align: right; font-variant-numeric: tabular-nums; }

.g-table__sort {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  margin: -4px -6px;
  padding: 4px 6px;
  border: 0;
  border-radius: var(--radius-gh-sm);
  background: transparent;
  color: inherit;
  font: inherit;
  letter-spacing: inherit;
  text-transform: inherit;
  cursor: pointer;
}
.g-table__sort:hover { background: var(--gh-ghost-hover); color: var(--gh-text); }
.g-table__sort:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: 1px;
}
.g-table__th[aria-sort='ascending'],
.g-table__th[aria-sort='descending'] { color: var(--gh-text); }
.g-table__sort-icon { width: 15px; height: 15px; flex: none; }
.g-table__sort-icon--idle { opacity: 0.6; }

.g-table__spacer td { padding: 0; border: 0; }
.g-table__empty { padding: 0; }
</style>
