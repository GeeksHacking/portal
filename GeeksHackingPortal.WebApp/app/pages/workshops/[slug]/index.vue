<script setup lang="ts">
import { Markdown } from '@comark/vue'
import {
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsGetEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsJoinEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsStatusEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import { useProfileName } from '~/composables/useProfileName'
import { getApiErrorMessage } from '~/utils/api-errors'
import { HACKATHON_TIME_ZONE, HACKATHON_TIME_ZONE_LABEL, isSameHackathonDay } from '~/utils/hackathon-date-time'

definePageMeta({
  auth: false,
})

const route = useRoute()
const config = useRuntimeConfig()
const queryClient = useQueryClient()
const toast = useToast()

const slug = computed(() => (route.params.slug as string | undefined) ?? '')

const { data: workshop, isLoading: isLoadingWorkshop, error: workshopError } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsGetEndpoint({ path: computed(() => ({ standaloneWorkshopIdOrShortCode: slug.value })) })
const { data: user, isSuccess: authResolved, isError: authErrored } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint({
  query: {
    retry: false,
    staleTime: 0,
    gcTime: 0,
  },
})

const workshopId = computed(() => workshop.value?.id ?? '')

const { data: statusData, isLoading: isLoadingStatus } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsStatusEndpoint({ path: computed(() => ({ standaloneWorkshopId: workshopId.value })) }, { query: { enabled: computed(() => !!workshopId.value && !!user.value) } })

const { data: questionsData, isLoading: isLoadingQuestions } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListEndpoint({ path: computed(() => ({ standaloneWorkshopId: workshopId.value })) }, { query: { enabled: computed(() => !!workshopId.value && statusData.value?.isRegistered === true) } })

const { data: submissionsData, isLoading: isLoadingSubmissions } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListEndpoint({ path: computed(() => ({ standaloneWorkshopId: workshopId.value })) }, { query: { enabled: computed(() => !!workshopId.value && statusData.value?.isRegistered === true) } })

const joinMutation = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsJoinEndpoint()

// The name lives on the user profile and is confirmed when joining, not in the workshop's registration answers
const { profileName, isProfileNameValid, saveProfileName, isSavingProfileName } = useProfileName(user)

useHead(() => ({
  title: workshop.value?.title ? `${workshop.value.title} - GeeksHacking` : 'Workshop - GeeksHacking',
}))

const registrationState = computed(() => {
  if (!authResolved.value && !authErrored.value)
    return 'checking-auth'
  if (!user.value)
    return 'signed-out'
  if (!workshopId.value)
    return 'loading'
  if (isLoadingStatus.value)
    return 'checking-status'
  if (!statusData.value?.isRegistered)
    return 'ready-to-join'
  if (isLoadingQuestions.value || isLoadingSubmissions.value)
    return 'loading-registration'
  if ((submissionsData.value?.requiredQuestionsRemaining ?? 0) === 0)
    return 'registered'
  return 'incomplete'
})

const registeredPath = computed(() => `/workshops/${slug.value}/registered`)

const hasEnded = computed(() => {
  if (!workshop.value?.endTime)
    return false
  return new Date(workshop.value.endTime).getTime() <= Date.now()
})

const isWorkshopFull = ref(false)

const capacityNote = computed(() => {
  const max = workshop.value?.maxParticipants
  if (!max || max <= 0)
    return null
  return `Limited to ${max} participants`
})

const journeyStepIndex = computed(() => {
  switch (registrationState.value) {
    case 'signed-out':
    case 'checking-auth':
      return 0
    case 'ready-to-join':
    case 'checking-status':
    case 'loading':
      return 1
    case 'incomplete':
    case 'loading-registration':
      return 2
    case 'registered':
      return 3
    default:
      return 0
  }
})

const journeySteps = [
  { key: 'sign-in', label: 'Sign in', shortLabel: 'Sign in' },
  { key: 'confirm', label: 'Confirm name', shortLabel: 'Name' },
  { key: 'questions', label: 'Answer questions', shortLabel: 'Questions' },
] as const

const panelCopy = computed(() => {
  if (hasEnded.value) {
    return {
      eyebrow: 'Registration closed',
      title: 'This workshop has ended',
      description: 'Registration is no longer available. Browse other GeeksHacking events instead.',
    }
  }

  if (isWorkshopFull.value && registrationState.value === 'ready-to-join') {
    return {
      eyebrow: 'Workshop full',
      title: 'No spots left',
      description: 'This workshop has reached capacity. Check Explore events for other sessions.',
    }
  }

  switch (registrationState.value) {
    case 'checking-auth':
    case 'checking-status':
    case 'loading':
    case 'loading-registration':
      return {
        eyebrow: 'Registration',
        title: 'Getting things ready',
        description: 'Checking your account and workshop status…',
      }
    case 'signed-out':
      return {
        eyebrow: 'Step 1 of 3',
        title: 'Sign in to register',
        description: 'Use GitHub to continue. We will bring you straight back to this workshop.',
      }
    case 'ready-to-join':
      return {
        eyebrow: 'Step 2 of 3',
        title: 'Confirm your name',
        description: 'Organizers use this name for check-in and communications. Then you can finish any questions.',
      }
    case 'incomplete':
      return {
        eyebrow: 'Step 3 of 3',
        title: 'Finish your registration',
        description: 'Answer the workshop questions below. After you submit, your spot is confirmed and answers are locked.',
      }
    case 'registered':
      return {
        eyebrow: 'You are in',
        title: 'Registration complete',
        description: 'Taking you to your confirmation…',
      }
    default:
      return {
        eyebrow: 'Registration',
        title: 'Register for this workshop',
        description: 'Follow the steps to secure your spot.',
      }
  }
})

const headerBadge = computed(() => {
  if (hasEnded.value)
    return { label: 'Ended', color: 'neutral' as const }
  if (isWorkshopFull.value && registrationState.value !== 'registered' && registrationState.value !== 'incomplete')
    return { label: 'Full', color: 'warning' as const }
  if (registrationState.value === 'registered')
    return { label: 'Registered', color: 'success' as const }
  if (registrationState.value === 'incomplete')
    return { label: 'Finish registration', color: 'warning' as const }
  if (workshop.value?.isPublished)
    return { label: 'Open for registration', color: 'success' as const }
  return { label: null, color: 'neutral' as const }
})

const showDecisionAside = computed(() =>
  registrationState.value !== 'registered' && registrationState.value !== 'incomplete',
)

const showInlineForm = computed(() => registrationState.value === 'incomplete')

const isPreparing = computed(() =>
  registrationState.value === 'checking-auth'
  || registrationState.value === 'checking-status'
  || registrationState.value === 'loading'
  || registrationState.value === 'loading-registration',
)

watch(registrationState, (state) => {
  if (state === 'registered' && slug.value) {
    navigateTo(registeredPath.value, { replace: true })
  }
})

function standaloneWorkshopStatusQueryKey(standaloneWorkshopId: string) {
  return [{ url: '/participants/standalone-workshops/:standaloneWorkshopId/status', params: { standaloneWorkshopId } }] as const
}

function standaloneWorkshopRegistrationQuestionsQueryKey(standaloneWorkshopId: string) {
  return [{ url: '/participants/standalone-workshops/:standaloneWorkshopId/registration/questions', params: { standaloneWorkshopId } }] as const
}

function standaloneWorkshopRegistrationSubmissionsQueryKey(standaloneWorkshopId: string) {
  return [{ url: '/participants/standalone-workshops/:standaloneWorkshopId/registration/submissions', params: { standaloneWorkshopId } }] as const
}

const formattedDateTime = computed(() => {
  if (!workshop.value?.startTime || !workshop.value?.endTime) {
    return {
      dateLabel: 'Date to be announced',
      timeLabel: 'Time to be announced',
    }
  }

  const start = new Date(workshop.value.startTime)
  const end = new Date(workshop.value.endTime)

  const sameDay = isSameHackathonDay(start, end)

  const dateFormatter = new Intl.DateTimeFormat(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    timeZone: HACKATHON_TIME_ZONE,
  })

  const dateWithYearFormatter = new Intl.DateTimeFormat(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    timeZone: HACKATHON_TIME_ZONE,
  })

  const timeFormatter = new Intl.DateTimeFormat(undefined, {
    hour: 'numeric',
    minute: '2-digit',
    timeZone: HACKATHON_TIME_ZONE,
  })

  return {
    dateLabel: sameDay
      ? dateWithYearFormatter.format(start)
      : `${dateFormatter.format(start)} to ${dateWithYearFormatter.format(end)}`,
    timeLabel: `${timeFormatter.format(start)} to ${timeFormatter.format(end)} ${HACKATHON_TIME_ZONE_LABEL}`,
  }
})

async function startRegistration() {
  if (!workshopId.value || !isProfileNameValid.value || hasEnded.value)
    return

  try {
    isWorkshopFull.value = false
    await saveProfileName()
    await joinMutation.mutateAsync({ path: { standaloneWorkshopId: workshopId.value } })
    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: standaloneWorkshopStatusQueryKey(workshopId.value),
      }),
      queryClient.invalidateQueries({
        queryKey: standaloneWorkshopRegistrationQuestionsQueryKey(workshopId.value),
      }),
      queryClient.invalidateQueries({
        queryKey: standaloneWorkshopRegistrationSubmissionsQueryKey(workshopId.value),
      }),
    ])
    toast.add({
      title: 'Name confirmed',
      description: 'Next: complete any workshop questions to finish signup.',
      color: 'success',
    })
  }
  catch (error) {
    console.error('Failed to start standalone workshop registration', error)
    const message = getApiErrorMessage(error, 'Please try again.')
    if (message.toLowerCase().includes('full')) {
      isWorkshopFull.value = true
      toast.add({
        title: 'Workshop is full',
        description: 'No spots are left for this session.',
        color: 'error',
      })
      return
    }
    toast.add({
      title: 'Unable to start registration',
      description: message,
      color: 'error',
    })
  }
}

async function onRegistrationSubmitted() {
  await navigateTo(registeredPath.value)
}

const loginUrl = computed(() =>
  `${config.public.api}/auth/login?redirect_uri=${encodeURIComponent(route.fullPath)}`,
)
</script>
