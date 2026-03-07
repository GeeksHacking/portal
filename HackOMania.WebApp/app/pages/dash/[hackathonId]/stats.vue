<script setup lang="ts">
import type { HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantItem } from '~/api-client/models'
import { useQuery } from '@tanstack/vue-query'
import { computed } from 'vue'
import { HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject } from '~/api-client/models'
import { challengeOrganizerQueries } from '~/composables/challenges'
import { participantOrganizerQueries } from '~/composables/participants'
import { teamOrganizerQueries } from '~/composables/teams'
import { venueOverviewQueries } from '~/composables/venue'

type ParticipantItem = HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantItem
type StatColor = 'primary' | 'success' | 'error' | 'warning' | 'neutral'

interface StatCard {
  label: string
  value: string | number
  icon: string
  color: StatColor
}

interface StatSection {
  key: string
  title: string
  cards: StatCard[]
  gridClass: string
  footer?: string | null
}

const cardBorderClass: Record<StatColor, string> = {
  primary: 'border-l-primary',
  success: 'border-l-green-500 dark:border-l-green-400',
  warning: 'border-l-amber-500 dark:border-l-amber-400',
  error: 'border-l-red-500 dark:border-l-red-400',
  neutral: 'border-l-gray-400 dark:border-l-gray-500',
}

const cardIconClass: Record<StatColor, string> = {
  primary: 'text-primary',
  success: 'text-green-500 dark:text-green-400',
  warning: 'text-amber-500 dark:text-amber-400',
  error: 'text-red-500 dark:text-red-400',
  neutral: 'text-(--ui-text-muted)',
}

const route = useRoute()
const hackathonId = computed(() => (route.params.hackathonId as string | undefined) || '')

const { data: participantsData, isLoading: isLoadingParticipants } = useQuery(
  computed(() => ({
    ...participantOrganizerQueries.list(hackathonId.value),
    enabled: !!hackathonId.value,
  })),
)

const { data: teamsData, isLoading: isLoadingTeams } = useQuery(
  computed(() => ({
    ...teamOrganizerQueries.list(hackathonId.value),
    enabled: !!hackathonId.value,
  })),
)

const { data: challengesData, isLoading: isLoadingChallenges } = useQuery(
  computed(() => ({
    ...challengeOrganizerQueries.list(hackathonId.value),
    enabled: !!hackathonId.value,
  })),
)

const { data: venueOverviewData, isLoading: isLoadingVenueOverview } = useQuery(
  computed(() => ({
    ...venueOverviewQueries.overview(hackathonId.value),
    enabled: !!hackathonId.value,
  })),
)

const REVIEW_OVERDUE_DAYS = 5
const REVIEW_OVERDUE_MS = REVIEW_OVERDUE_DAYS * 24 * 60 * 60 * 1000

const participants = computed(() => participantsData.value?.participants ?? [])
const teams = computed(() => teamsData.value?.teams ?? [])
const challenges = computed(() => challengesData.value?.challenges ?? [])
const checkInParticipants = computed(() => venueOverviewData.value?.participants ?? [])
const isLoading = computed(
  () => isLoadingParticipants.value || isLoadingTeams.value || isLoadingChallenges.value || isLoadingVenueOverview.value,
)

function isWithdrawn(participant: ParticipantItem) {
  const withdrawnAt = (participant as { withdrawnAt?: Date | string | null }).withdrawnAt
  return !!withdrawnAt
}

function isIncomplete(participant: ParticipantItem) {
  return !participant.registrationSubmissions?.length
}

function getApplicationTimeEpoch(participant: ParticipantItem) {
  const submissions = participant.registrationSubmissions ?? []
  if (!submissions.length)
    return 0

  return submissions.reduce((max, submission) => {
    const updatedAt = submission.updatedAt?.getTime() ?? 0
    return updatedAt > max ? updatedAt : max
  }, 0)
}

const activeParticipants = computed(() => participants.value.filter(participant => !isWithdrawn(participant)))
const completeActiveParticipants = computed(() => activeParticipants.value.filter(participant => !isIncomplete(participant)))
const withdrawnParticipants = computed(() => participants.value.filter(participant => isWithdrawn(participant)))

const pendingReviewParticipants = computed(() =>
  completeActiveParticipants.value.filter(participant =>
    participant.concludedStatus === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Pending
    || participant.concludedStatus === null
    || participant.concludedStatus === undefined,
  ),
)

const overdueParticipants = computed(() =>
  pendingReviewParticipants.value.filter((participant) => {
    const submittedAtEpoch = getApplicationTimeEpoch(participant)
    if (!submittedAtEpoch)
      return false

    return Date.now() - submittedAtEpoch >= REVIEW_OVERDUE_MS
  }),
)

const acceptedParticipants = computed(() =>
  completeActiveParticipants.value.filter(
    participant => participant.concludedStatus === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Accepted,
  ),
)

const rejectedParticipants = computed(() =>
  completeActiveParticipants.value.filter(
    participant => participant.concludedStatus === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Rejected,
  ),
)

const inTeamParticipants = computed(() => activeParticipants.value.filter(participant => !!participant.teamId))
const notInTeamParticipants = computed(() => activeParticipants.value.filter(participant => !participant.teamId))

const totalTeamMembers = computed(() =>
  teams.value.reduce((sum, team) => sum + (team.memberCount ?? 0), 0),
)

const averageMembersPerTeam = computed(() => {
  if (!teams.value.length)
    return 0

  return totalTeamMembers.value / teams.value.length
})

const largestTeamSize = computed(() =>
  teams.value.reduce((max, team) => Math.max(max, team.memberCount ?? 0), 0),
)

const acceptedInTeam = computed(() => acceptedParticipants.value.filter(participant => !!participant.teamId).length)
const acceptedNotInTeam = computed(() => acceptedParticipants.value.filter(participant => !participant.teamId).length)
const emailsSent = computed(() => participants.value.reduce((sum, participant) => sum + (participant.emailSentCount ?? 0), 0))

const acceptanceRate = computed(() => {
  const reviewedCount = acceptedParticipants.value.length + rejectedParticipants.value.length
  if (reviewedCount === 0)
    return null

  return ((acceptedParticipants.value.length / reviewedCount) * 100).toFixed(1)
})

const registrationCompletionRate = computed(() => {
  if (activeParticipants.value.length === 0)
    return null

  return ((completeActiveParticipants.value.length / activeParticipants.value.length) * 100).toFixed(1)
})

const teamFormationRate = computed(() => {
  if (activeParticipants.value.length === 0)
    return null

  return ((inTeamParticipants.value.length / activeParticipants.value.length) * 100).toFixed(1)
})

const memberCheckInByParticipantId = computed(() => {
  const entries = checkInParticipants.value.flatMap((participant) => {
    if (!participant.participantId)
      return []

    return [[participant.participantId, participant] as const]
  })

  return new Map(entries)
})

const activeParticipantsWithCheckIn = computed(() =>
  activeParticipants.value.map(participant => ({
    participant,
    checkIn: participant.id ? memberCheckInByParticipantId.value.get(participant.id) : undefined,
  })),
)

const checkedInMembers = computed(() =>
  activeParticipantsWithCheckIn.value.filter(({ checkIn }) => checkIn?.isCurrentlyCheckedIn).length,
)

const checkedOutMembers = computed(() =>
  activeParticipantsWithCheckIn.value.filter(
    ({ checkIn }) => !checkIn?.isCurrentlyCheckedIn && (checkIn?.totalCheckIns ?? 0) > 0,
  ).length,
)

const membersWithoutCheckIn = computed(() =>
  activeParticipantsWithCheckIn.value.filter(({ checkIn }) => (checkIn?.totalCheckIns ?? 0) === 0).length,
)

const totalMemberCheckIns = computed(() =>
  activeParticipantsWithCheckIn.value.reduce(
    (sum, { checkIn }) => sum + (checkIn?.totalCheckIns ?? 0),
    0,
  ),
)

const memberCheckInRate = computed(() => {
  if (activeParticipants.value.length === 0)
    return null

  return ((checkedInMembers.value / activeParticipants.value.length) * 100).toFixed(1)
})

const publishedChallenges = computed(() => challenges.value.filter(challenge => challenge.isPublished))
const draftChallenges = computed(() => challenges.value.filter(challenge => !challenge.isPublished))
const teamsWithSelectedChallenge = computed(() => teams.value.filter(team => !!team.challengeId))
const teamsWithoutSelectedChallenge = computed(() => teams.value.filter(team => !team.challengeId))

const teamCountByChallengeId = computed(() => {
  const counts = new Map<string, number>()

  for (const team of teams.value) {
    if (!team.challengeId)
      continue

    counts.set(team.challengeId, (counts.get(team.challengeId) ?? 0) + 1)
  }

  return counts
})

const challengesWithSelections = computed(() =>
  challenges.value.filter(challenge => (challenge.id ? (teamCountByChallengeId.value.get(challenge.id) ?? 0) : 0) > 0),
)

const challengesWithoutSelections = computed(() =>
  challenges.value.filter(challenge => (challenge.id ? (teamCountByChallengeId.value.get(challenge.id) ?? 0) : 0) === 0),
)

const averageTeamsPerPublishedChallenge = computed(() => {
  if (publishedChallenges.value.length === 0)
    return 0

  return teamsWithSelectedChallenge.value.length / publishedChallenges.value.length
})

const maxTeamsOnSingleChallenge = computed(() =>
  challenges.value.reduce((max, challenge) => {
    const count = challenge.id ? (teamCountByChallengeId.value.get(challenge.id) ?? 0) : 0
    return Math.max(max, count)
  }, 0),
)

const challengeSelectionRate = computed(() => {
  if (teams.value.length === 0)
    return null

  return ((teamsWithSelectedChallenge.value.length / teams.value.length) * 100).toFixed(1)
})

const activeTeamMembersByTeamId = computed(() => {
  const teamMembers = new Map<string, ParticipantItem[]>()

  for (const participant of activeParticipants.value) {
    if (!participant.teamId)
      continue

    const members = teamMembers.get(participant.teamId) ?? []
    members.push(participant)
    teamMembers.set(participant.teamId, members)
  }

  return teamMembers
})

const teamCheckInSummary = computed(() => {
  return teams.value.reduce(
    (summary, team) => {
      if (!team.id) {
        summary.noCheckedInTeams += 1
        return summary
      }

      const members = activeTeamMembersByTeamId.value.get(team.id) ?? []
      const checkedInMemberCount = members.reduce((count, participant) => {
        const checkIn = participant.id ? memberCheckInByParticipantId.value.get(participant.id) : undefined
        return count + (checkIn?.isCurrentlyCheckedIn ? 1 : 0)
      }, 0)

      if (members.length > 0 && checkedInMemberCount === members.length)
        summary.fullyCheckedInTeams += 1
      else if (checkedInMemberCount > 0)
        summary.partiallyCheckedInTeams += 1
      else
        summary.noCheckedInTeams += 1

      return summary
    },
    {
      fullyCheckedInTeams: 0,
      partiallyCheckedInTeams: 0,
      noCheckedInTeams: 0,
    },
  )
})

const registrationStats = computed<StatCard[]>(() => [
  { label: 'Total Registered', value: participants.value.length, icon: 'i-lucide-users', color: 'primary' },
  { label: 'Active', value: activeParticipants.value.length, icon: 'i-lucide-user-check', color: 'success' },
  { label: 'Complete Registrations', value: completeActiveParticipants.value.length, icon: 'i-lucide-file-check', color: 'success' },
  { label: 'Incomplete Registrations', value: activeParticipants.value.filter(participant => isIncomplete(participant)).length, icon: 'i-lucide-file-warning', color: 'warning' },
  { label: 'Withdrawn', value: withdrawnParticipants.value.length, icon: 'i-lucide-user-minus', color: 'neutral' },
])

const reviewStats = computed<StatCard[]>(() => [
  { label: 'Pending Review', value: pendingReviewParticipants.value.length, icon: 'i-lucide-clock', color: 'warning' },
  { label: `Overdue (${REVIEW_OVERDUE_DAYS}d+)`, value: overdueParticipants.value.length, icon: 'i-lucide-alert-triangle', color: 'error' },
  { label: 'Accepted', value: acceptedParticipants.value.length, icon: 'i-lucide-check-circle', color: 'success' },
  { label: 'Rejected', value: rejectedParticipants.value.length, icon: 'i-lucide-x-circle', color: 'error' },
])

const teamStats = computed<StatCard[]>(() => [
  { label: 'Number of Teams', value: teams.value.length, icon: 'i-lucide-users', color: 'primary' },
  { label: 'In a Team', value: inTeamParticipants.value.length, icon: 'i-lucide-user-round-check', color: 'success' },
  { label: 'Not in a Team', value: notInTeamParticipants.value.length, icon: 'i-lucide-user-round-x', color: 'warning' },
  { label: 'Avg Members / Team', value: averageMembersPerTeam.value.toFixed(1), icon: 'i-lucide-bar-chart', color: 'primary' },
  { label: 'Largest Team', value: largestTeamSize.value, icon: 'i-lucide-arrow-up', color: 'neutral' },
  { label: 'All Members In', value: teamCheckInSummary.value.fullyCheckedInTeams, icon: 'i-lucide-check-circle', color: 'success' },
  { label: 'Some Members In', value: teamCheckInSummary.value.partiallyCheckedInTeams, icon: 'i-lucide-users', color: 'warning' },
  { label: 'No Members In', value: teamCheckInSummary.value.noCheckedInTeams, icon: 'i-lucide-x-circle', color: 'neutral' },
])

const challengeStats = computed<StatCard[]>(() => [
  { label: 'Challenge Statements', value: challenges.value.length, icon: 'i-lucide-file-text', color: 'primary' },
  { label: 'Published', value: publishedChallenges.value.length, icon: 'i-lucide-eye', color: 'success' },
  { label: 'Drafts', value: draftChallenges.value.length, icon: 'i-lucide-file-pen-line', color: 'warning' },
  { label: 'Teams Selected', value: teamsWithSelectedChallenge.value.length, icon: 'i-lucide-check-check', color: 'success' },
  { label: 'Teams Unselected', value: teamsWithoutSelectedChallenge.value.length, icon: 'i-lucide-circle-dashed', color: 'warning' },
  { label: 'With Team Picks', value: challengesWithSelections.value.length, icon: 'i-lucide-target', color: 'success' },
  { label: 'Without Team Picks', value: challengesWithoutSelections.value.length, icon: 'i-lucide-target-off', color: 'neutral' },
  { label: 'Max Teams / Challenge', value: maxTeamsOnSingleChallenge.value, icon: 'i-lucide-trophy', color: 'primary' },
  { label: 'Avg Teams / Published', value: averageTeamsPerPublishedChallenge.value.toFixed(1), icon: 'i-lucide-bar-chart-3', color: 'primary' },
])

const venueCheckInStats = computed<StatCard[]>(() => [
  { label: 'Checked In Members', value: checkedInMembers.value, icon: 'i-lucide-user-check', color: 'success' },
  { label: 'Checked Out Members', value: checkedOutMembers.value, icon: 'i-lucide-log-out', color: 'warning' },
  { label: 'No Check-In Yet', value: membersWithoutCheckIn.value, icon: 'i-lucide-user-minus', color: 'neutral' },
  { label: 'Total Check-Ins', value: totalMemberCheckIns.value, icon: 'i-lucide-history', color: 'primary' },
])

const additionalStats = computed<StatCard[]>(() => [
  { label: 'Accepted (in team)', value: acceptedInTeam.value, icon: 'i-lucide-user-round-check', color: 'success' },
  { label: 'Accepted (no team)', value: acceptedNotInTeam.value, icon: 'i-lucide-user-round-x', color: 'warning' },
  { label: 'Emails Sent', value: emailsSent.value, icon: 'i-lucide-mail', color: 'neutral' },
])

const statSections = computed<StatSection[]>(() => [
  {
    key: 'registration',
    title: 'Registration',
    cards: registrationStats.value,
    gridClass: 'grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5',
    footer: registrationCompletionRate.value === null
      ? null
      : `Registration completion rate: ${registrationCompletionRate.value}%`,
  },
  {
    key: 'review',
    title: 'Review Status',
    cards: reviewStats.value,
    gridClass: 'grid grid-cols-2 gap-4 sm:grid-cols-4',
    footer: acceptanceRate.value === null
      ? null
      : `Acceptance rate: ${acceptanceRate.value}% of reviewed applications`,
  },
  {
    key: 'teams',
    title: 'Teams',
    cards: teamStats.value,
    gridClass: 'grid grid-cols-2 gap-4 sm:grid-cols-3 xl:grid-cols-4',
    footer: teamFormationRate.value === null
      ? null
      : `Team formation rate: ${teamFormationRate.value}% of active participants`,
  },
  {
    key: 'venue',
    title: 'Venue Check-In',
    cards: venueCheckInStats.value,
    gridClass: 'grid grid-cols-2 gap-4 sm:grid-cols-4',
    footer: memberCheckInRate.value === null
      ? null
      : `Current check-in rate: ${memberCheckInRate.value}% of active participants`,
  },
  {
    key: 'challenges',
    title: 'Challenge Statements',
    cards: challengeStats.value,
    gridClass: 'grid grid-cols-2 gap-4 sm:grid-cols-3 xl:grid-cols-5',
    footer: challengeSelectionRate.value === null
      ? null
      : `Challenge selection rate: ${challengeSelectionRate.value}% of teams`,
  },
  {
    key: 'additional',
    title: 'Additional',
    cards: additionalStats.value,
    gridClass: 'grid grid-cols-2 gap-4 sm:grid-cols-3',
  },
])
</script>

<template>
  <UDashboardPanel id="participant-stats">
    <template #header>
      <UDashboardNavbar title="Statistics">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="p-4 space-y-6 overflow-y-auto">
        <div
          v-if="isLoading"
          class="text-sm text-(--ui-text-muted)"
        >
          Loading statistics...
        </div>

        <template v-else>
          <UCard
            v-for="section in statSections"
            :key="section.key"
          >
            <template #header>
              <h3 class="text-sm font-semibold">
                {{ section.title }}
              </h3>
            </template>

            <div :class="section.gridClass">
              <div
                v-for="card in section.cards"
                :key="card.label"
                class="rounded-lg border border-(--ui-border) border-l-3 p-3"
                :class="cardBorderClass[card.color]"
              >
                <div class="mb-1 flex items-center gap-2">
                  <UIcon
                    :name="card.icon"
                    class="h-4 w-4 shrink-0"
                    :class="cardIconClass[card.color]"
                  />
                  <span class="text-xs text-(--ui-text-muted)">{{ card.label }}</span>
                </div>
                <p class="text-2xl font-semibold text-(--ui-text-highlighted)">
                  {{ card.value }}
                </p>
              </div>
            </div>

            <div
              v-if="section.footer"
              class="mt-3 text-xs text-(--ui-text-muted)"
            >
              {{ section.footer }}
            </div>
          </UCard>
        </template>
      </div>
    </template>
  </UDashboardPanel>
</template>
