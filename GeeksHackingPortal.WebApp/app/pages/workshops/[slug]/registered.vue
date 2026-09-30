<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListQuestionDto,
  GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListSubmissionDto,
} from '@geekshacking/portal-sdk'
import { useQueryClient } from '@tanstack/vue-query'
import {
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsGetEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsStatusEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsWithdrawEndpoint,
  geeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsStatusEndpointQueryKey,
} from '@geekshacking/portal-sdk/hooks'
import QRCode from 'qrcode'
import { HACKATHON_TIME_ZONE, HACKATHON_TIME_ZONE_LABEL, isSameHackathonDay } from '~/utils/hackathon-date-time'

definePageMeta({
  auth: false,
})

const route = useRoute()
const toast = useToast()
const queryClient = useQueryClient()
const config = useRuntimeConfig()

const slug = computed(() => (route.params.slug as string | undefined) ?? '')
const registrationPath = computed(() => `/workshops/${slug.value}`)
const loginUrl = computed(() =>
  `${config.public.api}/auth/login?redirect_uri=${encodeURIComponent(route.fullPath)}`,
)

function goToWorkshopDetails() {
  navigateTo(registrationPath.value)
}

const { data: workshop, isLoading: isLoadingWorkshop, error: workshopError } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsGetEndpoint({ path: computed(() => ({ standaloneWorkshopIdOrShortCode: slug.value })) })
const workshopId = computed(() => workshop.value?.id ?? '')

const { data: user, isLoading: isLoadingUser, isError: isAuthError } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint({
  query: {
    retry: false,
    staleTime: 0,
    gcTime: 0,
  },
})

const { data: statusData, isLoading: isLoadingStatus } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsStatusEndpoint({ path: computed(() => ({ standaloneWorkshopId: workshopId.value })) }, { query: { enabled: computed(() => !!workshopId.value && !!user.value) } })

const { data: questionsData, isLoading: isLoadingQuestions } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListEndpoint({ path: computed(() => ({ standaloneWorkshopId: workshopId.value })) }, { query: { enabled: computed(() => !!workshopId.value && statusData.value?.isRegistered === true) } })

const { data: submissionsData, isLoading: isLoadingSubmissions } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListEndpoint({ path: computed(() => ({ standaloneWorkshopId: workshopId.value })) }, { query: { enabled: computed(() => !!workshopId.value && statusData.value?.isRegistered === true) } })

const withdrawMutation = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsWithdrawEndpoint()
const isWithdrawModalOpen = ref(false)

const canWithdraw = computed(() =>
  !!workshop.value?.endTime && new Date(workshop.value.endTime).getTime() > Date.now(),
)

async function withdrawFromWorkshop() {
  if (!workshopId.value)
    return

  try {
    await withdrawMutation.mutateAsync({ path: { standaloneWorkshopId: workshopId.value } })
    await queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsStatusEndpointQueryKey({ path: { standaloneWorkshopId: workshopId.value } }) })
    isWithdrawModalOpen.value = false
    toast.add({
      title: 'Withdrawn',
      description: 'You have withdrawn from this workshop.',
      color: 'success',
    })
    await navigateTo(registrationPath.value, { replace: true })
  }
  catch (error) {
    console.error('Failed to withdraw from workshop', error)
    toast.add({
      title: 'Could not withdraw',
      description: 'Please try again in a moment.',
      color: 'error',
    })
  }
}

useHead(() => ({
  title: workshop.value?.title ? `Registration Complete - ${workshop.value.title}` : 'Registration Complete - GeeksHacking',
}))

const isLoading = computed(() =>
  isLoadingWorkshop.value
  || isLoadingUser.value
  || isLoadingStatus.value
  || isLoadingQuestions.value
  || isLoadingSubmissions.value,
)

watchEffect(() => {
  if (isLoadingWorkshop.value || !workshop.value)
    return

  if (!isLoadingUser.value && (!user.value || isAuthError.value)) {
    navigateTo(loginUrl.value, { external: true })
    return
  }

  if (!isLoadingStatus.value && statusData.value && !statusData.value.isRegistered) {
    navigateTo(registrationPath.value, { replace: true })
    return
  }

  if (!isLoadingSubmissions.value && submissionsData.value && (submissionsData.value.requiredQuestionsRemaining ?? 0) > 0) {
    navigateTo(registrationPath.value, { replace: true })
  }
})

const questionById = computed(() => {
  const questions = questionsData.value?.categories?.flatMap(category => category.questions ?? []) ?? []
  return new Map(questions.filter(question => question.id).map(question => [question.id as string, question]))
})

const registeredAtLabel = computed(() => {
  if (!statusData.value?.registeredAt)
    return 'Registration date unavailable'

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone: HACKATHON_TIME_ZONE,
  }).format(new Date(statusData.value.registeredAt))
})

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

function formatCalendarDate(value: string | undefined) {
  if (!value)
    return ''

  return new Date(value).toISOString().replace(/[-:]/g, '').replace(/\.\d{3}Z$/, 'Z')
}

function escapeCalendarText(value: string | null | undefined) {
  return (value ?? '')
    .replace(/\\/g, '\\\\')
    .replace(/\n/g, '\\n')
    .replace(/,/g, '\\,')
    .replace(/;/g, '\\;')
}

const calendarEvent = computed(() => {
  if (!workshop.value?.startTime || !workshop.value?.endTime)
    return null

  const title = workshop.value.title ?? 'Workshop'
  const location = workshop.value.location ?? ''
  const detailsUrl = import.meta.client ? `${window.location.origin}${registrationPath.value}` : registrationPath.value

  return {
    title,
    description: `Registered for ${title}. Details: ${detailsUrl}`,
    location,
    start: formatCalendarDate(workshop.value.startTime),
    end: formatCalendarDate(workshop.value.endTime),
  }
})

const googleCalendarUrl = computed(() => {
  if (!calendarEvent.value)
    return ''

  const event = calendarEvent.value
  const params = new URLSearchParams({
    action: 'TEMPLATE',
    text: event.title,
    dates: `${event.start}/${event.end}`,
    details: event.description,
    location: event.location,
  })

  return `https://calendar.google.com/calendar/render?${params.toString()}`
})

function downloadCalendarFile() {
  if (!calendarEvent.value || !import.meta.client)
    return

  const event = calendarEvent.value
  const content = [
    'BEGIN:VCALENDAR',
    'VERSION:2.0',
    'PRODID:-//GeeksHacking//Portal//EN',
    'BEGIN:VEVENT',
    `UID:${workshopId.value || slug.value}@geekshacking.com`,
    `DTSTAMP:${formatCalendarDate(new Date().toISOString())}`,
    `DTSTART:${event.start}`,
    `DTEND:${event.end}`,
    `SUMMARY:${escapeCalendarText(event.title)}`,
    `DESCRIPTION:${escapeCalendarText(event.description)}`,
    `LOCATION:${escapeCalendarText(event.location)}`,
    'END:VEVENT',
    'END:VCALENDAR',
  ].join('\r\n')

  const url = URL.createObjectURL(new Blob([content], { type: 'text/calendar;charset=utf-8' }))
  const link = document.createElement('a')
  link.href = url
  link.download = `${slug.value || 'workshop'}.ics`
  link.click()
  URL.revokeObjectURL(url)
}

type AnswerDetail = {
  key: string
  question: string
  answers: string[]
  followUps: { label: string, value: string }[]
}

type AnswerGroup = {
  name: string
  details: AnswerDetail[]
}

const allQuestions = computed(() =>
  questionsData.value?.categories?.flatMap(category => category.questions ?? []) ?? [],
)

function parseJsonValue(value: string | null | undefined) {
  if (!value)
    return null

  try {
    return JSON.parse(value) as unknown
  }
  catch {
    return null
  }
}

function optionLabel(question: GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListQuestionDto | undefined, value: string) {
  const option = question?.options?.find(item => item.optionValue === value)
  return option?.optionText ?? value
}

function formatAnswerValue(
  value: string | null | undefined,
  question: GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListQuestionDto | undefined,
) {
  const rawValue = value ?? ''
  const parsed = parseJsonValue(rawValue)

  if (Array.isArray(parsed)) {
    return parsed.map(item => optionLabel(question, String(item)))
  }

  if (rawValue === 'true')
    return ['Yes']
  if (rawValue === 'false')
    return ['No']

  return [optionLabel(question, rawValue)]
}

function formatAnswers(submission: GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListSubmissionDto) {
  const question = submission.questionId ? questionById.value.get(submission.questionId) : undefined
  return formatAnswerValue(submission.value, question)
}

function formatFollowUpValue(
  followUpValue: string | null | undefined,
  question: GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListQuestionDto | undefined,
) {
  if (!followUpValue)
    return []

  const parsed = parseJsonValue(followUpValue)
  if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
    return Object.entries(parsed as Record<string, string>)
      .filter(([, value]) => Boolean(value))
      .map(([optionValue, value]) => ({
        label: optionLabel(question, optionValue),
        value,
      }))
  }

  return [{ label: 'Additional details', value: followUpValue }]
}

function formatFollowUps(submission: GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListSubmissionDto) {
  const question = submission.questionId ? questionById.value.get(submission.questionId) : undefined
  return formatFollowUpValue(submission.followUpValue, question)
}

function addAnswerDetail(groups: Map<string, AnswerDetail[]>, category: string, detail: AnswerDetail) {
  const details = groups.get(category) ?? []
  details.push(detail)
  groups.set(category, details)
}

const answerGroups = computed<AnswerGroup[]>(() => {
  const groups = new Map<string, AnswerDetail[]>()
  const submissions = submissionsData.value?.submissions ?? []

  if (submissions.length > 0) {
    for (const submission of submissions) {
      const category = submission.category || 'Registration details'
      addAnswerDetail(groups, category, {
        key: submission.questionId ?? `${category}-${groups.get(category)?.length ?? 0}`,
        question: submission.questionText ?? submission.questionKey ?? 'Question',
        answers: formatAnswers(submission),
        followUps: formatFollowUps(submission),
      })
    }

    return Array.from(groups.entries()).map(([name, details]) => ({ name, details }))
  }

  for (const category of questionsData.value?.categories ?? []) {
    for (const question of category.questions ?? []) {
      const submission = question.currentSubmission
      if (!submission?.value)
        continue

      addAnswerDetail(groups, category.name ?? 'Registration details', {
        key: question.id ?? question.questionKey ?? `${category.name}-${groups.get(category.name ?? '')?.length ?? 0}`,
        question: question.questionText ?? question.questionKey ?? 'Question',
        answers: formatAnswerValue(submission.value, question),
        followUps: formatFollowUpValue(submission.followUpValue, question),
      })
    }
  }

  return Array.from(groups.entries()).map(([name, details]) => ({ name, details }))
})

const savedAnswersCount = computed(() => {
  const fallbackCount = answerGroups.value.reduce((count, group) => count + group.details.length, 0)
  const apiCount = submissionsData.value?.answeredQuestions ?? 0
  return apiCount > 0 ? apiCount : fallbackCount
})

const totalQuestionsCount = computed(() => {
  const apiCount = submissionsData.value?.totalQuestions ?? 0
  return apiCount > 0 ? apiCount : allQuestions.value.length
})

const isParticipantIdOpen = ref(false)
const participantQrCodeDataUrl = ref('')
const participantId = computed(() => user.value?.id ?? '')
const registrationId = computed(() => statusData.value?.registrationId ?? '')

async function showParticipantIdQrCode() {
  if (!participantId.value || !import.meta.client)
    return

  try {
    participantQrCodeDataUrl.value = await QRCode.toDataURL(JSON.stringify({
      kind: 'standalone-workshop-check-in',
      workshopId: workshopId.value,
      userId: participantId.value,
      registrationId: registrationId.value,
    }), {
      width: 320,
      margin: 2,
      color: {
        dark: '#000000',
        light: '#FFFFFF',
      },
    })
    isParticipantIdOpen.value = true
  }
  catch (error) {
    console.error('Failed to generate participant QR code', error)
    toast.add({
      title: 'Unable to show QR code',
      description: 'Please try again.',
      color: 'error',
    })
  }
}

</script>
