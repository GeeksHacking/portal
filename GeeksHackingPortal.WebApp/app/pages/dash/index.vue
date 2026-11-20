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

const toast = useToast()
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
    return { label: 'Open', color: 'success' as const }

  switch (status) {
    case 'Accepted':
      return { label: 'Accepted', color: 'success' as const }
    case 'Rejected':
      return { label: 'Not accepted', color: 'error' as const }
    default:
      return { label: 'Pending review', color: 'warning' as const }
  }
}

async function joinHackathon(hackathon: { id: string, shortCode: string }) {
  try {
    await joinMutation.mutateAsync({ path: { hackathonId: hackathon.id } })
    await queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpointQueryKey({ path: { hackathonId: hackathon.id } }) })
    await queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsParticipantsHackathonListEndpointQueryKey() })
    navigateTo(`/${hackathon.shortCode}/registration`)
  }
  catch (error) {
    console.error('[DASH] Failed to join hackathon', error)
    toast.add({
      title: 'Could not join',
      description: 'Please try again in a moment.',
      color: 'error',
    })
  }
}

const searchQuery = ref('')
const activeEventType = ref<'all' | 'hackathons' | 'workshops'>('all')
const activeAudience = ref<'all' | 'open' | 'joined'>('all')
const activeSort = ref<'date-asc' | 'date-desc' | 'name' | 'attention'>('attention')
const viewMode = useLocalStorage<'list' | 'card'>('dash-events-view', 'list')

const eventTypeFilters = [
  { label: 'All', value: 'all' as const, icon: 'i-lucide-layout-grid' },
  { label: 'Hackathons', value: 'hackathons' as const, icon: 'i-lucide-trophy' },
  { label: 'Workshops', value: 'workshops' as const, icon: 'i-lucide-brain-circuit' },
]

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
      description: hackathon.description || 'Details are being prepared by the organizers.',
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
      description: event.description || 'Workshop details are being prepared.',
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

const hasActiveFilters = computed(() =>
  !!searchQuery.value.trim() || activeEventType.value !== 'all' || activeAudience.value !== 'all',
)

function resetFilters() {
  searchQuery.value = ''
  activeEventType.value = 'all'
  activeAudience.value = 'all'
}

function eventDateRange(start?: string | null, end?: string | null) {
  return `${formatHackathonDate(start)} to ${formatHackathonDate(end)}`
}
</script>

<template>
  <GPanel id="dashboard">
    <template #header>
      <GPanelHeader title="Explore" />
    </template>

    <template #body>
      <div class="mx-auto flex w-full max-w-7xl flex-col gap-6">
        <section class="py-2">
          <h1 class="text-3xl font-semibold tracking-tight text-default sm:text-4xl">
            {{ user?.gitHubLogin ? `Welcome back, ${user.gitHubLogin}` : 'Explore' }}
          </h1>
          <p class="mt-2 text-base text-muted">
            Discover hackathons and workshops, pick up where you left off, and jump into your events.
          </p>
        </section>

        <section class="space-y-3">
          <div class="flex flex-col gap-2 lg:flex-row lg:items-center">
            <UInput
              v-model="searchQuery"
              icon="i-lucide-search"
              placeholder="Search events, venues, or short codes"
              class="lg:flex-1"
            />
            <USelect
              v-model="activeAudience"
              :items="audienceFilters"
              icon="i-lucide-user"
              class="lg:w-44"
              aria-label="Filter by participation"
            />
            <USelect
              v-model="activeSort"
              :items="sortOptions"
              icon="i-lucide-arrow-down-up"
              class="lg:w-52"
              aria-label="Sort events"
            />
          </div>

          <div class="flex flex-wrap items-center justify-between gap-2">
            <div class="flex gap-1">
              <UButton
                v-for="filter in eventTypeFilters"
                :key="filter.value"
                :icon="filter.icon"
                size="sm"
                :aria-pressed="activeEventType === filter.value"
                :color="activeEventType === filter.value ? 'primary' : 'neutral'"
                :variant="activeEventType === filter.value ? 'soft' : 'ghost'"
                @click="activeEventType = filter.value"
              >
                {{ filter.label }}
              </UButton>
            </div>
            <div class="flex items-center gap-3 text-xs text-muted">
              <span>{{ visibleEvents.length }} of {{ eventRows.length }} events</span>
              <UButton
                v-if="hasActiveFilters"
                size="xs"
                variant="link"
                color="neutral"
                icon="i-lucide-x"
                @click="resetFilters"
              >
                Clear filters
              </UButton>
              <div class="flex gap-0.5">
                <UButton
                  size="xs"
                  icon="i-lucide-list"
                  aria-label="List view"
                  :aria-pressed="viewMode === 'list'"
                  :color="viewMode === 'list' ? 'primary' : 'neutral'"
                  :variant="viewMode === 'list' ? 'soft' : 'ghost'"
                  @click="viewMode = 'list'"
                />
                <UButton
                  size="xs"
                  icon="i-lucide-layout-grid"
                  aria-label="Card view"
                  :aria-pressed="viewMode === 'card'"
                  :color="viewMode === 'card' ? 'primary' : 'neutral'"
                  :variant="viewMode === 'card' ? 'soft' : 'ghost'"
                  @click="viewMode = 'card'"
                />
              </div>
            </div>
          </div>
        </section>

        <section :class="viewMode === 'card' ? 'grid gap-4 sm:grid-cols-2 xl:grid-cols-3' : 'space-y-2'">
          <div
            v-if="isLoadingUser || isLoadingHackathons || (!!user?.id && isLoadingStandaloneEvents)"
            class="col-span-full space-y-2"
          >
            <USkeleton
              v-for="index in 4"
              :key="index"
              class="h-24 w-full"
            />
          </div>

          <template v-else>
            <UAlert
              v-if="!user?.id && activeEventType !== 'hackathons'"
              color="neutral"
              variant="soft"
              icon="i-lucide-log-in"
              class="col-span-full"
              title="Sign in to explore workshops"
              description="Workshop discovery is available after sign-in."
            />

            <UAlert
              v-if="!visibleEvents.length"
              color="neutral"
              variant="soft"
              icon="i-lucide-search-x"
              class="col-span-full"
              title="No matching events"
              description="Try a different search term or clear your filters."
            />

            <div
              v-for="row in visibleEvents"
              :key="row.key"
              class="rounded-lg border border-default bg-default p-4 transition hover:border-primary/40 hover:shadow-sm"
              :class="viewMode === 'card' ? 'flex flex-col gap-4' : 'grid gap-3 lg:grid-cols-[minmax(0,2.2fr)_minmax(0,1.4fr)_19rem] lg:items-center'"
            >
              <div class="min-w-0 space-y-1.5">
                <div
                  class="flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wide"
                  :class="row.kind === 'hackathon' ? 'text-primary' : 'text-info'"
                >
                  <UIcon
                    :name="row.kind === 'hackathon' ? 'i-lucide-trophy' : 'i-lucide-brain-circuit'"
                    class="size-3.5 shrink-0"
                  />
                  <span>{{ row.kind === 'hackathon' ? 'Hackathon' : 'Workshop' }}</span>
                </div>
                <h3 class="truncate text-base font-semibold text-default">
                  {{ row.name }}
                </h3>
                <p
                  class="text-sm text-muted"
                  :class="viewMode === 'card' ? 'line-clamp-3' : 'line-clamp-1'"
                >
                  {{ row.description }}
                </p>
                <div class="flex flex-wrap items-center gap-1.5">
                  <UBadge
                    v-if="row.hackathonCard"
                    :color="formatParticipantStatus(row.hackathonCard.status?.status ?? null, row.hackathonCard.status?.isParticipant).color"
                    variant="subtle"
                    size="sm"
                  >
                    {{ formatParticipantStatus(row.hackathonCard.status?.status ?? null, row.hackathonCard.status?.isParticipant).label }}
                  </UBadge>
                  <UBadge
                    v-else
                    color="success"
                    variant="subtle"
                    size="sm"
                  >
                    Open
                  </UBadge>
                  <UBadge
                    v-if="row.needsAttention"
                    color="warning"
                    variant="soft"
                    icon="i-lucide-circle-alert"
                    size="sm"
                  >
                    Registration incomplete
                  </UBadge>
                  <UBadge
                    :color="row.isOngoing ? 'success' : 'info'"
                    variant="outline"
                    size="sm"
                  >
                    {{ row.isOngoing ? 'Ongoing' : 'Upcoming' }}
                  </UBadge>
                </div>
                <UAlert
                  v-if="row.hackathonCard?.status?.status === 'Rejected' && row.hackathonCard.status.reviewReason"
                  color="error"
                  variant="soft"
                  icon="i-lucide-circle-alert"
                  :description="`Reason: ${row.hackathonCard.status.reviewReason}`"
                />
              </div>

              <div class="grid gap-1.5 text-sm text-muted">
                <div class="flex items-center gap-2">
                  <UIcon
                    name="i-lucide-calendar-days"
                    class="size-4 shrink-0"
                  />
                  <span>{{ eventDateRange(row.start, row.end) }}</span>
                </div>
                <div
                  v-if="row.location"
                  class="flex items-center gap-2"
                >
                  <UIcon
                    name="i-lucide-map-pin"
                    class="size-4 shrink-0"
                  />
                  <span class="truncate">{{ row.location }}</span>
                </div>
              </div>

              <div
                class="flex flex-wrap items-center gap-2"
                :class="viewMode === 'card' ? 'mt-auto' : 'lg:justify-end'"
              >
                <template v-if="row.hackathonCard">
                  <UButton
                    v-if="!row.hackathonCard.status?.isParticipant"
                    icon="i-lucide-user-plus"
                    size="sm"
                    :loading="joinMutation.isPending.value"
                    @click="joinHackathon({ id: row.hackathonCard.hackathon.id!, shortCode: row.hackathonCard.hackathon.shortCode! })"
                  >
                    Join event
                  </UButton>
                  <UButton
                    v-else-if="!row.hackathonCard.registrationComplete"
                    :to="`/${row.shortCode}/registration`"
                    icon="i-lucide-clipboard-pen-line"
                    size="sm"
                  >
                    Continue registration
                  </UButton>
                  <UButton
                    v-else-if="row.hackathonCard.status?.status !== 'Accepted'"
                    :to="`/dash/${row.hackathonCard.hackathon.id}/participant`"
                    icon="i-lucide-file-check"
                    size="sm"
                  >
                    View status
                  </UButton>
                  <UButton
                    v-else
                    :to="`/${row.shortCode}/team`"
                    icon="i-lucide-arrow-right"
                    trailing
                    size="sm"
                  >
                    Open event portal
                  </UButton>
                </template>
                <UButton
                  v-else-if="row.shortCode"
                  :to="`/workshops/${row.shortCode}`"
                  icon="i-lucide-clipboard-pen-line"
                  size="sm"
                >
                  Register
                </UButton>

                <UButton
                  v-if="row.homepageUri"
                  :to="row.homepageUri"
                  target="_blank"
                  color="neutral"
                  variant="outline"
                  icon="i-lucide-external-link"
                  size="sm"
                >
                  Website
                </UButton>
              </div>
            </div>
          </template>
        </section>
      </div>
    </template>
  </GPanel>
</template>
