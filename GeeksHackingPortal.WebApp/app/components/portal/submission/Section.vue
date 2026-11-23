<script setup lang="ts">
import {
  geeksHackingPortalApiEndpointsParticipantsHackathonSubmissionsListEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonChallengesListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonSubmissionsCreateEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonSubmissionsGetEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonSubmissionsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonTeamsGetMineEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import { computed, nextTick, reactive, ref } from 'vue'
import { HACKATHON_TIME_ZONE, HACKATHON_TIME_ZONE_LABEL } from '~/utils/hackathon-date-time'

/**
 * The event's Submission page. One submission per team, final once sent, so the form ends in a
 * review step. States: event not started / no team / submitted (read-only) / not submitted
 * (form, disabled until the window opens; gone once it closes).
 */
const route = useRoute()
const routeHackathonId = computed(() => (route.params.hackathonId as string) ?? '')
const { data: hackathon } = useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint({ path: computed(() => ({ hackathonIdOrShortCode: routeHackathonId.value })) })
const hackathonId = computed(() => hackathon.value?.id ?? '')
const shortCode = computed(() => hackathon.value?.shortCode ?? routeHackathonId.value)
const toast = useGToast()
const queryClient = useQueryClient()

const { data: statusData, isLoading: isLoadingStatus } = useGeeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpoint({ path: computed(() => ({ hackathonId: hackathonId.value })) }, { query: { enabled: computed(() => !!hackathonId.value) } })
const isParticipant = computed(() => !!statusData.value?.isParticipant)

const { data: teamData, isLoading: isLoadingTeam } = useGeeksHackingPortalApiEndpointsParticipantsHackathonTeamsGetMineEndpoint({ path: computed(() => ({ hackathonId: hackathonId.value })) }, { query: { enabled: computed(() => !!hackathonId.value && isParticipant.value) } })
const teamId = computed(() => teamData.value?.id ?? '')
const hasTeam = computed(() => !!teamId.value)
const challengeId = computed(() => teamData.value?.challengeId ?? null)

const submissionsPath = computed(() => ({ hackathonId: hackathonId.value, teamId: teamId.value }))
const { data: submissionsData, isLoading: isLoadingSubmissions } = useGeeksHackingPortalApiEndpointsParticipantsHackathonSubmissionsListEndpoint({ path: submissionsPath }, { query: { enabled: computed(() => !!hackathonId.value && hasTeam.value) } })
const listedSubmission = computed(() => submissionsData.value?.submissions?.[0] ?? null)

// The list endpoint returns its links as null (its SQL projection drops the Uri columns), so the
// links come from the single-submission endpoint, which maps the full row.
const submissionId = computed(() => listedSubmission.value?.id ?? '')
const { data: submissionDetail, isLoading: isLoadingDetail } = useGeeksHackingPortalApiEndpointsParticipantsHackathonSubmissionsGetEndpoint({ path: computed(() => ({ hackathonId: hackathonId.value, submissionId: submissionId.value })) }, { query: { enabled: computed(() => !!hackathonId.value && !!submissionId.value) } })
const submission = computed(() => {
  const listed = listedSubmission.value
  if (!listed)
    return null
  const detail = submissionDetail.value
  return {
    ...listed,
    repoUri: listed.repoUri ?? detail?.repositoryUri ?? null,
    slidesUri: listed.slidesUri ?? detail?.slidesUri ?? null,
    demoUri: listed.demoUri ?? detail?.demoUri ?? null,
  }
})

const { data: challengesData } = useGeeksHackingPortalApiEndpointsParticipantsHackathonChallengesListEndpoint({ path: computed(() => ({ hackathonId: hackathonId.value })) }, { query: { enabled: computed(() => !!hackathonId.value && hasTeam.value) } })
function challengeTitle(id: string | null | undefined) {
  const challenge = challengesData.value?.challenges?.find(c => c.id === id)
  if (!challenge?.title)
    return null
  return challenge.sponsor ? `${challenge.title} (${challenge.sponsor})` : challenge.title
}
const teamChallenge = computed(() => challengeTitle(challengeId.value))
const submittedChallenge = computed(() => challengeTitle(submission.value?.challengeId))

const isLoading = computed(() => !hackathonId.value || isLoadingStatus.value || (isParticipant.value && isLoadingTeam.value) || (hasTeam.value && isLoadingSubmissions.value) || (!!submissionId.value && isLoadingDetail.value))

// ---- dates (shown in event time) -------------------------------------------------------------
const dateFormatter = new Intl.DateTimeFormat('en-SG', { timeZone: HACKATHON_TIME_ZONE, weekday: 'short', day: 'numeric', month: 'short', hour: 'numeric', minute: '2-digit' })
function toDate(value: string | Date | null | undefined) {
  return value ? new Date(value) : null
}
function formatWhen(date: Date | null) {
  return date ? `${dateFormatter.format(date)} ${HACKATHON_TIME_ZONE_LABEL}` : ''
}

const eventStart = computed(() => toDate(hackathon.value?.eventStartDate))
const opensAt = computed(() => toDate(hackathon.value?.submissionsStartDate))
const closesAt = computed(() => toDate(hackathon.value?.submissionsEndDate))
const hasHackathonStarted = computed(() => !eventStart.value || new Date() >= eventStart.value)

const windowState = computed<'upcoming' | 'open' | 'closed'>(() => {
  const now = new Date()
  if (!opensAt.value || now < opensAt.value)
    return 'upcoming'
  if (closesAt.value && now > closesAt.value)
    return 'closed'
  return 'open'
})
const isOpen = computed(() => windowState.value === 'open')

// ---- form ------------------------------------------------------------------------------------
type Field = 'title' | 'summary' | 'repoUri' | 'slidesUri' | 'demoUri'
const form = reactive<Record<Field, string>>({ title: '', summary: '', repoUri: '', slidesUri: '', demoUri: '' })
const errors = reactive<Record<Field, string>>({ title: '', summary: '', repoUri: '', slidesUri: '', demoUri: '' })
const fields: Field[] = ['title', 'summary', 'repoUri', 'slidesUri', 'demoUri']

function isHttpUrl(value: string) {
  try {
    const url = new URL(value)
    return url.protocol === 'http:' || url.protocol === 'https:'
  }
  catch {
    return false
  }
}

function validate() {
  const title = form.title.trim()
  const summary = form.summary.trim()
  const repo = form.repoUri.trim()
  const slides = form.slidesUri.trim()
  const demo = form.demoUri.trim()
  errors.title = title ? '' : 'Give your project a title.'
  errors.summary = summary ? '' : 'Describe your project in a few sentences.'
  errors.repoUri = !repo ? 'Add the link to your code repository.' : isHttpUrl(repo) ? '' : 'Enter a full link starting with https://'
  errors.slidesUri = !slides ? 'Add the link to your slides.' : isHttpUrl(slides) ? '' : 'Enter a full link starting with https://'
  errors.demoUri = !demo || isHttpUrl(demo) ? '' : 'Enter a full link starting with https://'
  return fields.every(field => !errors[field])
}

const formEl = ref<HTMLFormElement | null>(null)
async function focusFirstError() {
  await nextTick()
  formEl.value?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus()
}

const showReview = ref(false)
const submitError = ref('')

function review() {
  submitError.value = ''
  if (!isOpen.value || !challengeId.value)
    return
  if (!validate()) {
    focusFirstError()
    return
  }
  showReview.value = true
}

const createSubmission = useGeeksHackingPortalApiEndpointsParticipantsHackathonSubmissionsCreateEndpoint()

async function confirmSubmit() {
  if (!challengeId.value || !hackathonId.value || !teamId.value)
    return
  submitError.value = ''
  try {
    await createSubmission.mutateAsync({
      path: { hackathonId: hackathonId.value, teamId: teamId.value },
      body: {
        challengeId: challengeId.value,
        title: form.title.trim(),
        summary: form.summary.trim(),
        repoUri: form.repoUri.trim(),
        slidesUri: form.slidesUri.trim(),
        demoUri: form.demoUri.trim() || undefined,
      },
    })
    await queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsParticipantsHackathonSubmissionsListEndpointQueryKey({ path: submissionsPath }) })
    showReview.value = false
    toast.add({ title: 'Project submitted', description: `${teamData.value?.name ?? 'Your team'}'s submission is in. Good luck!`, tone: 'ok' })
  }
  catch (error) {
    // field problems (e.g. the repository check) go back to the form, next to the field
    const bag = getApiValidationErrors(error)
    let onField = false
    for (const field of fields) {
      const message = getApiFieldError(bag, field)
      if (message) {
        errors[field] = message
        onField = true
      }
    }
    if (onField) {
      showReview.value = false
      focusFirstError()
      return
    }
    submitError.value = getApiErrorMessage(error, 'We couldn\'t send your submission. Try again.')
  }
}

// The API stores a placeholder when no demo link was given: don't show it as a real link
function isPlaceholder(uri: string | null | undefined) {
  if (!uri)
    return true
  try {
    return new URL(uri).hostname === 'example.com'
  }
  catch {
    return false
  }
}
const submittedLinks = computed(() => {
  const s = submission.value
  if (!s)
    return []
  return [
    { label: 'Repository', icon: 'material-symbols:code-rounded', href: s.repoUri },
    { label: 'Slides', icon: 'material-symbols:slideshow-rounded', href: s.slidesUri },
    { label: 'Demo', icon: 'material-symbols:play-circle-rounded', href: s.demoUri },
  ].filter((link): link is { label: string, icon: string, href: string } => !isPlaceholder(link.href))
})
</script>

<template>
  <section
    class="sub"
    aria-label="Submission"
  >
    <GLoading
      v-if="isLoading"
      label="Loading your submission…"
    />

    <GEmptyState
      v-else-if="!hasHackathonStarted"
      icon="material-symbols:schedule-rounded"
      title="Submissions open once the hackathon begins"
      :description="eventStart ? `The event starts ${formatWhen(eventStart)}.` : undefined"
    />

    <GEmptyState
      v-else-if="!isParticipant || !hasTeam"
      icon="material-symbols:group-rounded"
      title="You need a team to submit"
      description="Projects are submitted per team. Start or join one first."
    >
      <template #actions>
        <GButton
          :to="`/${shortCode}/team`"
          variant="primary"
          icon="material-symbols:group-add-rounded"
          label="Go to your team"
        />
      </template>
    </GEmptyState>

    <!-- submitted -->
    <template v-else-if="submission">
      <GAlert
        tone="ok"
        title="Your team has submitted"
        :description="`Sent ${formatWhen(toDate(submission.submittedAt))}. Submissions can't be changed; talk to the organisers if something is wrong.`"
      />

      <article class="sub__card">
        <div class="flex flex-col gap-1">
          <p class="sub__eyebrow">
            {{ teamData?.name }}
          </p>
          <h2 class="sub__title">
            {{ submission.title }}
          </h2>
          <p
            v-if="submittedChallenge"
            class="sub__challenge"
          >
            <Icon
              name="material-symbols:flag-rounded"
              aria-hidden="true"
            />
            {{ submittedChallenge }}
          </p>
        </div>

        <p
          v-if="submission.summary"
          class="sub__summary"
        >
          {{ submission.summary }}
        </p>

        <ul
          v-if="submittedLinks.length"
          class="sub__links"
        >
          <li
            v-for="link in submittedLinks"
            :key="link.label"
          >
            <a
              :href="link.href"
              target="_blank"
              rel="noopener noreferrer"
              class="sub__link"
            >
              <Icon
                :name="link.icon"
                class="sub__link-icon"
                aria-hidden="true"
              />
              <span class="min-w-0">
                <span class="sub__link-label">{{ link.label }}<span class="sr-only"> (opens in a new tab)</span></span>
                <span class="sub__link-url">{{ link.href }}</span>
              </span>
              <Icon
                name="material-symbols:open-in-new-rounded"
                class="sub__link-out"
                aria-hidden="true"
              />
            </a>
          </li>
        </ul>
      </article>
    </template>

    <!-- closed without a submission -->
    <GEmptyState
      v-else-if="windowState === 'closed'"
      icon="material-symbols:lock-rounded"
      title="Submissions have closed"
      :description="`${teamData?.name ?? 'Your team'} didn't submit a project${closesAt ? ` before ${formatWhen(closesAt)}` : ''}. Talk to the organisers if you think this is wrong.`"
    />

    <!-- form -->
    <template v-else>
      <div class="flex flex-col gap-1">
        <h2 class="sub__heading">
          Submit your project
        </h2>
        <p class="sub__lead">
          One submission per team, and it's final once sent.
          <template v-if="closesAt">
            Submit by <strong>{{ formatWhen(closesAt) }}</strong>.
          </template>
        </p>
      </div>

      <GAlert
        v-if="windowState === 'upcoming'"
        tone="neutral"
        title="Submissions aren't open yet"
        :description="opensAt ? `You can submit from ${formatWhen(opensAt)}.` : 'The organisers will open submissions during the event.'"
      />

      <GAlert
        v-if="!challengeId"
        tone="attention"
        title="Pick a challenge first"
        description="Your submission is judged under your team's challenge statement. Choose one on the Team page."
      >
        <template #actions>
          <GButton
            :to="`/${shortCode}/team`"
            size="sm"
            label="Choose a challenge"
            trailing-icon="material-symbols:arrow-forward-rounded"
          />
        </template>
      </GAlert>

      <form
        ref="formEl"
        class="sub__card"
        novalidate
        aria-label="Project submission"
        @submit.prevent="review"
      >
        <p
          v-if="teamChallenge"
          class="sub__challenge"
        >
          <Icon
            name="material-symbols:flag-rounded"
            aria-hidden="true"
          />
          <span><span class="sr-only">Challenge: </span>{{ teamChallenge }}</span>
        </p>

        <GFormField
          label="Project title"
          required
          :disabled="!isOpen"
          :error="errors.title"
        >
          <GInput
            v-model="form.title"
            autocomplete="off"
          />
        </GFormField>

        <GFormField
          label="Summary"
          required
          :disabled="!isOpen"
          :error="errors.summary"
          hint="What it does, who it's for and how you built it."
        >
          <GTextarea
            v-model="form.summary"
            :rows="4"
            autoresize
            :max-rows="12"
          />
        </GFormField>

        <div class="sub__grid">
          <GFormField
            label="Repository link"
            required
            :disabled="!isOpen"
            :error="errors.repoUri"
            hint="GitHub or another public repository."
          >
            <GInput
              v-model="form.repoUri"
              type="url"
              inputmode="url"
              autocomplete="url"
              spellcheck="false"
              placeholder="https://github.com/…"
            />
          </GFormField>

          <GFormField
            label="Slides link"
            required
            :disabled="!isOpen"
            :error="errors.slidesUri"
            hint="Make sure anyone with the link can view it."
          >
            <GInput
              v-model="form.slidesUri"
              type="url"
              inputmode="url"
              autocomplete="url"
              spellcheck="false"
              placeholder="https://"
            />
          </GFormField>
        </div>

        <GFormField
          label="Demo link"
          :disabled="!isOpen"
          :error="errors.demoUri"
          hint="Optional. A live site or a video walkthrough."
        >
          <GInput
            v-model="form.demoUri"
            type="url"
            inputmode="url"
            autocomplete="url"
            spellcheck="false"
            placeholder="https://"
          />
        </GFormField>

        <GButton
          type="submit"
          variant="primary"
          icon="material-symbols:fact-check-rounded"
          label="Review and submit"
          :disabled="!isOpen || !challengeId"
          class="self-start"
        />
      </form>
    </template>

    <!-- review -->
    <GModal
      v-model:open="showReview"
      title="Submit this project?"
      description="Check everything carefully. You can't edit the submission after it's sent."
    >
      <dl class="sub__review">
        <div>
          <dt>Team</dt>
          <dd>{{ teamData?.name }}</dd>
        </div>
        <div v-if="teamChallenge">
          <dt>Challenge</dt>
          <dd>{{ teamChallenge }}</dd>
        </div>
        <div>
          <dt>Title</dt>
          <dd>{{ form.title.trim() }}</dd>
        </div>
        <div>
          <dt>Summary</dt>
          <dd class="whitespace-pre-line">
            {{ form.summary.trim() }}
          </dd>
        </div>
        <div>
          <dt>Repository</dt>
          <dd class="sub__review-url">
            {{ form.repoUri.trim() }}
          </dd>
        </div>
        <div>
          <dt>Slides</dt>
          <dd class="sub__review-url">
            {{ form.slidesUri.trim() }}
          </dd>
        </div>
        <div>
          <dt>Demo</dt>
          <dd :class="form.demoUri.trim() ? 'sub__review-url' : 'sub__review-none'">
            {{ form.demoUri.trim() || 'None' }}
          </dd>
        </div>
      </dl>
      <GAlert
        v-if="submitError"
        tone="err"
        title="Not submitted"
        :description="submitError"
        class="mt-4"
      />
      <template #footer>
        <GButton
          variant="ghost"
          label="Keep editing"
          @click="showReview = false"
        />
        <GButton
          variant="primary"
          label="Submit project"
          :loading="createSubmission.isPending.value"
          loading-label="Submitting"
          @click="confirmSubmit"
        />
      </template>
    </GModal>
  </section>
</template>

<style scoped>
.sub {
  display: flex;
  flex-direction: column;
  gap: 20px;
  width: 100%;
}
.sub__heading {
  margin: 0;
  font-family: var(--font-display);
  font-size: clamp(24px, 3vw, 30px);
  font-weight: 700;
  line-height: 1.15;
}
.sub__lead {
  max-width: 65ch;
  margin: 0;
  color: var(--gh-text-muted);
}
.sub__lead strong {
  color: var(--gh-text);
  white-space: nowrap;
}
.sub__card {
  display: flex;
  flex-direction: column;
  gap: 18px;
  min-width: 0;
  padding: 20px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
@media (min-width: 640px) {
  .sub__card { padding: 24px; }
}
.sub__grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 18px;
}
@media (min-width: 768px) {
  .sub__grid { grid-template-columns: 1fr 1fr; }
}
.sub__challenge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  align-self: flex-start;
  margin: 0;
  font-size: 14px;
  font-weight: 600;
}
.sub__eyebrow {
  margin: 0;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.sub__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: clamp(24px, 3vw, 30px);
  font-weight: 700;
  line-height: 1.15;
  overflow-wrap: anywhere;
}
.sub__summary {
  max-width: 70ch;
  margin: 0;
  white-space: pre-line;
  overflow-wrap: anywhere;
}
.sub__links {
  display: grid;
  /* minmax(0, …): long URLs truncate instead of widening the tile past the card */
  grid-template-columns: minmax(0, 1fr);
  gap: 10px;
  margin: 0;
  padding: 0;
  list-style: none;
}
@media (min-width: 768px) {
  /* one equal column per tile, so two or three tiles always fill the card */
  .sub__links {
    grid-template-columns: none;
    grid-auto-flow: column;
    grid-auto-columns: minmax(0, 1fr);
  }
}
.sub__link {
  display: flex;
  align-items: center;
  gap: 10px;
  height: 100%;
  padding: 12px 14px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-sm);
  color: var(--gh-text);
  text-decoration: none;
}
.sub__link:hover {
  border-color: var(--gh-text);
}
.sub__link:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: 2px;
}
.sub__link-icon {
  flex: none;
  width: 22px;
  height: 22px;
}
.sub__link-label {
  display: block;
  font-weight: 700;
}
.sub__link-url {
  display: block;
  overflow: hidden;
  font-size: 13px;
  color: var(--gh-text-muted);
  text-overflow: ellipsis;
  white-space: nowrap;
}
.sub__link-out {
  flex: none;
  margin-left: auto;
  width: 18px;
  height: 18px;
  color: var(--gh-text-muted);
}
.sub__review {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin: 0;
}
.sub__review dt {
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.sub__review dd {
  margin: 2px 0 0;
  overflow-wrap: anywhere;
}
.sub__review-url {
  font-family: var(--font-mono);
  font-size: 14px;
}
.sub__review-none {
  color: var(--gh-text-muted);
}
</style>
