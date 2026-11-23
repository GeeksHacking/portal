<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListResponse,
  GeeksHackingPortalApiEndpointsParticipantsHackathonStatusParticipantStatusKey,
  GeeksHackingPortalApiEndpointsParticipantsHackathonStatusResponse,
} from '@geekshacking/portal-sdk'
import {
  geeksHackingPortalApiEndpointsParticipantsHackathonListEndpointQueryKey,
  geeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListEndpointQueryOptions,
  geeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpointQueryKey,
  geeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpointQueryOptions,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonJoinEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonListEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueries, useQueryClient } from '@tanstack/vue-query'
import { useLocalStorage } from '@vueuse/core'
import { computed, ref, unref } from 'vue'
import { formatHackathonDate } from '~/utils/hackathon-date-time'

/**
 * Explore: everyone's dashboard home. Upcoming and ongoing hackathons (with your status in each)
 * and workshops, with search, filters, sorting and a list/card view. The next step for each event
 * is its main button: join, continue registration, view status or open the event portal.
 *
 * Workshops come from the organiser list, so only organisers (and admins) see them here; a
 * participant workshop list needs a new API endpoint.
 */
const toast = useGToast()
const queryClient = useQueryClient()
const joinMutation = useGeeksHackingPortalApiEndpointsParticipantsHackathonJoinEndpoint()

const { data: user, isLoading: isLoadingUser } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const { data: hackathonsData, isLoading: isLoadingHackathons } = useGeeksHackingPortalApiEndpointsParticipantsHackathonListEndpoint()
const { data: standaloneEventsData, isLoading: isLoadingStandaloneEvents } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint({
  query: {
    enabled: computed(() => !!user.value?.id),
  },
})

function hasNotEnded(endTime?: string | null) {
  if (!endTime)
    return false

  return new Date(endTime).getTime() > Date.now()
}

const hackathons = computed(() => (hackathonsData.value?.hackathons ?? []).filter(hackathon => hasNotEnded(hackathon.eventEndDate)))
const publishedStandaloneEvents = computed(() =>
  (standaloneEventsData.value?.standaloneWorkshops ?? []).filter(event => event.isPublished && hasNotEnded(event.endTime)),
)

const statusQueries = useQueries({
  queries: computed(() =>
    hackathons.value.map(hackathon => ({
      ...geeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpointQueryOptions({ path: { hackathonId: hackathon.id ?? '' } }),
      enabled: !!hackathon.id,
    })),
  ),
})

const submissionQueries = useQueries({
  queries: computed(() =>
    hackathons.value.map((hackathon, index) => {
      const status = statusQueries.value[index]?.data as GeeksHackingPortalApiEndpointsParticipantsHackathonStatusResponse | undefined
      return {
        ...geeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListEndpointQueryOptions({ path: { hackathonId: hackathon.id ?? '' } }),
        enabled: !!hackathon.id && status?.isParticipant === true,
      }
    }),
  ),
})

function statusDataForIndex(index: number): GeeksHackingPortalApiEndpointsParticipantsHackathonStatusResponse | undefined {
  return unref(statusQueries.value[index]?.data) as GeeksHackingPortalApiEndpointsParticipantsHackathonStatusResponse | undefined
}

function submissionsDataForIndex(index: number): GeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListResponse | undefined {
  return unref(submissionQueries.value[index]?.data) as GeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListResponse | undefined
}

function isRegistrationComplete(index: number) {
  return submissionsDataForIndex(index)?.requiredQuestionsRemaining === 0
}

function formatParticipantStatus(status: GeeksHackingPortalApiEndpointsParticipantsHackathonStatusParticipantStatusKey | null | undefined, isParticipant?: boolean | null) {
  if (!isParticipant)
    return { label: 'Open', tone: 'ok' as const, icon: 'material-symbols:lock-open-right-rounded' }

  switch (status) {
    case 'Accepted':
      return { label: 'Accepted', tone: 'ok' as const, icon: 'material-symbols:check-circle-rounded' }
    case 'Rejected':
      return { label: 'Not accepted', tone: 'err' as const, icon: 'material-symbols:cancel-rounded' }
    default:
      return { label: 'Pending review', tone: 'pending' as const, icon: 'material-symbols:hourglass-top-rounded' }
  }
}

// only the clicked event's button spins
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
    console.error('[DASH] Failed to join hackathon', error)
    toast.add({
      title: 'Couldn\'t join',
      description: getApiErrorMessage(error, 'Try again in a moment.'),
      tone: 'err',
    })
  }
  finally {
    joiningId.value = null
  }
}

const searchQuery = ref('')
const activeEventType = ref<'all' | 'hackathons' | 'workshops'>('all')
const activeAudience = ref<'all' | 'open' | 'joined'>('all')
const activeSort = ref<'date-asc' | 'date-desc' | 'name' | 'attention'>('attention')
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

// GTabs speaks plain strings
const eventTypeModel = computed({
  get: () => activeEventType.value as string,
  set: value => (activeEventType.value = value as typeof activeEventType.value),
})
const viewModeModel = computed({
  get: () => viewMode.value as string,
  set: value => (viewMode.value = value as typeof viewMode.value),
})

const audienceFilters = [
  { label: 'All events', value: 'all' as const },
  { label: 'Open to join', value: 'open' as const },
  { label: 'Joined', value: 'joined' as const },
]

const sortOptions = [
  { label: 'Needs action first', value: 'attention' as const },
  { label: 'Date: soonest first', value: 'date-asc' as const },
  { label: 'Date: latest first', value: 'date-desc' as const },
  { label: 'Name: A to Z', value: 'name' as const },
]

const hackathonCards = computed(() =>
  hackathons.value.map((hackathon, index) => ({
    hackathon,
    index,
    status: statusDataForIndex(index),
    registrationComplete: isRegistrationComplete(index),
  })),
)

interface EventRow {
  key: string
  kind: 'hackathon' | 'workshop'
  name: string
  description: string
  location: string
  shortCode: string
  homepageUri: string | null | undefined
  start: string | undefined
  end: string | undefined
  startMs: number
  isOngoing: boolean
  isJoined: boolean
  needsAttention: boolean
  hackathonCard?: typeof hackathonCards.value[number]
}

function toStartMs(start?: string | null) {
  return start ? new Date(start).getTime() : Number.MAX_SAFE_INTEGER
}

const eventRows = computed<EventRow[]>(() => {
  const now = Date.now()
  const rows: EventRow[] = []

  for (const card of hackathonCards.value) {
    const { hackathon, status, registrationComplete } = card
    const startMs = toStartMs(hackathon.eventStartDate)
    rows.push({
      key: `h-${hackathon.id ?? card.index}`,
      kind: 'hackathon',
      name: hackathon.name ?? '',
      description: markdownToPlainText(hackathon.description) || 'Details are being prepared by the organisers.',
      location: hackathon.venue ?? '',
      shortCode: hackathon.shortCode ?? '',
      homepageUri: hackathon.homepageUri,
      start: hackathon.eventStartDate,
      end: hackathon.eventEndDate,
      startMs,
      isOngoing: startMs <= now,
      isJoined: !!status?.isParticipant,
      needsAttention: !!status?.isParticipant && !registrationComplete,
      hackathonCard: card,
    })
  }

  for (const event of publishedStandaloneEvents.value) {
    const startMs = toStartMs(event.startTime)
    rows.push({
      key: `w-${event.id}`,
      kind: 'workshop',
      name: event.title ?? '',
      description: markdownToPlainText(event.description) || 'Workshop details are being prepared.',
      location: event.location ?? '',
      shortCode: event.shortCode ?? '',
      homepageUri: event.homepageUri,
      start: event.startTime,
      end: event.endTime,
      startMs,
      isOngoing: startMs <= now,
      isJoined: false,
      needsAttention: false,
    })
  }

  return rows
})

const visibleEvents = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()

  const filtered = eventRows.value.filter((row) => {
    if (activeEventType.value === 'hackathons' && row.kind !== 'hackathon')
      return false
    if (activeEventType.value === 'workshops' && row.kind !== 'workshop')
      return false
    if (activeAudience.value === 'open' && row.isJoined)
      return false
    if (activeAudience.value === 'joined' && !row.isJoined)
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

const isLoadingEvents = computed(() => isLoadingUser.value || isLoadingHackathons.value || (!!user.value?.id && isLoadingStandaloneEvents.value))

const hasActiveFilters = computed(() =>
  !!searchQuery.value.trim() || activeEventType.value !== 'all' || activeAudience.value !== 'all',
)

function resetFilters() {
  searchQuery.value = ''
  activeEventType.value = 'all'
  activeAudience.value = 'all'
}

function eventDateRange(start?: string | null, end?: string | null) {
  const from = formatHackathonDate(start)
  const to = formatHackathonDate(end)
  // one-day events (most workshops) show a single date
  return from === to ? from : `${from} to ${to}`
}

</script>

<template>
  <GPanel id="dashboard">
    <template #header>
      <GPanelHeader title="Explore" />
    </template>

    <template #body>
      <div class="ex">
        <header class="ex-head">
          <h1 class="ex-head__title">
            {{ user?.gitHubLogin ? `Welcome back, ${user.gitHubLogin}` : 'Explore' }}
          </h1>
          <p class="ex-head__lead">
            Find hackathons and workshops, pick up where you left off, and jump into your events.
          </p>
        </header>

        <!-- search, filters, sort, view -->
        <section
          class="ex-tools"
          aria-label="Search and filter events"
        >
          <div class="ex-tools__row">
            <GInput
              v-model="searchQuery"
              type="search"
              icon="material-symbols:search-rounded"
              placeholder="Search events, venues or short codes"
              aria-label="Search events"
              class="ex-tools__search"
            />
            <GSelect
              v-model="activeAudience"
              :items="audienceFilters"
              icon="material-symbols:person-rounded"
              aria-label="Filter by whether you've joined"
              class="ex-tools__select"
            />
            <GSelect
              v-model="activeSort"
              :items="sortOptions"
              icon="material-symbols:swap-vert-rounded"
              aria-label="Sort events"
              class="ex-tools__select ex-tools__select--wide"
            />
          </div>
          <div class="ex-tools__row ex-tools__row--split">
            <GTabs
              v-model="eventTypeModel"
              :items="eventTypeFilters"
              :content="false"
              size="sm"
              aria-label="Event type"
            />
            <div class="ex-tools__meta">
              <p
                class="ex-tools__count"
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
          class="ex-list"
          :class="{ 'ex-list--cards': viewMode === 'card' }"
          aria-busy="true"
        >
          <span class="sr-only">Loading events…</span>
          <GSkeleton
            v-for="index in 4"
            :key="index"
            class="ex-skeleton"
          />
        </div>

        <template v-else>
          <GAlert
            v-if="!user?.id && activeEventType !== 'hackathons'"
            icon="material-symbols:login-rounded"
            title="Sign in to see workshops"
            description="Workshops show up here once you're signed in."
          />

          <GEmptyState
            v-if="!visibleEvents.length"
            icon="material-symbols:search-off-rounded"
            :title="eventRows.length ? 'No events match' : 'Nothing on right now'"
            :description="eventRows.length ? 'Try a different search, or clear your filters.' : 'New hackathons and workshops will show up here.'"
            bordered
          >
            <template
              v-if="hasActiveFilters"
              #actions
            >
              <GButton
                variant="secondary"
                icon="material-symbols:close-rounded"
                label="Clear filters"
                @click="resetFilters"
              />
            </template>
          </GEmptyState>

          <ul
            v-else
            class="ex-list"
            :class="{ 'ex-list--cards': viewMode === 'card' }"
          >
            <li
              v-for="row in visibleEvents"
              :key="row.key"
            >
              <article
                class="ex-event"
                :class="{ 'ex-event--attention': row.needsAttention }"
                :aria-labelledby="`ex-${row.key}`"
              >
                <div class="ex-event__main">
                  <p class="ex-event__kind">
                    <Icon
                      :name="row.kind === 'hackathon' ? 'material-symbols:trophy-rounded' : 'material-symbols:school-rounded'"
                      aria-hidden="true"
                    />
                    {{ row.kind === 'hackathon' ? 'Hackathon' : 'Workshop' }}
                  </p>
                  <h2
                    :id="`ex-${row.key}`"
                    class="ex-event__name"
                  >
                    {{ row.name }}
                  </h2>
                  <p class="ex-event__desc">
                    {{ row.description }}
                  </p>
                  <div class="ex-event__badges">
                    <GBadge
                      v-if="row.hackathonCard"
                      :tone="formatParticipantStatus(row.hackathonCard.status?.status ?? null, row.hackathonCard.status?.isParticipant).tone"
                      :icon="formatParticipantStatus(row.hackathonCard.status?.status ?? null, row.hackathonCard.status?.isParticipant).icon"
                      :label="formatParticipantStatus(row.hackathonCard.status?.status ?? null, row.hackathonCard.status?.isParticipant).label"
                    />
                    <GBadge
                      v-else
                      tone="ok"
                      icon="material-symbols:lock-open-right-rounded"
                      label="Open"
                    />
                    <GBadge
                      v-if="row.needsAttention"
                      tone="pending"
                      variant="solid"
                      icon="material-symbols:error-rounded"
                      label="Registration incomplete"
                    />
                    <GBadge
                      variant="outline"
                      :label="row.isOngoing ? 'Happening now' : 'Upcoming'"
                    />
                  </div>
                  <GAlert
                    v-if="row.hackathonCard?.status?.status === 'Rejected' && row.hackathonCard.status.reviewReason"
                    tone="err"
                    title="Reason"
                    :description="row.hackathonCard.status.reviewReason"
                  />
                </div>

                <dl class="ex-event__meta">
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
                </dl>

                <div class="ex-event__actions">
                  <template v-if="row.hackathonCard">
                    <GButton
                      v-if="!row.hackathonCard.status?.isParticipant"
                      variant="primary"
                      size="sm"
                      icon="material-symbols:person-add-rounded"
                      label="Join event"
                      :aria-label="`Join ${row.name}`"
                      :loading="joiningId === row.hackathonCard.hackathon.id"
                      loading-label="Joining"
                      :disabled="!!joiningId && joiningId !== row.hackathonCard.hackathon.id"
                      @click="joinHackathon({ id: row.hackathonCard.hackathon.id!, shortCode: row.hackathonCard.hackathon.shortCode! })"
                    />
                    <GButton
                      v-else-if="!row.hackathonCard.registrationComplete"
                      :to="`/${row.shortCode}/registration`"
                      variant="primary"
                      size="sm"
                      icon="material-symbols:edit-note-rounded"
                      label="Continue registration"
                    />
                    <GButton
                      v-else-if="row.hackathonCard.status?.status !== 'Accepted'"
                      :to="`/dash/${row.hackathonCard.hackathon.id}/participant`"
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
                  <GButton
                    v-else-if="row.shortCode"
                    :to="`/workshops/${row.shortCode}`"
                    variant="primary"
                    size="sm"
                    icon="material-symbols:edit-note-rounded"
                    label="Register"
                    :aria-label="`Register for ${row.name}`"
                  />

                  <GButton
                    v-if="row.homepageUri"
                    :href="row.homepageUri"
                    target="_blank"
                    variant="ghost"
                    size="sm"
                    trailing-icon="material-symbols:open-in-new-rounded"
                    label="Website"
                    :aria-label="`${row.name} website (opens in a new tab)`"
                  />
                </div>
              </article>
            </li>
          </ul>
        </template>
      </div>
    </template>
  </GPanel>
</template>

<style scoped>
.ex {
  display: flex;
  flex-direction: column;
  gap: 24px;
  width: 100%;
  max-width: 80rem;
  margin: 0 auto;
}
.ex-head {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.ex-head__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: clamp(28px, 4.5vw, 40px);
  font-weight: 700;
  line-height: 1.1;
  overflow-wrap: anywhere;
}
.ex-head__lead {
  max-width: 65ch;
  margin: 0;
  color: var(--gh-text-muted);
}

/* toolbar */
.ex-tools {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.ex-tools__row {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
@media (min-width: 1024px) {
  .ex-tools__row { flex-direction: row; align-items: center; }
  .ex-tools__search { flex: 1 1 auto; }
  .ex-tools__select { flex: 0 0 11rem; }
  .ex-tools__select--wide { flex-basis: 14rem; }
}
.ex-tools__row--split {
  flex-direction: row;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 8px 16px;
}
.ex-tools__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px 12px;
}
.ex-tools__count {
  margin: 0;
  font-size: 14px;
  color: var(--gh-text-muted);
  font-variant-numeric: tabular-nums;
}

/* events */
.ex-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.ex-list--cards {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 16px;
}
@media (min-width: 640px) {
  .ex-list--cards { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
@media (min-width: 1280px) {
  .ex-list--cards { grid-template-columns: repeat(3, minmax(0, 1fr)); }
}
.ex-list > li { min-width: 0; }
.ex-skeleton {
  display: block;
  height: 120px;
  border-radius: var(--radius-gh-lg);
}
.ex-list--cards .ex-skeleton { height: 260px; }

.ex-event {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 16px;
  height: 100%;
  padding: 20px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
/* list rows: details | when & where | actions */
@media (min-width: 1024px) {
  .ex-list:not(.ex-list--cards) .ex-event {
    grid-template-columns: minmax(0, 2.2fr) minmax(0, 1.3fr) auto;
    align-items: center;
    gap: 24px;
  }
  .ex-list:not(.ex-list--cards) .ex-event__actions { justify-content: flex-end; }
  .ex-list:not(.ex-list--cards) .ex-event__desc { -webkit-line-clamp: 1; line-clamp: 1; }
}
/* cards: actions pinned to the bottom */
.ex-list--cards .ex-event { grid-template-rows: 1fr auto auto; }
/* registration left unfinished: a yellow edge so it stands out in the list */
.ex-event--attention {
  box-shadow: inset 6px 0 0 var(--gh-accent);
}

.ex-event__main {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 0;
}
.ex-event__kind {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  margin: 0;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.ex-event__kind :deep(svg),
.ex-event__kind > * { width: 16px; height: 16px; flex: none; }
.ex-event__name {
  margin: 0;
  font-family: var(--font-display);
  font-size: 20px;
  font-weight: 700;
  line-height: 1.2;
  overflow-wrap: anywhere;
}
.ex-event__desc {
  display: -webkit-box;
  margin: 0;
  overflow: hidden;
  font-size: 14px;
  color: var(--gh-text-muted);
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 3;
  line-clamp: 3;
}
.ex-event__badges {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}
.ex-event__meta {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin: 0;
  font-size: 14px;
}
.ex-event__meta dd {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  margin: 0;
  overflow-wrap: anywhere;
}
.ex-event__meta dd > :first-child {
  flex: none;
  width: 18px;
  height: 18px;
  margin-top: 1px;
  color: var(--gh-text-muted);
}
.ex-event__actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}
</style>
