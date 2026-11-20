<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListResponse,
  GeeksHackingPortalApiEndpointsParticipantsHackathonStatusParticipantStatusKey,
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
import { useLocalStorage } from '@vueuse/core'
import { computed, ref, unref } from 'vue'
import { getApiErrorMessage, getApiFieldError, getApiValidationErrors, getApiValidationSummary } from '~/utils/api-errors'
import {
  formatHackathonDate,
  formatHackathonDateTimeInput,
  HACKATHON_TIME_ZONE_LABEL,
  parseHackathonDateTimeValue,
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
        path: { hackathonId: editingHackathonId.value },
        body: {
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
      await createMutation.mutateAsync({ body: formData })
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
  if (homepageUri && (!URL.canParse(homepageUri) || !['http:', 'https:'].includes(new URL(homepageUri).protocol)))
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
        path: { standaloneWorkshopId: editingStandaloneEventId.value },
        body: {
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
        body: {
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
    ...geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsAnalyticsEndpointQueryOptions({ path: { standaloneWorkshopId: event.id ?? '' } }),
    enabled: !!event.id,
  }))),
})

function standaloneEventAnalyticsForIndex(index: number): StandaloneEventAnalytics | undefined {
  return unref(standaloneEventAnalyticsQueries.value[index]?.data) as StandaloneEventAnalytics | undefined
}

// Fetch participation status per hackathon
const statusQueries = useQueries({
  queries: computed(() =>
    hackathons.value.map(hackathon => ({
      ...geeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpointQueryOptions({ path: { hackathonId: hackathon.id ?? '' } }),
      enabled: !!hackathon.id,
    })),
  ),
})

// Fetch registration submissions to check completion status
const submissionQueries = useQueries({
  queries: computed(() =>
    hackathons.value.map((hackathon) => {
      const status = statusQueries.value[hackathons.value.indexOf(hackathon)]?.data as GeeksHackingPortalApiEndpointsParticipantsHackathonStatusResponse | undefined
      const isParticipant = status?.isParticipant === true
      return {
        ...geeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListEndpointQueryOptions({ path: { hackathonId: hackathon.id ?? '' } }),
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

function formatParticipantStatus(status: GeeksHackingPortalApiEndpointsParticipantsHackathonStatusParticipantStatusKey | null | undefined, isParticipant?: boolean | null) {
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
type Lifecycle = 'upcoming' | 'ongoing' | 'past'
const activeLifecycle = ref<'all' | Lifecycle>('all')
const activePublishState = ref<'all' | 'published' | 'draft'>('all')
const activeSort = ref<'date-asc' | 'date-desc' | 'name' | 'attention'>('date-asc')
const viewMode = useLocalStorage<'list' | 'card'>('dash-events-view', 'list')

const eventTypeFilters = [
  { label: 'All', value: 'all' as const, icon: 'i-lucide-layout-grid' },
  { label: 'Hackathons', value: 'hackathons' as const, icon: 'i-lucide-trophy' },
  { label: 'Workshops', value: 'workshops' as const, icon: 'i-lucide-brain-circuit' },
]

const opportunityFilters = [
  { label: 'All events', value: 'all' as const },
  { label: 'Open to join', value: 'open' as const },
  { label: 'My events', value: 'mine' as const },
  { label: 'Organizing', value: 'organizing' as const },
]

const lifecycleFilters = [
  { label: 'Any time', value: 'all' as const },
  { label: 'Upcoming', value: 'upcoming' as const },
  { label: 'Ongoing', value: 'ongoing' as const },
  { label: 'Past', value: 'past' as const },
]

const publishFilters = [
  { label: 'Any state', value: 'all' as const },
  { label: 'Published', value: 'published' as const },
  { label: 'Draft', value: 'draft' as const },
]

const sortOptions = [
  { label: 'Date: soonest first', value: 'date-asc' as const },
  { label: 'Date: latest first', value: 'date-desc' as const },
  { label: 'Name: A to Z', value: 'name' as const },
  { label: 'Needs attention first', value: 'attention' as const },
]

const lifecycleMeta: Record<Lifecycle, { label: string, color: 'info' | 'success' | 'neutral' }> = {
  upcoming: { label: 'Upcoming', color: 'info' },
  ongoing: { label: 'Ongoing', color: 'success' },
  past: { label: 'Past', color: 'neutral' },
}

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
  start: string | undefined
  end: string | undefined
  startMs: number
  lifecycle: Lifecycle
  isPublished: boolean
  needsAttention: boolean
  isMine: boolean
  isOrganizing: boolean
  isOpen: boolean
  hackathonCard?: typeof hackathonCards.value[number]
  workshop?: typeof standaloneEvents.value[number]
}

const eventRows = computed<EventRow[]>(() => {
  const rows: EventRow[] = []

  for (const card of hackathonCards.value) {
    const { hackathon, status, registrationComplete } = card
    rows.push({
      key: `h-${hackathon.id ?? card.index}`,
      kind: 'hackathon',
      name: hackathon.name ?? '',
      description: hackathon.description ?? '',
      location: hackathon.venue ?? '',
      shortCode: hackathon.shortCode ?? '',
      start: hackathon.eventStartDate,
      end: hackathon.eventEndDate,
      startMs: parseHackathonDateTimeValue(hackathon.eventStartDate)?.getTime() ?? Number.MAX_SAFE_INTEGER,
      lifecycle: getLifecycle(hackathon.eventStartDate, hackathon.eventEndDate),
      isPublished: hackathon.isPublished !== false,
      needsAttention: !!status?.isParticipant && !registrationComplete,
      isMine: !!status?.isParticipant || !!status?.isOrganizer || !!user.value?.isRoot,
      isOrganizing: !!status?.isOrganizer || !!user.value?.isRoot,
      isOpen: !status?.isParticipant && !status?.isOrganizer,
      hackathonCard: card,
    })
  }

  for (const event of standaloneEvents.value) {
    rows.push({
      key: `w-${event.id}`,
      kind: 'workshop',
      name: event.title ?? '',
      description: event.description ?? '',
      location: event.location ?? '',
      shortCode: event.shortCode ?? '',
      start: event.startTime,
      end: event.endTime,
      startMs: parseHackathonDateTimeValue(event.startTime)?.getTime() ?? Number.MAX_SAFE_INTEGER,
      lifecycle: getLifecycle(event.startTime, event.endTime),
      isPublished: !!event.isPublished,
      needsAttention: !event.isPublished,
      isMine: !!user.value?.id,
      isOrganizing: !!user.value?.id,
      isOpen: !!event.isPublished,
      workshop: event,
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
    if (activeLifecycle.value !== 'all' && row.lifecycle !== activeLifecycle.value)
      return false
    if (activePublishState.value === 'published' && !row.isPublished)
      return false
    if (activePublishState.value === 'draft' && row.isPublished)
      return false
    if (activeOpportunity.value === 'open' && !row.isOpen)
      return false
    if (activeOpportunity.value === 'mine' && !row.isMine)
      return false
    if (activeOpportunity.value === 'organizing' && !row.isOrganizing)
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
  || activeOpportunity.value !== 'all'
  || activeLifecycle.value !== 'all'
  || activePublishState.value !== 'all',
)

function resetFilters() {
  searchQuery.value = ''
  activeEventType.value = 'all'
  activeOpportunity.value = 'all'
  activeLifecycle.value = 'all'
  activePublishState.value = 'all'
}

const discoverStats = computed(() => {
  const rows = eventRows.value
  return [
    { label: 'Total events', value: rows.length, icon: 'i-lucide-layout-grid', tone: 'text-primary' },
    { label: 'Upcoming', value: rows.filter(r => r.lifecycle === 'upcoming').length, icon: 'i-lucide-calendar-clock', tone: 'text-info' },
    { label: 'Ongoing', value: rows.filter(r => r.lifecycle === 'ongoing').length, icon: 'i-lucide-radio', tone: 'text-success' },
    { label: 'Drafts / needs attention', value: rows.filter(r => r.needsAttention).length, icon: 'i-lucide-circle-alert', tone: 'text-warning' },
  ]
})

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
  <GPanel id="dashboard-management">
    <template #header>
      <GPanelHeader title="Manage events" />
    </template>

    <template #body>
      <div class="mx-auto w-full max-w-7xl space-y-6">
        <section class="space-y-4">
          <div class="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div class="max-w-3xl space-y-3">
              <div class="flex flex-wrap items-center gap-2">
                <UBadge
                  color="primary"
                  variant="subtle"
                >
                  GeeksHacking Portal
                </UBadge>
                <span class="text-xs text-muted">
                  {{ user?.gitHubLogin ? `Signed in as @${user.gitHubLogin}` : 'Community dashboard' }}
                </span>
              </div>
              <div>
                <h1 class="text-2xl font-semibold tracking-tight text-default sm:text-3xl">
                  Manage GeeksHacking events.
                </h1>
                <p class="mt-2 text-sm leading-6 text-muted">
                  Create, publish, and maintain hackathons and standalone workshops from one organizer workspace.
                </p>
              </div>
            </div>

            <div
              v-if="user?.isRoot"
              class="flex flex-col gap-2 sm:flex-row lg:justify-end"
            >
              <UButton
                icon="i-lucide-plus"
                @click="openCreateHackathonModal"
              >
                Add Hackathon
              </UButton>
              <UButton
                color="neutral"
                variant="outline"
                icon="i-lucide-plus"
                @click="openCreateStandaloneEventModal"
              >
                Add Workshop
              </UButton>
            </div>
          </div>

          <div class="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <div
              v-for="stat in discoverStats"
              :key="stat.label"
              class="rounded-lg border border-default bg-default p-4"
            >
              <div class="flex items-center justify-between gap-3">
                <span class="text-sm font-medium text-muted">{{ stat.label }}</span>
                <UIcon
                  :name="stat.icon"
                  class="size-4"
                  :class="[stat.tone]"
                />
              </div>
              <div class="mt-3 text-2xl font-semibold text-default">
                {{ stat.value }}
              </div>
            </div>
          </div>
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
              v-model="activeLifecycle"
              :items="lifecycleFilters"
              icon="i-lucide-calendar-days"
              class="lg:w-40"
              aria-label="Filter by time"
            />
            <USelect
              v-model="activePublishState"
              :items="publishFilters"
              icon="i-lucide-globe"
              class="lg:w-40"
              aria-label="Filter by published state"
            />
            <USelect
              v-model="activeOpportunity"
              :items="opportunityFilters"
              icon="i-lucide-user"
              class="lg:w-40"
              aria-label="Filter by involvement"
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
            v-if="isLoadingHackathons || isLoadingUser || (!!user?.id && isLoadingStandaloneEvents)"
            class="col-span-full space-y-2"
          >
            <USkeleton
              v-for="index in 4"
              :key="index"
              class="h-20 w-full"
            />
          </div>

          <UAlert
            v-else-if="!visibleEvents.length"
            color="neutral"
            variant="soft"
            icon="i-lucide-search-x"
            class="col-span-full"
            title="No matching events"
            description="Try a different search term or clear your filters."
          />

          <template v-else>
            <div
              v-for="row in visibleEvents"
              :key="row.key"
              class="rounded-lg border border-default bg-default p-4 transition hover:border-primary/40 hover:shadow-sm"
              :class="viewMode === 'card' ? 'flex flex-col gap-4' : 'grid gap-3 lg:grid-cols-[minmax(0,2.2fr)_minmax(0,1.4fr)_15rem] lg:items-center'"
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
                  <span
                    v-if="row.shortCode"
                    class="font-normal normal-case tracking-normal text-muted"
                  >
                    · #{{ row.shortCode }}
                  </span>
                </div>
                <div class="flex items-center gap-2">
                  <h3 class="truncate text-base font-semibold text-default">
                    {{ row.name }}
                  </h3>
                  <UButton
                    v-if="user?.isRoot"
                    size="xs"
                    variant="ghost"
                    color="neutral"
                    icon="i-lucide-pencil"
                    :aria-label="row.kind === 'hackathon' ? 'Edit hackathon' : 'Edit workshop'"
                    @click.stop="row.hackathonCard ? openEditHackathonModal(row.hackathonCard.hackathon) : openEditStandaloneEventModal(row.workshop!)"
                  />
                </div>
                <div class="flex flex-wrap items-center gap-1.5">
                  <UBadge
                    v-if="row.kind === 'workshop' || row.hackathonCard?.hackathon.isPublished !== undefined"
                    :color="row.isPublished ? 'success' : 'warning'"
                    :icon="row.isPublished ? 'i-lucide-globe' : 'i-lucide-eye-off'"
                    variant="subtle"
                    size="sm"
                  >
                    {{ row.isPublished ? 'Published' : 'Draft · not public' }}
                  </UBadge>
                  <UBadge
                    :color="lifecycleMeta[row.lifecycle].color"
                    variant="outline"
                    size="sm"
                  >
                    {{ lifecycleMeta[row.lifecycle].label }}
                  </UBadge>
                  <template v-if="row.hackathonCard?.status && !row.isOrganizing">
                    <UBadge
                      :color="formatParticipantStatus(row.hackathonCard.status.status ?? null, row.hackathonCard.status.isParticipant).color"
                      variant="subtle"
                      size="sm"
                    >
                      {{ formatParticipantStatus(row.hackathonCard.status.status ?? null, row.hackathonCard.status.isParticipant).label }}
                    </UBadge>
                    <UBadge
                      v-if="row.needsAttention"
                      color="warning"
                      variant="soft"
                      size="sm"
                    >
                      Registration incomplete
                    </UBadge>
                  </template>
                </div>
              </div>

              <p
                v-if="viewMode === 'card' && row.description"
                class="line-clamp-2 text-sm text-muted"
              >
                {{ row.description }}
              </p>

              <div class="grid gap-1.5 text-sm text-muted">
                <div class="flex items-center gap-2">
                  <UIcon
                    name="i-lucide-calendar-days"
                    class="size-4 shrink-0"
                  />
                  <span>{{ formatHackathonDate(row.start) }} to {{ formatHackathonDate(row.end) }}</span>
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
                <div
                  v-if="row.workshop"
                  class="flex items-center gap-2"
                >
                  <UIcon
                    name="i-lucide-users"
                    class="size-4 shrink-0"
                  />
                  <span>
                    {{ standaloneEventAnalyticsForEvent(row.workshop.id)?.registeredCount ?? '...' }} / {{ row.workshop.maxParticipants ?? 0 }} registered
                    ({{ capacityPercent(row.workshop) }}%)
                  </span>
                </div>
              </div>

              <div
                class="flex flex-wrap items-center gap-2"
                :class="viewMode === 'card' ? 'mt-auto' : 'lg:justify-end'"
              >
                <template v-if="row.workshop">
                  <UButton
                    v-if="row.workshop.id"
                    :to="`/dash/standalone/${row.workshop.id}`"
                    icon="i-lucide-settings-2"
                    size="sm"
                  >
                    Manage
                  </UButton>
                  <UButton
                    v-if="row.workshop.shortCode"
                    :to="`/workshops/${row.workshop.shortCode}`"
                    color="neutral"
                    variant="outline"
                    icon="i-lucide-external-link"
                    size="sm"
                  >
                    View page
                  </UButton>
                  <UButton
                    v-else-if="row.workshop.homepageUri"
                    :to="row.workshop.homepageUri"
                    target="_blank"
                    color="neutral"
                    variant="outline"
                    icon="i-lucide-external-link"
                    size="sm"
                  >
                    Homepage
                  </UButton>
                </template>

                <template v-else-if="row.hackathonCard">
                  <template v-if="user?.isRoot || row.hackathonCard.status?.isOrganizer">
                    <UButton
                      :to="`/dash/${row.hackathonCard.hackathon.id}`"
                      icon="i-lucide-settings-2"
                      size="sm"
                    >
                      Manage
                    </UButton>
                    <UButton
                      :to="`/${row.hackathonCard.hackathon.shortCode}/team`"
                      color="neutral"
                      variant="outline"
                      icon="i-lucide-external-link"
                      size="sm"
                    >
                      View page
                    </UButton>
                  </template>

                  <template v-else-if="!row.hackathonCard.status?.isParticipant">
                    <UButton
                      icon="i-lucide-user-plus"
                      size="sm"
                      :loading="joinMutation.isPending.value"
                      @click="joinHackathon({ id: row.hackathonCard.hackathon.id!, shortCode: row.hackathonCard.hackathon.shortCode! })"
                    >
                      Join event
                    </UButton>
                  </template>

                  <template v-else-if="!row.hackathonCard.registrationComplete">
                    <UButton
                      :to="`/${row.hackathonCard.hackathon.shortCode}/registration`"
                      icon="i-lucide-clipboard-pen-line"
                      size="sm"
                    >
                      Continue registration
                    </UButton>
                  </template>

                  <template v-else-if="row.hackathonCard.status?.status !== 'Accepted'">
                    <UButton
                      :to="`/dash/${row.hackathonCard.hackathon.id}/participant`"
                      icon="i-lucide-file-check"
                      size="sm"
                    >
                      View status
                    </UButton>
                  </template>

                  <template v-else>
                    <UButton
                      :to="`/${row.hackathonCard.hackathon.shortCode}/team`"
                      icon="i-lucide-arrow-right"
                      trailing
                      size="sm"
                    >
                      Open portal
                    </UButton>
                  </template>
                </template>
              </div>
            </div>
          </template>
        </section>
      </div>
    </template>
  </GPanel>

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
