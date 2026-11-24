/**
 * Column definitions for <GTable>. Deliberately close to the old <UTable> (TanStack) columns,
 * so existing `{ accessorKey, header, meta: { class: { th, td } } }` objects keep working.
 */
export interface GTableColumn<T = Record<string, unknown>> {
  /** Slot names use this: `#<id>-cell`, `#<id>-header`. Defaults to `accessorKey`. */
  id?: string
  accessorKey?: Extract<keyof T, string>
  header?: string
  sortable?: boolean
  align?: 'left' | 'center' | 'right'
  meta?: { class?: { th?: string, td?: string } }
}

export interface GTableSort { id: string, desc: boolean }

export function gColumnId(column: GTableColumn<any>) {
  return column.id ?? column.accessorKey ?? ''
}

export function gCellValue<T>(row: T, column: GTableColumn<T>): unknown {
  return column.accessorKey ? (row as Record<string, unknown>)[column.accessorKey] : undefined
}

/** Empty values sort last; numbers numerically; everything else as natural-order text. */
export function gCompare(a: unknown, b: unknown) {
  const aEmpty = a === null || a === undefined || a === ''
  const bEmpty = b === null || b === undefined || b === ''
  if (aEmpty || bEmpty)
    return aEmpty === bEmpty ? 0 : aEmpty ? 1 : -1
  if (typeof a === 'number' && typeof b === 'number')
    return a - b
  return String(a).localeCompare(String(b), undefined, { numeric: true, sensitivity: 'base' })
}
