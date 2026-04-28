<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListResponse,
  GeeksHackingPortalApiEndpointsParticipantsHackathonStatusParticipantStatus,
  GeeksHackingPortalApiEndpointsParticipantsHackathonStatusResponse,
} from '@geekshacking/portal-sdk'
import { useQueries, useQueryClient } from '@tanstack/vue-query'
import {
  geeksHackingPortalApiEndpointsOrganizersHackathonListEndpointQueryKey,
  geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsAnalyticsEndpointQueryOptions,
  geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpointQueryKey,
  geeksHackingPortalApiEndpointsParticipantsHackathonListEndpointQueryKey,
  geeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListEndpointQueryOptions,
  geeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpointQueryKey,
  geeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpointQueryOptions,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonCreateEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonUpdateEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsCreateEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsUpdateEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonJoinEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonListEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { computed, ref, unref } from 'vue'
import {
  formatHackathonDate,
  formatHackathonDateTimeInput,
  HACKATHON_TIME_ZONE_LABEL,
  serializeHackathonDateTimeInput,
} from '~/utils/hackathon-date-time'

const toast = useToast()
const queryClient = useQueryClient()
const joinMutation = useGeeksHackingPortalApiEndpointsParticipantsHackathonJoinEndpoint()
const createMutation = useGeeksHackingPortalApiEndpointsOrganizersHackathonCreateEndpoint()
const updateMutation = useGeeksHackingPortalApiEndpointsOrganizersHackathonUpdateEndpoint()
const createStandaloneEventMutation = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsCreateEndpoint()
const updateStandaloneEventMutation = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsUpdateEndpoint()

type StandaloneEvent = {
  id?: string
  title?: string
  description?: string
  startTime?: string
  endTime?: string
  location?: string
  homepageUri?: string
  shortCode?: string
  maxParticipants?: number
  isPublished?: boolean
  createdAt?: string
  emailTemplates?: Record<string, string>
}

type StandaloneEventAnalytics = {
  registeredCount?: number
  withdrawnCount?: number
  capacityRemaining?: number
  capacityUsedPercent?: number
  checkInCount?: number
  currentlyCheckedInCount?: number
  resourceCount?: number
  resourceRedemptionCount?: number
  emailTemplateCount?: number
}

// Admin hackathon CRUD
const isHackathonModalOpen = ref(false)
const isEditingHackathon = ref(false)
const editingHackathonId = ref<string | null>(null)

const hackathonForm = ref({
  name: '',
  shortCode: '',
  description: '',
  venue: '',
  homepageUri: '',
  eventStartDate: '',
  eventEndDate: '',
  submissionsStartDate: '',
  challengeSelectionEndDate: '',
  submissionsEndDate: '',
  judgingStartDate: '',
  judgingEndDate: '',
  isPublished: false,
})

function resetHackathonForm() {
  hackathonForm.value = {
    name: '',
    shortCode: '',
    description: '',
    venue: '',
    homepageUri: '',
    eventStartDate: '',
    eventEndDate: '',
    submissionsStartDate: '',
    challengeSelectionEndDate: '',
    submissionsEndDate: '',
    judgingStartDate: '',
    judgingEndDate: '',
    isPublished: false,
  }
  isEditingHackathon.value = false
  editingHackathonId.value = null
}

function openCreateHackathonModal() {
  resetHackathonForm()
  isHackathonModalOpen.value = true
}

function openEditHackathonModal(hackathon: typeof hackathons.value[number]) {
  hackathonForm.value = {
    name: hackathon.name ?? '',
    shortCode: hackathon.shortCode ?? '',
    description: hackathon.description ?? '',
    venue: hackathon.venue ?? '',
    homepageUri: hackathon.homepageUri ?? '',
    eventStartDate: formatHackathonDateTimeInput(hackathon.eventStartDate),
    eventEndDate: formatHackathonDateTimeInput(hackathon.eventEndDate),
    submissionsStartDate: formatHackathonDateTimeInput(hackathon.submissionsStartDate),
    challengeSelectionEndDate: formatHackathonDateTimeInput(hackathon.challengeSelectionEndDate),
    submissionsEndDate: formatHackathonDateTimeInput(hackathon.submissionsEndDate),
    judgingStartDate: formatHackathonDateTimeInput(hackathon.judgingStartDate),
    judgingEndDate: formatHackathonDateTimeInput(hackathon.judgingEndDate),
    isPublished: hackathon.isPublished ?? false,
  }
  isEditingHackathon.value = true
  editingHackathonId.value = hackathon.id ?? null
  isHackathonModalOpen.value = true
}

async function handleHackathonSubmit() {
  const formData = {
    name: hackathonForm.value.name || undefined,
    shortCode: hackathonForm.value.shortCode || undefined,
    description: hackathonForm.value.description || undefined,
    venue: hackathonForm.value.venue || undefined,
    homepageUri: hackathonForm.value.homepageUri,
    eventStartDate: serializeHackathonDateTimeInput(hackathonForm.value.eventStartDate),
    eventEndDate: serializeHackathonDateTimeInput(hackathonForm.value.eventEndDate),
    submissionsStartDate: serializeHackathonDateTimeInput(hackathonForm.value.submissionsStartDate),
    challengeSelectionEndDate: serializeHackathonDateTimeInput(hackathonForm.value.challengeSelectionEndDate),
    submissionsEndDate: serializeHackathonDateTimeInput(hackathonForm.value.submissionsEndDate),
    judgingStartDate: serializeHackathonDateTimeInput(hackathonForm.value.judgingStartDate),
    judgingEndDate: serializeHackathonDateTimeInput(hackathonForm.value.judgingEndDate),
    isPublished: hackathonForm.value.isPublished,
  }

  try {
    if (isEditingHackathon.value && editingHackathonId.value) {
      await updateMutation.mutateAsync({
        hackathonId: editingHackathonId.value,
        data: formData,
      })
      toast.add({ title: 'Hackathon updated', color: 'success' })
    }
    else {
      await createMutation.mutateAsync({ data: formData })
      toast.add({ title: 'Hackathon created', color: 'success' })
    }
    await queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsParticipantsHackathonListEndpointQueryKey() })
    await queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsOrganizersHackathonListEndpointQueryKey() })
    isHackathonModalOpen.value = false
    resetHackathonForm()
  }
  catch (error) {
    console.error('Failed to save hackathon', error)
    toast.add({
      title: 'Failed to save hackathon',
      description: 'Please try again.',
      color: 'error',
    })
  }
}

const isHackathonSubmitting = computed(() => createMutation.isPending.value || updateMutation.isPending.value)

const isStandaloneEventModalOpen = ref(false)
const isEditingStandaloneEvent = ref(false)
const editingStandaloneEventId = ref<string | null>(null)

const standaloneEventForm = ref({
  title: '',
  description: '',
  startTime: '',
  endTime: '',
  location: '',
  homepageUri: '',
  shortCode: '',
  maxParticipants: 0,
  isPublished: false,
  emailTemplates: '{}',
})

function resetStandaloneEventForm() {
  standaloneEventForm.value = {
    title: '',
    description: '',
    startTime: '',
    endTime: '',
    location: '',
    homepageUri: '',
    shortCode: '',
    maxParticipants: 0,
    isPublished: false,
    emailTemplates: '{}',
  }
  isEditingStandaloneEvent.value = false
  editingStandaloneEventId.value = null
}

function openCreateStandaloneEventModal() {
  resetStandaloneEventForm()
  isStandaloneEventModalOpen.value = true
}

function formatEmailTemplatesInput(templates: Record<string, string> | undefined) {
  return JSON.stringify(templates ?? {}, null, 2)
}

function parseEmailTemplatesInput() {
  try {
    const value = JSON.parse(standaloneEventForm.value.emailTemplates || '{}')
    if (value === null || Array.isArray(value) || typeof value !== 'object')
      throw new Error('Expected an object')

    return Object.fromEntries(
      Object.entries(value)
        .filter((entry): entry is [string, string] => typeof entry[1] === 'string')
        .map(([key, value]) => [key, value.trim()]),
    )
  }
  catch {
    toast.add({
      title: 'Invalid email templates',
      description: 'Use a JSON object like { "registration-confirmed": "template-id" }.',
      color: 'error',
    })
    return null
  }
}

function openEditStandaloneEventModal(event: typeof standaloneEvents.value[number]) {
  standaloneEventForm.value = {
    title: event.title ?? '',
    description: event.description ?? '',
    startTime: formatHackathonDateTimeInput(event.startTime),
    endTime: formatHackathonDateTimeInput(event.endTime),
    location: event.location ?? '',
    homepageUri: event.homepageUri ?? '',
    shortCode: event.shortCode ?? '',
    maxParticipants: event.maxParticipants ?? 0,
    isPublished: event.isPublished ?? false,
    emailTemplates: formatEmailTemplatesInput(event.emailTemplates),
  }
  isEditingStandaloneEvent.value = true
  editingStandaloneEventId.value = event.id ?? null
  isStandaloneEventModalOpen.value = true
}

async function handleStandaloneEventSubmit() {
  const emailTemplates = parseEmailTemplatesInput()
  if (emailTemplates === null)
    return

  const formData = {
    title: standaloneEventForm.value.title || undefined,
    description: standaloneEventForm.value.description || undefined,
    startTime: serializeHackathonDateTimeInput(standaloneEventForm.value.startTime),
    endTime: serializeHackathonDateTimeInput(standaloneEventForm.value.endTime),
    location: standaloneEventForm.value.location || undefined,
    homepageUri: standaloneEventForm.value.homepageUri || undefined,
    shortCode: standaloneEventForm.value.shortCode || undefined,
    maxParticipants: Number(standaloneEventForm.value.maxParticipants) || undefined,
    isPublished: standaloneEventForm.value.isPublished,
    emailTemplates,
  }

  try {
    if (isEditingStandaloneEvent.value && editingStandaloneEventId.value) {
      await updateStandaloneEventMutation.mutateAsync({
        standaloneWorkshopId: editingStandaloneEventId.value,
        data: formData,
      })
      toast.add({ title: 'Standalone event updated', color: 'success' })
    }
    else {
      await createStandaloneEventMutation.mutateAsync({ data: formData })
      toast.add({ title: 'Standalone event created', color: 'success' })
    }

    await queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpointQueryKey() })
    isStandaloneEventModalOpen.value = false
    resetStandaloneEventForm()
  }
  catch (error) {
    console.error('Failed to save standalone event', error)
    toast.add({
      title: 'Failed to save standalone event',
      description: 'Please try again.',
      color: 'error',
    })
  }
}

const isStandaloneEventSubmitting = computed(() => createStandaloneEventMutation.isPending.value || updateStandaloneEventMutation.isPending.value)

const { data: user, isLoading: isLoadingUser } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()

// Fetch participant hackathons (published ones, for everyone)
const { data: participantHackathonsData, isLoading: isLoadingParticipantHackathons } = useGeeksHackingPortalApiEndpointsParticipantsHackathonListEndpoint()

// Fetch organizer hackathons (ones user can manage, only for authenticated users)
const { data: organizerHackathonsData, isLoading: isLoadingOrganizerHackathons } = useGeeksHackingPortalApiEndpointsOrganizersHackathonListEndpoint({
  query: {
    enabled: computed(() => !!user.value?.id),
  },
})

const { data: standaloneEventsData, isLoading: isLoadingStandaloneEvents } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint({
  query: {
    enabled: computed(() => !!user.value?.id),
  },
})

const isLoadingHackathons = computed(() =>
  isLoadingUser.value || isLoadingParticipantHackathons.value || (!!user.value?.id && isLoadingOrganizerHackathons.value),
)

// Merge and dedupe hackathons from both endpoints
const hackathons = computed(() => {
  const participantList = participantHackathonsData.value?.hackathons ?? []
  const organizerList = organizerHackathonsData.value?.hackathons ?? []

  // Create a map to dedupe by ID
  const hackathonMap = new Map<string, typeof participantList[number]>()

  // Add organizer hackathons first (they may include unpublished ones)
  for (const h of organizerList) {
    if (h.id)
      hackathonMap.set(h.id, h)
  }

  // Add participant hackathons (won't overwrite if already present)
  for (const h of participantList) {
    if (h.id && !hackathonMap.has(h.id))
      hackathonMap.set(h.id, h)
  }

  return Array.from(hackathonMap.values())
})

const standaloneEvents = computed(() => standaloneEventsData.value?.standaloneWorkshops ?? [])

const standaloneEventAnalyticsQueries = useQueries({
  queries: computed(() => standaloneEvents.value.map(event => ({
    ...geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsAnalyticsEndpointQueryOptions(event.id ?? ''),
    enabled: !!event.id,
  }))),
})

function standaloneEventAnalyticsForIndex(index: number): StandaloneEventAnalytics | undefined {
  return unref(standaloneEventAnalyticsQueries.value[index]?.data) as StandaloneEventAnalytics | undefined
}

// Fetch participation status per hackathon
const statusQueries = useQueries({
  queries: computed(() =>
    hackathons.value.map(hackathon => geeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpointQueryOptions(hackathon.id ?? '')),
  ),
})

// Fetch registration submissions to check completion status
const submissionQueries = useQueries({
  queries: computed(() =>
    hackathons.value.map((hackathon) => {
      const status = statusQueries.value[hackathons.value.indexOf(hackathon)]?.data as GeeksHackingPortalApiEndpointsParticipantsHackathonStatusResponse | undefined
      const isParticipant = status?.isParticipant === true
      return {
        ...geeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListEndpointQueryOptions(hackathon.id ?? ''),
        enabled: !!hackathon.id && isParticipant,
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

function isRegistrationComplete(index: number): boolean {
  const submissions = submissionsDataForIndex(index)
  return submissions?.requiredQuestionsRemaining === 0
}

async function joinHackathon(hackathon: { id: string, shortCode: string }) {
  try {
    await joinMutation.mutateAsync({ hackathonId: hackathon.id })
    await queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpointQueryKey(hackathon.id) })
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

function formatParticipantStatus(status: GeeksHackingPortalApiEndpointsParticipantsHackathonStatusParticipantStatus | null | undefined, isParticipant?: boolean | null) {
  if (!isParticipant)
    return { label: 'Not joined', color: 'neutral' as const }
  switch (status) {
    case 'Accepted':
      return { label: 'Accepted', color: 'success' as const }
    case 'Rejected':
      return { label: 'Rejected', color: 'error' as const }
    default:
      return { label: 'Pending review', color: 'warning' as const }
  }
}
</script>

<template>
  <div>
    <UDashboardPanel id="dashboard">
      <template #header>
        <UDashboardNavbar title="Dashboard">
          <template #leading>
            <UDashboardSidebarCollapse />
          </template>
        </UDashboardNavbar>
      </template>

      <template #body>
        <div class="space-y-3">
          <div class="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3">
            <div class="flex flex-col gap-1">
              <h2 class="text-lg font-semibold">
                Hackathons
              </h2>
              <p class="text-sm text-(--ui-text-muted)">
                Join a hackathon, complete your registration, and track your application status.
              </p>
            </div>
            <UButton
              v-if="user?.isRoot"
              icon="i-lucide-plus"
              size="sm"
              class="self-start"
              @click="openCreateHackathonModal"
            >
              Add Hackathon
            </UButton>
          </div>

          <div
            v-if="isLoadingHackathons"
            class="text-(--ui-text-muted)"
          >
            Loading hackathons...
          </div>

          <div
            v-else-if="!hackathons.length"
            class="text-(--ui-text-muted)"
          >
            No hackathons available.
          </div>

          <div
            v-else
            class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
          >
            <UCard
              v-for="(hackathon, index) in hackathons"
              :key="hackathon.id ?? index"
            >
              <template #header>
                <div class="flex items-center justify-between gap-2">
                  <h3 class="text-base font-semibold truncate">
                    {{ hackathon.name }}
                  </h3>
                  <div class="flex items-center gap-2 shrink-0">
                    <UButton
                      v-if="user?.isRoot"
                      size="xs"
                      variant="ghost"
                      icon="i-lucide-pencil"
                      @click.stop="openEditHackathonModal(hackathon)"
                    />
                    <UBadge
                      v-if="user?.isRoot"
                      color="warning"
                      variant="subtle"
                      size="sm"
                    >
                      Admin
                    </UBadge>
                    <UBadge
                      v-else-if="statusDataForIndex(index)?.isOrganizer"
                      color="info"
                      variant="subtle"
                      size="sm"
                    >
                      Organizer
                    </UBadge>
                    <UBadge
                      v-else-if="statusDataForIndex(index)"
                      :color="formatParticipantStatus(statusDataForIndex(index)?.status ?? null, statusDataForIndex(index)?.isParticipant).color"
                      variant="subtle"
                      size="sm"
                    >
                      {{ formatParticipantStatus(statusDataForIndex(index)?.status ?? null, statusDataForIndex(index)?.isParticipant).label }}
                    </UBadge>
                  </div>
                </div>
              </template>

              <p class="text-sm text-(--ui-text-muted) min-h-14">
                {{ hackathon.description }}
              </p>

              <div class="mt-3 flex flex-col gap-2">
                <div class="flex flex-wrap items-center gap-2 text-xs text-(--ui-text-muted)">
                  <span>
                    Starts: {{ formatHackathonDate(hackathon.eventStartDate) }}
                  </span>
                  <span>•</span>
                  <span>
                    Ends: {{ formatHackathonDate(hackathon.eventEndDate) }}
                  </span>
                </div>

                <div
                  v-if="statusDataForIndex(index)?.status === 'Rejected' && statusDataForIndex(index)?.reviewReason"
                  class="text-xs text-red-500 dark:text-red-400"
                >
                  Reason: {{ statusDataForIndex(index)?.reviewReason }}
                </div>

                <div class="flex flex-wrap items-center gap-2">
                  <!-- Root user: View + Manage -->
                  <template v-if="user?.isRoot">
                    <UButton
                      :to="`/dash/${hackathon.id}`"
                      color="neutral"
                      size="sm"
                      class="w-full sm:w-auto"
                    >
                      Manage
                    </UButton>
                  </template>

                  <!-- Organizer: Manage + Portal -->
                  <template v-else-if="statusDataForIndex(index)?.isOrganizer">
                    <UButton
                      :to="`/dash/${hackathon.id}`"
                      color="neutral"
                      size="sm"
                      class="w-full sm:w-auto"
                    >
                      Manage
                    </UButton>
                    <UButton
                      :to="`/${hackathon.shortCode}/team`"
                      color="neutral"
                      variant="outline"
                      size="sm"
                      class="w-full sm:w-auto"
                    >
                      Go to hackathon portal
                    </UButton>
                  </template>

                  <!-- Not joined: Join event -->
                  <template v-else-if="!statusDataForIndex(index)?.isParticipant">
                    <UButton
                      color="neutral"
                      size="sm"
                      class="w-full sm:w-auto"
                      :loading="joinMutation.isPending.value"
                      @click="joinHackathon({ id: hackathon.id!, shortCode: hackathon.shortCode! })"
                    >
                      Join event
                    </UButton>
                  </template>

                  <!-- Joined but registration incomplete: Continue registration -->
                  <template v-else-if="!isRegistrationComplete(index)">
                    <UButton
                      :to="`/${hackathon.shortCode}/registration`"
                      color="neutral"
                      size="sm"
                      class="w-full sm:w-auto"
                    >
                      Continue registration
                    </UButton>
                  </template>

                  <!-- Registration complete but not approved: View registration status -->
                  <template v-else-if="statusDataForIndex(index)?.status !== 'Accepted'">
                    <UButton
                      :to="`/dash/${hackathon.id}/participant`"
                      color="neutral"
                      size="sm"
                      class="w-full sm:w-auto"
                    >
                      View registration status
                    </UButton>
                  </template>

                  <!-- Approved participant: Portal -->
                  <template v-else>
                    <UButton
                      :to="`/${hackathon.shortCode}/team`"
                      color="neutral"
                      size="sm"
                      class="w-full sm:w-auto"
                    >
                      Go to hackathon portal
                    </UButton>
                  </template>
                </div>
              </div>
            </UCard>
          </div>

          <div class="pt-8 space-y-3">
            <div class="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3">
              <div class="flex flex-col gap-1">
                <h2 class="text-lg font-semibold">
                  Standalone Events
                </h2>
                <p class="text-sm text-(--ui-text-muted)">
                  Manage non-hackathon events, capacity, custom links, notification templates, and live analytics.
                </p>
              </div>
              <UButton
                v-if="user?.isRoot"
                icon="i-lucide-plus"
                size="sm"
                class="self-start"
                @click="openCreateStandaloneEventModal"
              >
                Add Standalone Event
              </UButton>
            </div>

            <div
              v-if="isLoadingUser || (!!user?.id && isLoadingStandaloneEvents)"
              class="text-(--ui-text-muted)"
            >
              Loading standalone events...
            </div>

            <div
              v-else-if="!user?.id"
              class="text-(--ui-text-muted)"
            >
              Sign in to view standalone events you organize.
            </div>

            <div
              v-else-if="!standaloneEvents.length"
              class="text-(--ui-text-muted)"
            >
              No standalone events available.
            </div>

            <div
              v-else
              class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
            >
              <UCard
                v-for="(event, index) in standaloneEvents"
                :key="event.id ?? index"
              >
                <template #header>
                  <div class="flex items-center justify-between gap-2">
                    <h3 class="text-base font-semibold truncate">
                      {{ event.title }}
                    </h3>
                    <div class="flex items-center gap-2 shrink-0">
                      <UButton
                        v-if="user?.isRoot"
                        size="xs"
                        variant="ghost"
                        icon="i-lucide-pencil"
                        @click.stop="openEditStandaloneEventModal(event)"
                      />
                      <UBadge
                        :color="event.isPublished ? 'success' : 'neutral'"
                        variant="subtle"
                        size="sm"
                      >
                        {{ event.isPublished ? 'Published' : 'Draft' }}
                      </UBadge>
                    </div>
                  </div>
                </template>

                <p class="text-sm text-(--ui-text-muted) min-h-14">
                  {{ event.description }}
                </p>

                <div class="mt-3 flex flex-col gap-3">
                  <div class="flex flex-wrap items-center gap-2 text-xs text-(--ui-text-muted)">
                    <span>Starts: {{ formatHackathonDate(event.startTime) }}</span>
                    <span>•</span>
                    <span>Ends: {{ formatHackathonDate(event.endTime) }}</span>
                  </div>

                  <div class="grid grid-cols-2 gap-2 text-xs">
                    <div class="rounded-lg border border-default p-2">
                      <div class="text-(--ui-text-muted)">
                        Registered
                      </div>
                      <div class="font-semibold">
                        {{ standaloneEventAnalyticsForIndex(index)?.registeredCount ?? '...' }} / {{ event.maxParticipants ?? 0 }}
                      </div>
                    </div>
                    <div class="rounded-lg border border-default p-2">
                      <div class="text-(--ui-text-muted)">
                        Capacity Used
                      </div>
                      <div class="font-semibold">
                        {{ standaloneEventAnalyticsForIndex(index)?.capacityUsedPercent ?? 0 }}%
                      </div>
                    </div>
                    <div class="rounded-lg border border-default p-2">
                      <div class="text-(--ui-text-muted)">
                        Checked In
                      </div>
                      <div class="font-semibold">
                        {{ standaloneEventAnalyticsForIndex(index)?.currentlyCheckedInCount ?? 0 }} current
                      </div>
                    </div>
                    <div class="rounded-lg border border-default p-2">
                      <div class="text-(--ui-text-muted)">
                        Resources
                      </div>
                      <div class="font-semibold">
                        {{ standaloneEventAnalyticsForIndex(index)?.resourceRedemptionCount ?? 0 }} redemptions
                      </div>
                    </div>
                  </div>

                  <div class="flex flex-wrap items-center gap-2 text-xs text-(--ui-text-muted)">
                    <span>{{ standaloneEventAnalyticsForIndex(index)?.withdrawnCount ?? 0 }} withdrawn</span>
                    <span>•</span>
                    <span>{{ standaloneEventAnalyticsForIndex(index)?.emailTemplateCount ?? Object.keys(event.emailTemplates ?? {}).length }} email templates</span>
                    <span>•</span>
                    <span>{{ event.location }}</span>
                  </div>

                  <div class="flex flex-wrap items-center gap-2">
                    <UButton
                      v-if="user?.isRoot"
                      color="neutral"
                      size="sm"
                      class="w-full sm:w-auto"
                      @click="openEditStandaloneEventModal(event)"
                    >
                      Manage
                    </UButton>
                    <UButton
                      v-if="event.homepageUri"
                      :to="event.homepageUri"
                      target="_blank"
                      color="neutral"
                      variant="outline"
                      size="sm"
                      class="w-full sm:w-auto"
                    >
                      Homepage
                    </UButton>
                  </div>
                </div>
              </UCard>
            </div>
          </div>
        </div>
      </template>
    </UDashboardPanel>

    <!-- Hackathon Create/Edit Modal (Admin only) -->
    <UModal
      v-if="user?.isRoot"
      v-model:open="isHackathonModalOpen"
    >
      <template #content>
        <UCard>
          <template #header>
            <div class="flex items-center justify-between">
              <h3 class="text-base font-semibold">
                {{ isEditingHackathon ? 'Edit Hackathon' : 'Create Hackathon' }}
              </h3>
              <UButton
                variant="ghost"
                icon="i-lucide-x"
                size="xs"
                @click="isHackathonModalOpen = false"
              />
            </div>
          </template>

          <form
            class="space-y-4 max-h-[60vh] sm:max-h-[70vh] overflow-y-auto pr-1"
            @submit.prevent="handleHackathonSubmit"
          >
            <UFormField
              label="Name"
              required
            >
              <UInput
                v-model="hackathonForm.name"
                placeholder="Hackathon name"
              />
            </UFormField>

            <UFormField label="Short Code">
              <UInput
                v-model="hackathonForm.shortCode"
                placeholder="e.g., hackathon-2025"
              />
            </UFormField>

            <UFormField label="Description">
              <UTextarea
                v-model="hackathonForm.description"
                placeholder="Hackathon description"
                :rows="3"
              />
            </UFormField>

            <UFormField label="Venue">
              <UInput
                v-model="hackathonForm.venue"
                placeholder="Event venue"
              />
            </UFormField>

            <UFormField label="Homepage URL">
              <UInput
                v-model="hackathonForm.homepageUri"
                placeholder="https://..."
              />
            </UFormField>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4">
              <p class="text-xs text-(--ui-text-muted) sm:col-span-2">
                Schedule fields use {{ HACKATHON_TIME_ZONE_LABEL }} (UTC+8).
              </p>
              <UFormField label="Event Start">
                <UInput
                  v-model="hackathonForm.eventStartDate"
                  type="datetime-local"
                />
              </UFormField>

              <UFormField label="Event End">
                <UInput
                  v-model="hackathonForm.eventEndDate"
                  type="datetime-local"
                />
              </UFormField>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4">
              <UFormField label="Submissions Start">
                <UInput
                  v-model="hackathonForm.submissionsStartDate"
                  type="datetime-local"
                />
              </UFormField>

              <UFormField label="Challenge Selection Deadline">
                <UInput
                  v-model="hackathonForm.challengeSelectionEndDate"
                  type="datetime-local"
                />
              </UFormField>

              <UFormField label="Project Submissions End">
                <UInput
                  v-model="hackathonForm.submissionsEndDate"
                  type="datetime-local"
                />
              </UFormField>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4">
              <UFormField label="Judging Start">
                <UInput
                  v-model="hackathonForm.judgingStartDate"
                  type="datetime-local"
                />
              </UFormField>

              <UFormField label="Judging End">
                <UInput
                  v-model="hackathonForm.judgingEndDate"
                  type="datetime-local"
                />
              </UFormField>
            </div>

            <UCheckbox
              v-model="hackathonForm.isPublished"
              label="Published"
            />

            <div class="flex flex-col-reverse sm:flex-row sm:justify-end gap-2 pt-4">
              <UButton
                variant="ghost"
                class="w-full sm:w-auto"
                @click="isHackathonModalOpen = false"
              >
                Cancel
              </UButton>
              <UButton
                type="submit"
                class="w-full sm:w-auto"
                :loading="isHackathonSubmitting"
              >
                {{ isEditingHackathon ? 'Update' : 'Create' }}
              </UButton>
            </div>
          </form>
        </UCard>
      </template>
    </UModal>

    <UModal
      v-if="user?.isRoot"
      v-model:open="isStandaloneEventModalOpen"
    >
      <template #content>
        <UCard>
          <template #header>
            <div class="flex items-center justify-between">
              <h3 class="text-base font-semibold">
                {{ isEditingStandaloneEvent ? 'Edit Standalone Event' : 'Create Standalone Event' }}
              </h3>
              <UButton
                variant="ghost"
                icon="i-lucide-x"
                size="xs"
                @click="isStandaloneEventModalOpen = false"
              />
            </div>
          </template>

          <form
            class="space-y-4 max-h-[60vh] sm:max-h-[70vh] overflow-y-auto pr-1"
            @submit.prevent="handleStandaloneEventSubmit"
          >
            <UFormField
              label="Title"
              required
            >
              <UInput
                v-model="standaloneEventForm.title"
                placeholder="Event title"
              />
            </UFormField>

            <UFormField label="Short Code">
              <UInput
                v-model="standaloneEventForm.shortCode"
                placeholder="e.g., workshop-2026"
              />
            </UFormField>

            <UFormField label="Description">
              <UTextarea
                v-model="standaloneEventForm.description"
                placeholder="Standalone event description"
                :rows="3"
              />
            </UFormField>

            <UFormField label="Location">
              <UInput
                v-model="standaloneEventForm.location"
                placeholder="Event location"
              />
            </UFormField>

            <UFormField label="Homepage URL">
              <UInput
                v-model="standaloneEventForm.homepageUri"
                placeholder="https://..."
              />
            </UFormField>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4">
              <p class="text-xs text-(--ui-text-muted) sm:col-span-2">
                Schedule fields use {{ HACKATHON_TIME_ZONE_LABEL }} (UTC+8).
              </p>
              <UFormField label="Event Start">
                <UInput
                  v-model="standaloneEventForm.startTime"
                  type="datetime-local"
                />
              </UFormField>

              <UFormField label="Event End">
                <UInput
                  v-model="standaloneEventForm.endTime"
                  type="datetime-local"
                />
              </UFormField>
            </div>

            <UFormField label="Max Participants">
              <UInput
                v-model.number="standaloneEventForm.maxParticipants"
                type="number"
                min="0"
              />
            </UFormField>

            <UFormField
              label="Email Templates"
              help="JSON object keyed by event name, e.g. registration-confirmed."
            >
              <UTextarea
                v-model="standaloneEventForm.emailTemplates"
                :rows="5"
                class="font-mono text-xs"
              />
            </UFormField>

            <UCheckbox
              v-model="standaloneEventForm.isPublished"
              label="Published"
            />

            <div class="flex flex-col-reverse sm:flex-row sm:justify-end gap-2 pt-4">
              <UButton
                variant="ghost"
                class="w-full sm:w-auto"
                @click="isStandaloneEventModalOpen = false"
              >
                Cancel
              </UButton>
              <UButton
                type="submit"
                class="w-full sm:w-auto"
                :loading="isStandaloneEventSubmitting"
              >
                {{ isEditingStandaloneEvent ? 'Update' : 'Create' }}
              </UButton>
            </div>
          </form>
        </UCard>
      </template>
    </UModal>
  </div>
</template>
