<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListQuestionDto,
  GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListSubmissionDto,
} from '@geekshacking/portal-sdk'
import {
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsGetEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsStatusEndpoint,
} from '@geekshacking/portal-sdk/hooks'

definePageMeta({
  auth: false,
})

const route = useRoute()
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
    timeZone: 'Asia/Singapore',
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
  const sameDay = start.toDateString() === end.toDateString()

  const dateFormatter = new Intl.DateTimeFormat(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    timeZone: 'Asia/Singapore',
  })

  const dateWithYearFormatter = new Intl.DateTimeFormat(undefined, {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    timeZone: 'Asia/Singapore',
  })

  const timeFormatter = new Intl.DateTimeFormat(undefined, {
    hour: 'numeric',
    minute: '2-digit',
    timeZone: 'Asia/Singapore',
  })

  return {
    dateLabel: sameDay
      ? dateWithYearFormatter.format(start)
      : `${dateFormatter.format(start)} to ${dateWithYearFormatter.format(end)}`,
    timeLabel: `${timeFormatter.format(start)} to ${timeFormatter.format(end)} SGT`,
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
</script>

<template>
  <div class="min-h-screen bg-(--ui-bg) text-(--ui-text)">
    <div class="mx-auto flex min-h-screen w-full max-w-5xl flex-col px-4 py-6 sm:px-6 lg:px-8">
      <div class="flex items-center justify-between border-b border-(--ui-border) pb-4">
        <NuxtLink
          to="/"
          class="text-sm font-medium tracking-[0.18em] text-(--ui-text-muted) uppercase transition-colors hover:text-(--ui-text-highlighted)"
        >
          GeeksHacking
        </NuxtLink>

        <div />
      </div>

      <div
        v-if="isLoading"
        class="flex flex-1 items-center justify-center"
      >
        <div class="space-y-3 text-center">
          <UIcon
            name="i-lucide-loader-circle"
            class="mx-auto size-8 animate-spin text-primary"
          />
          <p class="text-sm text-(--ui-text-muted)">
            Loading your registration...
          </p>
        </div>
      </div>

      <div
        v-else-if="workshopError || !workshop"
        class="flex flex-1 items-center justify-center"
      >
        <UCard class="max-w-xl">
          <div class="space-y-3 text-center">
            <p class="text-lg font-semibold text-(--ui-text-highlighted)">
              Workshop not found
            </p>
            <p class="text-sm text-(--ui-text-muted)">
              This workshop may no longer be public, or the link may be incorrect.
            </p>
          </div>
        </UCard>
      </div>

      <div
        v-else
        class="flex flex-1 justify-center py-8 lg:py-10"
      >
        <div class="w-full space-y-6">
          <UCard
            :ui="{ body: 'p-6 sm:p-8' }"
            class="overflow-hidden border-success/30 bg-success/5 shadow-2xl shadow-success/10 ring-1 ring-success/25"
          >
            <div class="space-y-7">
              <div class="flex flex-col gap-5 sm:flex-row sm:items-start sm:justify-between">
                <div class="flex gap-4">
                  <div class="flex size-14 shrink-0 items-center justify-center rounded-full bg-success text-white shadow-lg shadow-success/25 ring-4 ring-success/15">
                    <UIcon
                      name="i-lucide-check"
                      class="size-7"
                    />
                  </div>
                  <div class="space-y-2">
                    <UBadge
                      color="success"
                      variant="solid"
                      size="sm"
                    >
                      Signup successful
                    </UBadge>
                    <h1 class="text-3xl font-semibold tracking-tight text-(--ui-text-highlighted) sm:text-4xl">
                      You’re registered for {{ workshop.title }}
                    </h1>
                    <p class="max-w-2xl text-sm leading-6 text-(--ui-text-muted)">
                      Your signup is complete. A copy of your submitted details is below for reference.
                    </p>
                  </div>
                </div>
              </div>

              <UAlert
                color="success"
                variant="subtle"
                icon="i-lucide-check-circle-2"
                title="You are all set"
                description="If something looks incorrect, contact the workshop organizer."
              />

              <div class="grid gap-3 sm:grid-cols-2">
                <div class="rounded-2xl border border-(--ui-border) bg-(--ui-bg-elevated) p-4 shadow-lg shadow-black/5">
                  <p class="text-xs font-medium tracking-[0.14em] text-(--ui-text-muted) uppercase">
                    Registered
                  </p>
                  <p class="mt-2 text-sm font-semibold text-(--ui-text-highlighted)">
                    {{ registeredAtLabel }}
                  </p>
                </div>

                <div class="rounded-2xl border border-(--ui-border) bg-(--ui-bg-elevated) p-4 shadow-lg shadow-black/5">
                  <p class="text-xs font-medium tracking-[0.14em] text-(--ui-text-muted) uppercase">
                    Answers saved
                  </p>
                  <p class="mt-2 text-sm font-semibold text-(--ui-text-highlighted)">
                    {{ savedAnswersCount }} of {{ totalQuestionsCount }}
                  </p>
                </div>
              </div>

              <div class="grid gap-3 sm:grid-cols-2">
                <div class="rounded-2xl border border-(--ui-border) bg-(--ui-bg-elevated) p-4 shadow-lg shadow-black/5">
                  <p class="text-xs font-medium tracking-[0.14em] text-(--ui-text-muted) uppercase">
                    Date / time
                  </p>
                  <p class="mt-2 text-sm font-semibold leading-6 text-(--ui-text-highlighted)">
                    {{ formattedDateTime.dateLabel }}
                  </p>
                  <p class="mt-1 text-sm leading-6 text-(--ui-text-muted)">
                    {{ formattedDateTime.timeLabel }}
                  </p>
                </div>

                <div class="rounded-2xl border border-(--ui-border) bg-(--ui-bg-elevated) p-4 shadow-lg shadow-black/5">
                  <p class="text-xs font-medium tracking-[0.14em] text-(--ui-text-muted) uppercase">
                    Location
                  </p>
                  <p class="mt-2 text-sm font-semibold leading-6 text-(--ui-text-highlighted)">
                    {{ workshop.location || 'To be announced' }}
                  </p>
                </div>
              </div>

              <div class="flex flex-wrap gap-2">
                <UButton
                  color="neutral"
                  variant="outline"
                  icon="i-lucide-arrow-left"
                  @click="goToWorkshopDetails"
                >
                  View workshop details
                </UButton>
                <UButton
                  v-if="workshop.homepageUri"
                  :to="workshop.homepageUri"
                  external
                  target="_blank"
                  color="neutral"
                  variant="ghost"
                  icon="i-lucide-arrow-up-right"
                >
                  Visit event site
                </UButton>
              </div>

              <div
                v-if="calendarEvent"
                class="flex flex-col gap-3 rounded-2xl border border-(--ui-border) bg-(--ui-bg-elevated) p-4 shadow-lg shadow-black/5 sm:flex-row sm:items-center sm:justify-between"
              >
                <div class="space-y-1">
                  <p class="text-sm font-semibold text-(--ui-text-highlighted)">
                    Add this workshop to your calendar
                  </p>
                  <p class="text-sm leading-6 text-(--ui-text-muted)">
                    Save the date so you have the workshop time and location handy.
                  </p>
                </div>

                <div class="flex flex-col gap-2 sm:flex-row">
                  <UButton
                    :to="googleCalendarUrl"
                    external
                    target="_blank"
                    color="neutral"
                    variant="outline"
                    icon="i-lucide-calendar-plus"
                  >
                    Google Calendar
                  </UButton>
                  <UButton
                    color="neutral"
                    variant="soft"
                    icon="i-lucide-download"
                    @click="downloadCalendarFile"
                  >
                    Download .ics
                  </UButton>
                </div>
              </div>
            </div>
          </UCard>

          <div
            v-if="answerGroups.length"
            class="space-y-4"
          >
            <UCard
              v-for="group in answerGroups"
              :key="group.name"
              :ui="{ body: 'p-5 sm:p-6' }"
              class="border-(--ui-border) bg-(--ui-bg-elevated) shadow-xl shadow-black/8 ring-1 ring-(--ui-border)"
            >
              <div class="space-y-5">
                <div class="flex flex-wrap items-center justify-between gap-3">
                  <h2 class="text-base font-semibold text-(--ui-text-highlighted)">
                    {{ group.name }}
                  </h2>
                </div>

                <div class="divide-y divide-(--ui-border)">
                  <div
                    v-for="detail in group.details"
                    :key="detail.key"
                    class="grid gap-3 py-4 first:pt-0 last:pb-0 sm:grid-cols-[minmax(0,0.8fr)_minmax(0,1fr)] sm:gap-6"
                  >
                    <div class="space-y-1">
                      <p class="text-sm font-medium leading-6 text-(--ui-text-highlighted)">
                        {{ detail.question }}
                      </p>
                    </div>
                    <div class="space-y-2">
                      <div class="grid gap-2">
                        <div
                          v-for="answer in detail.answers"
                          :key="answer"
                          class="rounded-xl border border-(--ui-border) bg-(--ui-bg) px-3 py-2 text-sm leading-6 text-(--ui-text-highlighted) shadow-sm"
                        >
                          {{ answer }}
                        </div>
                      </div>

                      <div
                        v-if="detail.followUps.length"
                        class="space-y-2"
                      >
                        <div
                          v-for="followUp in detail.followUps"
                          :key="`${detail.key}-${followUp.label}`"
                          class="rounded-xl border border-(--ui-border) bg-(--ui-bg) p-3"
                        >
                          <p class="text-xs font-medium text-(--ui-text-muted)">
                            {{ followUp.label }}
                          </p>
                          <p class="mt-1 text-sm leading-6 text-(--ui-text-highlighted)">
                            {{ followUp.value }}
                          </p>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </UCard>
          </div>

          <UAlert
            v-else
            color="neutral"
            variant="soft"
            icon="i-lucide-info"
            title="No question responses"
            description="This workshop did not require any registration questions."
          />
        </div>
      </div>
    </div>
  </div>
</template>
