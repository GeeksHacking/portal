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

const statusBadgeClass = {
  success: 'border-transparent bg-success/10 text-success',
  error: 'border-transparent bg-error/10 text-error',
  warning: 'border-transparent bg-warning/10 text-warning',
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

const eventTypeFilters = [
  { label: 'All', value: 'all' as const, icon: 'i-lucide-layout-grid' },
  { label: 'Hackathons', value: 'hackathons' as const, icon: 'i-lucide-trophy' },
  { label: 'Workshops', value: 'workshops' as const, icon: 'i-lucide-brain-circuit' },
]

const audienceFilters = [
  { label: 'Explore', value: 'all' as const },
  { label: 'Open now', value: 'open' as const },
  { label: 'Joined', value: 'joined' as const },
]

const hackathonCards = computed(() =>
  hackathons.value.map((hackathon, index) => ({
    hackathon,
    index,
    status: statusDataForIndex(index),
    registrationComplete: isRegistrationComplete(index),
  })),
)

const visibleHackathonCards = computed(() => {
  if (activeEventType.value === 'workshops')
    return []

  const query = searchQuery.value.trim().toLowerCase()

  return hackathonCards.value.filter(({ hackathon, status }) => {
    const searchText = [
      hackathon.name,
      hackathon.description,
      hackathon.venue,
      hackathon.shortCode,
    ].filter(Boolean).join(' ').toLowerCase()

    if (query && !searchText.includes(query))
      return false

    if (activeAudience.value === 'open')
      return !status?.isParticipant

    if (activeAudience.value === 'joined')
      return !!status?.isParticipant

    return true
  }).sort((a, b) => {
    const aNeedsRegistration = a.status?.isParticipant && !a.registrationComplete
    const bNeedsRegistration = b.status?.isParticipant && !b.registrationComplete

    if (aNeedsRegistration !== bNeedsRegistration)
      return aNeedsRegistration ? -1 : 1

    return new Date(a.hackathon.eventStartDate ?? 0).getTime() - new Date(b.hackathon.eventStartDate ?? 0).getTime()
  })
})

const visibleStandaloneEvents = computed(() => {
  if (activeEventType.value === 'hackathons')
    return []

  const query = searchQuery.value.trim().toLowerCase()

  return publishedStandaloneEvents.value.filter((event) => {
    const searchText = [
      event.title,
      event.description,
      event.location,
      event.shortCode,
    ].filter(Boolean).join(' ').toLowerCase()

    if (query && !searchText.includes(query))
      return false

    return activeAudience.value !== 'joined'
  }).sort((a, b) =>
    new Date(a.startTime ?? 0).getTime() - new Date(b.startTime ?? 0).getTime(),
  )
})

const discoverStats = computed(() => {
  const joinedHackathons = hackathonCards.value.filter(({ status }) => status?.isParticipant).length
  const openHackathons = hackathonCards.value.filter(({ status }) => !status?.isParticipant).length

  return [
    {
      label: 'Hackathons',
      value: hackathons.value.length,
      icon: 'i-lucide-trophy',
    },
    {
      label: 'Workshops',
      value: publishedStandaloneEvents.value.length,
      icon: 'i-lucide-brain-circuit',
    },
    {
      label: 'Open to join',
      value: openHackathons + publishedStandaloneEvents.value.length,
      icon: 'i-lucide-sparkles',
    },
    {
      label: 'Joined hackathons',
      value: joinedHackathons,
      icon: 'i-lucide-user-check',
    },
  ]
})

function eventDateRange(start?: string | null, end?: string | null) {
  return `${formatHackathonDate(start)} to ${formatHackathonDate(end)}`
}
</script>

<template>
  <UDashboardPanel id="dashboard">
    <template #header>
      <UDashboardNavbar title="Explore events">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="mx-auto flex w-full max-w-7xl flex-col gap-8">
        <section class="space-y-5 py-2">
          <div class="flex flex-wrap items-center gap-2">
            <Badge class="border-transparent bg-primary/10 text-primary">
              GeeksHacking Portal
            </Badge>
            <span class="text-xs text-muted">
              {{ user?.gitHubLogin ? `Signed in as @${user.gitHubLogin}` : 'Event discovery' }}
            </span>
          </div>

          <div class="max-w-4xl">
            <h1 class="text-3xl font-semibold tracking-tight text-default sm:text-4xl">
              Explore hackathons and workshops from GeeksHacking
            </h1>
            <p class="mt-3 text-base leading-7 text-muted">
              Find programs to join, continue registrations already in progress, and jump back into event portals once you are accepted.
            </p>
          </div>

          <div class="rounded-lg bg-elevated/45 p-3">
            <div class="grid gap-3 lg:grid-cols-[minmax(260px,1fr)_auto] lg:items-center">
              <div class="relative">
                <Icon name="i-lucide-search" class="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  v-model="searchQuery"
                  aria-label="Search events, venues, or short codes"
                  placeholder="Search events, venues, or short codes"
                  class="h-10 pl-9"
                />
              </div>

              <div class="grid gap-2 sm:grid-cols-2 lg:flex lg:items-center lg:justify-end">
                <div class="grid grid-cols-3 gap-1 rounded-md bg-elevated p-1">
                  <Button
                    v-for="filter in eventTypeFilters"
                    :key="filter.value"
                    size="sm"
                    :variant="activeEventType === filter.value ? 'secondary' : 'ghost'"
                    :class="['justify-center', activeEventType === filter.value && 'text-primary']"
                    @click="activeEventType = filter.value"
                  >
                    <Icon :name="filter.icon" />
                    {{ filter.label }}
                  </Button>
                </div>

                <div class="grid grid-cols-3 gap-1 rounded-md bg-elevated p-1">
                  <Button
                    v-for="filter in audienceFilters"
                    :key="filter.value"
                    size="sm"
                    :variant="activeAudience === filter.value ? 'secondary' : 'ghost'"
                    :class="['justify-center', activeAudience === filter.value && 'text-primary']"
                    @click="activeAudience = filter.value"
                  >
                    {{ filter.label }}
                  </Button>
                </div>
              </div>
            </div>
          </div>

          <div class="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <div
              v-for="stat in discoverStats"
              :key="stat.label"
              class="rounded-md bg-elevated/60 p-4"
            >
              <div class="flex items-center justify-between gap-3">
                <span class="text-sm font-medium text-muted">{{ stat.label }}</span>
                <Icon :name="stat.icon" class="size-4 text-primary" />
              </div>
              <div class="mt-3 text-2xl font-semibold text-default">
                {{ stat.value }}
              </div>
            </div>
          </div>
        </section>

        <section
          v-if="activeEventType !== 'workshops'"
          class="space-y-3"
        >
          <div>
            <h2 class="text-lg font-semibold text-default">
              Hackathons
            </h2>
            <p class="text-sm text-muted">
              Competitive build events with registration, teams, challenges, and submission workflows.
            </p>
          </div>

          <div
            v-if="isLoadingUser || isLoadingHackathons"
            class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3"
          >
            <Card
              v-for="index in 3"
              :key="index"
              class="bg-muted/40 shadow-none"
            >
              <CardContent class="space-y-3">
                <Skeleton class="h-5 w-2/3" />
                <Skeleton class="h-16 w-full" />
                <Skeleton class="h-9 w-32" />
              </CardContent>
            </Card>
          </div>

          <Alert
            v-else-if="!visibleHackathonCards.length">
            <Icon name="i-lucide-search-x" />
            <AlertTitle>No matching hackathons</AlertTitle>
            <AlertDescription>Try a different search term or filter.</AlertDescription>
          </Alert>

          <div
            v-else
            class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3"
          >
            <Card
              v-for="{ hackathon, index, status, registrationComplete } in visibleHackathonCards"
              :key="hackathon.id ?? index"
              class="bg-transparent shadow-none transition hover:bg-muted/45"
            >
              <CardHeader>
                <div class="flex items-start justify-between gap-3">
                  <div class="min-w-0">
                    <div class="flex items-center gap-2">
                      <Icon name="i-lucide-trophy" class="size-4 shrink-0 text-primary" />
                      <h3 class="truncate text-base font-semibold text-default">
                        {{ hackathon.name }}
                      </h3>
                    </div>
                    <div class="mt-2 flex flex-wrap items-center gap-2">
                      <Badge
                        :class="statusBadgeClass[formatParticipantStatus(status?.status ?? null, status?.isParticipant).color]"
                      >
                        {{ formatParticipantStatus(status?.status ?? null, status?.isParticipant).label }}
                      </Badge>
                      <Badge variant="outline">
                        Hackathon
                      </Badge>
                    </div>
                  </div>
                </div>
              </CardHeader>

              <CardContent class="space-y-4">
                <p class="line-clamp-3 min-h-16 text-sm leading-6 text-muted">
                  {{ hackathon.description || 'Details are being prepared by the organizers.' }}
                </p>

                <div class="grid gap-2 text-sm text-muted">
                  <div class="flex items-center gap-2">
                    <Icon name="i-lucide-calendar-days" class="size-4" />
                    <span>{{ eventDateRange(hackathon.eventStartDate, hackathon.eventEndDate) }}</span>
                  </div>
                  <div
                    v-if="hackathon.venue"
                    class="flex items-center gap-2"
                  >
                    <Icon name="i-lucide-map-pin" class="size-4" />
                    <span class="truncate">{{ hackathon.venue }}</span>
                  </div>
                </div>

                <Alert variant="destructive"
            v-if="status?.status === 'Rejected' && status.reviewReason">
            <Icon name="i-lucide-circle-alert" />
            <AlertDescription>{{ `Reason: ${status.reviewReason}` }}</AlertDescription>
          </Alert>

                <div
                  v-if="status?.isParticipant && !registrationComplete"
                  class="rounded-lg border border-warning/30 bg-warning/10 p-3"
                >
                  <div class="flex items-center justify-between gap-3 text-sm">
                    <span class="font-medium text-default">Registration progress</span>
                    <span class="text-warning">Action needed</span>
                  </div>
                  <div class="mt-2 h-2 overflow-hidden rounded-full bg-muted">
                    <div class="h-full w-1/2 rounded-full bg-warning" />
                  </div>
                </div>

                <div class="flex flex-wrap items-center gap-2">
                  <Button
                    v-if="!status?.isParticipant"
                    size="sm"
                    :disabled="joinMutation.isPending.value"
                    :aria-busy="joinMutation.isPending.value"
                    @click="joinHackathon({ id: hackathon.id!, shortCode: hackathon.shortCode! })"
                  >
                    <Icon :name="joinMutation.isPending.value ? 'i-lucide-loader-circle' : 'i-lucide-user-plus'" :class="joinMutation.isPending.value && 'animate-spin'" />
                    {{ joinMutation.isPending.value ? 'Joining…' : 'Join event' }}
                  </Button>
                  <Button
                    v-else-if="!registrationComplete"
                    size="sm"
                    as-child
                  >
                    <NuxtLink :to="`/${hackathon.shortCode}/registration`">
                      <Icon name="i-lucide-clipboard-pen-line" />
                    Continue registration
                    </NuxtLink>
                  </Button>
                  <Button
                    v-else-if="status?.status !== 'Accepted'"
                    size="sm"
                    as-child
                  >
                    <NuxtLink :to="`/dash/${hackathon.id}/participant`">
                      <Icon name="i-lucide-file-check" />
                    View status
                    </NuxtLink>
                  </Button>
                  <Button
                    v-else
                    size="sm"
                    as-child
                  >
                    <NuxtLink :to="`/${hackathon.shortCode}/team`">
                      Open event portal <Icon name="i-lucide-arrow-right" />
                    </NuxtLink>
                  </Button>

                  <Button
                    v-if="hackathon.homepageUri"
                    variant="outline"
                    size="sm"
                    as-child
                  >
                    <NuxtLink :to="hackathon.homepageUri" target="_blank" rel="noopener">
                      <Icon name="i-lucide-external-link" />
                    Website
                    </NuxtLink>
                  </Button>
                </div>
              </CardContent>
            </Card>
          </div>
        </section>

        <section
          v-if="activeEventType !== 'hackathons'"
          class="space-y-3"
        >
          <div>
            <h2 class="text-lg font-semibold text-default">
              Workshops
            </h2>
            <p class="text-sm text-muted">
              Focused learning sessions and community workshops published on the portal.
            </p>
          </div>

          <div
            v-if="isLoadingUser || (!!user?.id && isLoadingStandaloneEvents)"
            class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3"
          >
            <Card
              v-for="index in 3"
              :key="index"
              class="bg-muted/40 shadow-none"
            >
              <CardContent class="space-y-3">
                <Skeleton class="h-5 w-2/3" />
                <Skeleton class="h-16 w-full" />
                <Skeleton class="h-9 w-32" />
              </CardContent>
            </Card>
          </div>

          <Alert
            v-else-if="!user?.id">
            <Icon name="i-lucide-log-in" />
            <AlertTitle>Sign in to explore workshops</AlertTitle>
            <AlertDescription>Workshop discovery is available after sign-in.</AlertDescription>
          </Alert>

          <Alert
            v-else-if="!visibleStandaloneEvents.length">
            <Icon name="i-lucide-search-x" />
            <AlertTitle>No matching workshops</AlertTitle>
            <AlertDescription>Try a different search term or filter.</AlertDescription>
          </Alert>

          <div
            v-else
            class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3"
          >
            <Card
              v-for="event in visibleStandaloneEvents"
              :key="event.id"
              class="bg-transparent shadow-none transition hover:bg-muted/45"
            >
              <CardHeader>
                <div class="flex items-start justify-between gap-3">
                  <div class="min-w-0">
                    <div class="flex items-center gap-2">
                      <Icon name="i-lucide-brain-circuit" class="size-4 shrink-0 text-info" />
                      <h3 class="truncate text-base font-semibold text-default">
                        {{ event.title }}
                      </h3>
                    </div>
                    <div class="mt-2 flex flex-wrap items-center gap-2">
                      <Badge :class="statusBadgeClass.success">
                        Open
                      </Badge>
                      <Badge variant="outline" class="border-info/40 text-info">
                        Workshop
                      </Badge>
                    </div>
                  </div>
                </div>
              </CardHeader>

              <CardContent class="space-y-4">
                <p class="line-clamp-3 min-h-16 text-sm leading-6 text-muted">
                  {{ event.description || 'Workshop details are being prepared.' }}
                </p>

                <div class="grid gap-2 text-sm text-muted">
                  <div class="flex items-center gap-2">
                    <Icon name="i-lucide-calendar-days" class="size-4" />
                    <span>{{ eventDateRange(event.startTime, event.endTime) }}</span>
                  </div>
                  <div
                    v-if="event.location"
                    class="flex items-center gap-2"
                  >
                    <Icon name="i-lucide-map-pin" class="size-4" />
                    <span class="truncate">{{ event.location }}</span>
                  </div>
                </div>

                <div class="flex flex-wrap items-center gap-2">
                  <Button
                    v-if="event.shortCode"
                    size="sm"
                    as-child
                  >
                    <NuxtLink :to="`/workshops/${event.shortCode}`">
                      <Icon name="i-lucide-clipboard-pen-line" />
                    Register
                    </NuxtLink>
                  </Button>
                  <Button
                    v-if="event.homepageUri"
                    variant="outline"
                    size="sm"
                    as-child
                  >
                    <NuxtLink :to="event.homepageUri" target="_blank" rel="noopener">
                      <Icon name="i-lucide-external-link" />
                    Website
                    </NuxtLink>
                  </Button>
                </div>
              </CardContent>
            </Card>
          </div>
        </section>
      </div>
    </template>
  </UDashboardPanel>
</template>
