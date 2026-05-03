<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListQuestionDto,
  GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListSubmissionDto,
} from '@geekshacking/portal-sdk'
import { Comark } from '@comark/vue'
import {
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsGetEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsJoinEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsStatusEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import QRCode from 'qrcode'

definePageMeta({
  auth: false,
})

const route = useRoute()
const config = useRuntimeConfig()
const queryClient = useQueryClient()
const toast = useToast()

const slug = computed(() => (route.params.slug as string | undefined) ?? '')

const { data: workshop, isLoading: isLoadingWorkshop, error: workshopError } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsGetEndpoint(slug)
const { data: user, isSuccess: authResolved, isError: authErrored } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint({
  query: {
    retry: false,
    staleTime: 0,
    gcTime: 0,
  },
})

const workshopId = computed(() => workshop.value?.id ?? '')

const { data: statusData, isLoading: isLoadingStatus } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsStatusEndpoint(
  workshopId,
  { query: { enabled: computed(() => !!workshopId.value && !!user.value) } },
)

const { data: questionsData, isLoading: isLoadingQuestions } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListEndpoint(
  workshopId,
  { query: { enabled: computed(() => !!workshopId.value && statusData.value?.isRegistered === true) } },
)

const { data: submissionsData, isLoading: isLoadingSubmissions } = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListEndpoint(
  workshopId,
  { query: { enabled: computed(() => !!workshopId.value && statusData.value?.isRegistered === true) } },
)

const joinMutation = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsJoinEndpoint()

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

async function startRegistration() {
  if (!workshopId.value)
    return

  try {
    await joinMutation.mutateAsync({ standaloneWorkshopId: workshopId.value })
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
      title: 'Registration started',
      description: 'Your registration form is ready.',
      color: 'success',
    })
  }
  catch (error) {
    console.error('Failed to start standalone workshop registration', error)
    toast.add({
      title: 'Unable to start registration',
      description: 'Please try again.',
      color: 'error',
    })
  }
}

const loginUrl = computed(() =>
  `${config.public.api}/auth/login?redirect_uri=${encodeURIComponent(route.fullPath)}`,
)

const isParticipantIdOpen = ref(false)
const participantQrCodeDataUrl = ref('')

const participantId = computed(() => statusData.value?.registrationId ?? '')

async function showParticipantIdQrCode() {
  if (!participantId.value || !import.meta.client)
    return

  try {
    participantQrCodeDataUrl.value = await QRCode.toDataURL(participantId.value, {
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

const registeredAtLabel = computed(() => {
  if (!statusData.value?.registeredAt)
    return 'Registration date unavailable'

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone: 'Asia/Singapore',
  }).format(new Date(statusData.value.registeredAt))
})

const questionById = computed(() => {
  const questions = questionsData.value?.categories?.flatMap(category => category.questions ?? []) ?? []
  return new Map(questions.filter(question => question.id).map(question => [question.id as string, question]))
})

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

  if (Array.isArray(parsed))
    return parsed.map(item => optionLabel(question, String(item)))

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

interface AnswerDetail {
  key: string
  question: string
  answers: string[]
  followUps: { label: string, value: string }[]
}

interface AnswerGroup {
  name: string
  details: AnswerDetail[]
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
  }
  else {
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
    <div class="mx-auto flex min-h-screen w-full max-w-7xl flex-col px-4 py-6 sm:px-6 lg:px-8">
      <div class="flex items-center justify-between border-b border-(--ui-border) pb-4">
        <NuxtLink
          to="/"
          class="text-sm font-medium tracking-[0.18em] text-(--ui-text-muted) uppercase transition-colors hover:text-(--ui-text-highlighted)"
        >
          GeeksHacking
        </NuxtLink>
        <div class="flex items-center gap-3">
          <UBadge
            v-if="workshop?.isPublished"
            size="sm"
            color="success"
            variant="subtle"
          >
            Open for registration
          </UBadge>
          <UButton
            v-if="!user && (authResolved || authErrored)"
            :to="loginUrl"
            external
            size="sm"
            variant="outline"
            color="neutral"
            icon="i-lucide-github"
          >
            Sign in
          </UButton>
        </div>
      </div>

      <div
        v-if="isLoadingWorkshop"
        class="flex flex-1 items-center justify-center"
      >
        <div class="space-y-3 text-center">
          <UIcon
            name="i-lucide-loader-circle"
            class="mx-auto size-8 animate-spin text-primary"
          />
          <p class="text-sm text-(--ui-text-muted)">
            Loading workshop details...
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
        class="flex flex-1 justify-center py-6 lg:py-8"
      >
        <div class="w-full max-w-6xl space-y-5 lg:space-y-6">
          <section class="reveal-up reveal-delay-1 overflow-hidden rounded-[2rem] border border-(--ui-border) bg-(--ui-bg-elevated) shadow-sm backdrop-blur transition-transform duration-300 ease-out hover:-translate-y-0.5">
            <div class="grid gap-5 p-5 sm:p-7 lg:gap-6 lg:p-10">
              <div class="flex flex-wrap items-center gap-3">
                <UBadge
                  color="warning"
                  variant="soft"
                  size="sm"
                >
                  Workshop
                </UBadge>
                <UBadge
                  v-if="registrationState === 'registered'"
                  color="success"
                  variant="subtle"
                  size="sm"
                >
                  Registered
                </UBadge>
                <UBadge
                  v-else-if="registrationState === 'incomplete'"
                  color="warning"
                  variant="subtle"
                  size="sm"
                >
                  Registration incomplete
                </UBadge>
              </div>

              <div class="space-y-4">
                <h1 class="max-w-4xl text-3xl font-semibold tracking-tight text-(--ui-text-highlighted) sm:text-5xl">
                  {{ workshop.title }}
                </h1>
                <div class="max-w-3xl text-sm leading-7 text-(--ui-text-muted) sm:text-base lg:text-lg">
                  <Suspense>
                    <Comark
                      :markdown="workshop.description"
                      :options="{ autoClose: true, autoUnwrap: true }"
                      class="workshop-markdown"
                    />
                  </Suspense>
                </div>
              </div>
            </div>
          </section>

          <div
            class="grid gap-6 lg:gap-8"
            :class="registrationState === 'registered' ? 'lg:grid-cols-1' : 'lg:grid-cols-[minmax(0,0.82fr)_minmax(22rem,0.64fr)]'"
          >
            <div class="space-y-5 lg:space-y-6">
              <div class="grid gap-3 sm:grid-cols-2">
                <UCard
                  :ui="{ body: 'p-4' }"
                  class="reveal-up reveal-delay-2 border-(--ui-border) bg-(--ui-bg-elevated) transition-all duration-300 ease-out hover:-translate-y-0.5"
                >
                  <p class="text-xs font-medium tracking-[0.14em] text-(--ui-text-muted) uppercase">
                    Date / time
                  </p>
                  <p class="mt-2 text-sm font-semibold leading-6 text-(--ui-text-highlighted)">
                    {{ formattedDateTime.dateLabel }}
                  </p>
                  <p class="mt-1 text-sm leading-6 text-(--ui-text-muted)">
                    {{ formattedDateTime.timeLabel }}
                  </p>
                </UCard>

                <UCard
                  :ui="{ body: 'p-4' }"
                  class="reveal-up reveal-delay-2 border-(--ui-border) bg-(--ui-bg-elevated) transition-all duration-300 ease-out hover:-translate-y-0.5"
                >
                  <p class="text-xs font-medium tracking-[0.14em] text-(--ui-text-muted) uppercase">
                    Location
                  </p>
                  <p class="mt-2 text-sm font-medium leading-6 text-(--ui-text-highlighted)">
                    {{ workshop.location || 'To be announced' }}
                  </p>
                </UCard>
              </div>

              <div class="flex flex-wrap gap-3">
                <UButton
                  v-if="workshop.homepageUri"
                  :to="workshop.homepageUri"
                  external
                  target="_blank"
                  size="lg"
                  color="neutral"
                  variant="outline"
                  icon="i-lucide-arrow-up-right"
                >
                  Visit event site
                </UButton>
                <div
                  v-if="registrationState !== 'registered'"
                  class="rounded-2xl border border-(--ui-border) bg-(--ui-bg-elevated) px-4 py-3 text-sm leading-6 text-(--ui-text-muted)"
                >
                  Sign in with GitHub to complete registration. Details are locked after signup.
                </div>
              </div>

              <section
                v-if="registrationState === 'registered'"
                class="reveal-up reveal-delay-3 space-y-5"
              >
                <div class="grid gap-3 sm:grid-cols-3">
                  <div class="rounded-md border border-(--ui-border) bg-(--ui-bg-elevated) p-4">
                    <p class="text-xs font-medium tracking-[0.14em] text-(--ui-text-muted) uppercase">
                      Registration status
                    </p>
                    <div class="mt-2 flex items-center gap-2 text-sm font-semibold text-(--ui-text-highlighted)">
                      <UIcon
                        name="i-lucide-check-circle-2"
                        class="size-4 text-success"
                      />
                      <span>Complete</span>
                    </div>
                  </div>

                  <div class="rounded-md border border-(--ui-border) bg-(--ui-bg-elevated) p-4">
                    <p class="text-xs font-medium tracking-[0.14em] text-(--ui-text-muted) uppercase">
                      Registered
                    </p>
                    <p class="mt-2 text-sm font-semibold leading-6 text-(--ui-text-highlighted)">
                      {{ registeredAtLabel }}
                    </p>
                  </div>

                  <div class="rounded-md border border-(--ui-border) bg-(--ui-bg-elevated) p-4">
                    <p class="text-xs font-medium tracking-[0.14em] text-(--ui-text-muted) uppercase">
                      Answers saved
                    </p>
                    <p class="mt-2 text-sm font-semibold text-(--ui-text-highlighted)">
                      {{ savedAnswersCount }} of {{ totalQuestionsCount }}
                    </p>
                  </div>
                </div>

                <div class="flex flex-wrap items-center gap-3 rounded-md border border-(--ui-border) bg-(--ui-bg-elevated) p-4">
                  <div class="min-w-0 flex-1">
                    <p class="text-sm font-semibold text-(--ui-text-highlighted)">
                      Participant ID
                    </p>
                    <p class="text-sm leading-6 text-(--ui-text-muted)">
                      Show this ID when an organizer needs to scan or verify your registration.
                    </p>
                  </div>
                  <UButton
                    icon="i-lucide-qr-code"
                    color="neutral"
                    variant="outline"
                    :disabled="!participantId"
                    @click="showParticipantIdQrCode"
                  >
                    Show QR code
                  </UButton>
                </div>

                <div
                  v-if="answerGroups.length"
                  class="space-y-4"
                >
                  <UCard
                    v-for="group in answerGroups"
                    :key="group.name"
                    :ui="{ body: 'p-5 sm:p-6' }"
                    class="border-(--ui-border) bg-(--ui-bg-elevated)"
                  >
                    <div class="space-y-4">
                      <h2 class="text-base font-semibold text-(--ui-text-highlighted)">
                        {{ group.name }}
                      </h2>

                      <div class="divide-y divide-(--ui-border)">
                        <div
                          v-for="detail in group.details"
                          :key="detail.key"
                          class="grid gap-3 py-4 first:pt-0 last:pb-0 sm:grid-cols-[minmax(0,0.8fr)_minmax(0,1fr)] sm:gap-6"
                        >
                          <p class="text-sm font-medium leading-6 text-(--ui-text-highlighted)">
                            {{ detail.question }}
                          </p>
                          <div class="space-y-2">
                            <div class="flex flex-wrap gap-2">
                              <UBadge
                                v-for="answer in detail.answers"
                                :key="answer"
                                color="neutral"
                                variant="soft"
                                class="max-w-full whitespace-normal text-left"
                              >
                                {{ answer }}
                              </UBadge>
                            </div>

                            <div
                              v-for="followUp in detail.followUps"
                              :key="`${detail.key}-${followUp.label}`"
                              class="rounded-md border border-(--ui-border) bg-(--ui-bg) p-3"
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
              </section>
            </div>

            <aside
              v-if="registrationState !== 'registered'"
              class="reveal-up reveal-delay-3 lg:sticky lg:top-6 lg:self-start"
            >
              <UCard
                :ui="{ body: 'p-5 sm:p-6 lg:p-7' }"
                class="border-(--ui-border) bg-(--ui-bg) shadow-2xl shadow-black/10 ring-1 ring-(--ui-border) transition-transform duration-300 ease-out hover:-translate-y-0.5 hover:shadow-black/15"
              >
                <div class="space-y-4 sm:space-y-5">
                  <div class="space-y-2">
                    <p class="text-xs font-medium tracking-[0.16em] text-(--ui-text-muted) uppercase">
                      Registration
                    </p>
                    <h2 class="text-2xl font-semibold text-(--ui-text-highlighted)">
                      Reserve your spot
                    </h2>
                    <p class="text-sm leading-6 text-(--ui-text-muted)">
                      Complete the signup form with your GitHub account. Your progress is saved when you submit.
                    </p>
                  </div>

                  <div
                    v-if="registrationState === 'checking-auth' || registrationState === 'checking-status' || registrationState === 'loading-registration'"
                    class="rounded-2xl border border-(--ui-border) bg-(--ui-bg-elevated) p-5 text-sm text-(--ui-text-muted) shadow-lg shadow-black/5 transition-all duration-300 ease-out"
                  >
                    <div class="flex items-center gap-3">
                      <UIcon
                        name="i-lucide-loader-circle"
                        class="size-5 animate-spin text-primary"
                      />
                      <span>Preparing your registration experience...</span>
                    </div>
                  </div>

                  <div
                    v-else-if="registrationState === 'signed-out'"
                    class="space-y-4 rounded-2xl border border-(--ui-border) bg-(--ui-bg-elevated) p-5 shadow-lg shadow-black/5"
                  >
                    <p class="text-sm leading-6 text-(--ui-text-muted)">
                      You can review the workshop details without signing in. To register, sign in first and we’ll bring you straight back here.
                    </p>
                    <UButton
                      :to="loginUrl"
                      external
                      block
                      size="lg"
                      icon="i-lucide-github"
                    >
                      Sign in to register
                    </UButton>
                  </div>

                  <div
                    v-else-if="registrationState === 'ready-to-join'"
                    class="space-y-4 rounded-2xl border border-(--ui-border) bg-(--ui-bg-elevated) p-5 shadow-lg shadow-black/5"
                  >
                    <p class="text-sm leading-6 text-(--ui-text-muted)">
                      You’re signed in. Start registration to unlock the workshop questions and save your spot.
                    </p>
                    <UButton
                      block
                      size="lg"
                      icon="i-lucide-ticket"
                      :loading="joinMutation.isPending.value"
                      @click="startRegistration"
                    >
                      Start registration
                    </UButton>
                  </div>

                  <div
                    v-else
                    class="space-y-5"
                  >
                    <div
                      v-if="questionsData?.categories?.length"
                      class="rounded-[1.5rem] bg-transparent"
                    >
                      <LazyWorkshopsRegistrationForm
                        :standalone-workshop-id="workshopId"
                        :workshop-title="workshop.title"
                        :questions="questionsData"
                      />
                    </div>

                    <div
                      v-else
                      class="rounded-2xl border border-dashed border-(--ui-border) bg-(--ui-bg-elevated) p-5 text-sm text-(--ui-text-muted) shadow-lg shadow-black/5"
                    >
                      No registration questions have been configured for this workshop yet.
                    </div>
                  </div>
                </div>
              </UCard>
            </aside>
          </div>
        </div>
      </div>
    </div>

    <UModal
      v-model:open="isParticipantIdOpen"
      title="Participant QR Code"
      description="Show this to an organizer when they need to scan your workshop registration."
    >
      <template #body>
        <div class="space-y-4">
          <div class="rounded-md border border-(--ui-border) bg-(--ui-bg-elevated) p-4 text-center">
            <p class="text-xs font-medium tracking-[0.14em] text-(--ui-text-muted) uppercase">
              {{ workshop?.title }}
            </p>
            <div class="mt-4 flex justify-center">
              <img
                v-if="participantQrCodeDataUrl"
                :src="participantQrCodeDataUrl"
                alt="Participant registration QR code"
                class="size-72 max-w-full rounded-md bg-white p-3"
              >
              <div
                v-else
                class="flex size-72 max-w-full items-center justify-center rounded-md border border-dashed border-(--ui-border) text-sm text-(--ui-text-muted)"
              >
                QR code unavailable
              </div>
            </div>
          </div>
        </div>
      </template>
    </UModal>
  </div>
</template>

<style scoped>
@media (prefers-reduced-motion: no-preference) {
  .reveal-up {
    animation: reveal-up 560ms cubic-bezier(0.22, 1, 0.36, 1) both;
  }

  .reveal-delay-1 {
    animation-delay: 60ms;
  }

  .reveal-delay-2 {
    animation-delay: 140ms;
  }

  .reveal-delay-3 {
    animation-delay: 220ms;
  }
}

@keyframes reveal-up {
  from {
    opacity: 0;
    transform: translateY(18px);
  }

  to {
    opacity: 1;
    transform: translateY(0);
  }
}

:deep(.workshop-markdown) {
  display: grid;
  gap: 0.9rem;
}

:deep(.workshop-markdown p) {
  margin: 0;
}

:deep(.workshop-markdown h1),
:deep(.workshop-markdown h2),
:deep(.workshop-markdown h3),
:deep(.workshop-markdown h4) {
  margin: 0;
  color: color-mix(in oklab, currentColor 88%, black 12%);
  font-weight: 600;
  letter-spacing: -0.02em;
}

:deep(.workshop-markdown h1) {
  font-size: 1.45em;
}

:deep(.workshop-markdown h2) {
  font-size: 1.2em;
}

:deep(.workshop-markdown h3),
:deep(.workshop-markdown h4) {
  font-size: 1.05em;
}

:deep(.workshop-markdown ul),
:deep(.workshop-markdown ol) {
  margin: 0;
  padding-left: 1.25rem;
}

:deep(.workshop-markdown li + li) {
  margin-top: 0.35rem;
}

:deep(.workshop-markdown a) {
  color: inherit;
  text-decoration: underline;
  text-decoration-color: color-mix(in oklab, currentColor 28%, transparent);
  text-underline-offset: 0.18em;
}

:deep(.workshop-markdown strong) {
  color: color-mix(in oklab, currentColor 92%, black 8%);
  font-weight: 600;
}

:deep(.workshop-markdown code) {
  border: 1px solid rgb(0 0 0 / 0.08);
  border-radius: 0.65rem;
  background: rgb(255 255 255 / 0.82);
  padding: 0.1rem 0.45rem;
  font-size: 0.92em;
}
</style>
