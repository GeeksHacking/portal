@@ -0,0 +1,350 @@
<script setup lang="ts">
    import type { HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantItem } from '~/api-client/models'
    import { useQuery } from '@tanstack/vue-query'
    import { HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject } from '~/api-client/models'

    type ParticipantItem = HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantItem

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

    const REVIEW_OVERDUE_DAYS = 5
    const REVIEW_OVERDUE_MS = REVIEW_OVERDUE_DAYS * 24 * 60 * 60 * 1000

    const participants = computed(() => participantsData.value?.participants ?? [])
    const teams = computed(() => teamsData.value?.teams ?? [])

    function isWithdrawn(participant: ParticipantItem) {
        const withdrawnAt = (participant as unknown as { withdrawnAt?: Date | string | null }).withdrawnAt
        return !!withdrawnAt
    }

    function isIncomplete(participant: ParticipantItem) {
        return !participant.registrationSubmissions?.length
    }

    function getApplicationTimeEpoch(participant: ParticipantItem) {
        const submissions = participant.registrationSubmissions ?? []
        if (!submissions.length)
            return 0
        return submissions.reduce((max, s) => {
            const t = s.updatedAt?.getTime() ?? 0
            return t > max ? t : max
        }, 0)
    }

    const activeParticipants = computed(() => participants.value.filter(p => !isWithdrawn(p)))
    const completeActiveParticipants = computed(() => activeParticipants.value.filter(p => !isIncomplete(p)))
    const withdrawnParticipants = computed(() => participants.value.filter(p => isWithdrawn(p)))

    const pendingReviewParticipants = computed(() => completeActiveParticipants.value.filter(
        p => p.concludedStatus === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Pending
            || p.concludedStatus === null
            || p.concludedStatus === undefined,
    ))

    const overdueParticipants = computed(() => pendingReviewParticipants.value.filter((p) => {
        const submittedAtEpoch = getApplicationTimeEpoch(p)
        if (!submittedAtEpoch)
            return false
        return Date.now() - submittedAtEpoch >= REVIEW_OVERDUE_MS
    }))

    const acceptedParticipants = computed(() => completeActiveParticipants.value.filter(
        p => p.concludedStatus === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Accepted,
    ))

    const rejectedParticipants = computed(() => completeActiveParticipants.value.filter(
        p => p.concludedStatus === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Rejected,
    ))

    const inTeamParticipants = computed(() => activeParticipants.value.filter(p => !!p.teamId))
    const notInTeamParticipants = computed(() => activeParticipants.value.filter(p => !p.teamId))

    const totalTeamMembers = computed(() => teams.value.reduce((sum, team) => sum + (team.memberCount ?? 0), 0))
    const averageMembersPerTeam = computed(() => {
        if (!teams.value.length)
            return 0
        return totalTeamMembers.value / teams.value.length
    })
    const largestTeamSize = computed(() => teams.value.reduce((max, team) => Math.max(max, team.memberCount ?? 0), 0))

    const acceptanceRate = computed(() => {
        const reviewed = acceptedParticipants.value.length + rejectedParticipants.value.length
        if (reviewed === 0)
            return null
        return ((acceptedParticipants.value.length / reviewed) * 100).toFixed(1)
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

    const acceptedInTeam = computed(() => acceptedParticipants.value.filter(p => !!p.teamId).length)
    const acceptedNotInTeam = computed(() => acceptedParticipants.value.filter(p => !p.teamId).length)
    const emailsSent = computed(() => participants.value.reduce((sum, p) => sum + (p.emailSentCount ?? 0), 0))

    type StatCard = {
        label: string
        value: string | number
        icon: string
        color: 'primary' | 'success' | 'error' | 'warning' | 'neutral'
    }

    const registrationStats = computed<StatCard[]>(() => [
        { label: 'Total Registered', value: participants.value.length, icon: 'i-lucide-users', color: 'primary' },
        { label: 'Active', value: activeParticipants.value.length, icon: 'i-lucide-user-check', color: 'success' },
        { label: 'Complete Registrations', value: completeActiveParticipants.value.length, icon: 'i-lucide-file-check', color: 'success' },
        { label: 'Incomplete Registrations', value: activeParticipants.value.filter(p => isIncomplete(p)).length, icon: 'i-lucide-file-warning', color: 'warning' },
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
    ])

    const additionalStats = computed<StatCard[]>(() => [
        { label: 'Accepted (in team)', value: acceptedInTeam.value, icon: 'i-lucide-user-round-check', color: 'success' },
        { label: 'Accepted (no team)', value: acceptedNotInTeam.value, icon: 'i-lucide-user-round-x', color: 'warning' },
        { label: 'Emails Sent', value: emailsSent.value, icon: 'i-lucide-mail', color: 'neutral' },
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
                <div v-if="isLoadingParticipants || isLoadingTeams"
                     class="text-sm text-(--ui-text-muted)">
                    Loading statistics...
                </div>

                <template v-else>
                    <UCard>
                        <template #header>
                            <h3 class="text-sm font-semibold">
                                Registration
                            </h3>
                        </template>
                        <div class="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
                            <div v-for="card in registrationStats"
                                 :key="card.label"
                                 class="rounded-lg border border-(--ui-border) p-3 border-l-3"
                                 :class="{
                  'border-l-primary': card.color === 'primary',
                  'border-l-green-500 dark:border-l-green-400': card.color === 'success',
                  'border-l-amber-500 dark:border-l-amber-400': card.color === 'warning',
                  'border-l-red-500 dark:border-l-red-400': card.color === 'error',
                  'border-l-gray-400 dark:border-l-gray-500': card.color === 'neutral',
                }">
                                <div class="flex items-center gap-2 mb-1">
                                    <UIcon :name="card.icon"
                                           class="w-4 h-4 shrink-0"
                                           :class="{
                      'text-primary': card.color === 'primary',
                      'text-green-500 dark:text-green-400': card.color === 'success',
                      'text-amber-500 dark:text-amber-400': card.color === 'warning',
                      'text-red-500 dark:text-red-400': card.color === 'error',
                      'text-(--ui-text-muted)': card.color === 'neutral',
                    }" />
                                    <span class="text-xs text-(--ui-text-muted)">{{ card.label }}</span>
                                </div>
                                <p class="text-2xl font-semibold text-(--ui-text-highlighted)">
                                    {{ card.value }}
                                </p>
                            </div>
                        </div>
                        <div v-if="registrationCompletionRate !== null"
                             class="mt-3 text-xs text-(--ui-text-muted)">
                            Registration completion rate: <strong>{{ registrationCompletionRate }}%</strong>
                        </div>
                    </UCard>

                    <UCard>
                        <template #header>
                            <h3 class="text-sm font-semibold">
                                Review Status
                            </h3>
                        </template>
                        <div class="grid grid-cols-2 gap-4 sm:grid-cols-4">
                            <div v-for="card in reviewStats"
                                 :key="card.label"
                                 class="rounded-lg border border-(--ui-border) p-3 border-l-3"
                                 :class="{
                  'border-l-primary': card.color === 'primary',
                  'border-l-green-500 dark:border-l-green-400': card.color === 'success',
                  'border-l-amber-500 dark:border-l-amber-400': card.color === 'warning',
                  'border-l-red-500 dark:border-l-red-400': card.color === 'error',
                  'border-l-gray-400 dark:border-l-gray-500': card.color === 'neutral',
                }">
                                <div class="flex items-center gap-2 mb-1">
                                    <UIcon :name="card.icon"
                                           class="w-4 h-4 shrink-0"
                                           :class="{
                      'text-primary': card.color === 'primary',
                      'text-green-500 dark:text-green-400': card.color === 'success',
                      'text-amber-500 dark:text-amber-400': card.color === 'warning',
                      'text-red-500 dark:text-red-400': card.color === 'error',
                      'text-(--ui-text-muted)': card.color === 'neutral',
                    }" />
                                    <span class="text-xs text-(--ui-text-muted)">{{ card.label }}</span>
                                </div>
                                <p class="text-2xl font-semibold text-(--ui-text-highlighted)">
                                    {{ card.value }}
                                </p>
                            </div>
                        </div>
                        <div v-if="acceptanceRate !== null"
                             class="mt-3 text-xs text-(--ui-text-muted)">
                            Acceptance rate: <strong>{{ acceptanceRate }}%</strong> of reviewed applications
                        </div>
                    </UCard>

                    <UCard>
                        <template #header>
                            <h3 class="text-sm font-semibold">
                                Teams
                            </h3>
                        </template>
                        <div class="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
                            <div v-for="card in teamStats"
                                 :key="card.label"
                                 class="rounded-lg border border-(--ui-border) p-3 border-l-3"
                                 :class="{
                  'border-l-primary': card.color === 'primary',
                  'border-l-green-500 dark:border-l-green-400': card.color === 'success',
                  'border-l-amber-500 dark:border-l-amber-400': card.color === 'warning',
                  'border-l-red-500 dark:border-l-red-400': card.color === 'error',
                  'border-l-gray-400 dark:border-l-gray-500': card.color === 'neutral',
                }">
                                <div class="flex items-center gap-2 mb-1">
                                    <UIcon :name="card.icon"
                                           class="w-4 h-4 shrink-0"
                                           :class="{
                      'text-primary': card.color === 'primary',
                      'text-green-500 dark:text-green-400': card.color === 'success',
                      'text-amber-500 dark:text-amber-400': card.color === 'warning',
                      'text-red-500 dark:text-red-400': card.color === 'error',
                      'text-(--ui-text-muted)': card.color === 'neutral',
                    }" />
                                    <span class="text-xs text-(--ui-text-muted)">{{ card.label }}</span>
                                </div>
                                <p class="text-2xl font-semibold text-(--ui-text-highlighted)">
                                    {{ card.value }}
                                </p>
                            </div>
                        </div>
                        <div v-if="teamFormationRate !== null"
                             class="mt-3 text-xs text-(--ui-text-muted)">
                            Team formation rate: <strong>{{ teamFormationRate }}%</strong> of active participants
                        </div>
                    </UCard>

                    <UCard>
                        <template #header>
                            <h3 class="text-sm font-semibold">
                                Additional
                            </h3>
                        </template>
                        <div class="grid grid-cols-2 gap-4 sm:grid-cols-3">
                            <div v-for="card in additionalStats"
                                 :key="card.label"
                                 class="rounded-lg border border-(--ui-border) p-3 border-l-3"
                                 :class="{
                  'border-l-primary': card.color === 'primary',
                  'border-l-green-500 dark:border-l-green-400': card.color === 'success',
                  'border-l-amber-500 dark:border-l-amber-400': card.color === 'warning',
                  'border-l-red-500 dark:border-l-red-400': card.color === 'error',
                  'border-l-gray-400 dark:border-l-gray-500': card.color === 'neutral',
                }">
                                <div class="flex items-center gap-2 mb-1">
                                    <UIcon :name="card.icon"
                                           class="w-4 h-4 shrink-0"
                                           :class="{
                      'text-primary': card.color === 'primary',
                      'text-green-500 dark:text-green-400': card.color === 'success',
                      'text-amber-500 dark:text-amber-400': card.color === 'warning',
                      'text-red-500 dark:text-red-400': card.color === 'error',
                      'text-(--ui-text-muted)': card.color === 'neutral',
                    }" />
                                    <span class="text-xs text-(--ui-text-muted)">{{ card.label }}</span>
                                </div>
                                <p class="text-2xl font-semibold text-(--ui-text-highlighted)">
                                    {{ card.value }}
                                </p>
                            </div>
                        </div>
                    </UCard>
                </template>
            </div>
        </template>
    </UDashboardPanel>
</template>
