<script setup lang="ts">
import { computed } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import { participantOrganizerQueries } from '~/composables/participants'
import { HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject } from '~/api-client/models'

const props = defineProps<{
  hackathonId: string
  isOrganizer: boolean
}>()

const REVIEW_OVERDUE_DAYS = 5
const REVIEW_OVERDUE_MS = REVIEW_OVERDUE_DAYS * 24 * 60 * 60 * 1000

const { data: participantsData, isLoading } = useQuery(
  computed(() => ({
    ...participantOrganizerQueries.list(props.hackathonId),
    enabled: !!props.hackathonId && props.isOrganizer,
  })),
)

const stats = computed(() => {
  const all = participantsData.value?.participants ?? []
  const complete = all.filter(p => (p.registrationSubmissions?.length ?? 0) > 0)
  const incomplete = all.filter(p => !p.registrationSubmissions?.length)

  const pending = complete.filter(
    p =>
      p.concludedStatus
      === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Pending
      || p.concludedStatus === null
      || p.concludedStatus === undefined,
  )

  const overdue = pending.filter((p) => {
    const submissions = p.registrationSubmissions ?? []
    if (!submissions.length) return false
    const latestMs = submissions.reduce((max, s) => {
      const t = s.updatedAt?.getTime() ?? 0
      return t > max ? t : max
    }, 0)
    return latestMs > 0 && Date.now() - latestMs >= REVIEW_OVERDUE_MS
  })

  const accepted = complete.filter(
    p =>
      p.concludedStatus
      === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Accepted,
  )

  const rejected = complete.filter(
    p =>
      p.concludedStatus
      === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Rejected,
  )

  const inTeam = accepted.filter(p => !!p.teamId)
  const notInTeam = accepted.filter(p => !p.teamId)

  const teamIds = new Set(inTeam.map(p => p.teamId).filter(Boolean))
  const numTeams = teamIds.size
  const avgPerTeam = numTeams > 0 ? Math.round((inTeam.length / numTeams) * 10) / 10 : 0

  return {
    total: all.length,
    incomplete: incomplete.length,
    pending: pending.length,
    overdue: overdue.length,
    accepted: accepted.length,
    rejected: rejected.length,
    inTeam: inTeam.length,
    notInTeam: notInTeam.length,
    numTeams,
    avgPerTeam,
  }
})

const statItems = computed(() => [
  {
    label: 'Total Registered',
    value: stats.value.total,
    icon: 'i-lucide-users',
    color: 'text-(--ui-text)',
    bg: 'bg-(--ui-bg-elevated)',
  },
  {
    label: 'Incomplete',
    value: stats.value.incomplete,
    icon: 'i-lucide-file-question',
    color: 'text-(--ui-text-muted)',
    bg: 'bg-(--ui-bg-elevated)',
  },
  {
    label: 'Pending Review',
    value: stats.value.pending,
    icon: 'i-lucide-clock',
    color: 'text-warning-500',
    bg: 'bg-warning-50 dark:bg-warning-950/30',
  },
  {
    label: `Overdue (${REVIEW_OVERDUE_DAYS}d+)`,
    value: stats.value.overdue,
    icon: 'i-lucide-alert-triangle',
    color: 'text-error-500',
    bg: 'bg-error-50 dark:bg-error-950/30',
  },
  {
    label: 'Accepted',
    value: stats.value.accepted,
    icon: 'i-lucide-check-circle',
    color: 'text-success-500',
    bg: 'bg-success-50 dark:bg-success-950/30',
  },
  {
    label: 'Rejected',
    value: stats.value.rejected,
    icon: 'i-lucide-x-circle',
    color: 'text-error-500',
    bg: 'bg-error-50 dark:bg-error-950/30',
  },
  {
    label: 'In a Team',
    value: stats.value.inTeam,
    icon: 'i-lucide-users-round',
    color: 'text-primary-500',
    bg: 'bg-primary-50 dark:bg-primary-950/30',
  },
  {
    label: 'Not in a Team',
    value: stats.value.notInTeam,
    icon: 'i-lucide-user',
    color: 'text-warning-500',
    bg: 'bg-warning-50 dark:bg-warning-950/30',
  },
  {
    label: 'Teams Formed',
    value: stats.value.numTeams,
    icon: 'i-lucide-layout-grid',
    color: 'text-primary-500',
    bg: 'bg-primary-50 dark:bg-primary-950/30',
  },
  {
    label: 'Avg per Team',
    value: stats.value.avgPerTeam,
    icon: 'i-lucide-bar-chart-2',
    color: 'text-(--ui-text-muted)',
    bg: 'bg-(--ui-bg-elevated)',
  },
])
</script>

<template>
  <UCard>
    <template #header>
      <h3 class="text-sm font-semibold">
        Participant Analytics
      </h3>
    </template>

    <div
      v-if="isLoading"
      class="text-(--ui-text-muted) text-sm"
    >
      Loading analytics...
    </div>

    <div
      v-else
      class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3"
    >
      <div
        v-for="item in statItems"
        :key="item.label"
        :class="['rounded-lg p-4 flex flex-col gap-2', item.bg]"
      >
        <div class="flex items-center justify-between">
          <span class="text-xs text-(--ui-text-muted) font-medium">{{ item.label }}</span>
          <UIcon
            :name="item.icon"
            :class="['w-4 h-4', item.color]"
          />
        </div>
        <span :class="['text-2xl font-bold', item.color]">{{ item.value }}</span>
      </div>
    </div>
  </UCard>
</template>
