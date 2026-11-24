<script setup lang="ts">
import type { DateValue } from '@internationalized/date'
import { CalendarDateTime, getLocalTimeZone, parseDateTime, today } from '@internationalized/date'
import {
  DatePickerAnchor,
  DatePickerCalendar,
  DatePickerCell,
  DatePickerCellTrigger,
  DatePickerContent,
  DatePickerField,
  DatePickerGrid,
  DatePickerGridBody,
  DatePickerGridHead,
  DatePickerGridRow,
  DatePickerHeadCell,
  DatePickerHeader,
  DatePickerHeading,
  DatePickerInput,
  DatePickerNext,
  DatePickerPrev,
  DatePickerRoot,
  DatePickerTrigger,
} from 'reka-ui'

/**
 * GDateTimePicker: a date and time, typed or picked from a calendar, in the portal's style.
 *
 * - `v-model` is a local date-time string `YYYY-MM-DDTHH:mm` (what `<input type="datetime-local">`
 *   gives, so it drops in for one), or '' when empty. No time zone: the forms treat it as SGT.
 * - The box has a segment per part (day, month, year, hour, minute) that you type into or change
 *   with the arrow keys; the calendar button opens a month grid plus the time.
 * - Inside <GFormField> it picks up its id (the label focuses the first segment), description,
 *   invalid, required and disabled state, like <GInput>.
 * - `min` / `max` (same string format) grey out days outside the range.
 */
defineOptions({ inheritAttrs: false })

const props = withDefaults(defineProps<{
  invalid?: boolean
  disabled?: boolean
  required?: boolean
  id?: string
  min?: string
  max?: string
  /** formatting and day order; the portal's events are in Singapore */
  locale?: string
}>(), {
  locale: 'en-SG',
})

const model = defineModel<string | null>()

const attrs = useAttrs()
const { field, id, invalid, disabled, required, describedBy } = useGFieldControl(props)
// segments only say "day", "month"…: name the group after the field's label (or aria-label)
const groupLabelledBy = computed(() => (attrs['aria-label'] || !field) ? undefined : `${field.id.value}-label`)
// the time dropdowns inside are separate controls: don't hand them this field's id, error or required
provide(G_FORM_FIELD_KEY, undefined)

function toDate(value: string | null | undefined): CalendarDateTime | undefined {
  if (!value)
    return undefined
  try {
    return parseDateTime(value.slice(0, 16))
  }
  catch {
    return undefined
  }
}

function toString(value: DateValue | null | undefined) {
  if (!value)
    return ''
  const dt = 'hour' in value ? value : new CalendarDateTime(value.year, value.month, value.day)
  return dt.toString().slice(0, 16)
}

const value = computed<DateValue | undefined>({
  get: () => toDate(model.value),
  set: (next) => {
    // a day picked before any time is set lands at 09:00 rather than midnight
    if (next && !model.value && 'hour' in next && next.hour === 0 && next.minute === 0)
      next = (next as CalendarDateTime).set({ hour: 9 })
    model.value = toString(next)
  },
})
// ---- the pop-up's time dropdowns (12-hour clock, minutes in 5s) ---------------------------------
function setTime(hour: number, minute: number) {
  const current = value.value
  if (current && 'hour' in current)
    value.value = current.set({ hour, minute })
}
const hour24 = computed(() => (value.value && 'hour' in value.value ? value.value.hour : 9))
const currentMinute = computed(() => (value.value && 'minute' in value.value ? value.value.minute : 0))

const hour12 = computed<number>({
  get: () => hour24.value % 12 || 12,
  set: h => setTime((h % 12) + (period.value === 'PM' ? 12 : 0), currentMinute.value),
})
const minute = computed<number>({
  get: () => currentMinute.value,
  set: m => setTime(hour24.value, m),
})
const period = computed<'AM' | 'PM'>({
  get: () => (hour24.value < 12 ? 'AM' : 'PM'),
  set: p => setTime((hour24.value % 12) + (p === 'PM' ? 12 : 0), currentMinute.value),
})

const hourItems = Array.from({ length: 12 }, (_, i) => ({ label: String(i + 1), value: i + 1 }))
const periodItems = [{ label: 'AM', value: 'AM' }, { label: 'PM', value: 'PM' }]
// every 5 minutes, plus the saved minute when it's off that grid (e.g. 9:47)
const minuteItems = computed(() => {
  const steps = Array.from({ length: 12 }, (_, i) => i * 5)
  if (!steps.includes(currentMinute.value))
    steps.push(currentMinute.value)
  return steps.sort((a, b) => a - b).map(m => ({ label: String(m).padStart(2, '0'), value: m }))
})

const minValue = computed(() => toDate(props.min))
const maxValue = computed(() => toDate(props.max))
// the month the calendar shows: set each time it opens (see openingMonth)
function todayAtNine() {
  const t = today(getLocalTimeZone())
  return new CalendarDateTime(t.year, t.month, t.day, 9, 0)
}
const placeholder = ref<DateValue>(todayAtNine())
/** Open on the chosen date's month; when empty, on the earliest allowed date if that's later
 * than today (an end time opens on its start's month), else today. */
function openingMonth(): DateValue {
  if (value.value)
    return value.value
  const now = todayAtNine()
  return minValue.value && minValue.value.compare(now) > 0 ? minValue.value : now
}

const open = ref(false)
const root = ref<HTMLElement | null>(null)
const { theme, sync } = useGPortalTheme(root)
watch(open, (isOpen) => {
  if (isOpen) {
    placeholder.value = openingMonth()
    sync()
    makeRoomBelow()
  }
})

/**
 * The calendar opens centred under the field, and flips above it when there isn't room below.
 * To keep it below where possible, scroll the form up just enough before it opens (never so far
 * that the field itself leaves the top). Near the end of a page that can't scroll further it
 * still flips, which keeps it on screen. Runs before the calendar is placed, so it doesn't jump.
 */
const CALENDAR_ROOM = 440 // calendar ≈ 410px tall + offset + edge padding
function scrollParentOf(el: HTMLElement): HTMLElement | null {
  for (let node = el.parentElement; node; node = node.parentElement) {
    const { overflowY } = getComputedStyle(node)
    if ((overflowY === 'auto' || overflowY === 'scroll') && node.scrollHeight > node.clientHeight)
      return node
  }
  return null
}
function makeRoomBelow() {
  const el = root.value
  if (!el || typeof window === 'undefined')
    return
  const scroller = scrollParentOf(el)
  const area = scroller?.getBoundingClientRect()
  const rect = el.getBoundingClientRect()
  const bottomEdge = Math.min(window.innerHeight, area?.bottom ?? window.innerHeight)
  const topEdge = Math.max(0, area?.top ?? 0)
  const lack = CALENDAR_ROOM - (bottomEdge - rect.bottom)
  if (lack <= 0)
    return
  const by = Math.min(lack, rect.top - topEdge - 8)
  if (by > 0)
    (scroller ?? document.scrollingElement)?.scrollBy({ top: by })
}

const wrapperAttrs = computed(() => ({ class: attrs.class, style: attrs.style }))
const fieldAttrs = computed(() => {
  const { class: _class, style: _style, ...rest } = attrs
  return rest
})
</script>

<template>
  <div
    ref="root"
    class="g-dtp"
    v-bind="wrapperAttrs"
  >
    <DatePickerRoot
      :id="id"
      v-model="value"
      v-model:open="open"
      v-model:placeholder="placeholder"
      granularity="minute"
      :locale="locale"
      :min-value="minValue"
      :max-value="maxValue"
      :disabled="disabled"
      :required="required"
      :close-on-select="false"
      fixed-weeks
    >
      <!-- the calendar opens centred under the whole field, not beside the small calendar button -->
      <DatePickerAnchor class="g-dtp__anchor">
        <DatePickerField
          v-slot="{ segments }"
          class="g-dtp__field"
          :class="{ 'g-dtp__field--invalid': invalid, 'g-dtp__field--disabled': disabled }"
          role="group"
          :aria-labelledby="groupLabelledBy"
          :aria-invalid="invalid || undefined"
          :aria-describedby="describedBy"
          v-bind="fieldAttrs"
        >
          <div class="g-dtp__segments">
            <template
              v-for="item in segments"
              :key="item.part"
            >
              <DatePickerInput
                v-if="item.part === 'literal'"
                :part="item.part"
                class="g-dtp__literal"
              >
                {{ item.value }}
              </DatePickerInput>
              <DatePickerInput
                v-else
                :part="item.part"
                class="g-dtp__segment"
                :class="{ 'g-dtp__segment--empty': !value }"
              >
                {{ item.value }}
              </DatePickerInput>
            </template>
          </div>
          <DatePickerTrigger
            class="g-dtp__trigger"
            aria-label="Choose date and time"
          >
            <Icon
              name="material-symbols:calendar-month-rounded"
              aria-hidden="true"
            />
          </DatePickerTrigger>
        </DatePickerField>
      </DatePickerAnchor>

      <DatePickerContent
        class="g-dtp__content"
        :data-gh-theme="theme"
        side="bottom"
        :side-offset="6"
        align="center"
        :collision-padding="12"
      >
        <DatePickerCalendar
          v-slot="{ weekDays, grid }"
          class="g-dtp__calendar"
        >
          <DatePickerHeader class="g-dtp__header">
            <DatePickerPrev
              class="g-dtp__nav"
              aria-label="Previous month"
            >
              <Icon
                name="material-symbols:chevron-left-rounded"
                aria-hidden="true"
              />
            </DatePickerPrev>
            <DatePickerHeading class="g-dtp__heading" />
            <DatePickerNext
              class="g-dtp__nav"
              aria-label="Next month"
            >
              <Icon
                name="material-symbols:chevron-right-rounded"
                aria-hidden="true"
              />
            </DatePickerNext>
          </DatePickerHeader>

          <DatePickerGrid
            v-for="month in grid"
            :key="month.value.toString()"
            class="g-dtp__grid"
          >
            <DatePickerGridHead>
              <DatePickerGridRow class="g-dtp__row">
                <DatePickerHeadCell
                  v-for="day in weekDays"
                  :key="day"
                  class="g-dtp__head-cell"
                >
                  {{ day }}
                </DatePickerHeadCell>
              </DatePickerGridRow>
            </DatePickerGridHead>
            <DatePickerGridBody>
              <DatePickerGridRow
                v-for="(weekDates, index) in month.rows"
                :key="`week-${index}`"
                class="g-dtp__row"
              >
                <DatePickerCell
                  v-for="weekDate in weekDates"
                  :key="weekDate.toString()"
                  :date="weekDate"
                  class="g-dtp__cell"
                >
                  <DatePickerCellTrigger
                    :day="weekDate"
                    :month="month.value"
                    class="g-dtp__day"
                  />
                </DatePickerCell>
              </DatePickerGridRow>
            </DatePickerGridBody>
          </DatePickerGrid>
        </DatePickerCalendar>

        <div class="g-dtp__time">
          <span
            :id="`${id ?? 'g-dtp'}-time-label`"
            class="g-dtp__time-label"
          >Time</span>
          <div
            class="g-dtp__time-selects"
            role="group"
            :aria-labelledby="`${id ?? 'g-dtp'}-time-label`"
          >
            <GSelect
              v-model="hour12"
              :items="hourItems"
              size="sm"
              aria-label="Hour"
              :disabled="disabled || !value"
              class="g-dtp__time-select"
            />
            <span
              class="g-dtp__colon"
              aria-hidden="true"
            >:</span>
            <GSelect
              v-model="minute"
              :items="minuteItems"
              size="sm"
              aria-label="Minute"
              :disabled="disabled || !value"
              class="g-dtp__time-select"
            />
            <GSelect
              v-model="period"
              :items="periodItems"
              size="sm"
              aria-label="AM or PM"
              :disabled="disabled || !value"
              class="g-dtp__time-select g-dtp__time-select--period"
            />
          </div>
          <GButton
            variant="primary"
            size="sm"
            label="Done"
            class="g-dtp__done"
            @click="open = false"
          />
        </div>
      </DatePickerContent>
    </DatePickerRoot>
  </div>
</template>

<!-- not scoped: the pop-up is teleported to <body> -->
<style>
.g-dtp {
  width: 100%;
}
/* the calendar's anchor: the whole field */
.g-dtp__anchor {
  display: block;
  width: 100%;
}

/* the box: same as GInput */
.g-dtp__field {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  min-height: 44px;
  padding: 0 6px 0 14px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh);
  background: var(--gh-surface);
  color: var(--gh-text);
  font-family: var(--font-body);
  font-size: 15px;
  letter-spacing: var(--gh-tracking);
  font-variant-numeric: tabular-nums;
  transition:
    box-shadow var(--gh-dur-fast) var(--ease-gh),
    border-color var(--gh-dur-fast) var(--ease-gh);
}
.g-dtp__field:focus-within {
  border-color: var(--gh-field-focus-border);
  box-shadow: var(--gh-field-focus);
}
.g-dtp__field--invalid,
.g-dtp__field--invalid:focus-within {
  border-color: var(--gh-err);
  box-shadow: var(--gh-field-error);
}
.g-dtp__field--disabled {
  cursor: not-allowed;
  background: var(--gh-surface-sunken);
  border-color: var(--gh-line-disabled);
  color: var(--gh-text-disabled);
  box-shadow: none;
}

.g-dtp__segments {
  display: flex;
  flex: 1;
  flex-wrap: wrap;
  align-items: center;
  min-width: 0;
}
.g-dtp__segment {
  padding: 2px 1px;
  border-radius: 4px;
  outline: none;
  caret-color: transparent;
}
.g-dtp__segment[data-placeholder],
.g-dtp__segment--empty {
  color: var(--gh-text-subtle);
}
/* the part being typed into: the brand's yellow */
.g-dtp__segment:focus {
  background: var(--gh-accent);
  color: var(--gh-on-accent);
}
.g-dtp__literal {
  padding: 0;
  white-space: pre;
  color: var(--gh-text-muted);
}

.g-dtp__trigger,
.g-dtp__nav {
  display: inline-flex;
  flex: none;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  padding: 0;
  border: var(--gh-border-width) solid transparent;
  border-radius: var(--radius-gh-sm);
  background: none;
  color: var(--gh-text-muted);
  cursor: pointer;
}
.g-dtp__trigger > *,
.g-dtp__nav > * {
  width: 20px;
  height: 20px;
}
.g-dtp__trigger:hover,
.g-dtp__nav:hover {
  background: var(--gh-ghost-hover);
  color: var(--gh-text);
}
.g-dtp__trigger:focus-visible,
.g-dtp__nav:focus-visible,
.g-dtp__day:focus-visible {
  outline: 2px solid var(--gh-focus);
  outline-offset: 2px;
}
.g-dtp__trigger[data-disabled] {
  cursor: not-allowed;
  color: var(--gh-text-disabled);
}

/* the pop-up */
.g-dtp__content {
  z-index: 60;
  display: flex;
  flex-direction: column;
  gap: 12px;
  width: min(20rem, calc(100vw - 24px));
  padding: 12px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface-raised);
  color: var(--gh-text);
  box-shadow: var(--gh-shadow-overlay);
  font-family: var(--font-body);
  letter-spacing: var(--gh-tracking);
}
.g-dtp__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}
.g-dtp__heading {
  font-family: var(--font-display);
  font-size: 16px;
  font-weight: 700;
}
.g-dtp__grid {
  width: 100%;
  border-collapse: collapse;
}
.g-dtp__row {
  display: grid;
  grid-template-columns: repeat(7, minmax(0, 1fr));
}
.g-dtp__head-cell {
  padding: 4px 0;
  font-size: 12px;
  font-weight: 700;
  color: var(--gh-text-muted);
  text-align: center;
}
.g-dtp__cell {
  display: flex;
  justify-content: center;
  padding: 1px;
}
.g-dtp__day {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border: var(--gh-border-width) solid transparent;
  border-radius: var(--radius-gh-sm);
  font-size: 14px;
  font-variant-numeric: tabular-nums;
  cursor: pointer;
  outline: none;
}
.g-dtp__day:hover {
  background: var(--gh-ghost-hover);
}
.g-dtp__day[data-outside-view] {
  color: var(--gh-text-subtle);
}
.g-dtp__day[data-today] {
  border-color: var(--gh-line);
  font-weight: 700;
}
/* the picked day: the brand's yellow block */
.g-dtp__day[data-selected] {
  border-color: var(--gh-on-accent);
  background: var(--gh-accent);
  color: var(--gh-on-accent);
  font-weight: 700;
}
.g-dtp__day[data-disabled],
.g-dtp__day[data-unavailable] {
  color: var(--gh-text-disabled);
  text-decoration: line-through;
  cursor: not-allowed;
  background: none;
}

.g-dtp__time {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px 12px;
  padding-top: 12px;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.g-dtp__time-label {
  font-size: 14px;
  font-weight: 700;
}
.g-dtp__time-selects {
  display: flex;
  align-items: center;
  gap: 4px;
}
.g-dtp__time-select { width: 4.25rem; }
.g-dtp__time-select--period { width: 4.75rem; margin-left: 4px; }
.g-dtp__colon {
  font-weight: 700;
  color: var(--gh-text-muted);
}
.g-dtp__done {
  margin-left: auto;
}

@media (prefers-reduced-motion: no-preference) {
  .g-dtp__content[data-state='open'] {
    animation: g-dtp-in var(--gh-dur-fast) var(--ease-gh);
  }
}
@keyframes g-dtp-in {
  from { opacity: 0; transform: translateY(-4px); }
}
</style>
