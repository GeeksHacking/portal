<script setup lang="ts">
import { unref } from 'vue'
import { useQuery, useQueries, useQueryClient } from '@tanstack/vue-query'
import { formatParticipantStatus, hackathonQueries as participantHackathonQueries } from '~/composables/hackathons'
import { useJoinHackathonMutation } from '~/composables/hackathon'
import type {
  HackOManiaApiEndpointsParticipantsHackathonStatusResponse,
  HackOManiaApiEndpointsParticipantsHackathonRegistrationSubmissionsListResponse,
} from '~/api-client/models'

const toast = useToast()
const queryClient = useQueryClient()
const joinMutation = useJoinHackathonMutation()

const { data: user } = useQuery(authQueries.whoAmI)

const { data: hackathonsData, isLoading: isLoadingHackathons } = useQuery(
  participantHackathonQueries.list,
)

const hackathons = computed(() => hackathonsData.value?.hackathons ?? [])

// Fetch participation status per hackathon
const statusQueries = useQueries({
  queries: computed(() =>
    hackathons.value.map(hackathon => ({
      ...participantHackathonQueries.status(hackathon.id ?? ''),
      enabled: !!hackathon.id,
    })),
  ),
})

// Fetch registration submissions to check completion status
const submissionQueries = useQueries({
  queries: computed(() =>
    hackathons.value.map((hackathon) => {
      const status = statusQueries.value[hackathons.value.indexOf(hackathon)]?.data as HackOManiaApiEndpointsParticipantsHackathonStatusResponse | undefined
      const isParticipant = status?.isParticipant === true
      return {
        queryKey: ['hackathons', hackathon.id, 'registration', 'submissions'],
        queryFn: () => useNuxtApp().$apiClient.participants.hackathons
          .byHackathonIdOrShortCodeId(hackathon.id ?? '')
          .registration.submissions.get(),
        enabled: !!hackathon.id && isParticipant,
      }
    }),
  ),
})

const statusDataForIndex = (index: number): HackOManiaApiEndpointsParticipantsHackathonStatusResponse | undefined =>
  unref(statusQueries.value[index]?.data) as HackOManiaApiEndpointsParticipantsHackathonStatusResponse | undefined

const submissionsDataForIndex = (index: number): HackOManiaApiEndpointsParticipantsHackathonRegistrationSubmissionsListResponse | undefined =>
  unref(submissionQueries.value[index]?.data) as HackOManiaApiEndpointsParticipantsHackathonRegistrationSubmissionsListResponse | undefined

const isRegistrationComplete = (index: number): boolean => {
  const submissions = submissionsDataForIndex(index)
  return submissions?.requiredQuestionsRemaining === 0
}

const joinHackathon = async (hackathonId: string) => {
  try {
    await joinMutation.mutateAsync(hackathonId)
    await queryClient.invalidateQueries({ queryKey: participantHackathonQueries.status(hackathonId).queryKey })
    await queryClient.invalidateQueries({ queryKey: participantHackathonQueries.list.queryKey })
    navigateTo(`/${hackathonId}/registration`)
  }
  catch (error) {
    console.error('[DASH] Failed to join hackathon', error)
    toast.add({
      title: 'Could not join',
      description: 'Please try again in a moment.',
      color: 'error',
    })
  }
}
</script>

<template>
  <UDashboardPanel id="dashboard">
    <template #header>
      <UDashboardNavbar title="Dashboard">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="p-4 space-y-4">
        <div class="flex flex-col gap-1">
          <h2 class="text-lg font-semibold">
            Hackathons
          </h2>
          <p class="text-sm text-(--ui-text-muted)">
            Join a hackathon, complete your registration, and track your application status.
          </p>
        </div>

        <div v-if="isLoadingHackathons" class="text-(--ui-text-muted)">
          Loading hackathons...
        </div>

        <div v-else-if="!hackathons.length" class="text-(--ui-text-muted)">
          No hackathons available.
        </div>

        <div v-else class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          <UCard v-for="(hackathon, index) in hackathons" :key="hackathon.id!">
            <template #header>
              <div class="flex items-start justify-between gap-2">
                <div>
                  <h3 class="font-semibold leading-tight">
                    {{ hackathon.name }}
                  </h3>
                  <p class="text-xs text-(--ui-text-muted) leading-tight">
                    {{ hackathon.venue }}
                  </p>
                </div>
                <UBadge
                  v-if="user?.isRoot"
                  color="warning"
                  variant="subtle"
                  size="sm"
                >
                  Admin
                </UBadge>
                <UBadge
                  v-else-if="statusDataForIndex(index)?.isOrganizer"
                  color="info"
                  variant="subtle"
                  size="sm"
                >
                  Organizer
                </UBadge>
                <UBadge
                  v-else-if="statusDataForIndex(index)"
                  :color="formatParticipantStatus(statusDataForIndex(index)?.status ?? null, statusDataForIndex(index)?.isParticipant).color"
                  variant="subtle"
                  size="sm"
                >
                  {{ formatParticipantStatus(statusDataForIndex(index)?.status ?? null, statusDataForIndex(index)?.isParticipant).label }}
                </UBadge>
              </div>
            </template>

            <p class="text-sm text-(--ui-text-muted) min-h-14">
              {{ hackathon.description }}
            </p>

            <div class="mt-3 flex flex-col gap-2">
              <div class="flex items-center gap-2 text-xs text-(--ui-text-muted)">
                <span>
                  Starts: {{ hackathon.eventStartDate ? new Date(hackathon.eventStartDate).toLocaleDateString() : 'TBC' }}
                </span>
                <span>•</span>
                <span>
                  Ends: {{ hackathon.eventEndDate ? new Date(hackathon.eventEndDate).toLocaleDateString() : 'TBC' }}
                </span>
              </div>

              <div
                v-if="statusDataForIndex(index)?.status === 2 && statusDataForIndex(index)?.reviewReason"
                class="text-xs text-red-500 dark:text-red-400"
              >
                Reason: {{ statusDataForIndex(index)?.reviewReason }}
              </div>

              <div class="flex items-center gap-2">
                <!-- Admin or Organizer: Manage + Portal -->
                <template v-if="user?.isRoot || statusDataForIndex(index)?.isOrganizer">
                  <UButton
                    :to="`/dash/${hackathon.id}`"
                    color="neutral"
                    size="sm"
                  >
                    Manage
                  </UButton>
                  <UButton
                    :to="`/${hackathon.id}/team`"
                    color="neutral"
                    variant="outline"
                    size="sm"
                  >
                    Go to hackathon portal
                  </UButton>
                </template>

                <!-- Not joined: Join event -->
                <template v-else-if="!statusDataForIndex(index)?.isParticipant">
                  <UButton
                    color="neutral"
                    size="sm"
                    :loading="joinMutation.isPending.value"
                    @click="joinHackathon(hackathon.id!)"
                  >
                    Join event
                  </UButton>
                </template>

                <!-- Joined but registration incomplete: Continue registration -->
                <template v-else-if="!isRegistrationComplete(index)">
                  <UButton
                    :to="`/${hackathon.id}/registration`"
                    color="neutral"
                    size="sm"
                  >
                    Continue registration
                  </UButton>
                </template>

                <!-- Registration complete but not approved: View registration status -->
                <template v-else-if="statusDataForIndex(index)?.status !== 1">
                  <UButton
                    :to="`/dash/${hackathon.id}/participant`"
                    color="neutral"
                    size="sm"
                  >
                    View registration status
                  </UButton>
                </template>

                <!-- Approved participant: Portal -->
                <template v-else>
                  <UButton
                    :to="`/${hackathon.id}/team`"
                    color="neutral"
                    size="sm"
                  >
                    Go to hackathon portal
                  </UButton>
                </template>
              </div>
            </div>
          </UCard>
        </div>
      </div>
    </template>
  </UDashboardPanel>
</template>
