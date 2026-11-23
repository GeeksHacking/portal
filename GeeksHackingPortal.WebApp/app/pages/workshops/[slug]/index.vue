<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListQuestionDto,
  GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListSubmissionDto,
} from '@geekshacking/portal-sdk'
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
import QRCode from 'qrcode'
import { useProfileName } from '~/composables/useProfileName'
import { HACKATHON_TIME_ZONE, HACKATHON_TIME_ZONE_LABEL, isSameHackathonDay } from '~/utils/hackathon-date-time'
import { githubLoginUrl } from '~/utils/login-url'

/**
 * A workshop's public page, inside the portal shell (signed out, the sidebar offers "Sign in").
 * Details and the description on the left; the registration card on the right walks through
 * sign in → confirm your name → answer the questions → registered. While the questions are being
 * answered the form takes the main column. Registered people get their QR code and answers here.
 */
definePageMeta({
  auth: false,
  layout: 'portal',
})

const route = useRoute()
const config = useRuntimeConfig()
const queryClient = useQueryClient()
const toast = useGToast()

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
  title: workshop.value?.title ?? 'Workshop',
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

// Registration journey shown as a stepper: sign in -> confirm name -> answer questions -> done
const registrationSteps = ['Sign in', 'Confirm your name', 'Answer the questions', 'Registered']

// Index of the step the participant is currently on (steps before it are complete)
const currentStepIndex = computed(() => {
  switch (registrationState.value) {
    case 'registered':
      return 4
    case 'incomplete':
    case 'loading-registration':
      return 2
    case 'ready-to-join':
    case 'checking-status':
      return 1
    default:
      return 0
  }
})

const hasEnded = computed(() =>
  !!workshop.value?.endTime && new Date(workshop.value.endTime).getTime() <= Date.now(),
)

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

const joinError = ref('')

async function startRegistration() {
  if (!workshopId.value || !isProfileNameValid.value)
    return

  joinError.value = ''
  try {
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
  }
  catch (error) {
    console.error('Failed to start standalone workshop registration', error)
    joinError.value = getApiErrorMessage(error, 'We couldn\'t start your registration. Try again.')
  }
}

const loginUrl = computed(() => githubLoginUrl(config.public.api, route.fullPath))

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
      title: 'Couldn\'t show your QR code',
      description: 'Try again in a moment.',
      tone: 'err',
    })
  }
}

const registeredAtLabel = computed(() => {
  if (!statusData.value?.registeredAt)
    return 'Registration date unavailable'

  return `${new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone: HACKATHON_TIME_ZONE,
  }).format(new Date(statusData.value.registeredAt))} ${HACKATHON_TIME_ZONE_LABEL}`
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

const isBusy = computed(() => ['checking-auth', 'loading', 'checking-status', 'loading-registration'].includes(registrationState.value))
// the questions take the main column while they're being answered (even after the end, as before)
const showFormInMain = computed(() => registrationState.value === 'incomplete')
</script>

<template>
  <GPanel id="workshop">
    <template #header>
      <GPanelHeader
        title="Workshop"
        as="p"
      >
        <template #right>
          <GButton
            v-if="registrationState === 'registered'"
            variant="secondary"
            size="sm"
            icon="material-symbols:qr-code-2-rounded"
            label="My QR code"
            :disabled="!participantId"
            @click="showParticipantIdQrCode"
          />
        </template>
      </GPanelHeader>
    </template>

    <template #body>
      <div class="ws">
        <GLoading
          v-if="isLoadingWorkshop"
          label="Loading the workshop…"
        />

        <GEmptyState
          v-else-if="workshopError || !workshop"
          icon="material-symbols:search-off-rounded"
          title="Workshop not found"
          description="It may no longer be public, or the link may be wrong."
        >
          <template #actions>
            <GButton
              to="/dash"
              variant="secondary"
              icon="material-symbols:explore-rounded"
              label="Explore events"
            />
          </template>
        </GEmptyState>

        <template v-else>
          <!-- header -->
          <header class="ws-head">
            <div class="ws-head__tags">
              <GLabelStrip label="Workshop" />
              <GBadge
                v-if="registrationState === 'registered'"
                tone="ok"
                icon="material-symbols:check-circle-rounded"
                label="You're registered"
              />
              <GBadge
                v-else-if="registrationState === 'incomplete'"
                tone="pending"
                label="Registration incomplete"
              />
              <GBadge
                v-if="hasEnded"
                label="Ended"
              />
            </div>
            <h1 class="ws-head__title">
              {{ workshop.title }}
            </h1>
            <ul class="ws-head__meta">
              <li>
                <Icon
                  name="material-symbols:calendar-month-rounded"
                  aria-hidden="true"
                />
                <span>
                  <span class="ws-head__meta-main">{{ formattedDateTime.dateLabel }}</span>
                  <span class="ws-head__meta-sub">{{ formattedDateTime.timeLabel }}</span>
                </span>
              </li>
              <li>
                <Icon
                  name="material-symbols:location-on-rounded"
                  aria-hidden="true"
                />
                <span>
                  <span class="ws-head__meta-main">{{ workshop.location || 'Location to be announced' }}</span>
                  <span class="ws-head__meta-sub">In person</span>
                </span>
              </li>
              <li v-if="workshop.homepageUri">
                <Icon
                  name="material-symbols:link-rounded"
                  aria-hidden="true"
                />
                <a
                  :href="workshop.homepageUri"
                  target="_blank"
                  rel="noopener noreferrer"
                  class="ws-link"
                >Event site<span class="sr-only"> (opens in a new tab)</span></a>
              </li>
            </ul>
          </header>

          <div class="ws-grid">
            <div class="ws-main">
              <!-- the questions, while they're being answered -->
              <section
                v-if="showFormInMain"
                class="ws-card"
                aria-labelledby="ws-questions-title"
              >
                <h2
                  id="ws-questions-title"
                  class="ws-card__title"
                >
                  Answer the questions
                </h2>
                <LazyWorkshopsRegistrationForm
                  v-if="questionsData?.categories?.length"
                  :standalone-workshop-id="workshopId"
                  :workshop-title="workshop.title"
                  :questions="questionsData"
                />
                <GEmptyState
                  v-else
                  size="sm"
                  :bordered="false"
                  icon="material-symbols:help-rounded"
                  title="No questions yet"
                  description="The organisers haven't set up the registration questions. Check back soon."
                />
              </section>

              <section
                class="ws-card"
                aria-labelledby="ws-about-title"
              >
                <h2
                  id="ws-about-title"
                  class="ws-card__title"
                >
                  About this workshop
                </h2>
                <div class="ws-md">
                  <Suspense>
                    <Markdown
                      :value="workshop.description"
                      :options="{ autoClose: true, autoUnwrap: true }"
                    />
                  </Suspense>
                </div>
              </section>

              <!-- registered: your answers -->
              <section
                v-if="registrationState === 'registered'"
                class="ws-answers"
                aria-labelledby="ws-answers-title"
              >
                <h2
                  id="ws-answers-title"
                  class="ws-card__title"
                >
                  Your answers
                </h2>
                <template v-if="answerGroups.length">
                  <div
                    v-for="group in answerGroups"
                    :key="group.name"
                    class="ws-card"
                  >
                    <h3 class="ws-answers__group">
                      {{ group.name }}
                    </h3>
                    <dl class="ws-answers__list">
                      <div
                        v-for="detail in group.details"
                        :key="detail.key"
                        class="ws-answers__row"
                      >
                        <dt>{{ detail.question }}</dt>
                        <dd>
                          <span class="ws-answers__values">{{ detail.answers.join(', ') }}</span>
                          <span
                            v-for="followUp in detail.followUps"
                            :key="`${detail.key}-${followUp.label}`"
                            class="ws-answers__followup"
                          >
                            <span class="ws-answers__followup-label">{{ followUp.label }}</span>
                            {{ followUp.value }}
                          </span>
                        </dd>
                      </div>
                    </dl>
                  </div>
                </template>
                <GAlert
                  v-else
                  title="No answers to show"
                  description="This workshop didn't ask any registration questions."
                />
              </section>
            </div>

            <!-- registration card -->
            <aside
              class="ws-aside"
              aria-labelledby="ws-reg-title"
            >
              <div class="ws-card ws-reg">
                <div class="flex flex-col gap-1">
                  <h2
                    id="ws-reg-title"
                    class="ws-card__title"
                  >
                    {{ registrationState === 'registered' ? 'You\'re in' : 'Reserve your spot' }}
                  </h2>
                  <p
                    v-if="registrationState !== 'registered'"
                    class="ws-reg__lead"
                  >
                    Free to join. Sign in, confirm your name and answer a few questions.
                  </p>
                </div>

                <RegistrationSteps
                  :current="currentStepIndex"
                  :steps="registrationSteps"
                />

                <GAlert
                  v-if="hasEnded && registrationState !== 'registered'"
                  title="This workshop has ended"
                  description="Registration is closed."
                >
                  <template #actions>
                    <GButton
                      to="/dash"
                      size="sm"
                      label="Explore events"
                      trailing-icon="material-symbols:arrow-forward-rounded"
                    />
                  </template>
                </GAlert>

                <GLoading
                  v-if="isBusy"
                  label="Loading your registration…"
                />

                <!-- registered -->
                <template v-else-if="registrationState === 'registered'">
                  <dl class="ws-reg__facts">
                    <div>
                      <dt>Registered</dt>
                      <dd>{{ registeredAtLabel }}</dd>
                    </div>
                    <div>
                      <dt>Answers saved</dt>
                      <dd>{{ savedAnswersCount }} of {{ totalQuestionsCount }}</dd>
                    </div>
                  </dl>
                  <p class="ws-reg__lead">
                    Show your QR code when an organiser checks you in.
                  </p>
                  <GButton
                    variant="primary"
                    icon="material-symbols:qr-code-2-rounded"
                    label="Show my QR code"
                    :disabled="!participantId"
                    block
                    @click="showParticipantIdQrCode"
                  />
                </template>

                <template v-else-if="hasEnded" />

                <!-- step 1: sign in -->
                <template v-else-if="registrationState === 'signed-out'">
                  <p class="ws-reg__lead">
                    Sign in with GitHub to register. We'll bring you straight back here.
                  </p>
                  <GButton
                    :href="loginUrl"
                    variant="primary"
                    size="lg"
                    icon="fa6-brands:github"
                    label="Sign in to register"
                    block
                  />
                </template>

                <!-- step 2: confirm name -->
                <form
                  v-else-if="registrationState === 'ready-to-join'"
                  class="flex flex-col gap-4"
                  novalidate
                  @submit.prevent="startRegistration"
                >
                  <GFormField
                    label="First name"
                    required
                  >
                    <GInput
                      v-model="profileName.firstName"
                      autocomplete="given-name"
                    />
                  </GFormField>
                  <GFormField
                    label="Last name"
                    required
                    hint="Your GeeksHacking profile name. Organisers of every event you join see it, and changing it here updates it everywhere."
                  >
                    <GInput
                      v-model="profileName.lastName"
                      autocomplete="family-name"
                    />
                  </GFormField>
                  <GAlert
                    v-if="joinError"
                    tone="err"
                    title="Not started"
                    :description="joinError"
                  />
                  <GButton
                    type="submit"
                    variant="primary"
                    size="lg"
                    label="Continue to questions"
                    trailing-icon="material-symbols:arrow-forward-rounded"
                    :disabled="!isProfileNameValid"
                    :loading="joinMutation.isPending.value || isSavingProfileName"
                    loading-label="Starting"
                    block
                  />
                </form>

                <!-- step 3: questions are in the main column -->
                <p
                  v-else-if="registrationState === 'incomplete'"
                  class="ws-reg__lead"
                >
                  Answer the questions to finish. Your answers can't be changed after you submit.
                </p>
              </div>
            </aside>
          </div>
        </template>
      </div>
    </template>
  </GPanel>

  <GModal
    v-model:open="isParticipantIdOpen"
    title="Your QR code"
    description="Show this to an organiser when they check you in."
    size="sm"
  >
    <div class="flex flex-col items-center gap-3">
      <img
        v-if="participantQrCodeDataUrl"
        :src="participantQrCodeDataUrl"
        alt="Your workshop check-in QR code"
        class="h-auto w-full max-w-[280px] rounded-gh border-[1.5px] border-gh-line bg-white"
      >
      <p class="gh-small text-gh-muted">
        {{ workshop?.title }}
      </p>
    </div>
  </GModal>
</template>

<style scoped>
.ws {
  display: flex;
  flex-direction: column;
  gap: 24px;
  width: 100%;
  max-width: 72rem;
  margin: 0 auto;
}
.ws-head {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.ws-head__tags {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}
.ws-head__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: clamp(28px, 4.5vw, 44px);
  font-weight: 700;
  line-height: 1.1;
  overflow-wrap: anywhere;
}
.ws-head__meta {
  display: flex;
  flex-wrap: wrap;
  gap: 12px 32px;
  margin: 4px 0 0;
  padding: 0;
  list-style: none;
}
.ws-head__meta li {
  display: flex;
  align-items: flex-start;
  gap: 8px;
}
.ws-head__meta li > :deep(svg),
.ws-head__meta li > .iconify {
  flex: none;
  width: 20px;
  height: 20px;
  margin-top: 1px;
}
.ws-head__meta-main {
  display: block;
  font-weight: 600;
}
.ws-head__meta-sub {
  display: block;
  font-size: 14px;
  color: var(--gh-text-muted);
}
.ws-link {
  font-weight: 600;
  color: var(--gh-text);
  text-decoration: underline;
  text-underline-offset: 3px;
}
.ws-link:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: 2px;
  border-radius: 2px;
}

.ws-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 24px;
  align-items: start;
}
@media (min-width: 1024px) {
  .ws-grid { grid-template-columns: minmax(0, 1fr) 22rem; }
  .ws-aside {
    position: sticky;
    top: 0;
  }
}
/* below 1024px the registration card comes first: it's what people came to do */
@media (max-width: 1023px) {
  .ws-aside { order: -1; }
}
.ws-main {
  display: flex;
  flex-direction: column;
  gap: 24px;
  min-width: 0;
}
.ws-card {
  display: flex;
  flex-direction: column;
  gap: 16px;
  min-width: 0;
  padding: 20px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
@media (min-width: 640px) {
  .ws-card { padding: 24px; }
}
.ws-card__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 22px;
  font-weight: 700;
  line-height: 1.2;
}
.ws-reg__lead {
  margin: 0;
  font-size: 14px;
  color: var(--gh-text-muted);
}
.ws-reg__facts {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
  margin: 0;
}
.ws-reg__facts dt,
.ws-answers__row dt {
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.ws-reg__facts dd {
  margin: 2px 0 0;
  font-weight: 600;
}

.ws-answers {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.ws-answers__group {
  margin: 0;
  font-size: 16px;
  font-weight: 700;
}
.ws-answers__list {
  display: flex;
  flex-direction: column;
  margin: 0;
}
.ws-answers__row {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 4px 24px;
  padding: 12px 0;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.ws-answers__row:first-child {
  padding-top: 0;
  border-top: 0;
}
.ws-answers__row:last-child {
  padding-bottom: 0;
}
@media (min-width: 768px) {
  .ws-answers__row { grid-template-columns: minmax(0, 2fr) minmax(0, 3fr); }
  .ws-answers__row dt {
    font-size: 14px;
    font-weight: 600;
    letter-spacing: normal;
    text-transform: none;
    color: var(--gh-text);
  }
}
.ws-answers__row dd {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin: 0;
  overflow-wrap: anywhere;
}
.ws-answers__followup {
  padding-left: 12px;
  border-left: 3px solid var(--gh-line-soft);
  font-size: 14px;
}
.ws-answers__followup-label {
  display: block;
  font-size: 12px;
  color: var(--gh-text-muted);
}

/* workshop description (Markdown). The renderer wraps the content in its own element, so
   spacing goes between siblings rather than on a grid around them. */
.ws-md {
  max-width: 70ch;
  line-height: 1.7;
}
.ws-md :deep(:is(p, ul, ol, h1, h2, h3, h4, pre, blockquote)) {
  margin: 0;
}
.ws-md :deep(* + :is(p, ul, ol, pre, blockquote)) {
  margin-top: 0.9em;
}
.ws-md :deep(* + :is(h1, h2, h3, h4)) {
  margin-top: 1.4em;
}
.ws-md :deep(:is(h1, h2, h3, h4) + *) {
  margin-top: 0.4em;
}
.ws-md :deep(:is(h1, h2, h3, h4)) {
  font-family: var(--font-display);
  font-weight: 700;
  line-height: 1.25;
}
.ws-md :deep(h1) { font-size: 1.4em; }
.ws-md :deep(h2) { font-size: 1.2em; }
.ws-md :deep(h3),
.ws-md :deep(h4) { font-size: 1.05em; }
.ws-md :deep(ul),
.ws-md :deep(ol) {
  padding-left: 1.25rem;
}
.ws-md :deep(ul) { list-style: disc; }
.ws-md :deep(ol) { list-style: decimal; }
.ws-md :deep(li + li) {
  margin-top: 0.35rem;
}
.ws-md :deep(a) {
  color: var(--gh-text);
  font-weight: 600;
  text-decoration: underline;
  text-underline-offset: 3px;
}
.ws-md :deep(strong) {
  font-weight: 700;
}
.ws-md :deep(code) {
  padding: 0.1rem 0.4rem;
  border: var(--gh-border-width) solid var(--gh-line-soft);
  border-radius: var(--radius-gh-sm);
  background: var(--gh-surface-sunken);
  font-family: var(--font-mono);
  font-size: 0.9em;
}
</style>
