<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListResponse,
  GeeksHackingPortalApiEndpointsParticipantsHackathonStatusParticipantStatus,
  GeeksHackingPortalApiEndpointsParticipantsHackathonStatusResponse,
} from '@geekshacking/portal-sdk'
import {
  geeksHackingPortalApiEndpointsOrganizersHackathonListEndpointQueryKey,
  geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsAnalyticsEndpointQueryOptions,
  geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpointQueryKey,
  geeksHackingPortalApiEndpointsParticipantsHackathonListEndpointQueryKey,
  geeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListEndpointQueryOptions,
  geeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpointQueryKey,
  geeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpointQueryOptions,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesHackathonsEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonCreateEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsCreateEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsUpdateEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonJoinEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonListEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueries, useQueryClient } from '@tanstack/vue-query'
import { computed, ref, unref } from 'vue'
import { getApiErrorMessage, getApiFieldError, getApiValidationErrors, getApiValidationSummary } from '~/utils/api-errors'
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
const updateHackathonMutation = useGeeksHackingPortalApiEndpointsOrganizersActivitiesHackathonsEndpoint()
const createStandaloneEventMutation = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsCreateEndpoint()
const updateStandaloneEventMutation = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsUpdateEndpoint()

interface StandaloneEventAnalytics {
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
  emailTemplates: {} as Record<string, string>,
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
    emailTemplates: {},
  }
  isEditingHackathon.value = false
  editingHackathonId.value = null
}

function openCreateHackathonModal() {
  resetHackathonForm()
  isHackathonModalOpen.value = true
}

function getHackathonEmailTemplates(hackathon: typeof hackathons.value[number]) {
  if ('emailTemplates' in hackathon && hackathon.emailTemplates)
    return hackathon.emailTemplates as Record<string, string>

  return {}
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
    emailTemplates: getHackathonEmailTemplates(hackathon),
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
    emailTemplates: hackathonForm.value.emailTemplates,
  }

  try {
    if (isEditingHackathon.value && editingHackathonId.value) {
      await updateHackathonMutation.mutateAsync({
        hackathonId: editingHackathonId.value,
        data: {
          name: formData.name,
          description: formData.description,
          venue: formData.venue,
          homepageUri: formData.homepageUri,
          shortCode: formData.shortCode,
          eventStartDate: formData.eventStartDate,
          eventEndDate: formData.eventEndDate,
          submissionsStartDate: formData.submissionsStartDate,
          challengeSelectionEndDate: formData.challengeSelectionEndDate,
          submissionsEndDate: formData.submissionsEndDate,
          judgingStartDate: formData.judgingStartDate,
          judgingEndDate: formData.judgingEndDate,
          isPublished: formData.isPublished,
          emailTemplates: formData.emailTemplates,
        },
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

const isHackathonSubmitting = computed(() =>
  createMutation.isPending.value
  || updateHackathonMutation.isPending.value,
)

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
  emailTemplates: {} as Record<string, string>,
})
const standaloneEventFieldErrors = ref<Record<string, string>>({})

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
    emailTemplates: {},
  }
  isEditingStandaloneEvent.value = false
  editingStandaloneEventId.value = null
  standaloneEventFieldErrors.value = {}
}

function openCreateStandaloneEventModal() {
  resetStandaloneEventForm()
  isStandaloneEventModalOpen.value = true
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
    emailTemplates: event.emailTemplates ?? {},
  }
  isEditingStandaloneEvent.value = true
  editingStandaloneEventId.value = event.id ?? null
  standaloneEventFieldErrors.value = {}
  isStandaloneEventModalOpen.value = true
}

function normalizeOptionalUrl(value: string | null | undefined) {
  const trimmed = (value ?? '').trim()
  return trimmed === '' ? null : trimmed
}

function setStandaloneEventFieldError(field: string, message: string) {
  standaloneEventFieldErrors.value = {
    ...standaloneEventFieldErrors.value,
    [field]: message,
  }
}

function validateStandaloneEventForm() {
  standaloneEventFieldErrors.value = {}

  if (!standaloneEventForm.value.title.trim())
    setStandaloneEventFieldError('title', 'Title is required.')

  if (!standaloneEventForm.value.description.trim())
    setStandaloneEventFieldError('description', 'Description is required.')

  if (!standaloneEventForm.value.location.trim())
    setStandaloneEventFieldError('location', 'Location is required.')

  if (!standaloneEventForm.value.shortCode.trim()) {
    setStandaloneEventFieldError('shortCode', 'Short code is required.')
  }
  else if (!/^[A-Z0-9-]{3,16}$/i.test(standaloneEventForm.value.shortCode.trim())) {
    setStandaloneEventFieldError('shortCode', 'Use 3 to 16 letters, numbers, or hyphens.')
  }

  const homepageUri = normalizeOptionalUrl(standaloneEventForm.value.homepageUri)
  if (!isEditingStandaloneEvent.value && !homepageUri)
    setStandaloneEventFieldError('homepageUri', 'Homepage URL is required.')
  else if (homepageUri && (!URL.canParse(homepageUri) || !['http:', 'https:'].includes(new URL(homepageUri).protocol)))
    setStandaloneEventFieldError('homepageUri', 'Homepage URL must start with http:// or https://.')

  if (!standaloneEventForm.value.startTime)
    setStandaloneEventFieldError('startTime', 'Start time is required.')

  if (!standaloneEventForm.value.endTime) {
    setStandaloneEventFieldError('endTime', 'End time is required.')
  }
  else if (standaloneEventForm.value.startTime && standaloneEventForm.value.endTime <= standaloneEventForm.value.startTime) {
    setStandaloneEventFieldError('endTime', 'End time must be after start time.')
  }

  if (!Number.isFinite(Number(standaloneEventForm.value.maxParticipants)) || Number(standaloneEventForm.value.maxParticipants) <= 0)
    setStandaloneEventFieldError('maxParticipants', 'Max participants must be greater than 0.')

  return Object.keys(standaloneEventFieldErrors.value).length === 0
}

function applyStandaloneEventApiErrors(error: unknown) {
  const errorBag = getApiValidationErrors(error)
  const fieldErrors = {
    title: getApiFieldError(errorBag, 'Title'),
    description: getApiFieldError(errorBag, 'Description'),
    startTime: getApiFieldError(errorBag, 'StartTime'),
    endTime: getApiFieldError(errorBag, 'EndTime'),
    location: getApiFieldError(errorBag, 'Location'),
    homepageUri: getApiFieldError(errorBag, 'HomepageUri'),
    shortCode: getApiFieldError(errorBag, 'ShortCode'),
    maxParticipants: getApiFieldError(errorBag, 'MaxParticipants'),
    emailTemplates: getApiFieldError(errorBag, 'EmailTemplates'),
  }

  standaloneEventFieldErrors.value = Object.fromEntries(
    Object.entries(fieldErrors).filter((entry): entry is [string, string] => Boolean(entry[1])),
  )
}

async function handleStandaloneEventSubmit() {
  if (!validateStandaloneEventForm()) {
    toast.add({
      title: 'Review standalone event details',
      description: Object.values(standaloneEventFieldErrors.value)[0],
      color: 'error',
    })
    return
  }

  const formData = {
    title: standaloneEventForm.value.title.trim(),
    description: standaloneEventForm.value.description.trim(),
    startTime: serializeHackathonDateTimeInput(standaloneEventForm.value.startTime),
    endTime: serializeHackathonDateTimeInput(standaloneEventForm.value.endTime),
    location: standaloneEventForm.value.location.trim(),
    homepageUri: normalizeOptionalUrl(standaloneEventForm.value.homepageUri),
    shortCode: standaloneEventForm.value.shortCode.trim(),
    maxParticipants: Number(standaloneEventForm.value.maxParticipants),
    isPublished: standaloneEventForm.value.isPublished,
    emailTemplates: standaloneEventForm.value.emailTemplates,
  }

  try {
    if (isEditingStandaloneEvent.value && editingStandaloneEventId.value) {
      await updateStandaloneEventMutation.mutateAsync({
        standaloneWorkshopId: editingStandaloneEventId.value,
        data: {
          title: formData.title,
          description: formData.description,
          startTime: formData.startTime,
          endTime: formData.endTime,
          location: formData.location,
          isPublished: formData.isPublished,
          emailTemplates: formData.emailTemplates,
          // Keep the key present when clearing; null means "remove homepage URL".
          homepageUri: normalizeOptionalUrl(standaloneEventForm.value.homepageUri),
          shortCode: formData.shortCode,
          maxParticipants: formData.maxParticipants,
        },
      })
      toast.add({ title: 'Standalone event updated', color: 'success' })
    }
    else {
      await createStandaloneEventMutation.mutateAsync({
        data: {
          ...formData,
          homepageUri: formData.homepageUri,
        },
      })
      toast.add({ title: 'Standalone event created', color: 'success' })
    }

    await queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpointQueryKey() })
    isStandaloneEventModalOpen.value = false
    resetStandaloneEventForm()
  }
  catch (error) {
    console.error('Failed to save standalone event', error)
    applyStandaloneEventApiErrors(error)
    toast.add({
      title: 'Failed to save standalone event',
      description: getApiValidationSummary(error) ?? getApiErrorMessage(error, 'Please review the highlighted fields and try again.'),
      color: 'error',
    })
  }
}

const isStandaloneEventSubmitting = computed(() =>
  createStandaloneEventMutation.isPending.value
  || updateStandaloneEventMutation.isPending.value,
)

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

const searchQuery = ref('')
const activeEventType = ref<'all' | 'hackathons' | 'workshops'>('all')
const activeOpportunity = ref<'all' | 'open' | 'mine' | 'organizing'>('all')

const eventTypeFilters = [
  { label: 'All', value: 'all' as const, icon: 'i-lucide-layout-grid' },
  { label: 'Hackathons', value: 'hackathons' as const, icon: 'i-lucide-trophy' },
  { label: 'Workshops', value: 'workshops' as const, icon: 'i-lucide-brain-circuit' },
]

const opportunityFilters = [
  { label: 'Discover', value: 'all' as const },
  { label: 'Open to join', value: 'open' as const },
  { label: 'My events', value: 'mine' as const },
  { label: 'Organizing', value: 'organizing' as const },
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

    if (activeOpportunity.value === 'open')
      return !status?.isParticipant && !status?.isOrganizer

    if (activeOpportunity.value === 'mine')
      return !!status?.isParticipant || !!status?.isOrganizer || !!user.value?.isRoot

    if (activeOpportunity.value === 'organizing')
      return !!status?.isOrganizer || !!user.value?.isRoot

    return true
  }).sort((a, b) => {
    const aNeedsAttention = a.status?.isParticipant && !a.registrationComplete
    const bNeedsAttention = b.status?.isParticipant && !b.registrationComplete

    if (aNeedsAttention !== bNeedsAttention)
      return aNeedsAttention ? -1 : 1

    return new Date(a.hackathon.eventStartDate ?? 0).getTime() - new Date(b.hackathon.eventStartDate ?? 0).getTime()
  })
})

const visibleStandaloneEvents = computed(() => {
  if (activeEventType.value === 'hackathons')
    return []

  const query = searchQuery.value.trim().toLowerCase()

  return standaloneEvents.value.filter((event) => {
    const searchText = [
      event.title,
      event.description,
      event.location,
      event.shortCode,
    ].filter(Boolean).join(' ').toLowerCase()

    if (query && !searchText.includes(query))
      return false

    if (activeOpportunity.value === 'open')
      return !!event.isPublished

    if (activeOpportunity.value === 'mine' || activeOpportunity.value === 'organizing')
      return !!user.value?.id

    return true
  }).sort((a, b) =>
    new Date(a.startTime ?? 0).getTime() - new Date(b.startTime ?? 0).getTime(),
  )
})

const discoverStats = computed(() => {
  const joinedHackathons = hackathonCards.value.filter(({ status }) => status?.isParticipant).length
  const openHackathons = hackathonCards.value.filter(({ status }) => !status?.isParticipant && !status?.isOrganizer).length
  const organizingHackathons = hackathonCards.value.filter(({ status }) => status?.isOrganizer).length

  return [
    {
      label: 'Hackathons',
      value: hackathons.value.length,
      icon: 'i-lucide-trophy',
      tone: 'text-primary',
    },
    {
      label: 'Workshops',
      value: standaloneEvents.value.length,
      icon: 'i-lucide-brain-circuit',
      tone: 'text-info',
    },
    {
      label: 'Open to join',
      value: openHackathons + standaloneEvents.value.filter(event => event.isPublished).length,
      icon: 'i-lucide-sparkles',
      tone: 'text-success',
    },
    {
      label: 'In your orbit',
      value: joinedHackathons + organizingHackathons + standaloneEvents.value.length,
      icon: 'i-lucide-user-check',
      tone: 'text-warning',
    },
  ]
})

const featuredHackathonCard = computed(() =>
  hackathonCards.value.find(({ status, registrationComplete }) => status?.isParticipant && !registrationComplete)
  ?? hackathonCards.value.find(({ status }) => !status?.isParticipant && !status?.isOrganizer)
  ?? hackathonCards.value[0],
)

const featuredHackathonActionLabel = computed(() => {
  const card = featuredHackathonCard.value
  if (!card)
    return 'Browse events'

  if (user.value?.isRoot || card.status?.isOrganizer)
    return 'Manage event'

  if (!card.status?.isParticipant)
    return 'Join event'

  if (!card.registrationComplete)
    return 'Continue registration'

  if (card.status?.status !== 'Accepted')
    return 'View status'

  return 'Open portal'
})

function selectFeaturedHackathon() {
  const card = featuredHackathonCard.value
  const hackathon = card?.hackathon

  if (!card || !hackathon?.id || !hackathon.shortCode)
    return

  if (user.value?.isRoot || card.status?.isOrganizer) {
    navigateTo(`/dash/${hackathon.id}`)
    return
  }

  if (!card.status?.isParticipant) {
    joinHackathon({ id: hackathon.id, shortCode: hackathon.shortCode })
    return
  }

  if (!card.registrationComplete) {
    navigateTo(`/${hackathon.shortCode}/registration`)
    return
  }

  if (card.status?.status !== 'Accepted') {
    navigateTo(`/dash/${hackathon.id}/participant`)
    return
  }

  navigateTo(`/${hackathon.shortCode}/team`)
}

function standaloneEventAnalyticsForEvent(eventId: string | null | undefined): StandaloneEventAnalytics | undefined {
  const index = standaloneEvents.value.findIndex(event => event.id === eventId)
  return index === -1 ? undefined : standaloneEventAnalyticsForIndex(index)
}

function capacityPercent(event: typeof standaloneEvents.value[number]) {
  const analytics = standaloneEventAnalyticsForEvent(event.id)
  const usedPercent = analytics?.capacityUsedPercent

  if (typeof usedPercent === 'number')
    return Math.min(Math.max(usedPercent, 0), 100)

  const registered = analytics?.registeredCount ?? 0
  const capacity = event.maxParticipants ?? 0

  return capacity > 0 ? Math.min(Math.round((registered / capacity) * 100), 100) : 0
}
</script>

<template>
  <UDashboardPanel id="dashboard">
    <template #header>
      <UDashboardNavbar title="Dashboard">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="space-y-5">
        <section class="overflow-hidden rounded-xl border border-default bg-elevated/35">
          <div class="grid gap-0 lg:grid-cols-[minmax(0,1.5fr)_minmax(320px,0.9fr)]">
            <div class="p-5 sm:p-6 lg:p-7">
              <div class="flex flex-wrap items-center gap-2">
                <UBadge
                  color="primary"
                  variant="subtle"
                  icon="i-lucide-sparkles"
                >
                  GeeksHacking Portal
                </UBadge>
                <UBadge
                  color="neutral"
                  variant="outline"
                >
                  {{ user?.gitHubLogin ? `@${user.gitHubLogin}` : 'Community dashboard' }}
                </UBadge>
              </div>

              <div class="mt-5 max-w-3xl space-y-3">
                <h1 class="text-2xl font-semibold tracking-tight text-default sm:text-3xl">
                  Discover hackathons and workshops built for builders.
                </h1>
                <p class="text-sm leading-6 text-muted sm:text-base">
                  Browse upcoming opportunities, continue registrations, and jump back into events you organize from one focused command center.
                </p>
              </div>

              <div class="mt-6 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                <div
                  v-for="stat in discoverStats"
                  :key="stat.label"
                  class="rounded-lg border border-default bg-default/60 p-3"
                >
                  <div class="flex items-center justify-between gap-3">
                    <span class="text-xs font-medium text-muted">{{ stat.label }}</span>
                    <UIcon
                      :name="stat.icon"
                      class="size-4" :class="[stat.tone]"
                    />
                  </div>
                  <div class="mt-2 text-2xl font-semibold text-default">
                    {{ stat.value }}
                  </div>
                </div>
              </div>
            </div>

            <div class="border-t border-default bg-muted/40 p-5 sm:p-6 lg:border-l lg:border-t-0">
              <div class="flex items-center justify-between gap-3">
                <div>
                  <div class="text-xs font-medium uppercase text-muted">
                    Suggested next step
                  </div>
                  <h2 class="mt-1 text-lg font-semibold text-default">
                    {{ featuredHackathonCard?.hackathon.name ?? 'Find your next event' }}
                  </h2>
                </div>
                <UIcon
                  name="i-lucide-compass"
                  class="size-6 text-primary"
                />
              </div>

              <p class="mt-3 line-clamp-3 text-sm leading-6 text-muted">
                {{ featuredHackathonCard?.hackathon.description ?? 'Search the catalog for hackathons, workshops, and organizer spaces as they become available.' }}
              </p>

              <div
                v-if="featuredHackathonCard"
                class="mt-4 grid gap-2 text-sm"
              >
                <div class="flex items-center gap-2 text-muted">
                  <UIcon
                    name="i-lucide-calendar-days"
                    class="size-4"
                  />
                  <span>{{ formatHackathonDate(featuredHackathonCard.hackathon.eventStartDate) }}</span>
                </div>
                <div
                  v-if="featuredHackathonCard.hackathon.venue"
                  class="flex items-center gap-2 text-muted"
                >
                  <UIcon
                    name="i-lucide-map-pin"
                    class="size-4"
                  />
                  <span>{{ featuredHackathonCard.hackathon.venue }}</span>
                </div>
              </div>

              <UButton
                class="mt-5 w-full justify-center"
                icon="i-lucide-arrow-right"
                trailing
                :disabled="!featuredHackathonCard"
                :loading="joinMutation.isPending.value"
                @click="selectFeaturedHackathon"
              >
                {{ featuredHackathonActionLabel }}
              </UButton>
            </div>
          </div>
        </section>

        <section class="rounded-xl border border-default bg-default p-3 sm:p-4">
          <div class="grid gap-3 lg:grid-cols-[minmax(240px,1fr)_auto] lg:items-center">
            <UInput
              v-model="searchQuery"
              icon="i-lucide-search"
              placeholder="Search by event name, venue, topic, or shortcode"
              size="lg"
            />

            <div class="flex flex-col gap-2 sm:flex-row sm:items-center lg:justify-end">
              <div class="flex rounded-lg border border-default bg-elevated/40 p-1">
                <UButton
                  v-for="filter in eventTypeFilters"
                  :key="filter.value"
                  :icon="filter.icon"
                  size="sm"
                  :color="activeEventType === filter.value ? 'primary' : 'neutral'"
                  :variant="activeEventType === filter.value ? 'solid' : 'ghost'"
                  @click="activeEventType = filter.value"
                >
                  {{ filter.label }}
                </UButton>
              </div>
            </div>
          </div>

          <div class="mt-3 flex gap-2 overflow-x-auto pb-1">
            <UButton
              v-for="filter in opportunityFilters"
              :key="filter.value"
              size="sm"
              :color="activeOpportunity === filter.value ? 'primary' : 'neutral'"
              :variant="activeOpportunity === filter.value ? 'subtle' : 'outline'"
              class="shrink-0"
              @click="activeOpportunity = filter.value"
            >
              {{ filter.label }}
            </UButton>
          </div>
        </section>

        <section
          v-if="activeEventType !== 'workshops'"
          class="space-y-3"
        >
          <div class="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <h2 class="text-lg font-semibold text-default">
                Hackathons
              </h2>
              <p class="text-sm text-muted">
                Join, register, or manage full hackathon programs.
              </p>
            </div>
            <UButton
              v-if="user?.isRoot"
              icon="i-lucide-plus"
              size="sm"
              class="self-start sm:self-auto"
              @click="openCreateHackathonModal"
            >
              Add Hackathon
            </UButton>
          </div>

          <div
            v-if="isLoadingHackathons"
            class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3"
          >
            <UCard
              v-for="index in 3"
              :key="index"
            >
              <div class="space-y-3">
                <USkeleton class="h-5 w-2/3" />
                <USkeleton class="h-16 w-full" />
                <USkeleton class="h-9 w-32" />
              </div>
            </UCard>
          </div>

          <UAlert
            v-else-if="!visibleHackathonCards.length"
            color="neutral"
            variant="soft"
            icon="i-lucide-search-x"
            title="No matching hackathons"
            description="Try a different search term or filter."
          />

          <div
            v-else
            class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3"
          >
            <UCard
              v-for="{ hackathon, index, status, registrationComplete } in visibleHackathonCards"
              :key="hackathon.id ?? index"
              class="transition hover:-translate-y-0.5 hover:shadow-md"
            >
              <template #header>
                <div class="flex items-start justify-between gap-3">
                  <div class="min-w-0">
                    <div class="flex items-center gap-2">
                      <UIcon
                        name="i-lucide-trophy"
                        class="size-4 shrink-0 text-primary"
                      />
                      <h3 class="truncate text-base font-semibold text-default">
                        {{ hackathon.name }}
                      </h3>
                    </div>
                    <div class="mt-2 flex flex-wrap items-center gap-2">
                      <UBadge
                        v-if="user?.isRoot"
                        color="warning"
                        variant="subtle"
                        size="sm"
                      >
                        Admin
                      </UBadge>
                      <UBadge
                        v-else-if="status?.isOrganizer"
                        color="info"
                        variant="subtle"
                        size="sm"
                      >
                        Organizer
                      </UBadge>
                      <UBadge
                        v-else-if="status"
                        :color="formatParticipantStatus(status.status ?? null, status.isParticipant).color"
                        variant="subtle"
                        size="sm"
                      >
                        {{ formatParticipantStatus(status.status ?? null, status.isParticipant).label }}
                      </UBadge>
                    </div>
                  </div>

                  <UButton
                    v-if="user?.isRoot"
                    size="xs"
                    variant="ghost"
                    icon="i-lucide-pencil"
                    aria-label="Edit hackathon"
                    @click.stop="openEditHackathonModal(hackathon)"
                  />
                </div>
              </template>

              <div class="space-y-4">
                <p class="line-clamp-3 min-h-16 text-sm leading-6 text-muted">
                  {{ hackathon.description || 'Details are being prepared by the organizers.' }}
                </p>

                <div class="grid gap-2 text-sm text-muted">
                  <div class="flex items-center gap-2">
                    <UIcon
                      name="i-lucide-calendar-days"
                      class="size-4"
                    />
                    <span>{{ formatHackathonDate(hackathon.eventStartDate) }} to {{ formatHackathonDate(hackathon.eventEndDate) }}</span>
                  </div>
                  <div
                    v-if="hackathon.venue"
                    class="flex items-center gap-2"
                  >
                    <UIcon
                      name="i-lucide-map-pin"
                      class="size-4"
                    />
                    <span class="truncate">{{ hackathon.venue }}</span>
                  </div>
                </div>

                <UAlert
                  v-if="status?.status === 'Rejected' && status.reviewReason"
                  color="error"
                  variant="soft"
                  icon="i-lucide-circle-alert"
                  :description="`Reason: ${status.reviewReason}`"
                />

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
                  <template v-if="user?.isRoot">
                    <UButton
                      :to="`/dash/${hackathon.id}`"
                      icon="i-lucide-settings-2"
                      size="sm"
                    >
                      Manage
                    </UButton>
                  </template>

                  <template v-else-if="status?.isOrganizer">
                    <UButton
                      :to="`/dash/${hackathon.id}`"
                      icon="i-lucide-settings-2"
                      size="sm"
                    >
                      Manage
                    </UButton>
                    <UButton
                      :to="`/${hackathon.shortCode}/team`"
                      color="neutral"
                      variant="outline"
                      icon="i-lucide-external-link"
                      size="sm"
                    >
                      Portal
                    </UButton>
                  </template>

                  <template v-else-if="!status?.isParticipant">
                    <UButton
                      icon="i-lucide-user-plus"
                      size="sm"
                      :loading="joinMutation.isPending.value"
                      @click="joinHackathon({ id: hackathon.id!, shortCode: hackathon.shortCode! })"
                    >
                      Join event
                    </UButton>
                  </template>

                  <template v-else-if="!registrationComplete">
                    <UButton
                      :to="`/${hackathon.shortCode}/registration`"
                      icon="i-lucide-clipboard-pen-line"
                      size="sm"
                    >
                      Continue registration
                    </UButton>
                  </template>

                  <template v-else-if="status?.status !== 'Accepted'">
                    <UButton
                      :to="`/dash/${hackathon.id}/participant`"
                      icon="i-lucide-file-check"
                      size="sm"
                    >
                      View status
                    </UButton>
                  </template>

                  <template v-else>
                    <UButton
                      :to="`/${hackathon.shortCode}/team`"
                      icon="i-lucide-arrow-right"
                      trailing
                      size="sm"
                    >
                      Open portal
                    </UButton>
                  </template>
                </div>
              </div>
            </UCard>
          </div>
        </section>

        <section
          v-if="activeEventType !== 'hackathons'"
          class="space-y-3"
        >
          <div class="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <h2 class="text-lg font-semibold text-default">
                Workshops
              </h2>
              <p class="text-sm text-muted">
                Discover standalone workshops and lightweight learning sessions.
              </p>
            </div>
            <UButton
              v-if="user?.isRoot"
              icon="i-lucide-plus"
              size="sm"
              class="self-start sm:self-auto"
              @click="openCreateStandaloneEventModal"
            >
              Add Workshop
            </UButton>
          </div>

          <div
            v-if="isLoadingUser || (!!user?.id && isLoadingStandaloneEvents)"
            class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3"
          >
            <UCard
              v-for="index in 3"
              :key="index"
            >
              <div class="space-y-3">
                <USkeleton class="h-5 w-2/3" />
                <USkeleton class="h-16 w-full" />
                <USkeleton class="h-20 w-full" />
              </div>
            </UCard>
          </div>

          <UAlert
            v-else-if="!user?.id"
            color="neutral"
            variant="soft"
            icon="i-lucide-log-in"
            title="Sign in to view workshops"
            description="Workshop discovery is available after sign-in."
          />

          <UAlert
            v-else-if="!visibleStandaloneEvents.length"
            color="neutral"
            variant="soft"
            icon="i-lucide-search-x"
            title="No matching workshops"
            description="Try a different search term or filter."
          />

          <div
            v-else
            class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3"
          >
            <UCard
              v-for="event in visibleStandaloneEvents"
              :key="event.id"
              class="transition hover:-translate-y-0.5 hover:shadow-md"
            >
              <template #header>
                <div class="flex items-start justify-between gap-3">
                  <div class="min-w-0">
                    <div class="flex items-center gap-2">
                      <UIcon
                        name="i-lucide-brain-circuit"
                        class="size-4 shrink-0 text-info"
                      />
                      <h3 class="truncate text-base font-semibold text-default">
                        {{ event.title }}
                      </h3>
                    </div>
                    <div class="mt-2 flex flex-wrap items-center gap-2">
                      <UBadge
                        :color="event.isPublished ? 'success' : 'neutral'"
                        variant="subtle"
                        size="sm"
                      >
                        {{ event.isPublished ? 'Published' : 'Draft' }}
                      </UBadge>
                      <UBadge
                        color="info"
                        variant="outline"
                        size="sm"
                      >
                        Workshop
                      </UBadge>
                    </div>
                  </div>

                  <UButton
                    v-if="user?.isRoot"
                    size="xs"
                    variant="ghost"
                    icon="i-lucide-pencil"
                    aria-label="Edit workshop"
                    @click.stop="openEditStandaloneEventModal(event)"
                  />
                </div>
              </template>

              <div class="space-y-4">
                <p class="line-clamp-3 min-h-16 text-sm leading-6 text-muted">
                  {{ event.description || 'Workshop details are being prepared.' }}
                </p>

                <div class="grid gap-2 text-sm text-muted">
                  <div class="flex items-center gap-2">
                    <UIcon
                      name="i-lucide-calendar-days"
                      class="size-4"
                    />
                    <span>{{ formatHackathonDate(event.startTime) }} to {{ formatHackathonDate(event.endTime) }}</span>
                  </div>
                  <div
                    v-if="event.location"
                    class="flex items-center gap-2"
                  >
                    <UIcon
                      name="i-lucide-map-pin"
                      class="size-4"
                    />
                    <span class="truncate">{{ event.location }}</span>
                  </div>
                </div>

                <div class="rounded-lg border border-default bg-elevated/40 p-3">
                  <div class="flex items-center justify-between gap-3 text-sm">
                    <span class="font-medium text-default">Capacity</span>
                    <span class="text-muted">
                      {{ standaloneEventAnalyticsForEvent(event.id)?.registeredCount ?? '...' }} / {{ event.maxParticipants ?? 0 }}
                    </span>
                  </div>
                  <div class="mt-2 h-2 overflow-hidden rounded-full bg-muted">
                    <div
                      class="h-full rounded-full bg-info"
                      :style="{ width: `${capacityPercent(event)}%` }"
                    />
                  </div>
                  <div class="mt-2 grid grid-cols-2 gap-2 text-xs text-muted">
                    <span>{{ standaloneEventAnalyticsForEvent(event.id)?.currentlyCheckedInCount ?? 0 }} checked in</span>
                    <span class="text-right">{{ standaloneEventAnalyticsForEvent(event.id)?.resourceRedemptionCount ?? 0 }} redemptions</span>
                  </div>
                </div>

                <div class="flex flex-wrap items-center gap-2">
                  <UButton
                    v-if="event.id"
                    :to="`/dash/standalone/${event.id}`"
                    icon="i-lucide-settings-2"
                    size="sm"
                  >
                    Manage
                  </UButton>
                  <UButton
                    v-if="event.shortCode"
                    :to="`/workshops/${event.shortCode}`"
                    color="neutral"
                    variant="outline"
                    icon="i-lucide-clipboard-pen-line"
                    size="sm"
                  >
                    Register
                  </UButton>
                  <UButton
                    v-else-if="event.homepageUri"
                    :to="event.homepageUri"
                    target="_blank"
                    color="neutral"
                    variant="outline"
                    icon="i-lucide-external-link"
                    size="sm"
                  >
                    Homepage
                  </UButton>
                </div>
              </div>
            </UCard>
          </div>
        </section>
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

          <UFormField label="Email Templates">
            <OrganizersEmailTemplateEditor
              v-model="hackathonForm.emailTemplates"
              event-kind="hackathon"
            />
          </UFormField>

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
            :error="standaloneEventFieldErrors.title"
          >
            <UInput
              v-model="standaloneEventForm.title"
              placeholder="Event title"
            />
          </UFormField>

          <UFormField
            label="Short Code"
            required
            :error="standaloneEventFieldErrors.shortCode"
          >
            <UInput
              v-model="standaloneEventForm.shortCode"
              placeholder="e.g., workshop-2026"
            />
          </UFormField>

          <UFormField
            label="Description"
            required
            :error="standaloneEventFieldErrors.description"
          >
            <UTextarea
              v-model="standaloneEventForm.description"
              placeholder="Standalone event description"
              :rows="3"
            />
          </UFormField>

          <UFormField
            label="Location"
            required
            :error="standaloneEventFieldErrors.location"
          >
            <UInput
              v-model="standaloneEventForm.location"
              placeholder="Event location"
            />
          </UFormField>

          <UFormField
            label="Homepage URL"
            :required="!isEditingStandaloneEvent"
            :error="standaloneEventFieldErrors.homepageUri"
          >
            <UInput
              v-model="standaloneEventForm.homepageUri"
              placeholder="https://..."
            />
          </UFormField>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4">
            <p class="text-xs text-(--ui-text-muted) sm:col-span-2">
              Schedule fields use {{ HACKATHON_TIME_ZONE_LABEL }} (UTC+8).
            </p>
            <UFormField
              label="Event Start"
              required
              :error="standaloneEventFieldErrors.startTime"
            >
              <UInput
                v-model="standaloneEventForm.startTime"
                type="datetime-local"
              />
            </UFormField>

            <UFormField
              label="Event End"
              required
              :error="standaloneEventFieldErrors.endTime"
            >
              <UInput
                v-model="standaloneEventForm.endTime"
                type="datetime-local"
              />
            </UFormField>
          </div>

          <UFormField
            label="Max Participants"
            required
            :error="standaloneEventFieldErrors.maxParticipants"
          >
            <UInput
              v-model.number="standaloneEventForm.maxParticipants"
              type="number"
              min="0"
            />
          </UFormField>

          <UFormField
            label="Email Templates"
            :error="standaloneEventFieldErrors.emailTemplates"
          >
            <OrganizersEmailTemplateEditor
              v-model="standaloneEventForm.emailTemplates"
              event-kind="standalone"
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
</template>
