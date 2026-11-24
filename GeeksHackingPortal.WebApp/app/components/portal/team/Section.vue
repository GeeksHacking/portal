<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonTeamsGetMineEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { computed } from 'vue'

/**
 * The event's Team page: the survival guide, then one of three states (not a participant /
 * no team yet / your team), and once the event has started, the team's challenge pick.
 */
const route = useRoute()
const routeHackathonId = computed(() => (route.params.hackathonId as string) ?? '')
const { data: hackathon } = useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint({ path: computed(() => ({ hackathonIdOrShortCode: routeHackathonId.value })) })
const hackathonId = computed(() => hackathon.value?.id ?? '')

// Invite links carry ?joinCode=… : prefill the join form with it
const joinCodeParam = computed(() => {
  const code = route.query.joinCode
  return typeof code === 'string' ? code : null
})

const { data: statusData, isLoading: isLoadingStatus } = useGeeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpoint({ path: computed(() => ({ hackathonId: hackathonId.value })) }, { query: { enabled: computed(() => !!hackathonId.value) } })

const { data: teamData, isLoading: isLoadingTeam } = useGeeksHackingPortalApiEndpointsParticipantsHackathonTeamsGetMineEndpoint({ path: computed(() => ({ hackathonId: hackathonId.value })) }, { query: { enabled: computed(() => !!hackathonId.value && !!statusData.value?.isParticipant) } })

const isLoading = computed(() => !hackathonId.value || isLoadingStatus.value || isLoadingTeam.value)
const isParticipant = computed(() => !!statusData.value?.isParticipant)
const hasTeam = computed(() => !!teamData.value?.id)
const survivalGuideUrl = 'https://drive.google.com/file/d/1Bs9V5yBeRH_-jgqPNNvWIKN_dHP6x9AL/view?usp=drive_link'

// The challenge pick only opens once the event has started
const hasHackathonStarted = computed(() => {
  const start = hackathon.value?.eventStartDate
  if (!start)
    return true
  return new Date() >= new Date(start)
})
</script>

<template>
  <section
    class="team-page"
    aria-label="Team"
  >
    <!-- survival guide -->
    <div class="team-guide">
      <Icon
        name="material-symbols:menu-book-rounded"
        class="team-guide__icon"
        aria-hidden="true"
      />
      <div class="team-guide__body">
        <p class="team-guide__title">
          Participant survival guide
        </p>
        <p class="team-guide__text">
          Event details, reminders and logistics, all in one place.
        </p>
      </div>
      <GButton
        :href="survivalGuideUrl"
        target="_blank"
        variant="secondary"
        size="sm"
        trailing-icon="material-symbols:open-in-new-rounded"
        label="Open the guide"
        aria-label="Open the survival guide (opens in a new tab)"
      />
    </div>

    <GLoading
      v-if="isLoading"
      label="Loading your team…"
    />

    <GEmptyState
      v-else-if="!isParticipant"
      icon="material-symbols:person-off-rounded"
      :title="`You're not taking part in ${hackathon?.name ?? 'this hackathon'}`"
      description="Register for the event first. Once you're accepted you can form or join a team here."
    />

    <PortalTeamParticipantWithoutTeam
      v-else-if="!hasTeam"
      :hackathon-id="hackathonId"
      :initial-join-code="joinCodeParam"
    />

    <template v-else>
      <PortalTeamParticipantWithTeam
        :team="teamData!"
        :hackathon-id="hackathonId"
        :short-code="hackathon?.shortCode ?? routeHackathonId"
      />
      <PortalTeamChallengeStatement
        v-if="hasHackathonStarted"
        :team-name="teamData!.name!"
        :team-id="teamData!.id!"
        :hackathon-id="hackathonId"
        :selected-challenge-id="teamData!.challengeId ?? null"
      />
    </template>
  </section>
</template>

<style scoped>
.team-page {
  display: flex;
  flex-direction: column;
  gap: 32px;
  width: 100%;
}
.team-guide {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 12px 16px;
  padding: 14px 16px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.team-guide__icon {
  flex: none;
  width: 28px;
  height: 28px;
}
.team-guide__body {
  flex: 1 1 14rem;
  min-width: 0;
}
.team-guide__title {
  margin: 0;
  font-weight: 700;
}
.team-guide__text {
  margin: 0;
  font-size: 14px;
  color: var(--gh-text-muted);
}
</style>
