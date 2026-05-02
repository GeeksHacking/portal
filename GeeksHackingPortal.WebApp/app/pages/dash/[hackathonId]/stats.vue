<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListParticipantItem,
  GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesListResponseResponseResource,
  GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsRecentRedemptionItem,
  GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsResponse,
  GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsTeamBreakdownItem,
} from '@geekshacking/portal-sdk'
import {
  useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesListEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonTeamsListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueOverviewEndpoint1,
} from '@geekshacking/portal-sdk/hooks'
import { computed, ref, watch } from 'vue'
import * as XLSX from 'xlsx'
import { HACKATHON_TIME_ZONE, HACKATHON_TIME_ZONE_LABEL, parseHackathonDateTimeValue } from '~/utils/hackathon-date-time'

type ParticipantItem = GeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListParticipantItem
type StatColor = 'primary' | 'success' | 'error' | 'warning' | 'neutral'

interface StatCard {
  label: string
  value: string | number
  icon: string
  color: StatColor
}

interface DenseStatRow {
  label: string
  value: string | number
  color?: StatColor
}

interface DenseStatSection {
  key: string
  title: string
  description: string
  rows: DenseStatRow[]
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

const metricValueClass: Record<StatColor, string> = {
  primary: 'text-primary',
  success: 'text-green-600 dark:text-green-400',
  warning: 'text-amber-600 dark:text-amber-300',
  error: 'text-red-600 dark:text-red-400',
  neutral: 'text-(--ui-text-highlighted)',
}

const route = useRoute()
const hackathonId = computed(() => (route.params.hackathonId as string | undefined) || '')

const { data: participantsData, isLoading: isLoadingParticipants } = useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListEndpoint(
  computed(() => hackathonId.value),
)

const { data: teamsData, isLoading: isLoadingTeams } = useGeeksHackingPortalApiEndpointsOrganizersHackathonTeamsListEndpoint(
  hackathonId,
  { query: { enabled: computed(() => !!hackathonId.value) } },
)

const { data: challengesData, isLoading: isLoadingChallenges } = useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesListEndpoint(
  computed(() => hackathonId.value),
)

const { data: venueOverviewData, isLoading: isLoadingVenueOverview } = useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueOverviewEndpoint1(
  hackathonId,
  { query: { enabled: computed(() => !!hackathonId.value) } },
)

const { data: resourcesData, isLoading: isLoadingResources } = useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesListEndpoint1(
  hackathonId,
  { query: { enabled: computed(() => !!hackathonId.value) } },
)

const selectedResourceStatsId = ref(ALL_RESOURCES_VALUE)
const resourceBreakdownSearch = ref('')
const resourceRedemptionMemberThreshold = ref(3)

const organizerResources = computed<GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesListResponseResponseResource[]>(() => resourcesData.value?.resources ?? [])

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
} = useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsEndpoint1(
  hackathonId,
  computed(() => selectedResourceStatsResourceId.value ? { resourceId: selectedResourceStatsResourceId.value } : {}),
  { query: { enabled: computed(() => !!hackathonId.value) } },
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
const resourceStatistics = computed<GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsResponse | null>(() => resourceStatisticsData.value ?? null)
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

function summarizeParticipantResources(teamParticipant: GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsTeamBreakdownItem['participants'][number]) {
  const resourceNames = [...new Set(teamParticipant.redemptions.map(redemption => redemption.resourceName))]
  if (!resourceNames.length)
    return '—'

  if (resourceNames.length <= 2)
    return resourceNames.join(', ')

  return `${resourceNames.slice(0, 2).join(', ')} +${resourceNames.length - 2} more`
}

function getResourceTeamRedeemerGap(team: GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsTeamBreakdownItem) {
  return Math.max((team.memberCount ?? 0) - team.redeemerCount, 0)
}

function createExportFilename(extension: 'csv' | 'xlsx') {
  const dateStamp = new Date().toISOString().split('T')[0]
  const scopeName = selectedResourceStatsResource.value?.name || 'all-resources'
  const normalizedScope = scopeName
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '') || 'resource-redemptions'

  return `resource-redemptions-${normalizedScope}-${dateStamp}.${extension}`
}

const normalizedResourceRedemptionMemberThreshold = computed(() => {
  const parsedValue = Math.trunc(Number(resourceRedemptionMemberThreshold.value))
  if (!Number.isFinite(parsedValue) || parsedValue < 1)
    return 1

  return parsedValue
})

const activeParticipants = computed(() => participants.value.filter(participant => !isWithdrawn(participant)))
const completeActiveParticipants = computed(() => activeParticipants.value.filter(participant => !isIncomplete(participant)))
const withdrawnParticipants = computed(() => participants.value.filter(participant => isWithdrawn(participant)))

const pendingReviewParticipants = computed(() =>
  completeActiveParticipants.value.filter(participant =>
    participant.concludedStatus === 'Pending'
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
    participant => participant.concludedStatus === 'Accepted',
  ),
)

const rejectedParticipants = computed(() =>
  completeActiveParticipants.value.filter(
    participant => participant.concludedStatus === 'Rejected',
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

const overviewStats = computed<StatCard[]>(() => [
  { label: 'Active Participants', value: activeParticipants.value.length, icon: 'i-lucide-users', color: 'primary' },
  { label: 'Complete Registrations', value: completeActiveParticipants.value.length, icon: 'i-lucide-file-check', color: 'success' },
  { label: 'Pending Review', value: pendingReviewParticipants.value.length, icon: 'i-lucide-clock-3', color: 'warning' },
  { label: `Overdue (${REVIEW_OVERDUE_DAYS}d+)`, value: overdueParticipants.value.length, icon: 'i-lucide-triangle-alert', color: 'error' },
  { label: 'Teams', value: teams.value.length, icon: 'i-lucide-users-round', color: 'primary' },
  { label: 'Checked In Now', value: checkedInMembers.value, icon: 'i-lucide-user-check', color: 'success' },
  { label: 'Published Challenges', value: publishedChallenges.value.length, icon: 'i-lucide-trophy', color: 'primary' },
  { label: 'Emails Sent', value: emailsSent.value, icon: 'i-lucide-mail', color: 'neutral' },
])

const denseStatSections = computed<DenseStatSection[]>(() => [
  {
    key: 'registration',
    title: 'Registration Pipeline',
    description: 'Current participant volume and completion status.',
    rows: [
      { label: 'Total registered', value: participants.value.length, color: 'primary' },
      { label: 'Active', value: activeParticipants.value.length, color: 'success' },
      { label: 'Complete', value: completeActiveParticipants.value.length, color: 'success' },
      { label: 'Incomplete', value: activeParticipants.value.filter(participant => isIncomplete(participant)).length, color: 'warning' },
      { label: 'Withdrawn', value: withdrawnParticipants.value.length, color: 'neutral' },
    ],
    footer: registrationCompletionRate.value === null
      ? null
      : `Completion rate: ${registrationCompletionRate.value}% of active participants`,
  },
  {
    key: 'review',
    title: 'Review Queue',
    description: 'Admissions work that still needs organizer attention.',
    rows: [
      { label: 'Pending review', value: pendingReviewParticipants.value.length, color: 'warning' },
      { label: `Overdue (${REVIEW_OVERDUE_DAYS}d+)`, value: overdueParticipants.value.length, color: 'error' },
      { label: 'Accepted', value: acceptedParticipants.value.length, color: 'success' },
      { label: 'Rejected', value: rejectedParticipants.value.length, color: 'error' },
      { label: 'Accepted in teams', value: acceptedInTeam.value, color: 'success' },
      { label: 'Accepted without teams', value: acceptedNotInTeam.value, color: 'warning' },
    ],
    footer: acceptanceRate.value === null
      ? null
      : `Acceptance rate: ${acceptanceRate.value}% of reviewed applications`,
  },
  {
    key: 'teams',
    title: 'Team Health',
    description: 'Formation, sizing, and venue attendance by team.',
    rows: [
      { label: 'Number of teams', value: teams.value.length, color: 'primary' },
      { label: 'Participants in a team', value: inTeamParticipants.value.length, color: 'success' },
      { label: 'Participants without a team', value: notInTeamParticipants.value.length, color: 'warning' },
      { label: 'Average members / team', value: averageMembersPerTeam.value.toFixed(1), color: 'primary' },
      { label: 'Largest team', value: largestTeamSize.value, color: 'neutral' },
      { label: 'All members checked in', value: teamCheckInSummary.value.fullyCheckedInTeams, color: 'success' },
      { label: 'Partially checked in', value: teamCheckInSummary.value.partiallyCheckedInTeams, color: 'warning' },
      { label: 'No members checked in', value: teamCheckInSummary.value.noCheckedInTeams, color: 'neutral' },
    ],
    footer: teamFormationRate.value === null
      ? null
      : `Team formation rate: ${teamFormationRate.value}% of active participants`,
  },
  {
    key: 'venue',
    title: 'Venue Activity',
    description: 'Check-in coverage across current participants.',
    rows: [
      { label: 'Checked in now', value: checkedInMembers.value, color: 'success' },
      { label: 'Checked out', value: checkedOutMembers.value, color: 'warning' },
      { label: 'No check-in yet', value: membersWithoutCheckIn.value, color: 'neutral' },
      { label: 'Total check-ins', value: totalMemberCheckIns.value, color: 'primary' },
    ],
    footer: memberCheckInRate.value === null
      ? null
      : `Current check-in rate: ${memberCheckInRate.value}% of active participants`,
  },
  {
    key: 'challenges',
    title: 'Challenge Coverage',
    description: 'Publishing status and how evenly teams are assigning themselves.',
    rows: [
      { label: 'Challenge statements', value: challenges.value.length, color: 'primary' },
      { label: 'Published', value: publishedChallenges.value.length, color: 'success' },
      { label: 'Drafts', value: draftChallenges.value.length, color: 'warning' },
      { label: 'Teams selected', value: teamsWithSelectedChallenge.value.length, color: 'success' },
      { label: 'Teams unselected', value: teamsWithoutSelectedChallenge.value.length, color: 'warning' },
      { label: 'Challenges with picks', value: challengesWithSelections.value.length, color: 'success' },
      { label: 'Challenges without picks', value: challengesWithoutSelections.value.length, color: 'neutral' },
      { label: 'Max teams / challenge', value: maxTeamsOnSingleChallenge.value, color: 'primary' },
      { label: 'Average teams / published', value: averageTeamsPerPublishedChallenge.value.toFixed(1), color: 'primary' },
    ],
    footer: challengeSelectionRate.value === null
      ? null
      : `Challenge selection rate: ${challengeSelectionRate.value}% of teams`,
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
    { label: 'Redeemed Resources', value: stats.resourcesWithRedemptions, icon: 'i-lucide-badge-check', color: 'success' },
    { label: 'Total Redemptions', value: stats.totalRedemptions, icon: 'i-lucide-history', color: 'primary' },
    { label: 'Participants Redeemed', value: stats.participantsWithRedemptions, icon: 'i-lucide-user-check', color: 'success' },
    { label: 'Not Yet Redeemed', value: stats.participantsWithoutRedemptions, icon: 'i-lucide-user-minus', color: 'warning' },
    { label: 'Teams Represented', value: stats.teamsWithRedemptions, icon: 'i-lucide-users', color: 'primary' },
    { label: 'Resources in Scope', value: stats.resourceCount, icon: 'i-lucide-package', color: 'neutral' },
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
const recentResourceActivity = computed<GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsRecentRedemptionItem[]>(() => resourceStatistics.value?.recentActivity ?? [])
const normalizedResourceBreakdownSearch = computed(() => resourceBreakdownSearch.value.trim().toLowerCase())

const resourceTeamBreakdown = computed<GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsTeamBreakdownItem[]>(() => {
  const teamBreakdown = resourceStatistics.value?.teamBreakdown ?? []
  const teamBreakdownById = new Map(
    teamBreakdown
      .filter((team): team is GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsTeamBreakdownItem & { teamId: string } => !!team.teamId)
      .map(team => [team.teamId, team] as const),
  )
  const noTeamBreakdown = teamBreakdown.find(team => !team.teamId)

  const mergedTeams = teams.value.map((team) => {
    const existingTeam = team.id ? teamBreakdownById.get(team.id) : undefined

    return {
      teamId: team.id ?? null,
      teamName: team.name ?? 'Unnamed team',
      memberCount: team.memberCount ?? existingTeam?.memberCount ?? 0,
      redeemerCount: existingTeam?.redeemerCount ?? 0,
      totalRedemptions: existingTeam?.totalRedemptions ?? 0,
      distinctResourcesRedeemed: existingTeam?.distinctResourcesRedeemed ?? 0,
      lastRedeemedAt: existingTeam?.lastRedeemedAt ?? null,
      participants: existingTeam?.participants ?? [],
    }
  })

  const rows = noTeamBreakdown ? [...mergedTeams, noTeamBreakdown] : mergedTeams

  return rows.sort((a, b) => {
    const flaggedDelta = Number(isResourceTeamFlagged(b)) - Number(isResourceTeamFlagged(a))
    if (flaggedDelta !== 0)
      return flaggedDelta

    if (b.totalRedemptions !== a.totalRedemptions)
      return b.totalRedemptions - a.totalRedemptions

    const lastRedeemedDelta = (parseHackathonDateTimeValue(b.lastRedeemedAt)?.getTime() ?? 0)
      - (parseHackathonDateTimeValue(a.lastRedeemedAt)?.getTime() ?? 0)

    if (lastRedeemedDelta !== 0)
      return lastRedeemedDelta

    return a.teamName.localeCompare(b.teamName, undefined, { sensitivity: 'base' })
  })
})

const flaggedResourceTeams = computed(() =>
  resourceTeamBreakdown.value.filter(team => isResourceTeamFlagged(team)),
)

const flaggedResourceTeamSummary = computed(() => {
  const threshold = normalizedResourceRedemptionMemberThreshold.value
  const count = flaggedResourceTeams.value.length
  if (!count)
    return `No teams are below the ${threshold}-member redemption threshold.`

  return `${count} team${count === 1 ? '' : 's'} below the ${threshold}-member redemption threshold.`
})

function isResourceTeamFlagged(team: GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsTeamBreakdownItem) {
  return !!team.teamId && team.redeemerCount < normalizedResourceRedemptionMemberThreshold.value
}

const resourceExportSummaryRows = computed(() => {
  const stats = resourceStatistics.value
  if (!stats)
    return []

  return [
    { Metric: 'Scope', Value: selectedResourceStatsResource.value?.name || 'All resources' },
    { Metric: 'Resources in Scope', Value: stats.resourceCount },
    { Metric: 'Redeemed Resources', Value: stats.resourcesWithRedemptions },
    { Metric: 'Resources Without Redemptions', Value: stats.resourcesWithoutRedemptions },
    { Metric: 'Total Participants', Value: stats.totalParticipants },
    { Metric: 'Participants Redeemed', Value: stats.participantsWithRedemptions },
    { Metric: 'Participants Without Redemption', Value: stats.participantsWithoutRedemptions },
    { Metric: 'Teams Represented', Value: stats.teamsWithRedemptions },
    { Metric: 'No-Team Redeemers', Value: stats.redeemersWithoutTeam },
    { Metric: 'Total Redemptions', Value: stats.totalRedemptions },
    { Metric: 'Average Redemptions Per Redeemer', Value: stats.averageRedemptionsPerRedeemer },
    { Metric: 'First Redemption', Value: formatResourceStatsTime(stats.firstRedeemedAt) },
    { Metric: 'Latest Redemption', Value: formatResourceStatsTime(stats.lastRedeemedAt) },
    { Metric: 'Highlight Threshold', Value: normalizedResourceRedemptionMemberThreshold.value },
    { Metric: 'Flagged Teams', Value: flaggedResourceTeams.value.length },
  ]
})

const resourceExportResourceRows = computed(() =>
  resourceLeaderboard.value.map(resource => ({
    'Resource Name': resource.resourceName,
    'Published': resource.isPublished ? 'Yes' : 'No',
    'Total Redemptions': resource.totalRedemptions,
    'Unique Redeemers': resource.uniqueRedeemers,
    'Last Redeemed': formatResourceStatsTime(resource.lastRedeemedAt),
  })),
)

const resourceExportTeamRows = computed(() =>
  resourceTeamBreakdown.value.map(team => ({
    'Team Name': team.teamName,
    'Team Type': team.teamId ? 'Team' : 'No team',
    'Member Count': team.memberCount,
    'Redeemer Count': team.redeemerCount,
    'Total Redemptions': team.totalRedemptions,
    'Distinct Resources Redeemed': team.distinctResourcesRedeemed,
    'Missing Redeemers': getResourceTeamRedeemerGap(team),
    'Flagged Below Threshold': isResourceTeamFlagged(team) ? 'Yes' : 'No',
    'Latest Redemption': formatResourceStatsTime(team.lastRedeemedAt),
  })),
)

const resourceExportParticipantRows = computed(() =>
  resourceTeamBreakdown.value.flatMap(team =>
    team.participants.map(participant => ({
      'Team Name': team.teamName,
      'Participant Name': participant.userName,
      'User ID': participant.userId,
      'Member Count': team.memberCount,
      'Redeemer Count': team.redeemerCount,
      'Participant Redemption Count': participant.redemptionCount,
      'Distinct Resources Redeemed': participant.distinctResourcesRedeemed,
      'First Redeemed': formatResourceStatsTime(participant.firstRedeemedAt),
      'Latest Redeemed': formatResourceStatsTime(participant.lastRedeemedAt),
      'Resources': summarizeParticipantResources(participant),
      'Flagged Team': isResourceTeamFlagged(team) ? 'Yes' : 'No',
      'Timeline': participant.redemptions
        .map((redemption) => {
          const timestamp = formatResourceStatsTime(redemption.timestamp)
          return hasMultipleResourceScope.value ? `${redemption.resourceName}: ${timestamp}` : timestamp
        })
        .join(' | '),
    })),
  ),
)

const resourceExportActivityRows = computed(() =>
  recentResourceActivity.value.map(activity => ({
    'Team Name': activity.teamName,
    'Participant Name': activity.userName,
    'User ID': activity.userId,
    'Resource Name': activity.resourceName,
    'Redeemed At': formatResourceStatsTime(activity.timestamp),
  })),
)

function downloadCsv(filename: string, rows: Record<string, string | number>[]) {
  if (!rows.length || typeof window === 'undefined')
    return

  const worksheet = XLSX.utils.json_to_sheet(rows)
  const csvContent = XLSX.utils.sheet_to_csv(worksheet)
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}

function exportResourceStatsCsv() {
  downloadCsv(createExportFilename('csv'), resourceExportParticipantRows.value)
}

function exportResourceStatsExcel() {
  if (typeof window === 'undefined')
    return

  const workbook = XLSX.utils.book_new()

  XLSX.utils.book_append_sheet(
    workbook,
    XLSX.utils.json_to_sheet(resourceExportSummaryRows.value),
    'Summary',
  )
  XLSX.utils.book_append_sheet(
    workbook,
    XLSX.utils.json_to_sheet(resourceExportResourceRows.value),
    'Resources',
  )
  XLSX.utils.book_append_sheet(
    workbook,
    XLSX.utils.json_to_sheet(resourceExportTeamRows.value),
    'Teams',
  )
  XLSX.utils.book_append_sheet(
    workbook,
    XLSX.utils.json_to_sheet(resourceExportParticipantRows.value),
    'Participants',
  )
  XLSX.utils.book_append_sheet(
    workbook,
    XLSX.utils.json_to_sheet(resourceExportActivityRows.value),
    'Recent Activity',
  )

  XLSX.writeFile(workbook, createExportFilename('xlsx'))
}

const filteredResourceTeamBreakdown = computed<GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsTeamBreakdownItem[]>(() => {
  const groups = resourceTeamBreakdown.value
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
      <div class="space-y-4">
        <div
          v-if="isLoading"
          class="text-sm text-(--ui-text-muted)"
        >
          Loading statistics...
        </div>

        <template v-else>
          <UCard>
            <template #header>
              <div class="flex flex-col gap-3 xl:flex-row xl:items-start xl:justify-between">
                <div>
                  <h3 class="text-sm font-semibold">
                    Operations Overview
                  </h3>
                  <p class="mt-1 text-xs text-(--ui-text-muted)">
                    Core organizer metrics surfaced first, with the detailed breakdown below.
                  </p>
                </div>

                <div class="flex flex-wrap gap-2 text-xs">
                  <UBadge
                    variant="soft"
                    color="neutral"
                    size="sm"
                  >
                    Completion {{ registrationCompletionRate === null ? '—' : `${registrationCompletionRate}%` }}
                  </UBadge>
                  <UBadge
                    variant="soft"
                    color="neutral"
                    size="sm"
                  >
                    Acceptance {{ acceptanceRate === null ? '—' : `${acceptanceRate}%` }}
                  </UBadge>
                  <UBadge
                    variant="soft"
                    color="neutral"
                    size="sm"
                  >
                    Team formation {{ teamFormationRate === null ? '—' : `${teamFormationRate}%` }}
                  </UBadge>
                  <UBadge
                    variant="soft"
                    color="neutral"
                    size="sm"
                  >
                    Checked in {{ memberCheckInRate === null ? '—' : `${memberCheckInRate}%` }}
                  </UBadge>
                </div>
              </div>
            </template>

            <div class="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
              <div
                v-for="card in overviewStats"
                :key="card.label"
                class="rounded-xl border border-(--ui-border) border-l-3 bg-(--ui-bg-elevated) p-3"
                :class="cardBorderClass[card.color]"
              >
                <div class="flex items-start justify-between gap-3">
                  <div>
                    <p class="text-xs text-(--ui-text-muted)">
                      {{ card.label }}
                    </p>
                    <p class="mt-2 text-2xl font-semibold text-(--ui-text-highlighted)">
                      {{ card.value }}
                    </p>
                  </div>
                  <div class="rounded-lg bg-(--ui-bg) p-2">
                    <UIcon
                      :name="card.icon"
                      class="h-4 w-4 shrink-0"
                      :class="cardIconClass[card.color]"
                    />
                  </div>
                </div>
              </div>
            </div>
          </UCard>

          <UCard>
            <template #header>
              <div>
                <h3 class="text-sm font-semibold">
                  Breakdown
                </h3>
                <p class="mt-1 text-xs text-(--ui-text-muted)">
                  Denser operational summaries grouped by workflow instead of one card per metric block.
                </p>
              </div>
            </template>

            <div class="grid gap-4 xl:grid-cols-2">
              <section
                v-for="section in denseStatSections"
                :key="section.key"
                class="rounded-xl border border-(--ui-border) bg-(--ui-bg-elevated) p-4"
              >
                <div class="flex flex-col gap-1">
                  <h4 class="text-sm font-semibold">
                    {{ section.title }}
                  </h4>
                  <p class="text-xs text-(--ui-text-muted)">
                    {{ section.description }}
                  </p>
                </div>

                <dl class="mt-4 divide-y divide-(--ui-border)">
                  <div
                    v-for="row in section.rows"
                    :key="row.label"
                    class="flex items-center justify-between gap-4 py-2.5"
                  >
                    <dt class="text-sm text-(--ui-text-muted)">
                      {{ row.label }}
                    </dt>
                    <dd
                      class="text-sm font-semibold"
                      :class="metricValueClass[row.color ?? 'neutral']"
                    >
                      {{ row.value }}
                    </dd>
                  </div>
                </dl>

                <p
                  v-if="section.footer"
                  class="mt-4 text-xs text-(--ui-text-muted)"
                >
                  {{ section.footer }}
                </p>
              </section>
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
                    {{ selectedResourceStatsResource?.description || 'View redemption statistics for all resources or drill into a single resource with team-grouped participant detail.' }}
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

                  <div class="flex flex-wrap gap-2">
                    <UButton
                      icon="i-lucide-file-spreadsheet"
                      size="sm"
                      variant="soft"
                      color="primary"
                      :disabled="!resourceStatistics"
                      @click="exportResourceStatsExcel"
                    >
                      Excel
                    </UButton>

                    <UButton
                      icon="i-lucide-file-text"
                      size="sm"
                      variant="soft"
                      color="neutral"
                      :disabled="!resourceExportParticipantRows.length"
                      @click="exportResourceStatsCsv"
                    >
                      CSV
                    </UButton>

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
              </div>
            </template>

            <div class="space-y-4">
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
                  <span class="text-xs text-(--ui-text-muted)">
                    {{ resourceStatisticsScopeDescription }}
                  </span>
                </div>

                <div class="grid grid-cols-2 gap-3 sm:grid-cols-3 xl:grid-cols-6">
                  <div
                    v-for="card in resourceStatisticsCards"
                    :key="card.label"
                    class="rounded-xl border border-(--ui-border) border-l-3 bg-(--ui-bg-elevated) p-3"
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

                  <div class="grid gap-3 lg:grid-cols-[minmax(0,16rem)_1fr] lg:items-end">
                    <UFormField label="Highlight teams with fewer than">
                      <UInput
                        :model-value="String(normalizedResourceRedemptionMemberThreshold)"
                        type="number"
                        min="1"
                        size="sm"
                        @update:model-value="resourceRedemptionMemberThreshold = Math.max(1, Number($event || 1))"
                      />
                    </UFormField>

                    <div
                      class="rounded-lg border p-3 text-sm"
                      :class="flaggedResourceTeams.length ? 'border-amber-300 bg-amber-50/70 text-amber-900 dark:border-amber-700 dark:bg-amber-900/20 dark:text-amber-100' : 'border-(--ui-border) bg-(--ui-bg-elevated) text-(--ui-text-muted)'"
                    >
                      <div class="flex flex-wrap items-center gap-2">
                        <UIcon
                          :name="flaggedResourceTeams.length ? 'i-lucide-triangle-alert' : 'i-lucide-badge-check'"
                          class="h-4 w-4 shrink-0"
                        />
                        <span class="font-medium">{{ flaggedResourceTeamSummary }}</span>
                      </div>
                      <p
                        v-if="flaggedResourceTeams.length"
                        class="mt-1 text-xs"
                      >
                        Missing redeemers are calculated against current team member count in this hackathon.
                      </p>
                    </div>
                  </div>

                  <div
                    v-if="!resourceTeamBreakdown.length"
                    class="mt-4 text-sm text-(--ui-text-muted)"
                  >
                    No teams found for this hackathon.
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
                      class="rounded-lg border"
                      :class="isResourceTeamFlagged(team) ? 'border-amber-300 bg-amber-50/30 dark:border-amber-700 dark:bg-amber-900/10' : 'border-(--ui-border)'"
                    >
                      <div
                        class="border-b border-(--ui-border) p-4"
                        :class="isResourceTeamFlagged(team) ? 'bg-amber-50/80 dark:bg-amber-900/20' : 'bg-(--ui-bg-elevated)'"
                      >
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
                              <UBadge
                                v-if="isResourceTeamFlagged(team)"
                                size="xs"
                                variant="soft"
                                color="warning"
                              >
                                Below threshold
                              </UBadge>
                            </div>
                            <p class="mt-1 text-xs text-(--ui-text-muted)">
                              {{ pluralize(team.redeemerCount, 'redeemer') }} ·
                              {{ pluralize(team.totalRedemptions, 'redemption') }} ·
                              {{ pluralize(team.memberCount, 'member') }}
                            </p>
                            <p
                              v-if="isResourceTeamFlagged(team)"
                              class="mt-1 text-xs text-amber-800 dark:text-amber-200"
                            >
                              {{ pluralize(getResourceTeamRedeemerGap(team), 'member') }} still without a redemption in this scope.
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
                            <tr v-if="!team.participants.length">
                              <td
                                colspan="6"
                                class="px-4 py-4 text-sm text-(--ui-text-muted)"
                              >
                                No one in this team has redeemed within the selected resource scope yet.
                              </td>
                            </tr>
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
