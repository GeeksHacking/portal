<script setup lang="ts">
import type { HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantItem } from '~/api-client/models'
import type {
  OrganizerResourceItem,
  OrganizerResourceStatisticsRecentActivityItem,
  OrganizerResourceStatisticsResponse,
  OrganizerResourceStatisticsTeamItem,
} from '~/composables/resources'
import { useQuery } from '@tanstack/vue-query'
import { computed, ref, watch } from 'vue'
import { HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject } from '~/api-client/models'
import { challengeOrganizerQueries } from '~/composables/challenges'
import { participantOrganizerQueries } from '~/composables/participants'
import { resourceOrganizerQueries } from '~/composables/resources'
import { teamOrganizerQueries } from '~/composables/teams'
import { venueOverviewQueries } from '~/composables/venue'
import { HACKATHON_TIME_ZONE, HACKATHON_TIME_ZONE_LABEL, parseHackathonDateTimeValue } from '~/utils/hackathon-date-time'

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

const ALL_RESOURCES_VALUE = '__all__'

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

const { data: resourcesData, isLoading: isLoadingResources } = useQuery(
  computed(() => ({
    ...resourceOrganizerQueries.list(hackathonId.value),
    enabled: !!hackathonId.value,
  })),
)

const selectedResourceStatsId = ref(ALL_RESOURCES_VALUE)
const resourceBreakdownSearch = ref('')

const organizerResources = computed<OrganizerResourceItem[]>(() => resourcesData.value?.resources ?? [])

watch(organizerResources, (items) => {
  if (selectedResourceStatsId.value === ALL_RESOURCES_VALUE)
    return

  if (!items.some(item => item.id === selectedResourceStatsId.value))
    selectedResourceStatsId.value = ALL_RESOURCES_VALUE
}, { immediate: true })

const selectedResourceStatsResourceId = computed(() =>
  selectedResourceStatsId.value === ALL_RESOURCES_VALUE ? undefined : selectedResourceStatsId.value,
)

const selectedResourceStatsResource = computed(() =>
  organizerResources.value.find(resource => resource.id === selectedResourceStatsResourceId.value) ?? null,
)

const {
  data: resourceStatisticsData,
  isLoading: isLoadingResourceStatistics,
  refetch: refetchResourceStatistics,
  dataUpdatedAt: resourceStatisticsUpdatedAt,
} = useQuery(
  computed(() => ({
    ...resourceOrganizerQueries.statistics(hackathonId.value, selectedResourceStatsResourceId.value),
    enabled: !!hackathonId.value,
  })),
)

const REVIEW_OVERDUE_DAYS = 5
const REVIEW_OVERDUE_MS = REVIEW_OVERDUE_DAYS * 24 * 60 * 60 * 1000

const resourceStatsTimeFormatter = new Intl.DateTimeFormat(undefined, {
  dateStyle: 'medium',
  timeStyle: 'short',
  timeZone: HACKATHON_TIME_ZONE,
})

const participants = computed(() => participantsData.value?.participants ?? [])
const teams = computed(() => teamsData.value?.teams ?? [])
const challenges = computed(() => challengesData.value?.challenges ?? [])
const checkInParticipants = computed(() => venueOverviewData.value?.participants ?? [])
const resourceStatistics = computed<OrganizerResourceStatisticsResponse | null>(() => resourceStatisticsData.value ?? null)
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

function pluralize(value: number, singular: string, plural = `${singular}s`) {
  return `${value} ${value === 1 ? singular : plural}`
}

function formatResourceStatsTime(value: Date | string | null | undefined) {
  const date = parseHackathonDateTimeValue(value)
  if (!date)
    return '—'

  return `${resourceStatsTimeFormatter.format(date)} ${HACKATHON_TIME_ZONE_LABEL}`
}

function summarizeParticipantResources(teamParticipant: OrganizerResourceStatisticsTeamItem['participants'][number]) {
  const resourceNames = [...new Set(teamParticipant.redemptions.map(redemption => redemption.resourceName))]
  if (!resourceNames.length)
    return '—'

  if (resourceNames.length <= 2)
    return resourceNames.join(', ')

  return `${resourceNames.slice(0, 2).join(', ')} +${resourceNames.length - 2} more`
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

const memberCheckInByUserId = computed(() => {
  const entries = checkInParticipants.value.flatMap((participant) => {
    if (!participant.userId)
      return []

    return [[participant.userId, participant] as const]
  })

  return new Map(entries)
})

const activeParticipantsWithCheckIn = computed(() =>
  activeParticipants.value.map(participant => ({
    participant,
    checkIn: participant.id ? memberCheckInByUserId.value.get(participant.id) : undefined,
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
        const checkIn = participant.id ? memberCheckInByUserId.value.get(participant.id) : undefined
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

const resourceStatisticsOptions = computed(() => [
  { label: 'All resources', value: ALL_RESOURCES_VALUE },
  ...organizerResources.value.map(resource => ({
    label: resource.isPublished ? resource.name : `${resource.name} (Unpublished)`,
    value: resource.id,
  })),
])

const resourceStatisticsCards = computed<StatCard[]>(() => {
  const stats = resourceStatistics.value
  if (!stats)
    return []

  return [
    { label: 'Resources in Scope', value: stats.resourceCount, icon: 'i-lucide-package', color: 'primary' },
    { label: 'Redeemed Resources', value: stats.resourcesWithRedemptions, icon: 'i-lucide-badge-check', color: 'success' },
    { label: 'Total Redemptions', value: stats.totalRedemptions, icon: 'i-lucide-history', color: 'primary' },
    { label: 'Participants Redeemed', value: stats.participantsWithRedemptions, icon: 'i-lucide-user-check', color: 'success' },
    { label: 'Not Yet Redeemed', value: stats.participantsWithoutRedemptions, icon: 'i-lucide-user-minus', color: 'warning' },
    { label: 'Teams Represented', value: stats.teamsWithRedemptions, icon: 'i-lucide-users', color: 'primary' },
    { label: 'No-Team Redeemers', value: stats.redeemersWithoutTeam, icon: 'i-lucide-user-round-x', color: 'neutral' },
    { label: 'Avg / Redeemer', value: stats.averageRedemptionsPerRedeemer.toFixed(2), icon: 'i-lucide-bar-chart-3', color: 'neutral' },
  ]
})

const resourceStatisticsScopeLabel = computed(() =>
  selectedResourceStatsResource.value ? 'Selected resource' : 'All resources',
)

const resourceStatisticsScopeDescription = computed(() => {
  if (selectedResourceStatsResource.value)
    return selectedResourceStatsResource.value.description || 'No description provided for this resource.'

  return 'Combined redemption activity across every organizer resource in this hackathon.'
})

const resourceStatisticsFooter = computed(() => {
  const stats = resourceStatistics.value
  if (!stats)
    return null

  const parts = [
    `${stats.participantsWithRedemptions} of ${stats.totalParticipants} participants have redeemed in this scope`,
  ]

  if (stats.firstRedeemedAt)
    parts.push(`First redemption: ${formatResourceStatsTime(stats.firstRedeemedAt)}`)

  if (stats.lastRedeemedAt)
    parts.push(`Latest redemption: ${formatResourceStatsTime(stats.lastRedeemedAt)}`)

  return parts.join(' · ')
})

const resourceLeaderboard = computed(() =>
  [...(resourceStatistics.value?.resourceSummaries ?? [])]
    .sort((a, b) => {
      if (b.totalRedemptions !== a.totalRedemptions)
        return b.totalRedemptions - a.totalRedemptions

      const timeDiff = (parseHackathonDateTimeValue(b.lastRedeemedAt)?.getTime() ?? 0)
        - (parseHackathonDateTimeValue(a.lastRedeemedAt)?.getTime() ?? 0)

      if (timeDiff !== 0)
        return timeDiff

      return a.resourceName.localeCompare(b.resourceName, undefined, { sensitivity: 'base' })
    }),
)

const hasMultipleResourceScope = computed(() => (resourceStatistics.value?.resourceCount ?? 0) > 1)
const recentResourceActivity = computed<OrganizerResourceStatisticsRecentActivityItem[]>(() => resourceStatistics.value?.recentActivity ?? [])
const normalizedResourceBreakdownSearch = computed(() => resourceBreakdownSearch.value.trim().toLowerCase())

const filteredResourceTeamBreakdown = computed<OrganizerResourceStatisticsTeamItem[]>(() => {
  const groups = resourceStatistics.value?.teamBreakdown ?? []
  const query = normalizedResourceBreakdownSearch.value

  if (!query)
    return groups

  return groups.flatMap((group) => {
    const matchesTeam = group.teamName.toLowerCase().includes(query)
    if (matchesTeam)
      return [group]

    const participants = group.participants.filter((participant) => {
      return [participant.userName, participant.userId]
        .filter(Boolean)
        .join(' ')
        .toLowerCase()
        .includes(query)
    })

    if (!participants.length)
      return []

    const lastRedeemedAt = participants.reduce<Date | string | null | undefined>((latest, participant) => {
      const participantEpoch = parseHackathonDateTimeValue(participant.lastRedeemedAt)?.getTime() ?? 0
      const latestEpoch = parseHackathonDateTimeValue(latest)?.getTime() ?? 0
      return participantEpoch > latestEpoch ? participant.lastRedeemedAt : latest
    }, null)

    return [{
      ...group,
      redeemerCount: participants.length,
      totalRedemptions: participants.reduce((sum, participant) => sum + participant.redemptionCount, 0),
      distinctResourcesRedeemed: new Set(
        participants.flatMap(participant => participant.redemptions.map(redemption => redemption.resourceId)),
      ).size,
      lastRedeemedAt,
      participants,
    }]
  })
})
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
      <div class="space-y-6 overflow-y-auto p-4">
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

          <UCard>
            <template #header>
              <div class="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
                <div>
                  <div class="flex flex-wrap items-center gap-2">
                    <h3 class="text-sm font-semibold">
                      Resource Redemption
                    </h3>
                    <UBadge
                      size="xs"
                      variant="soft"
                      color="primary"
                    >
                      {{ resourceStatisticsScopeLabel }}
                    </UBadge>
                  </div>
                  <p class="mt-1 text-xs text-(--ui-text-muted)">
                    View redemption statistics for all resources or drill into a single resource with team-grouped participant detail.
                  </p>
                </div>

                <div class="flex flex-col gap-2 sm:flex-row sm:items-end">
                  <UFormField label="Resource scope">
                    <USelect
                      :model-value="selectedResourceStatsId"
                      :items="resourceStatisticsOptions"
                      size="sm"
                      class="w-full min-w-60"
                      :loading="isLoadingResources"
                      @update:model-value="selectedResourceStatsId = String($event || ALL_RESOURCES_VALUE)"
                    />
                  </UFormField>

                  <UButton
                    icon="i-lucide-refresh-cw"
                    size="sm"
                    variant="soft"
                    color="neutral"
                    :loading="isLoadingResourceStatistics"
                    @click="refetchResourceStatistics()"
                  >
                    Refresh
                  </UButton>
                </div>
              </div>
            </template>

            <div class="space-y-4">
              <div class="rounded-xl border border-(--ui-border) bg-(--ui-bg-elevated) p-4">
                <div class="flex flex-wrap items-center gap-2">
                  <h4 class="text-sm font-semibold">
                    {{ selectedResourceStatsResource?.name || 'All resources' }}
                  </h4>
                  <UBadge
                    size="xs"
                    variant="soft"
                    :color="selectedResourceStatsResource?.isPublished === false ? 'warning' : 'success'"
                  >
                    {{
                      selectedResourceStatsResource
                        ? selectedResourceStatsResource.isPublished ? 'Published' : 'Unpublished'
                        : 'Aggregate view'
                    }}
                  </UBadge>
                </div>
                <p class="mt-2 text-sm text-(--ui-text-muted)">
                  {{ resourceStatisticsScopeDescription }}
                </p>
              </div>

              <div
                v-if="!organizerResources.length"
                class="rounded-xl border border-dashed border-(--ui-border) p-4 text-sm text-(--ui-text-muted)"
              >
                No resources have been configured for this hackathon yet.
              </div>

              <div
                v-else-if="isLoadingResourceStatistics && !resourceStatistics"
                class="rounded-xl border border-dashed border-(--ui-border) p-4 text-sm text-(--ui-text-muted)"
              >
                Loading redemption statistics...
              </div>

              <template v-else-if="resourceStatistics">
                <div class="grid grid-cols-2 gap-4 sm:grid-cols-3 xl:grid-cols-4">
                  <div
                    v-for="card in resourceStatisticsCards"
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
                  v-if="resourceStatisticsFooter"
                  class="text-xs text-(--ui-text-muted)"
                >
                  {{ resourceStatisticsFooter }}
                </div>

                <div class="grid gap-4 xl:grid-cols-[minmax(0,1fr)_minmax(0,1.2fr)]">
                  <div class="rounded-xl border border-(--ui-border) p-4">
                    <div class="flex items-center justify-between gap-2">
                      <div>
                        <h4 class="text-sm font-semibold">
                          {{ hasMultipleResourceScope ? 'Resource Distribution' : 'Resource Snapshot' }}
                        </h4>
                        <p class="mt-1 text-xs text-(--ui-text-muted)">
                          {{
                            hasMultipleResourceScope
                              ? 'See which resources are driving redemption volume.'
                              : 'The selected resource summary for this scope.'
                          }}
                        </p>
                      </div>
                      <span class="text-xs text-(--ui-text-muted)">
                        {{ pluralize(resourceLeaderboard.length, 'resource') }}
                      </span>
                    </div>

                    <div
                      v-if="!resourceLeaderboard.length"
                      class="mt-4 text-sm text-(--ui-text-muted)"
                    >
                      No redemptions captured yet.
                    </div>

                    <ul
                      v-else
                      class="mt-4 space-y-3"
                    >
                      <li
                        v-for="resource in resourceLeaderboard"
                        :key="resource.resourceId"
                        class="rounded-lg border border-(--ui-border) bg-(--ui-bg-elevated) p-3"
                      >
                        <div class="flex flex-wrap items-center justify-between gap-2">
                          <div class="flex items-center gap-2">
                            <span class="font-medium text-(--ui-text-highlighted)">{{ resource.resourceName }}</span>
                            <UBadge
                              size="xs"
                              variant="soft"
                              :color="resource.isPublished ? 'success' : 'warning'"
                            >
                              {{ resource.isPublished ? 'Published' : 'Unpublished' }}
                            </UBadge>
                          </div>
                          <span class="text-sm font-medium text-(--ui-text-highlighted)">
                            {{ pluralize(resource.totalRedemptions, 'redemption') }}
                          </span>
                        </div>
                        <p class="mt-2 text-xs text-(--ui-text-muted)">
                          {{ pluralize(resource.uniqueRedeemers, 'participant') }} redeemed · Latest {{ formatResourceStatsTime(resource.lastRedeemedAt) }}
                        </p>
                      </li>
                    </ul>
                  </div>

                  <div class="rounded-xl border border-(--ui-border) p-4">
                    <div class="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                      <div>
                        <h4 class="text-sm font-semibold">
                          Recent Activity
                        </h4>
                        <p class="mt-1 text-xs text-(--ui-text-muted)">
                          Latest 25 redemptions in the selected scope.
                        </p>
                      </div>
                      <span
                        v-if="resourceStatisticsUpdatedAt"
                        class="text-xs text-(--ui-text-muted)"
                      >
                        Updated {{ new Date(resourceStatisticsUpdatedAt).toLocaleTimeString() }}
                      </span>
                    </div>

                    <div
                      v-if="!recentResourceActivity.length"
                      class="mt-4 text-sm text-(--ui-text-muted)"
                    >
                      No redemption activity yet.
                    </div>

                    <ul
                      v-else
                      class="mt-4 space-y-3"
                    >
                      <li
                        v-for="activity in recentResourceActivity"
                        :key="activity.redemptionId"
                        class="rounded-lg border border-(--ui-border) bg-(--ui-bg-elevated) p-3"
                      >
                        <p class="text-sm text-(--ui-text-muted)">
                          <span class="font-medium text-(--ui-text-highlighted)">{{ activity.userName }}</span>
                          redeemed
                          <span class="font-medium text-(--ui-text-highlighted)">{{ activity.resourceName }}</span>
                        </p>
                        <p class="mt-1 text-xs text-(--ui-text-muted)">
                          {{ formatResourceStatsTime(activity.timestamp) }} · {{ activity.teamName }}
                        </p>
                      </li>
                    </ul>
                  </div>
                </div>

                <div class="rounded-xl border border-(--ui-border) p-4">
                  <div class="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
                    <div>
                      <h4 class="text-sm font-semibold">
                        Team Breakdown
                      </h4>
                      <p class="mt-1 text-xs text-(--ui-text-muted)">
                        Participants that redeemed in this scope, grouped by team with counts and redemption timing.
                      </p>
                    </div>

                    <UInput
                      v-model="resourceBreakdownSearch"
                      icon="i-lucide-search"
                      size="sm"
                      class="w-full lg:max-w-sm"
                      placeholder="Search by team, participant, or user ID..."
                    />
                  </div>

                  <div
                    v-if="!resourceStatistics.teamBreakdown.length"
                    class="mt-4 text-sm text-(--ui-text-muted)"
                  >
                    No participants have redeemed in this scope yet.
                  </div>

                  <div
                    v-else-if="!filteredResourceTeamBreakdown.length"
                    class="mt-4 text-sm text-(--ui-text-muted)"
                  >
                    No teams or participants matching "{{ resourceBreakdownSearch }}".
                  </div>

                  <div
                    v-else
                    class="mt-4 space-y-4"
                  >
                    <section
                      v-for="team in filteredResourceTeamBreakdown"
                      :key="team.teamId ?? team.teamName"
                      class="rounded-lg border border-(--ui-border)"
                    >
                      <div class="border-b border-(--ui-border) bg-(--ui-bg-elevated) p-4">
                        <div class="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
                          <div>
                            <div class="flex flex-wrap items-center gap-2">
                              <h5 class="text-sm font-semibold">
                                {{ team.teamName }}
                              </h5>
                              <UBadge
                                size="xs"
                                variant="soft"
                                :color="team.teamId ? 'primary' : 'neutral'"
                              >
                                {{ team.teamId ? 'Team' : 'No team' }}
                              </UBadge>
                              <UBadge
                                v-if="hasMultipleResourceScope"
                                size="xs"
                                variant="soft"
                                color="neutral"
                              >
                                {{ pluralize(team.distinctResourcesRedeemed, 'resource') }}
                              </UBadge>
                            </div>
                            <p class="mt-1 text-xs text-(--ui-text-muted)">
                              {{ pluralize(team.redeemerCount, 'redeemer') }} ·
                              {{ pluralize(team.totalRedemptions, 'redemption') }} ·
                              {{ pluralize(team.memberCount, 'member') }}
                            </p>
                          </div>

                          <p class="text-xs text-(--ui-text-muted)">
                            Latest redemption: {{ formatResourceStatsTime(team.lastRedeemedAt) }}
                          </p>
                        </div>
                      </div>

                      <div class="overflow-x-auto">
                        <table class="w-full min-w-[56rem] text-sm">
                          <thead>
                            <tr class="text-left text-(--ui-text-muted)">
                              <th class="px-4 py-3 font-medium">
                                Participant
                              </th>
                              <th class="px-4 py-3 font-medium">
                                Count
                              </th>
                              <th class="px-4 py-3 font-medium">
                                {{ hasMultipleResourceScope ? 'Resources' : 'Scope' }}
                              </th>
                              <th class="px-4 py-3 font-medium">
                                First redeemed
                              </th>
                              <th class="px-4 py-3 font-medium">
                                Latest redeemed
                              </th>
                              <th class="px-4 py-3 font-medium">
                                Timeline
                              </th>
                            </tr>
                          </thead>
                          <tbody class="divide-y divide-(--ui-border)">
                            <tr
                              v-for="participant in team.participants"
                              :key="participant.userId"
                            >
                              <td class="px-4 py-3">
                                <div class="font-medium text-(--ui-text-highlighted)">
                                  {{ participant.userName }}
                                </div>
                                <div class="text-xs text-(--ui-text-muted)">
                                  {{ participant.userId }}
                                </div>
                              </td>
                              <td class="px-4 py-3 text-(--ui-text-highlighted)">
                                {{ participant.redemptionCount }}
                              </td>
                              <td class="px-4 py-3 text-(--ui-text-muted)">
                                <template v-if="hasMultipleResourceScope">
                                  <div>{{ pluralize(participant.distinctResourcesRedeemed, 'resource') }}</div>
                                  <div class="text-xs">
                                    {{ summarizeParticipantResources(participant) }}
                                  </div>
                                </template>
                                <span v-else>
                                  {{ selectedResourceStatsResource?.name || resourceStatistics.resourceName || 'Selected resource' }}
                                </span>
                              </td>
                              <td class="px-4 py-3 text-(--ui-text-muted)">
                                {{ formatResourceStatsTime(participant.firstRedeemedAt) }}
                              </td>
                              <td class="px-4 py-3 text-(--ui-text-muted)">
                                {{ formatResourceStatsTime(participant.lastRedeemedAt) }}
                              </td>
                              <td class="px-4 py-3 text-(--ui-text-muted)">
                                <details class="group max-w-md">
                                  <summary class="cursor-pointer text-xs hover:text-(--ui-text)">
                                    View timeline
                                  </summary>
                                  <ul class="mt-2 space-y-1 text-xs">
                                    <li
                                      v-for="redemption in participant.redemptions"
                                      :key="redemption.redemptionId"
                                    >
                                      <template v-if="hasMultipleResourceScope">
                                        <span class="text-(--ui-text-highlighted)">{{ redemption.resourceName }}</span>
                                        <span> · </span>
                                      </template>
                                      <span>{{ formatResourceStatsTime(redemption.timestamp) }}</span>
                                    </li>
                                  </ul>
                                </details>
                              </td>
                            </tr>
                          </tbody>
                        </table>
                      </div>
                    </section>
                  </div>
                </div>
              </template>
            </div>
          </UCard>
        </template>
      </div>
    </template>
  </UDashboardPanel>
</template>
