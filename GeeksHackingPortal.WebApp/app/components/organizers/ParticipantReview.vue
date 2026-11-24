<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsGetParticipantEmailDeliveryItem,
  GeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListParticipantItem,
} from '@geekshacking/portal-sdk'
import type { OrganizerAnswer } from '~/components/organizers/AnswerList.vue'
import {
  geeksHackingPortalApiEndpointsOrganizersHackathonParticipantsGetEndpointQueryKey,
  geeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsReviewEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import {
  applicationTimeOf,
  formatOrganizerDateTime,
  getErrorStatusCode,
  isReviewOverdue,
  REVIEW_OVERDUE_DAYS,
  reviewDecisionLabel,
  reviewStateBadge,
  reviewStateOf,
  waitedFor,
} from '~/utils/participant-review'

/**
 * One hackathon application in the review queue: who they are, their answers, earlier
 * decisions and emails, with the approve / reject form pinned at the bottom. Applications
 * already decided show the decision, with "Change decision" to review again.
 */
type ParticipantItem = GeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListParticipantItem
type EmailDelivery = GeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsGetParticipantEmailDeliveryItem
type Decision = 'accept' | 'reject'

const props = defineProps<{
  hackathonId: string
  participant: ParticipantItem
  /** Their answers, in the form's question order. */
  answers: OrganizerAnswer[]
  emails?: EmailDelivery[]
  loadingEmails?: boolean
  /** Who's next in the queue after this one, if anyone. */
  nextName?: string | null
}>()

const emit = defineEmits<{
  close: []
  reviewed: [decision: Decision]
  next: []
}>()

const queryClient = useQueryClient()
const reviewMutation = useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsReviewEndpoint()

const name = computed(() => props.participant.name || 'Unnamed participant')
const firstName = computed(() => name.value.split(/\s+/)[0] || name.value)
const state = computed(() => reviewStateOf(props.participant))
const badge = computed(() => reviewStateBadge[state.value])
const overdue = computed(() => isReviewOverdue(props.participant))
const applied = computed(() => applicationTimeOf(props.participant))

// newest decision first
const reviews = computed(() => [...(props.participant.reviews ?? [])].sort((a, b) =>
  new Date(b.createdAt ?? 0).getTime() - new Date(a.createdAt ?? 0).getTime()))
const latestReview = computed(() => reviews.value[0])

const emailsSorted = computed(() => [...(props.emails ?? [])].sort((a, b) =>
  new Date(b.sentAt ?? 0).getTime() - new Date(a.sentAt ?? 0).getTime()))

// ---- decision form ----
const isChanging = ref(false)
const decision = ref<Decision | null>(null)
const reason = ref('')
const reasonError = ref('')
const submitError = ref('')
const conflictNotice = ref('')
const reasonField = ref<{ focus: () => void } | null>(null)

const showForm = computed(() => state.value === 'pending' || isChanging.value)

function resetForm() {
  isChanging.value = false
  decision.value = null
  reason.value = ''
  reasonError.value = ''
  submitError.value = ''
}

watch(() => props.participant.id, () => {
  resetForm()
  conflictNotice.value = ''
})
watch(decision, () => {
  reasonError.value = ''
  submitError.value = ''
})

const submitLabel = computed(() => {
  if (decision.value === 'accept')
    return `Approve ${firstName.value}`
  if (decision.value === 'reject')
    return `Reject ${firstName.value}`
  return ''
})

async function invalidate() {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListEndpointQueryKey({ path: { hackathonId: props.hackathonId } }) }),
    queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsOrganizersHackathonParticipantsGetEndpointQueryKey({ path: { hackathonId: props.hackathonId, userId: props.participant.id ?? '' } }) }),
  ])
}

async function submit() {
  if (!decision.value || !props.participant.id || reviewMutation.isPending.value)
    return
  const note = reason.value.trim()
  if (decision.value === 'reject' && !note) {
    reasonError.value = `Tell ${firstName.value} why, so they know what happened.`
    reasonField.value?.focus()
    return
  }

  submitError.value = ''
  const made = decision.value
  try {
    await reviewMutation.mutateAsync({
      path: { hackathonId: props.hackathonId, participantUserId: props.participant.id },
      body: { decision: made, reason: note || null },
    })
    await invalidate()
    resetForm()
    conflictNotice.value = ''
    emit('reviewed', made)
  }
  catch (error) {
    if (getErrorStatusCode(error) === 409) {
      await invalidate()
      resetForm()
      conflictNotice.value = `Another organiser reviewed ${name.value} while you had this open. Their decision is shown below.`
      return
    }
    submitError.value = getApiErrorMessage(error, 'Your decision wasn\'t saved. Try again.')
  }
}
</script>

<template>
  <OrganizersDetailPane
    :title="name"
    close-label="Close application"
    @close="emit('close')"
  >
    <template #meta>
      <GBadge
        :tone="badge.tone"
        :variant="badge.tone === 'pending' ? 'solid' : 'soft'"
        :icon="badge.icon"
        :label="badge.label"
      />
      <GBadge
        v-if="overdue"
        tone="err"
        icon="material-symbols:schedule-rounded"
        :label="`Waiting ${REVIEW_OVERDUE_DAYS}+ days`"
      />
      <GBadge
        v-if="participant.isWithdrawn"
        icon="material-symbols:person-off-rounded"
        label="Withdrew"
      />
    </template>

    <GAlert
      v-if="conflictNotice"
      tone="attention"
      title="Already reviewed"
      :description="conflictNotice"
      dismissible
      @close="conflictNotice = ''"
    />

    <dl class="opr-facts">
      <div>
        <dt>Team</dt>
        <dd>{{ participant.teamName || 'No team yet' }}</dd>
      </div>
      <div>
        <dt>Email</dt>
        <dd>
          <a
            v-if="participant.email"
            :href="`mailto:${participant.email}`"
            class="opr-link"
          >{{ participant.email }}</a>
          <template v-else>
            —
          </template>
        </dd>
      </div>
      <div>
        <dt>Applied</dt>
        <dd>
          {{ formatOrganizerDateTime(applied) }}
          <span
            v-if="applied"
            class="opr-muted"
          >· {{ waitedFor(applied) }}</span>
        </dd>
      </div>
      <div v-if="participant.isWithdrawn">
        <dt>Withdrew</dt>
        <dd>{{ formatOrganizerDateTime(participant.withdrawnAt) }}</dd>
      </div>
    </dl>

    <section
      class="opr-section"
      aria-labelledby="opr-answers"
    >
      <h3
        id="opr-answers"
        class="opr-section__title"
      >
        Answers
      </h3>
      <OrganizersAnswerList
        :answers="answers"
        :empty-text="`${firstName} hasn't submitted the registration form yet.`"
      />
    </section>

    <section
      v-if="reviews.length"
      class="opr-section"
      aria-labelledby="opr-history"
    >
      <h3
        id="opr-history"
        class="opr-section__title"
      >
        Decisions
      </h3>
      <ol class="opr-history">
        <li
          v-for="(review, index) in reviews"
          :key="review.id ?? index"
        >
          <div class="opr-history__head">
            <GBadge
              :tone="review.status === 'Accepted' ? 'ok' : review.status === 'Rejected' ? 'err' : 'neutral'"
              size="sm"
              :label="reviewDecisionLabel(review.status)"
            />
            <span class="opr-muted">{{ formatOrganizerDateTime(review.createdAt) }}</span>
            <span
              v-if="index === 0 && reviews.length > 1"
              class="opr-muted"
            >· current</span>
          </div>
          <p :class="review.reason ? 'opr-history__note' : 'opr-muted'">
            {{ review.reason || 'No note' }}
          </p>
        </li>
      </ol>
    </section>

    <section
      v-if="loadingEmails || emailsSorted.length"
      class="opr-section"
      aria-labelledby="opr-emails"
    >
      <h3
        id="opr-emails"
        class="opr-section__title"
      >
        Emails sent
      </h3>
      <GSkeleton
        v-if="loadingEmails && !emailsSorted.length"
        class="opr-skeleton"
      />
      <ul
        v-else
        class="opr-emails"
      >
        <li
          v-for="(email, index) in emailsSorted"
          :key="email.id ?? index"
        >
          <span class="opr-emails__what">{{ email.eventKey || 'Email' }}</span>
          <span class="opr-muted">{{ formatOrganizerDateTime(email.sentAt) }}</span>
          <GBadge
            v-if="email.status"
            :tone="email.errorMessage || /fail|error|bounce/i.test(email.status) ? 'err' : 'neutral'"
            size="sm"
            :label="email.status"
          />
          <span
            v-if="email.errorMessage"
            class="opr-emails__error"
          >{{ email.errorMessage }}</span>
        </li>
      </ul>
    </section>

    <template
      v-if="state !== 'incomplete'"
      #footer
    >
      <!-- decided: show it, offer to change it -->
      <div
        v-if="!showForm"
        class="opr-decided"
      >
        <p class="opr-decided__text">
          <strong>{{ badge.label }}</strong>
          <template v-if="latestReview?.createdAt">
            on {{ formatOrganizerDateTime(latestReview.createdAt) }}
          </template>
        </p>
        <div class="opr-decided__actions">
          <GButton
            v-if="nextName"
            variant="ghost"
            icon="material-symbols:arrow-forward-rounded"
            :label="`Next: ${nextName}`"
            @click="emit('next')"
          />
          <GButton
            variant="secondary"
            icon="material-symbols:edit-rounded"
            label="Change decision"
            @click="isChanging = true"
          />
        </div>
      </div>

      <!-- the decision form -->
      <form
        v-else
        class="opr-form"
        novalidate
        @submit.prevent="submit"
      >
        <fieldset class="opr-choice">
          <legend class="opr-choice__legend">
            {{ isChanging ? `New decision for ${firstName}` : 'Decision' }}
          </legend>
          <label
            class="opr-choice__option opr-choice__option--ok"
            :class="{ 'is-on': decision === 'accept' }"
          >
            <input
              v-model="decision"
              type="radio"
              name="opr-decision"
              value="accept"
            >
            <Icon
              name="material-symbols:check-circle-rounded"
              aria-hidden="true"
            />
            Approve
          </label>
          <label
            class="opr-choice__option opr-choice__option--err"
            :class="{ 'is-on': decision === 'reject' }"
          >
            <input
              v-model="decision"
              type="radio"
              name="opr-decision"
              value="reject"
            >
            <Icon
              name="material-symbols:cancel-rounded"
              aria-hidden="true"
            />
            Reject
          </label>
        </fieldset>

        <GFormField
          v-if="decision"
          :label="`Note for ${firstName}`"
          :hint="decision === 'reject' ? 'Required. They see it on their registration status page.' : 'Optional. They see it on their registration status page.'"
          :error="reasonError"
          :required="decision === 'reject'"
        >
          <GTextarea
            ref="reasonField"
            v-model="reason"
            :rows="2"
            autoresize
            :max-rows="6"
            :placeholder="decision === 'reject' ? 'Say briefly and kindly why' : 'e.g. See you on the day!'"
          />
        </GFormField>

        <GAlert
          v-if="submitError"
          tone="err"
          title="Not saved"
          :description="submitError"
        />

        <div
          v-if="decision || isChanging"
          class="opr-form__actions"
        >
          <GButton
            v-if="isChanging"
            variant="ghost"
            label="Cancel"
            :disabled="reviewMutation.isPending.value"
            @click="resetForm"
          />
          <GButton
            type="submit"
            :variant="decision === 'reject' ? 'danger' : 'primary'"
            v-if="decision"
            :label="submitLabel"
            :loading="reviewMutation.isPending.value"
            loading-label="Saving decision"
          />
        </div>
      </form>
    </template>
  </OrganizersDetailPane>
</template>

<style scoped>
.opr-facts {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr));
  gap: 12px 24px;
  margin: 0;
}
.opr-facts dt {
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.opr-facts dd {
  margin: 2px 0 0;
  overflow-wrap: anywhere;
}
.opr-link {
  color: inherit;
  text-decoration: underline;
  text-underline-offset: 3px;
}
.opr-muted {
  margin: 0;
  color: var(--gh-text-muted);
}

.opr-section {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.opr-section__title {
  margin: 0;
  font-size: 16px;
  font-weight: 700;
}
.opr-skeleton {
  display: block;
  height: 40px;
}

.opr-history,
.opr-emails {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.opr-history li {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding-left: 12px;
  border-left: 3px solid var(--gh-line-soft);
}
.opr-history__head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px 8px;
  font-size: 14px;
}
.opr-history__note {
  margin: 0;
  white-space: pre-line;
  overflow-wrap: anywhere;
}
.opr-emails li {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px 8px;
  font-size: 14px;
}
.opr-emails__what { font-weight: 600; }
.opr-emails__error {
  flex-basis: 100%;
  color: var(--gh-err);
}

/* footer */
.opr-decided {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 8px 16px;
}
.opr-decided__text { margin: 0; }
.opr-decided__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
.opr-form {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.opr-form__actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 8px;
}

.opr-choice {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 8px;
  margin: 0;
  padding: 0;
  border: 0;
}
.opr-choice__legend {
  grid-column: 1 / -1;
  margin-bottom: 8px;
  padding: 0;
  font-size: 14px;
  font-weight: 600;
}
.opr-choice__option {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  min-height: 44px;
  padding: 8px 12px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh);
  background: var(--gh-surface);
  font-weight: 600;
  cursor: pointer;
  transition: background-color var(--gh-dur-fast) var(--ease-gh);
}
.opr-choice__option:hover { background: var(--gh-ghost-hover); }
.opr-choice__option input {
  position: absolute;
  opacity: 0;
  pointer-events: none;
}
.opr-choice__option:has(input:focus-visible) {
  outline: 2px solid var(--gh-focus);
  outline-offset: 2px;
}
.opr-choice__option :deep(svg),
.opr-choice__option .iconify {
  width: 20px;
  height: 20px;
}
/* picked: solid status fill, text on it from the on-status tokens */
.opr-choice__option--ok.is-on {
  border-color: var(--gh-ok);
  background: var(--gh-ok);
  color: var(--gh-on-ok);
}
.opr-choice__option--err.is-on {
  border-color: var(--gh-err);
  background: var(--gh-err);
  color: var(--gh-on-err);
}
@media (prefers-reduced-motion: reduce) {
  .opr-choice__option { transition: none; }
}
</style>
