<script setup lang="ts">
import {
  geeksHackingPortalApiEndpointsParticipantsHackathonListEndpointQueryKey,
  geeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListEndpointQueryKey,
  geeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonOrganizersListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsReviewEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonJoinEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonWithdrawEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import { HACKATHON_TIME_ZONE, HACKATHON_TIME_ZONE_LABEL } from '~/utils/hackathon-date-time'

/**
 * "View status" for a hackathon (linked from Explore and the workspace): where your registration
 * stands, the organisers' note, the event's dates and place, the next step, and a way to withdraw.
 *
 * With `?userId=` an organiser also gets Approve / Reject for that participant (nothing links
 * here that way today; the participants review queue is the main place for it).
 */
const route = useRoute()
const toast = useGToast()
const queryClient = useQueryClient()

const hackathonIdOrShortCode = computed(() => (route.params.hackathonId as string | undefined) ?? '')
const participantUserId = computed(() => route.query.userId as string | undefined)

const { data: hackathon, isLoading: isLoadingHackathon, error: hackathonError } = useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint({ path: computed(() => ({ hackathonIdOrShortCode: hackathonIdOrShortCode.value })) }, { query: { enabled: computed(() => !!hackathonIdOrShortCode.value) } })
const hackathonId = computed(() => hackathon.value?.id ?? '')

const { data: statusData, isLoading: isLoadingStatus, error: statusError } = useGeeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpoint({ path: computed(() => ({ hackathonId: hackathonId.value })) }, { query: { enabled: computed(() => !!hackathonId.value) } })
const isParticipant = computed(() => statusData.value?.isParticipant === true)

const { data: submissionsData, isLoading: isLoadingSubmissions } = useGeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListEndpoint({ path: computed(() => ({ hackathonId: hackathonId.value })) }, { query: { enabled: computed(() => !!hackathonId.value && isParticipant.value) } })

const { data: user } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const { data: organizersData } = useGeeksHackingPortalApiEndpointsOrganizersHackathonOrganizersListEndpoint({ path: computed(() => ({ hackathonId: hackathonId.value })) }, { query: { enabled: computed(() => !!hackathonId.value) } })

const isOrganizer = computed(() => {
  if (!user.value?.id)
    return false
  if (user.value.isRoot)
    return true
  return organizersData.value?.organizers?.some(org => org.userId === user.value?.id) ?? false
})

useHead(() => ({ title: hackathon.value?.name ? `Your status · ${hackathon.value.name}` : 'Your status' }))

const isLoading = computed(() =>
  isLoadingHackathon.value
  || (!!hackathonId.value && isLoadingStatus.value)
  || (isParticipant.value && isLoadingSubmissions.value),
)

// ---- where the registration stands (same states and copy as /<code>/registration/status) ------
type RegistrationState = 'not_registered' | 'incomplete' | 'pending_review' | 'rejected' | 'approved'

const registrationState = computed<RegistrationState>(() => {
  if (!isParticipant.value)
    return 'not_registered'
  if (statusData.value?.status === 'Accepted')
    return 'approved'
  if (statusData.value?.status === 'Rejected')
    return 'rejected'
  // required answers left, or the event has no questions yet (the form sets them up)
  if (submissionsData.value && (submissionsData.value.requiredQuestionsRemaining > 0 || submissionsData.value.totalQuestions === 0))
    return 'incomplete'
  return 'pending_review'
})

const stateContent = computed(() => {
  const shortCode = hackathon.value?.shortCode ?? ''
  switch (registrationState.value) {
    case 'not_registered':
      return {
        badge: { label: 'Not joined', tone: 'neutral' as const, icon: 'material-symbols:person-add-rounded' },
        title: 'You haven\'t joined yet',
        description: 'Join to start your registration. It takes about five minutes.',
        step: 0,
        tone: null,
        primary: null,
      }
    case 'incomplete':
      return {
        badge: { label: 'Registration incomplete', tone: 'pending' as const, icon: 'material-symbols:error-rounded' },
        title: 'Finish your registration',
        description: 'Some required questions are still unanswered. Your answers so far are saved.',
        step: 1,
        tone: null,
        primary: { label: 'Continue registration', to: `/${shortCode}/registration/form`, icon: 'material-symbols:edit-note-rounded' },
      }
    case 'rejected':
      return {
        badge: { label: 'Not accepted', tone: 'err' as const, icon: 'material-symbols:cancel-rounded' },
        title: 'Your registration wasn\'t approved',
        description: 'The organisers have reviewed your application and can\'t offer you a place this time.',
        step: 2,
        tone: 'err' as const,
        primary: null,
      }
    case 'approved':
      return {
        badge: { label: 'Accepted', tone: 'ok' as const, icon: 'material-symbols:check-circle-rounded' },
        title: 'You\'re in!',
        description: 'Your registration has been approved. Head to the event portal to form or join a team.',
        step: 3,
        tone: 'ok' as const,
        primary: { label: 'Go to the event portal', to: `/${shortCode}/team`, icon: undefined },
      }
    case 'pending_review':
    default:
      return {
        badge: { label: 'Pending review', tone: 'pending' as const, icon: 'material-symbols:hourglass-top-rounded' },
        title: 'Your registration is under review',
        description: 'Thanks for registering. The organisers are reviewing your answers and will email you once they\'ve decided.',
        step: 2,
        tone: null,
        primary: null,
      }
  }
})

const when = computed(() => formatWorkshopWhen(hackathon.value?.eventStartDate, hackathon.value?.eventEndDate))

const reviewedAtLabel = computed(() => {
  const value = statusData.value?.reviewedAt
  if (!value)
    return null
  const date = new Date(value)
  if (Number.isNaN(date.getTime()))
    return null
  return `${new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeZone: HACKATHON_TIME_ZONE }).format(date)} ${HACKATHON_TIME_ZONE_LABEL}`
})

async function refreshStatus() {
  if (!hackathonId.value)
    return
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpointQueryKey({ path: { hackathonId: hackathonId.value } }) }),
    queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsParticipantsHackathonListEndpointQueryKey() }),
    queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListEndpointQueryKey({ path: { hackathonId: hackathonId.value } }) }),
  ])
}

// ---- join ---------------------------------------------------------------------------------------
const joinMutation = useGeeksHackingPortalApiEndpointsParticipantsHackathonJoinEndpoint()

async function joinHackathon() {
  if (!hackathonId.value || !hackathon.value)
    return
  try {
    await joinMutation.mutateAsync({ path: { hackathonId: hackathonId.value } })
    await refreshStatus()
    await navigateTo(`/${hackathon.value.shortCode}/registration`)
  }
  catch (error) {
    console.error('[DASH] Failed to join hackathon', error)
    toast.add({ title: 'Couldn\'t join', description: getApiErrorMessage(error, 'Try again in a moment.'), tone: 'err' })
  }
}

// ---- withdraw -----------------------------------------------------------------------------------
const withdrawMutation = useGeeksHackingPortalApiEndpointsParticipantsHackathonWithdrawEndpoint()
const isWithdrawModalOpen = ref(false)
const withdrawError = ref('')

watch(isWithdrawModalOpen, (open) => {
  if (open)
    withdrawError.value = ''
})

async function withdrawFromHackathon() {
  if (!hackathonId.value)
    return
  withdrawError.value = ''
  try {
    await withdrawMutation.mutateAsync({ path: { hackathonId: hackathonId.value } })
    await refreshStatus()
    isWithdrawModalOpen.value = false
    toast.add({ title: 'You\'ve withdrawn', description: hackathon.value?.name ? `You're no longer taking part in ${hackathon.value.name}.` : undefined, tone: 'ok' })
  }
  catch (error) {
    console.error('[DASH] Failed to withdraw from hackathon', error)
    // a 400 here usually means the participant is still in a team
    withdrawError.value = getApiErrorMessage(error, getErrorStatusCode(error) === 400
      ? 'Leave your team before withdrawing from this hackathon.'
      : 'We couldn\'t withdraw you. Try again in a moment.')
  }
}

// ---- organiser review (?userId=) ----------------------------------------------------------------
const reviewMutation = useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsReviewEndpoint()
const isReviewModalOpen = ref(false)
const reviewForm = ref({ decision: 'accept' as 'accept' | 'reject', reason: '' })
const reviewError = ref('')

function openReviewModal(decision: 'accept' | 'reject') {
  reviewForm.value = { decision, reason: '' }
  reviewError.value = ''
  isReviewModalOpen.value = true
}

async function handleReview() {
  if (!participantUserId.value || !hackathonId.value)
    return
  reviewError.value = ''
  try {
    await reviewMutation.mutateAsync({
      path: { hackathonId: hackathonId.value, participantUserId: participantUserId.value },
      body: { decision: reviewForm.value.decision, reason: reviewForm.value.reason || null },
    })
    await queryClient.invalidateQueries({ queryKey: ['hackathons', hackathonId.value, 'participants'] })
    isReviewModalOpen.value = false
    toast.add({ title: reviewForm.value.decision === 'accept' ? 'Participant approved' : 'Participant rejected', tone: 'ok' })
  }
  catch (error) {
    if (getErrorStatusCode(error) === 409) {
      await queryClient.invalidateQueries({ queryKey: ['hackathons', hackathonId.value, 'participants'] })
      isReviewModalOpen.value = false
      toast.add({ title: 'Already reviewed', description: 'Another organiser reviewed this participant first.' })
      return
    }
    console.error('[DASH] Failed to review participant', error)
    reviewError.value = getApiErrorMessage(error, 'The review wasn\'t saved. Try again.')
  }
}

function getErrorStatusCode(error: unknown): number | null {
  if (!error || typeof error !== 'object')
    return null
  const e = error as { responseStatusCode?: unknown, statusCode?: unknown, response?: { status?: unknown } }
  if (typeof e.responseStatusCode === 'number')
    return e.responseStatusCode
  if (typeof e.statusCode === 'number')
    return e.statusCode
  if (typeof e.response?.status === 'number')
    return e.response.status
  return null
}
</script>

<template>
  <GPanel id="hackathon-participant">
    <template #header>
      <GPanelHeader
        title="Your status"
        as="p"
      />
    </template>

    <template #body>
      <div class="ps">
        <GLoading
          v-if="isLoading"
          label="Loading your status…"
        />

        <GEmptyState
          v-else-if="hackathonError || !hackathon"
          icon="material-symbols:search-off-rounded"
          title="Hackathon not found"
          description="It may have ended or no longer be public, or the link may be wrong."
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

        <GAlert
          v-else-if="statusError"
          tone="err"
          title="Couldn't load your status"
          description="Refresh the page to try again."
        />

        <template v-else>
          <!-- where it stands -->
          <header class="ps-head">
            <div class="flex flex-wrap items-center gap-2">
              <GLabelStrip label="Hackathon" />
              <GBadge
                :tone="stateContent.badge.tone"
                :icon="stateContent.badge.icon"
                :label="stateContent.badge.label"
              />
            </div>
            <h1 class="ps-head__title">
              {{ hackathon.name }}
            </h1>
          </header>

          <section
            class="ps-card"
            aria-labelledby="ps-state-title"
          >
            <div class="flex flex-col gap-2">
              <h2
                id="ps-state-title"
                class="ps-card__title"
              >
                {{ stateContent.title }}
              </h2>
              <p class="ps-card__text">
                {{ stateContent.description }}
                <template v-if="reviewedAtLabel && (registrationState === 'approved' || registrationState === 'rejected')">
                  Reviewed {{ reviewedAtLabel }}.
                </template>
              </p>
            </div>

            <RegistrationSteps
              :current="stateContent.step"
              :rejected="registrationState === 'rejected'"
            />

            <GAlert
              v-if="statusData?.reviewReason && (registrationState === 'approved' || registrationState === 'rejected')"
              :tone="stateContent.tone ?? 'neutral'"
              title="Message from the organisers"
              :description="statusData.reviewReason"
            />

            <div
              v-if="registrationState === 'not_registered' || stateContent.primary"
              class="flex flex-wrap gap-2"
            >
              <GButton
                v-if="registrationState === 'not_registered'"
                variant="primary"
                icon="material-symbols:person-add-rounded"
                label="Join hackathon"
                :loading="joinMutation.isPending.value"
                loading-label="Joining"
                @click="joinHackathon"
              />
              <GButton
                v-else-if="stateContent.primary"
                :to="stateContent.primary.to"
                variant="primary"
                :icon="stateContent.primary.icon"
                :trailing-icon="stateContent.primary.icon ? undefined : 'material-symbols:arrow-forward-rounded'"
                :label="stateContent.primary.label"
              />
            </div>
          </section>

          <!-- the event -->
          <section
            class="ps-card"
            aria-labelledby="ps-event-title"
          >
            <h2
              id="ps-event-title"
              class="ps-card__title"
            >
              About the event
            </h2>
            <p
              v-if="hackathon.description"
              class="ps-card__text ps-card__text--body"
            >
              {{ hackathon.description }}
            </p>
            <dl class="ps-facts">
              <div>
                <dt>When</dt>
                <dd>
                  {{ when.dateLabel }}
                  <span class="ps-facts__sub">{{ when.timeLabel }}</span>
                </dd>
              </div>
              <div>
                <dt>Where</dt>
                <dd>{{ hackathon.venue || 'To be announced' }}</dd>
              </div>
            </dl>
            <div
              v-if="hackathon.homepageUri"
              class="ps-actions"
            >
              <GButton
                :href="hackathon.homepageUri"
                target="_blank"
                variant="ghost"
                trailing-icon="material-symbols:open-in-new-rounded"
                label="Event site"
                aria-label="Event site (opens in a new tab)"
              />
            </div>
          </section>

          <!-- organiser: review the participant in ?userId= -->
          <section
            v-if="isOrganizer && participantUserId"
            class="ps-card"
            aria-labelledby="ps-review-title"
          >
            <div class="flex flex-col gap-2">
              <h2
                id="ps-review-title"
                class="ps-card__title"
              >
                Review this participant
              </h2>
              <p class="ps-card__text">
                As an organiser you can approve or reject this participant's application.
              </p>
            </div>
            <div class="flex flex-wrap gap-2">
              <GButton
                variant="primary"
                icon="material-symbols:check-rounded"
                label="Approve"
                @click="openReviewModal('accept')"
              />
              <GButton
                variant="danger"
                icon="material-symbols:close-rounded"
                label="Reject"
                @click="openReviewModal('reject')"
              />
            </div>
          </section>

          <!-- withdraw -->
          <section
            v-if="isParticipant"
            class="ps-withdraw"
            aria-labelledby="ps-withdraw-title"
          >
            <div class="ps-withdraw__body">
              <h2
                id="ps-withdraw-title"
                class="ps-withdraw__title"
              >
                Can't make it?
              </h2>
              <p class="ps-withdraw__text">
                Withdraw to give up your place. If you're in a team, leave it first.
              </p>
            </div>
            <GButton
              variant="danger"
              icon="material-symbols:person-remove-rounded"
              label="Withdraw"
              @click="isWithdrawModalOpen = true"
            />
          </section>
        </template>
      </div>
    </template>
  </GPanel>

  <GModal
    v-model:open="isWithdrawModalOpen"
    :title="`Withdraw from ${hackathon?.name ?? 'this hackathon'}?`"
    description="You'll lose your place and your participant access. You must leave your team first if you're in one."
    size="sm"
  >
    <GAlert
      v-if="withdrawError"
      tone="err"
      title="Not withdrawn"
      :description="withdrawError"
    />
    <template #footer>
      <GButton
        variant="ghost"
        label="Stay registered"
        @click="isWithdrawModalOpen = false"
      />
      <GButton
        variant="danger"
        label="Withdraw"
        :loading="withdrawMutation.isPending.value"
        loading-label="Withdrawing"
        @click="withdrawFromHackathon"
      />
    </template>
  </GModal>

  <GModal
    v-model:open="isReviewModalOpen"
    :title="reviewForm.decision === 'accept' ? 'Approve participant' : 'Reject participant'"
    :description="reviewForm.decision === 'accept' ? 'They\'ll get access to the event portal.' : 'They won\'t be offered a place this time.'"
    size="sm"
  >
    <form
      id="ps-review-form"
      class="flex flex-col gap-4"
      @submit.prevent="handleReview"
    >
      <GFormField
        :label="reviewForm.decision === 'accept' ? 'Note (optional)' : 'Reason (optional)'"
        hint="The participant sees this on their status page."
      >
        <GTextarea
          v-model="reviewForm.reason"
          :rows="3"
          :placeholder="reviewForm.decision === 'accept' ? 'Add a note for the participant' : 'Explain why, if you can'"
        />
      </GFormField>
      <GAlert
        v-if="reviewError"
        tone="err"
        title="Not saved"
        :description="reviewError"
      />
    </form>
    <template #footer>
      <GButton
        variant="ghost"
        label="Cancel"
        @click="isReviewModalOpen = false"
      />
      <GButton
        type="submit"
        form="ps-review-form"
        :variant="reviewForm.decision === 'accept' ? 'primary' : 'danger'"
        :label="reviewForm.decision === 'accept' ? 'Approve' : 'Reject'"
        :loading="reviewMutation.isPending.value"
        :loading-label="reviewForm.decision === 'accept' ? 'Approving' : 'Rejecting'"
      />
    </template>
  </GModal>
</template>

<style scoped>
.ps {
  display: flex;
  flex-direction: column;
  gap: 24px;
  width: 100%;
  max-width: 48rem;
  margin: 0 auto;
}
.ps-head {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.ps-head__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: clamp(28px, 4.5vw, 40px);
  font-weight: 700;
  line-height: 1.1;
  overflow-wrap: anywhere;
}
.ps-card {
  display: flex;
  flex-direction: column;
  gap: 20px;
  min-width: 0;
  padding: 20px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
@media (min-width: 640px) {
  .ps-card { padding: 24px; }
}
.ps-card__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 22px;
  font-weight: 700;
  line-height: 1.2;
}
.ps-card__text {
  max-width: 65ch;
  margin: 0;
  color: var(--gh-text-muted);
}
.ps-card__text--body {
  color: var(--gh-text);
  white-space: pre-line;
}
.ps-facts {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 16px 24px;
  margin: 0;
}
@media (min-width: 640px) {
  .ps-facts { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
.ps-facts dt {
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.ps-facts dd {
  margin: 2px 0 0;
  font-weight: 600;
  overflow-wrap: anywhere;
}
.ps-facts__sub {
  display: block;
  font-size: 14px;
  font-weight: 400;
  color: var(--gh-text-muted);
}
.ps-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin: -8px 0 -8px -12px;
}
.ps-withdraw {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px 24px;
  padding: 20px;
  border: var(--gh-border-width) dashed var(--gh-line);
  border-radius: var(--radius-gh-lg);
}
.ps-withdraw__body {
  flex: 1 1 18rem;
  min-width: 0;
}
.ps-withdraw__title {
  margin: 0;
  font-size: 16px;
  font-weight: 700;
}
.ps-withdraw__text {
  max-width: 60ch;
  margin: 4px 0 0;
  font-size: 14px;
  color: var(--gh-text-muted);
}
</style>
