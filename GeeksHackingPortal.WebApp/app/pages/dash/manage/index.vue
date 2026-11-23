<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListResponse,
  GeeksHackingPortalApiEndpointsParticipantsHackathonStatusParticipantStatusKey,
  GeeksHackingPortalApiEndpointsParticipantsHackathonStatusResponse,
} from '@geekshacking/portal-sdk'
import {
  geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsAnalyticsEndpointQueryOptions,
  geeksHackingPortalApiEndpointsParticipantsHackathonListEndpointQueryKey,
  geeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListEndpointQueryOptions,
  geeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpointQueryKey,
  geeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpointQueryOptions,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonJoinEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonListEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueries, useQueryClient } from '@tanstack/vue-query'
import { useLocalStorage } from '@vueuse/core'
import { formatHackathonDate, parseHackathonDateTimeValue } from '~/utils/hackathon-date-time'

/**
 * Workspace: every hackathon and workshop the organiser can manage (drafts included), past and
 * future, with search, filters, sorting and a list/card view. Each event links to its organiser
 * pages ("Manage") and its public page. Admins also get "New hackathon" / "New workshop" and an
 * Edit link per event; those open the full-page forms under /dash/manage/….
 *
 * Hackathons the person only takes part in still show their next step (join, continue
 * registration, status, event portal), as before.
 */
const toast = useGToast()
const queryClient = useQueryClient()
const joinMutation = useGeeksHackingPortalApiEndpointsParticipantsHackathonJoinEndpoint()

interface StandaloneEventAnalytics {
  registeredCount?: number
  capacityUsedPercent?: number
}

const { data: user, isLoading: isLoadingUser } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const isAdmin = computed(() => !!user.value?.isRoot)

// published hackathons (for everyone) + the ones this person manages (drafts too)
const { data: participantHackathonsData, isLoading: isLoadingParticipantHackathons } = useGeeksHackingPortalApiEndpointsParticipantsHackathonListEndpoint()
const { data: organizerHackathonsData, isLoading: isLoadingOrganizerHackathons } = useGeeksHackingPortalApiEndpointsOrganizersHackathonListEndpoint({
  query: { enabled: computed(() => !!user.value?.id) },
})
const { data: standaloneEventsData, isLoading: isLoadingStandaloneEvents } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint({
  query: { enabled: computed(() => !!user.value?.id) },
})

const isLoadingEvents = computed(() =>
  isLoadingUser.value
  || isLoadingParticipantHackathons.value
  || (!!user.value?.id && (isLoadingOrganizerHackathons.value || isLoadingStandaloneEvents.value)),
)

// organiser copies first (they include drafts), then published ones not already listed
const hackathons = computed(() => {
  const all = [...(organizerHackathonsData.value?.hackathons ?? []), ...(participantHackathonsData.value?.hackathons ?? [])]
  const byId = new Map<string, typeof all[number]>()
  for (const h of all) {
    if (h.id && !byId.has(h.id))
      byId.set(h.id, h)
  }
  return [...byId.values()]
})

const standaloneEvents = computed(() => standaloneEventsData.value?.standaloneWorkshops ?? [])

const analyticsQueries = useQueries({
  queries: computed(() => standaloneEvents.value.map(event => ({
    ...geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsAnalyticsEndpointQueryOptions({ path: { standaloneWorkshopId: event.id ?? '' } }),
    enabled: !!event.id,
  }))),
})

function analyticsFor(eventId: string | null | undefined): StandaloneEventAnalytics | undefined {
  const index = standaloneEvents.value.findIndex(event => event.id === eventId)
  return index === -1 ? undefined : unref(analyticsQueries.value[index]?.data) as StandaloneEventAnalytics | undefined
}

const statusQueries = useQueries({
  queries: computed(() => hackathons.value.map(hackathon => ({
    ...geeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpointQueryOptions({ path: { hackathonId: hackathon.id ?? '' } }),
    enabled: !!hackathon.id,
  }))),
})

const submissionQueries = useQueries({
  queries: computed(() => hackathons.value.map((hackathon, index) => {
    const status = unref(statusQueries.value[index]?.data) as GeeksHackingPortalApiEndpointsParticipantsHackathonStatusResponse | undefined
    return {
      ...geeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListEndpointQueryOptions({ path: { hackathonId: hackathon.id ?? '' } }),
      enabled: !!hackathon.id && status?.isParticipant === true,
    }
  })),
})

function statusFor(index: number) {
  return unref(statusQueries.value[index]?.data) as GeeksHackingPortalApiEndpointsParticipantsHackathonStatusResponse | undefined
}

function isRegistrationComplete(index: number) {
  const submissions = unref(submissionQueries.value[index]?.data) as GeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListResponse | undefined
  return submissions?.requiredQuestionsRemaining === 0
}

// ---- join (for hackathons the person doesn't organise) ------------------------------------------
const joiningId = ref<string | null>(null)

async function joinHackathon(hackathon: { id: string, shortCode: string }) {
  joiningId.value = hackathon.id
  try {
    await joinMutation.mutateAsync({ path: { hackathonId: hackathon.id } })
    await queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpointQueryKey({ path: { hackathonId: hackathon.id } }) })
    await queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsParticipantsHackathonListEndpointQueryKey() })
    navigateTo(`/${hackathon.shortCode}/registration`)
  }
  catch (error) {
    console.error('[WORKSPACE] Failed to join hackathon', error)
    toast.add({ title: 'Couldn\'t join', description: getApiErrorMessage(error, 'Try again in a moment.'), tone: 'err' })
  }
  finally {
    joiningId.value = null
  }
}

function participantStatus(status: GeeksHackingPortalApiEndpointsParticipantsHackathonStatusParticipantStatusKey | null | undefined, isParticipant?: boolean | null) {
  if (!isParticipant)
    return { label: 'Not joined', tone: 'neutral' as const, icon: undefined }
  switch (status) {
    case 'Accepted':
      return { label: 'Accepted', tone: 'ok' as const, icon: 'material-symbols:check-circle-rounded' }
    case 'Rejected':
      return { label: 'Not accepted', tone: 'err' as const, icon: 'material-symbols:cancel-rounded' }
    default:
      return { label: 'Pending review', tone: 'pending' as const, icon: 'material-symbols:hourglass-top-rounded' }
  }
}

// ---- rows ---------------------------------------------------------------------------------------
type Lifecycle = 'upcoming' | 'ongoing' | 'past'
const lifecycleLabel: Record<Lifecycle, string> = { upcoming: 'Upcoming', ongoing: 'Happening now', past: 'Past' }

function getLifecycle(start: string | null | undefined, end: string | null | undefined): Lifecycle {
  const now = Date.now()
  const startMs = parseHackathonDateTimeValue(start)?.getTime()
  const endMs = parseHackathonDateTimeValue(end)?.getTime()
  if (endMs !== undefined && endMs <= now)
    return 'past'
  if (startMs !== undefined && startMs > now)
    return 'upcoming'
  return startMs === undefined && endMs === undefined ? 'upcoming' : 'ongoing'
}

interface EventRow {
  key: string
  kind: 'hackathon' | 'workshop'
  id: string
  name: string
  description: string
  location: string
  shortCode: string
  homepageUri: string | null | undefined
  start: string | undefined
  end: string | undefined
  startMs: number
  lifecycle: Lifecycle
  isPublished: boolean
  needsAttention: boolean
  isMine: boolean
  isOrganizing: boolean
  isOpen: boolean
  status?: GeeksHackingPortalApiEndpointsParticipantsHackathonStatusResponse
  registrationComplete?: boolean
  maxParticipants?: number
}

const eventRows = computed<EventRow[]>(() => {
  const rows: EventRow[] = []

  hackathons.value.forEach((hackathon, index) => {
    const status = statusFor(index)
    const registrationComplete = isRegistrationComplete(index)
    rows.push({
      key: `h-${hackathon.id ?? index}`,
      kind: 'hackathon',
      id: hackathon.id ?? '',
      name: hackathon.name ?? '',
      description: hackathon.description ?? '',
      location: hackathon.venue ?? '',
      shortCode: hackathon.shortCode ?? '',
      homepageUri: hackathon.homepageUri,
      start: hackathon.eventStartDate,
      end: hackathon.eventEndDate,
      startMs: parseHackathonDateTimeValue(hackathon.eventStartDate)?.getTime() ?? Number.MAX_SAFE_INTEGER,
      lifecycle: getLifecycle(hackathon.eventStartDate, hackathon.eventEndDate),
      isPublished: hackathon.isPublished !== false,
      // a draft they run, or a registration they left unfinished
      needsAttention: (hackathon.isPublished === false && (!!status?.isOrganizer || isAdmin.value))
        || (!!status?.isParticipant && !registrationComplete),
      isMine: !!status?.isParticipant || !!status?.isOrganizer || isAdmin.value,
      isOrganizing: !!status?.isOrganizer || isAdmin.value,
      isOpen: !status?.isParticipant && !status?.isOrganizer,
      status,
      registrationComplete,
    })
  })

  for (const event of standaloneEvents.value) {
    rows.push({
      key: `w-${event.id}`,
      kind: 'workshop',
      id: event.id ?? '',
      name: event.title ?? '',
      description: event.description ?? '',
      location: event.location ?? '',
      shortCode: event.shortCode ?? '',
      homepageUri: event.homepageUri,
      start: event.startTime,
      end: event.endTime,
      startMs: parseHackathonDateTimeValue(event.startTime)?.getTime() ?? Number.MAX_SAFE_INTEGER,
      lifecycle: getLifecycle(event.startTime, event.endTime),
      isPublished: !!event.isPublished,
      needsAttention: !event.isPublished,
      isMine: !!user.value?.id,
      isOrganizing: !!user.value?.id,
      isOpen: !!event.isPublished,
      maxParticipants: event.maxParticipants ?? 0,
    })
  }

  return rows
})

// ---- search, filters, sort, view ----------------------------------------------------------------
const searchQuery = ref('')
const activeEventType = ref<'all' | 'hackathons' | 'workshops'>('all')
const activeInvolvement = ref<'all' | 'open' | 'mine' | 'organizing'>('all')
const activeLifecycle = ref<'all' | Lifecycle>('all')
const activePublishState = ref<'all' | 'published' | 'draft'>('all')
const activeSort = ref<'date-asc' | 'date-desc' | 'name' | 'attention'>('date-asc')
const viewMode = useLocalStorage<'list' | 'card'>('dash-events-view', 'list')

const eventTypeFilters = [
  { label: 'All', value: 'all' },
  { label: 'Hackathons', value: 'hackathons', icon: 'material-symbols:trophy-rounded' },
  { label: 'Workshops', value: 'workshops', icon: 'material-symbols:school-rounded' },
]
const viewModes = [
  { label: 'List', value: 'list', icon: 'material-symbols:view-list-rounded' },
  { label: 'Cards', value: 'card', icon: 'material-symbols:grid-view-rounded' },
]
const lifecycleFilters = [
  { label: 'Any time', value: 'all' as const },
  { label: 'Upcoming', value: 'upcoming' as const },
  { label: 'Happening now', value: 'ongoing' as const },
  { label: 'Past', value: 'past' as const },
]
const publishFilters = [
  { label: 'Published or draft', value: 'all' as const },
  { label: 'Published', value: 'published' as const },
  { label: 'Drafts', value: 'draft' as const },
]
const involvementFilters = [
  { label: 'All events', value: 'all' as const },
  { label: 'Open to join', value: 'open' as const },
  { label: 'My events', value: 'mine' as const },
  { label: 'Organising', value: 'organizing' as const },
]
const sortOptions = [
  { label: 'Date: soonest first', value: 'date-asc' as const },
  { label: 'Date: latest first', value: 'date-desc' as const },
  { label: 'Name: A to Z', value: 'name' as const },
  { label: 'Needs attention first', value: 'attention' as const },
]

// GTabs speaks plain strings
const eventTypeModel = computed({
  get: () => activeEventType.value as string,
  set: value => (activeEventType.value = value as typeof activeEventType.value),
})
const viewModeModel = computed({
  get: () => viewMode.value as string,
  set: value => (viewMode.value = value as typeof viewMode.value),
})

const visibleEvents = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()
  const filtered = eventRows.value.filter((row) => {
    if (activeEventType.value === 'hackathons' && row.kind !== 'hackathon')
      return false
    if (activeEventType.value === 'workshops' && row.kind !== 'workshop')
      return false
    if (activeLifecycle.value !== 'all' && row.lifecycle !== activeLifecycle.value)
      return false
    if (activePublishState.value === 'published' && !row.isPublished)
      return false
    if (activePublishState.value === 'draft' && row.isPublished)
      return false
    if (activeInvolvement.value === 'open' && !row.isOpen)
      return false
    if (activeInvolvement.value === 'mine' && !row.isMine)
      return false
    if (activeInvolvement.value === 'organizing' && !row.isOrganizing)
      return false
    if (query && ![row.name, row.description, row.location, row.shortCode].join(' ').toLowerCase().includes(query))
      return false
    return true
  })

  return filtered.sort((a, b) => {
    switch (activeSort.value) {
      case 'date-desc':
        return b.startMs - a.startMs
      case 'name':
        return a.name.localeCompare(b.name)
      case 'attention':
        if (a.needsAttention !== b.needsAttention)
          return a.needsAttention ? -1 : 1
        return a.startMs - b.startMs
      default:
        return a.startMs - b.startMs
    }
  })
})

const hasActiveFilters = computed(() =>
  !!searchQuery.value.trim()
  || activeEventType.value !== 'all'
  || activeInvolvement.value !== 'all'
  || activeLifecycle.value !== 'all'
  || activePublishState.value !== 'all',
)

function resetFilters() {
  searchQuery.value = ''
  activeEventType.value = 'all'
  activeInvolvement.value = 'all'
  activeLifecycle.value = 'all'
  activePublishState.value = 'all'
}

const stats = computed(() => {
  const rows = eventRows.value
  return {
    total: rows.length,
    upcoming: rows.filter(r => r.lifecycle === 'upcoming').length,
    ongoing: rows.filter(r => r.lifecycle === 'ongoing').length,
    attention: rows.filter(r => r.needsAttention).length,
  }
})

function showAttention() {
  resetFilters()
  activeSort.value = 'attention'
}

// ---- formatting ---------------------------------------------------------------------------------
function eventDateRange(start?: string | null, end?: string | null) {
  const from = formatHackathonDate(start)
  const to = formatHackathonDate(end)
  return from === to ? from : `${from} to ${to}`
}

function capacity(row: EventRow) {
  const analytics = analyticsFor(row.id)
  const max = row.maxParticipants ?? 0
  const registered = analytics?.registeredCount
  const percent = typeof analytics?.capacityUsedPercent === 'number'
    ? analytics.capacityUsedPercent
    : (max > 0 && registered !== undefined ? (registered / max) * 100 : 0)
  return {
    label: registered === undefined ? `${max} places` : `${registered} of ${max} places taken`,
    percent: Math.min(Math.max(Math.round(percent), 0), 100),
    known: registered !== undefined,
  }
}

useHead({ title: 'Workspace' })
</script>

<template>
  <GPanel id="dashboard-management">
    <template #header>
      <GPanelHeader title="Workspace" />
    </template>

    <template #body>
      <div class="ws">
        <header class="ws-head">
          <div class="ws-head__text">
            <h1 class="ws-head__title">
              Your events
            </h1>
            <p class="ws-head__lead">
              Every hackathon and workshop you run, drafts included. Open one to manage participants, check-ins and more.
            </p>
          </div>
          <div
            v-if="isAdmin"
            class="ws-head__actions"
          >
            <GButton
              to="/dash/manage/hackathon/new"
              variant="primary"
              icon="material-symbols:add-rounded"
              label="New hackathon"
            />
            <GButton
              to="/dash/manage/workshop/new"
              variant="secondary"
              icon="material-symbols:add-rounded"
              label="New workshop"
            />
          </div>
        </header>

        <div class="ws-stats">
          <GStatTile
            label="Events"
            :value="stats.total"
            icon="material-symbols:event-rounded"
            :loading="isLoadingEvents"
          />
          <GStatTile
            label="Upcoming"
            :value="stats.upcoming"
            icon="material-symbols:calendar-clock-rounded"
            :loading="isLoadingEvents"
          />
          <GStatTile
            label="Happening now"
            :value="stats.ongoing"
            icon="material-symbols:radio-button-checked"
            :loading="isLoadingEvents"
          />
          <GStatTile
            label="Need attention"
            :value="stats.attention"
            hint="Drafts and unfinished registrations"
            icon="material-symbols:error-rounded"
            :highlight="stats.attention > 0"
            :loading="isLoadingEvents"
          />
        </div>

        <!-- search, filters, sort, view -->
        <section
          class="ws-tools"
          aria-label="Search and filter events"
        >
          <div class="ws-tools__row">
            <GInput
              v-model="searchQuery"
              type="search"
              icon="material-symbols:search-rounded"
              placeholder="Search events, venues or short codes"
              aria-label="Search events"
              class="ws-tools__search"
            />
            <GSelect
              v-model="activeLifecycle"
              :items="lifecycleFilters"
              icon="material-symbols:calendar-today-rounded"
              aria-label="Filter by time"
              class="ws-tools__select"
            />
            <GSelect
              v-model="activePublishState"
              :items="publishFilters"
              icon="material-symbols:public"
              aria-label="Filter by published or draft"
              class="ws-tools__select"
            />
            <GSelect
              v-model="activeInvolvement"
              :items="involvementFilters"
              icon="material-symbols:person-rounded"
              aria-label="Filter by your involvement"
              class="ws-tools__select"
            />
          </div>
          <div class="ws-tools__row ws-tools__row--split">
            <GTabs
              v-model="eventTypeModel"
              :items="eventTypeFilters"
              :content="false"
              size="sm"
              aria-label="Event type"
            />
            <div class="ws-tools__meta">
              <p
                class="ws-tools__count"
                aria-live="polite"
              >
                {{ visibleEvents.length }} of {{ eventRows.length }} events
              </p>
              <GButton
                v-if="hasActiveFilters"
                variant="ghost"
                size="sm"
                icon="material-symbols:close-rounded"
                label="Clear filters"
                @click="resetFilters"
              />
              <GSelect
                v-model="activeSort"
                :items="sortOptions"
                icon="material-symbols:swap-vert-rounded"
                size="sm"
                aria-label="Sort events"
                class="ws-tools__sort"
              />
              <GTabs
                v-model="viewModeModel"
                :items="viewModes"
                :content="false"
                size="sm"
                aria-label="View"
              />
            </div>
          </div>
        </section>

        <!-- events -->
        <div
          v-if="isLoadingEvents"
          class="ws-list"
          :class="{ 'ws-list--cards': viewMode === 'card' }"
          aria-busy="true"
        >
          <span class="sr-only">Loading events…</span>
          <GSkeleton
            v-for="index in 4"
            :key="index"
            class="ws-skeleton"
          />
        </div>

        <GEmptyState
          v-else-if="!visibleEvents.length"
          icon="material-symbols:search-off-rounded"
          :title="eventRows.length ? 'No events match' : 'No events yet'"
          :description="eventRows.length ? 'Try a different search, or clear your filters.' : (isAdmin ? 'Create a hackathon or workshop to get started.' : 'Events you organise show up here.')"
          bordered
        >
          <template
            v-if="hasActiveFilters || (!eventRows.length && isAdmin)"
            #actions
          >
            <GButton
              v-if="hasActiveFilters"
              variant="secondary"
              icon="material-symbols:close-rounded"
              label="Clear filters"
              @click="resetFilters"
            />
            <template v-else>
              <GButton
                to="/dash/manage/hackathon/new"
                variant="primary"
                icon="material-symbols:add-rounded"
                label="New hackathon"
              />
              <GButton
                to="/dash/manage/workshop/new"
                variant="secondary"
                icon="material-symbols:add-rounded"
                label="New workshop"
              />
            </template>
          </template>
        </GEmptyState>

        <ul
          v-else
          class="ws-list"
          :class="{ 'ws-list--cards': viewMode === 'card' }"
        >
          <li
            v-for="row in visibleEvents"
            :key="row.key"
          >
            <article
              class="ws-event"
              :class="{ 'ws-event--attention': row.needsAttention }"
              :aria-labelledby="`ws-${row.key}`"
            >
              <div class="ws-event__main">
                <p class="ws-event__kind">
                  <Icon
                    :name="row.kind === 'hackathon' ? 'material-symbols:trophy-rounded' : 'material-symbols:school-rounded'"
                    aria-hidden="true"
                  />
                  {{ row.kind === 'hackathon' ? 'Hackathon' : 'Workshop' }}
                  <span
                    v-if="row.shortCode"
                    class="ws-event__code"
                  >· {{ row.shortCode }}</span>
                </p>
                <h2
                  :id="`ws-${row.key}`"
                  class="ws-event__name"
                >
                  {{ row.name || 'Untitled event' }}
                </h2>
                <p
                  v-if="row.description"
                  class="ws-event__desc"
                >
                  {{ row.description }}
                </p>
                <div class="ws-event__badges">
                  <GBadge
                    :tone="row.isPublished ? 'ok' : 'pending'"
                    :variant="row.isPublished ? 'soft' : 'solid'"
                    :icon="row.isPublished ? 'material-symbols:public' : 'material-symbols:visibility-off-rounded'"
                    :label="row.isPublished ? 'Published' : 'Draft · not public'"
                  />
                  <GBadge
                    variant="outline"
                    :label="lifecycleLabel[row.lifecycle]"
                  />
                  <template v-if="row.kind === 'hackathon' && !row.isOrganizing">
                    <GBadge
                      :tone="participantStatus(row.status?.status ?? null, row.status?.isParticipant).tone"
                      :icon="participantStatus(row.status?.status ?? null, row.status?.isParticipant).icon"
                      :label="participantStatus(row.status?.status ?? null, row.status?.isParticipant).label"
                    />
                    <GBadge
                      v-if="row.needsAttention && row.isPublished"
                      tone="pending"
                      variant="solid"
                      icon="material-symbols:error-rounded"
                      label="Registration incomplete"
                    />
                  </template>
                </div>
              </div>

              <dl class="ws-event__meta">
                <div>
                  <dt class="sr-only">
                    When
                  </dt>
                  <dd>
                    <Icon
                      name="material-symbols:calendar-today-rounded"
                      aria-hidden="true"
                    />
                    <span>{{ eventDateRange(row.start, row.end) }}</span>
                  </dd>
                </div>
                <div v-if="row.location">
                  <dt class="sr-only">
                    Where
                  </dt>
                  <dd>
                    <Icon
                      name="material-symbols:location-on-rounded"
                      aria-hidden="true"
                    />
                    <span>{{ row.location }}</span>
                  </dd>
                </div>
                <div v-if="row.kind === 'workshop'">
                  <dt class="sr-only">
                    Places
                  </dt>
                  <dd class="ws-cap">
                    <Icon
                      name="material-symbols:group-rounded"
                      aria-hidden="true"
                    />
                    <span class="ws-cap__body">
                      <span>{{ capacity(row).label }}</span>
                      <span
                        v-if="capacity(row).known"
                        class="ws-cap__track"
                        role="img"
                        :aria-label="`${capacity(row).percent}% full`"
                      >
                        <span
                          class="ws-cap__bar"
                          :style="{ width: `${capacity(row).percent}%` }"
                        />
                      </span>
                    </span>
                  </dd>
                </div>
              </dl>

              <div class="ws-event__actions">
                <!-- events this person runs -->
                <template v-if="row.kind === 'workshop' || row.isOrganizing">
                  <GButton
                    v-if="row.id"
                    :to="row.kind === 'workshop' ? `/dash/standalone/${row.id}` : `/dash/${row.id}`"
                    variant="primary"
                    size="sm"
                    icon="material-symbols:settings-rounded"
                    label="Manage"
                    :aria-label="`Manage ${row.name}`"
                  />
                  <GButton
                    v-if="isAdmin && row.id"
                    :to="`/dash/manage/${row.kind}/${row.id}/edit`"
                    variant="secondary"
                    size="sm"
                    icon="material-symbols:edit-rounded"
                    label="Edit"
                    :aria-label="`Edit ${row.name}`"
                  />
                  <GButton
                    v-if="row.shortCode"
                    :to="row.kind === 'workshop' ? `/workshops/${row.shortCode}` : `/${row.shortCode}/team`"
                    variant="ghost"
                    size="sm"
                    trailing-icon="material-symbols:arrow-forward-rounded"
                    label="View page"
                    :aria-label="`View the ${row.name} page`"
                  />
                  <GButton
                    v-else-if="row.homepageUri"
                    :href="row.homepageUri"
                    target="_blank"
                    variant="ghost"
                    size="sm"
                    trailing-icon="material-symbols:open-in-new-rounded"
                    label="Website"
                    :aria-label="`${row.name} website (opens in a new tab)`"
                  />
                </template>

                <!-- hackathons this person takes part in: their next step -->
                <template v-else>
                  <GButton
                    v-if="!row.status?.isParticipant"
                    variant="primary"
                    size="sm"
                    icon="material-symbols:person-add-rounded"
                    label="Join event"
                    :aria-label="`Join ${row.name}`"
                    :loading="joiningId === row.id"
                    loading-label="Joining"
                    :disabled="!!joiningId && joiningId !== row.id"
                    @click="joinHackathon({ id: row.id, shortCode: row.shortCode })"
                  />
                  <GButton
                    v-else-if="!row.registrationComplete"
                    :to="`/${row.shortCode}/registration`"
                    variant="primary"
                    size="sm"
                    icon="material-symbols:edit-note-rounded"
                    label="Continue registration"
                  />
                  <GButton
                    v-else-if="row.status?.status !== 'Accepted'"
                    :to="`/dash/${row.id}/participant`"
                    variant="secondary"
                    size="sm"
                    icon="material-symbols:fact-check-rounded"
                    label="View status"
                  />
                  <GButton
                    v-else
                    :to="`/${row.shortCode}/team`"
                    variant="primary"
                    size="sm"
                    trailing-icon="material-symbols:arrow-forward-rounded"
                    label="Open event portal"
                  />
                </template>
              </div>
            </article>
          </li>
        </ul>

        <p
          v-if="!isLoadingEvents && stats.attention && activeSort !== 'attention'"
          class="ws-foot"
        >
          <GButton
            variant="ghost"
            size="sm"
            icon="material-symbols:error-rounded"
            :label="`Show the ${stats.attention} that need attention first`"
            @click="showAttention"
          />
        </p>
      </div>
    </template>
  </GPanel>
</template>

<style scoped>
.ws {
  display: flex;
  flex-direction: column;
  gap: 24px;
  width: 100%;
  max-width: 80rem;
  margin: 0 auto;
}
.ws-head {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  justify-content: space-between;
  gap: 16px 24px;
}
.ws-head__text {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 0;
  flex: 1 1 28rem;
}
.ws-head__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: clamp(28px, 4.5vw, 40px);
  font-weight: 700;
  line-height: 1.1;
}
.ws-head__lead {
  max-width: 65ch;
  margin: 0;
  color: var(--gh-text-muted);
}
.ws-head__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.ws-stats {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}
@media (min-width: 1024px) {
  .ws-stats { grid-template-columns: repeat(4, minmax(0, 1fr)); }
}

/* toolbar */
.ws-tools {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.ws-tools__row {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
@media (min-width: 1024px) {
  .ws-tools__row { flex-direction: row; align-items: center; }
  .ws-tools__search { flex: 1 1 auto; }
  .ws-tools__select { flex: 0 0 12rem; }
}
.ws-tools__row--split {
  flex-direction: row;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 8px 16px;
}
.ws-tools__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px 12px;
}
.ws-tools__count {
  margin: 0;
  font-size: 14px;
  color: var(--gh-text-muted);
  font-variant-numeric: tabular-nums;
}
.ws-tools__sort { width: 14rem; }

/* events */
.ws-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.ws-list--cards {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 16px;
}
@media (min-width: 640px) {
  .ws-list--cards { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
@media (min-width: 1280px) {
  .ws-list--cards { grid-template-columns: repeat(3, minmax(0, 1fr)); }
}
.ws-list > li { min-width: 0; }
.ws-skeleton {
  display: block;
  height: 120px;
  border-radius: var(--radius-gh-lg);
}
.ws-list--cards .ws-skeleton { height: 260px; }

.ws-event {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 16px;
  height: 100%;
  padding: 20px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
/* list rows: details | when, where, places | actions */
@media (min-width: 1024px) {
  .ws-list:not(.ws-list--cards) .ws-event {
    grid-template-columns: minmax(0, 2.2fr) minmax(0, 1.3fr) auto;
    align-items: center;
    gap: 24px;
  }
  .ws-list:not(.ws-list--cards) .ws-event__actions {
    justify-content: flex-end;
    max-width: 17rem;
  }
  .ws-list:not(.ws-list--cards) .ws-event__desc { -webkit-line-clamp: 1; line-clamp: 1; }
}
.ws-list--cards .ws-event { grid-template-rows: 1fr auto auto; }
/* drafts and unfinished registrations: a yellow edge */
.ws-event--attention {
  box-shadow: inset 6px 0 0 var(--gh-accent);
}

.ws-event__main {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 0;
}
.ws-event__kind {
  display: inline-flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px;
  margin: 0;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.ws-event__kind > :first-child { width: 16px; height: 16px; flex: none; }
.ws-event__code {
  font-weight: 600;
  letter-spacing: 0;
  text-transform: none;
}
.ws-event__name {
  margin: 0;
  font-family: var(--font-display);
  font-size: 20px;
  font-weight: 700;
  line-height: 1.2;
  overflow-wrap: anywhere;
}
.ws-event__desc {
  display: -webkit-box;
  margin: 0;
  overflow: hidden;
  font-size: 14px;
  color: var(--gh-text-muted);
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 3;
  line-clamp: 3;
}
.ws-event__badges {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}
.ws-event__meta {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin: 0;
  font-size: 14px;
}
.ws-event__meta dd {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  margin: 0;
  overflow-wrap: anywhere;
}
.ws-event__meta dd > :first-child {
  flex: none;
  width: 18px;
  height: 18px;
  margin-top: 1px;
  color: var(--gh-text-muted);
}
.ws-cap__body {
  display: flex;
  flex-direction: column;
  gap: 6px;
  flex: 1 1 auto;
  min-width: 0;
}
.ws-cap__track {
  display: block;
  width: 100%;
  max-width: 12rem;
  height: 6px;
  overflow: hidden;
  border-radius: 999px;
  background: var(--gh-viz-track);
}
.ws-cap__bar {
  display: block;
  height: 100%;
  border-radius: 999px;
  background: var(--gh-viz-1);
}
.ws-event__actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}
.ws-foot {
  display: flex;
  justify-content: center;
  margin: 0;
}
</style>
