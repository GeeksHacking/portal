<script setup lang="ts">
import { useGeeksHackingPortalApiEndpointsParticipantsHackathonTimelineListEndpoint } from '@geekshacking/portal-sdk/hooks'
import { useNow } from '@vueuse/core'
import { computed } from 'vue'
import { day1Items, day2Items } from '~/data/timeline'
import { HACKATHON_TIME_ZONE, HACKATHON_TIME_ZONE_LABEL } from '~/utils/hackathon-date-time'

/**
 * Event schedule, one block per day. Uses the hackathon's timeline from the API; while that is
 * empty it falls back to the fixed HackOMania timetable. During the event the item happening
 * now is marked.
 */
const route = useRoute()
const shortCode = computed(() => (route.params.hackathonId as string) ?? '')

const { data, isLoading, error } = useGeeksHackingPortalApiEndpointsParticipantsHackathonTimelineListEndpoint(
  { path: computed(() => ({ hackathonIdOrShortCode: shortCode.value })) },
  { query: { enabled: computed(() => !!shortCode.value) } },
)

interface ScheduleRow {
  key: string
  time: string
  endTime?: string
  title: string
  description?: string | null
  isNow: boolean
}
// Headed like a calendar page: big date numeral, month strip, weekday heading
interface ScheduleDay {
  key: string
  numeral: string
  month?: string
  title: string
  /** the full date for assistive tech (the numeral is decorative) */
  accessibleName: string
  rows: ScheduleRow[]
}

const now = useNow({ interval: 60_000 })

const dayKeyFormatter = new Intl.DateTimeFormat('en-CA', { timeZone: HACKATHON_TIME_ZONE, year: 'numeric', month: '2-digit', day: '2-digit' })
const dayLabelFormatter = new Intl.DateTimeFormat('en-SG', { timeZone: HACKATHON_TIME_ZONE, weekday: 'long', day: 'numeric', month: 'long' })
const dayNumberFormatter = new Intl.DateTimeFormat('en-SG', { timeZone: HACKATHON_TIME_ZONE, day: '2-digit' })
const monthFormatter = new Intl.DateTimeFormat('en-SG', { timeZone: HACKATHON_TIME_ZONE, month: 'long' })
const weekdayFormatter = new Intl.DateTimeFormat('en-SG', { timeZone: HACKATHON_TIME_ZONE, weekday: 'long' })
const timeFormatter = new Intl.DateTimeFormat('en-SG', { timeZone: HACKATHON_TIME_ZONE, hour: 'numeric', minute: '2-digit' })

const apiDays = computed<ScheduleDay[]>(() => {
  const items = [...(data.value?.timelineItems ?? [])]
    .map(item => ({ ...item, start: new Date(item.startTime ?? ''), end: new Date(item.endTime ?? '') }))
    .filter(item => !Number.isNaN(item.start.getTime()))
    .sort((a, b) => a.start.getTime() - b.start.getTime())

  const days = new Map<string, ScheduleDay>()
  for (const item of items) {
    const key = dayKeyFormatter.format(item.start)
    if (!days.has(key))
      days.set(key, {
        key,
        numeral: dayNumberFormatter.format(item.start),
        month: monthFormatter.format(item.start),
        title: weekdayFormatter.format(item.start),
        accessibleName: dayLabelFormatter.format(item.start),
        rows: [],
      })
    const hasEnd = !Number.isNaN(item.end.getTime()) && item.end > item.start
    days.get(key)!.rows.push({
      key: item.id ?? `${key}-${item.title}`,
      time: timeFormatter.format(item.start),
      endTime: hasEnd ? timeFormatter.format(item.end) : undefined,
      title: item.title ?? '',
      description: item.description,
      isNow: hasEnd && now.value >= item.start && now.value < item.end,
    })
  }
  return [...days.values()]
})

const fallbackTitles = ['First day', 'Second day']
const fallbackDays: ScheduleDay[] = [day1Items, day2Items].map((items, index) => ({
  key: `fallback-${index}`,
  numeral: String(index + 1).padStart(2, '0'),
  title: fallbackTitles[index] ?? `Day ${index + 1}`,
  accessibleName: fallbackTitles[index] ?? `Day ${index + 1}`,
  rows: items.map((item, i) => ({ key: `${index}-${i}`, time: item.time, title: item.event, isNow: false })),
}))

const days = computed(() => (apiDays.value.length ? apiDays.value : fallbackDays))
</script>

<template>
  <section
    class="schedule"
    aria-label="Event schedule"
  >
    <GLoading
      v-if="isLoading"
      label="Loading the schedule…"
    />

    <template v-else>
      <GAlert
        v-if="error"
        tone="attention"
        title="We couldn't load the latest schedule"
        description="This is the standard timetable. Refresh to try again."
      />
      <p class="schedule__note">
        All times are {{ HACKATHON_TIME_ZONE_LABEL }} (Singapore time).
      </p>

      <div class="schedule__days">
        <section
          v-for="day in days"
          :key="day.key"
          class="schedule__day"
          :aria-label="day.accessibleName"
        >
          <GSectionHeading
            :numeral="day.numeral"
            :label="day.month"
            :title="day.title"
            :level="2"
          />
          <ol class="schedule__list">
            <li
              v-for="row in day.rows"
              :key="row.key"
              class="schedule__row"
              :class="{ 'schedule__row--now': row.isNow }"
              :aria-current="row.isNow ? 'time' : undefined"
            >
              <span class="schedule__time">
                {{ row.time }}<template v-if="row.endTime">
                  <span class="schedule__time-end">– {{ row.endTime }}</span>
                </template>
              </span>
              <span class="schedule__what">
                <span class="schedule__title">
                  {{ row.title }}
                  <span
                    v-if="row.isNow"
                    class="schedule__now"
                  >Now</span>
                </span>
                <span
                  v-if="row.description"
                  class="schedule__desc"
                >{{ row.description }}</span>
              </span>
            </li>
          </ol>
        </section>
      </div>
    </template>
  </section>
</template>

<style scoped>
.schedule {
  display: flex;
  flex-direction: column;
  gap: 16px;
  width: 100%;
}
.schedule__note {
  margin: 0;
  font-size: 13px;
  color: var(--gh-text-muted);
}
.schedule__days {
  display: grid;
  grid-template-columns: 1fr;
  gap: 40px 32px;
}
@media (min-width: 1024px) {
  .schedule__days { grid-template-columns: 1fr 1fr; }
}
.schedule__day {
  display: flex;
  flex-direction: column;
  gap: 16px;
  min-width: 0;
}
.schedule__list {
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0;
  list-style: none;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
  overflow: hidden;
}
.schedule__row {
  display: grid;
  grid-template-columns: 7.5rem 1fr;
  gap: 12px;
  padding: 12px 16px;
}
.schedule__row + .schedule__row {
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.schedule__row--now {
  background: var(--gh-accent);
  color: var(--gh-on-accent);
}
.schedule__time {
  display: flex;
  flex-direction: column;
  font-family: var(--font-display);
  font-size: 15px;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}
.schedule__time-end {
  font-size: 12px;
  font-weight: 600;
  color: var(--gh-text-muted);
}
.schedule__row--now .schedule__time-end,
.schedule__row--now .schedule__desc {
  color: var(--gh-on-accent);
}
.schedule__what {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}
.schedule__title {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  font-weight: 600;
}
.schedule__now {
  padding: 1px 8px;
  border-radius: var(--radius-gh-pill);
  background: var(--color-gh-ink);
  color: var(--color-gh-yellow);
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}
.schedule__desc {
  font-size: 13px;
  color: var(--gh-text-muted);
}
@media (max-width: 480px) {
  .schedule__row { grid-template-columns: 5.5rem 1fr; }
}
</style>
