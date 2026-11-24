<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsParticipantsHackathonChallengesListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonTeamsSelectChallengeEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { computed, ref, watch } from 'vue'
import { HACKATHON_TIME_ZONE, HACKATHON_TIME_ZONE_LABEL } from '~/utils/hackathon-date-time'

/**
 * The team's challenge pick. Choosing in the menu doesn't save by itself: "Save choice" does,
 * so a stray tap can't switch the team's challenge. After the selection deadline the pick is
 * locked and shown read-only.
 */
const props = defineProps<{
  teamName: string
  teamId: string
  hackathonId: string
  selectedChallengeId: string | null
}>()

const toast = useGToast()
const { refresh } = useMyTeamCache(() => props.hackathonId)

const { data: hackathon } = useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint({ path: computed(() => ({ hackathonIdOrShortCode: props.hackathonId })) })
const { data: challengesData, isLoading } = useGeeksHackingPortalApiEndpointsParticipantsHackathonChallengesListEndpoint({ path: computed(() => ({ hackathonId: props.hackathonId })) })

const challenges = computed(() => [...(challengesData.value?.challenges ?? [])].reverse())
const challengeItems = computed(() =>
  challenges.value.map(challenge => ({
    label: challenge.sponsor ? `${challenge.title} (${challenge.sponsor})` : (challenge.title ?? ''),
    value: challenge.id ?? '',
  })),
)
const currentChallenge = computed(() => challenges.value.find(c => c.id === props.selectedChallengeId) ?? null)

const selectionEnd = computed(() => {
  const value = hackathon.value?.challengeSelectionEndDate
  return value ? new Date(value) : null
})
const isClosed = computed(() => !!selectionEnd.value && new Date() > selectionEnd.value)
const deadlineText = computed(() => {
  if (!selectionEnd.value)
    return ''
  const formatted = new Intl.DateTimeFormat('en-SG', { timeZone: HACKATHON_TIME_ZONE, weekday: 'short', day: 'numeric', month: 'short', hour: 'numeric', minute: '2-digit' }).format(selectionEnd.value)
  return `${formatted} ${HACKATHON_TIME_ZONE_LABEL}`
})

const choice = ref<string>(props.selectedChallengeId ?? '')
watch(() => props.selectedChallengeId, (value) => {
  choice.value = value ?? ''
})
const isDirty = computed(() => !!choice.value && choice.value !== (props.selectedChallengeId ?? ''))

const selectChallengeMutation = useGeeksHackingPortalApiEndpointsParticipantsHackathonTeamsSelectChallengeEndpoint()
const saveError = ref('')

async function saveChoice() {
  if (!isDirty.value || isClosed.value)
    return
  saveError.value = ''
  try {
    await selectChallengeMutation.mutateAsync({ path: { hackathonId: props.hackathonId, teamId: props.teamId }, body: { challengeId: choice.value } })
    await refresh()
    const picked = challenges.value.find(c => c.id === choice.value)
    toast.add({ title: 'Challenge saved', description: picked?.title ? `${props.teamName} is working on “${picked.title}”.` : undefined, tone: 'ok' })
  }
  catch (error) {
    saveError.value = getApiErrorMessage(error, 'We couldn\'t save your team\'s challenge. Try again.')
  }
}
</script>

<template>
  <section
    class="pick"
    aria-labelledby="pick-title"
  >
    <div class="flex flex-col gap-1">
      <h2
        id="pick-title"
        class="pick__title"
      >
        Your challenge
      </h2>
      <p
        v-if="isClosed"
        class="pick__lead"
      >
        The challenge statement {{ teamName }} is working on.
      </p>
      <p
        v-else
        class="pick__lead"
      >
        Pick the challenge statement {{ teamName }} will work on.
        <template v-if="deadlineText && !isClosed">
          You can change it until <strong>{{ deadlineText }}</strong>.
        </template>
      </p>
    </div>

    <GLoading
      v-if="isLoading"
      label="Loading challenges…"
    />

    <GEmptyState
      v-else-if="!challenges.length"
      size="sm"
      icon="material-symbols:flag-rounded"
      title="No challenges yet"
      description="Once the organisers release the challenge statements you can pick one here."
    />

    <!-- locked -->
    <template v-else-if="isClosed">
      <GAlert
        tone="neutral"
        title="Challenge selection has closed"
        :description="currentChallenge ? 'Your team\'s pick is locked in.' : 'Your team didn\'t pick a challenge before the deadline. Talk to the organisers.'"
      />
      <div
        v-if="currentChallenge"
        class="pick__locked"
      >
        <Icon
          name="material-symbols:lock-rounded"
          aria-hidden="true"
        />
        <span>
          <span class="pick__locked-title">{{ currentChallenge.title }}</span>
          <span
            v-if="currentChallenge.sponsor"
            class="pick__locked-by"
          > · {{ currentChallenge.sponsor }}</span>
        </span>
      </div>
    </template>

    <!-- open -->
    <form
      v-else
      class="pick__form"
      novalidate
      @submit.prevent="saveChoice"
    >
      <GFormField
        label="Challenge statement"
        :hint="currentChallenge ? `Saved: ${currentChallenge.title}` : 'Not chosen yet.'"
        :error="saveError"
        class="min-w-0 flex-1"
      >
        <GSelect
          v-model="choice"
          :items="challengeItems"
          placeholder="Choose a challenge"
        />
      </GFormField>
      <GButton
        type="submit"
        variant="primary"
        label="Save choice"
        :disabled="!isDirty"
        :loading="selectChallengeMutation.isPending.value"
        loading-label="Saving"
        class="pick__save"
      />
    </form>

    <NuxtLink
      v-if="challenges.length"
      :to="`/${hackathon?.shortCode ?? ''}/challenges`"
      class="pick__link"
    >
      Read the full challenge statements
      <Icon
        name="material-symbols:arrow-forward-rounded"
        aria-hidden="true"
      />
    </NuxtLink>
  </section>
</template>

<style scoped>
.pick {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding-top: 24px;
  border-top: var(--gh-border-width) solid var(--gh-line);
}
.pick__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 24px;
  font-weight: 700;
}
.pick__lead {
  max-width: 65ch;
  margin: 0;
  color: var(--gh-text-muted);
}
.pick__lead strong {
  color: var(--gh-text);
}
.pick__form {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  gap: 12px;
}
/* line the button up with the control, not the label */
.pick__save {
  margin-top: 27px;
}
.pick__locked {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 16px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.pick__locked-title {
  font-weight: 700;
}
.pick__locked-by {
  color: var(--gh-text-muted);
}
.pick__link {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  align-self: flex-start;
  font-size: 14px;
  font-weight: 600;
  color: var(--gh-text);
  text-decoration: underline;
  text-underline-offset: 3px;
}
.pick__link:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: 2px;
  border-radius: 2px;
}
</style>
