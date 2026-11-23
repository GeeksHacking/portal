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

const formattedDateTime = computed(() => formatWorkshopWhen(workshop.value?.startTime, workshop.value?.endTime))

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

const participantId = computed(() => user.value?.id ?? '')
const registrationId = computed(() => statusData.value?.registrationId ?? '')

const registeredAtLabel = computed(() => formatWorkshopRegisteredAt(statusData.value?.registeredAt))

const { answerGroups, savedAnswersCount, totalQuestionsCount } = useWorkshopAnswers(questionsData, submissionsData)

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
          <WorkshopsCheckinQr
            v-if="registrationState === 'registered' && workshop"
            :workshop-id="workshopId"
            :user-id="participantId"
            :registration-id="registrationId"
            :workshop-title="workshop.title"
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
                <WorkshopsAnswerList :groups="answerGroups" />
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
                  <WorkshopsCheckinQr
                    :workshop-id="workshopId"
                    :user-id="participantId"
                    :registration-id="registrationId"
                    :workshop-title="workshop.title"
                    label="Show my QR code"
                    variant="primary"
                    size="md"
                    block
                  />
                  <GButton
                    :to="`/workshops/${slug}/registered`"
                    variant="ghost"
                    icon="material-symbols:edit-calendar-rounded"
                    label="Manage registration"
                    block
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
.ws-reg__facts dt {
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
