<script setup lang="ts">
import { computed, unref } from 'vue'
import { useQuery, useQueries } from '@tanstack/vue-query'
import { formatParticipantStatus, hackathonQueries as participantHackathonQueries } from '~/composables/hackathons'
import type { HackOManiaApiEndpointsParticipantsHackathonStatusResponse } from '~/api-client/models'

const { data: hackathonsData, isLoading: isLoadingHackathons } = useQuery(
  participantHackathonQueries.list,
)

const hackathons = computed(() => hackathonsData.value?.hackathons ?? [])

// Fetch participation status per hackathon so we can show application state.
const statusQueries = useQueries({
  queries: computed(() =>
    hackathons.value.map(hackathon => ({
      ...participantHackathonQueries.status(hackathon.id ?? ''),
      enabled: !!hackathon.id,
    })),
  ),
})

const statusDataForIndex = (index: number): HackOManiaApiEndpointsParticipantsHackathonStatusResponse | undefined =>
  unref(statusQueries.value[index]?.data) as HackOManiaApiEndpointsParticipantsHackathonStatusResponse | undefined

const goToHackathon = (hackathonId: string) => {
  navigateTo(`/dash/${hackathonId}`)
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
          <p class="text-sm text-muted">
            Join a hackathon, complete your registration, and track your application status.
          </p>
        </div>

        <div v-if="isLoadingHackathons" class="text-muted">
          Loading hackathons...
        </div>

        <div v-else-if="!hackathons.length" class="text-muted">
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
                  <p class="text-xs text-muted leading-tight">
                    {{ hackathon.venue }}
                  </p>
                </div>
                <UBadge
                  v-if="statusDataForIndex(index)"
                  :color="formatParticipantStatus(statusDataForIndex(index)?.status ?? null, statusDataForIndex(index)?.isParticipant).color"
                  variant="subtle"
                  size="sm"
                >
                  {{ formatParticipantStatus(statusDataForIndex(index)?.status ?? null, statusDataForIndex(index)?.isParticipant).label }}
                </UBadge>
              </div>
            </template>

            <p class="text-sm text-muted min-h-14">
              {{ hackathon.description }}
            </p>

            <div class="mt-3 flex flex-col gap-2">
              <div class="flex items-center gap-2 text-xs text-muted">
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
                class="text-xs text-red-600"
              >
                Reason: {{ statusDataForIndex(index)?.reviewReason }}
              </div>

              <div class="flex items-center gap-2">
                <UButton
                  size="sm"
                  color="black"
                  variant="solid"
                  @click="goToHackathon(hackathon.id!)"
                >
                  {{ statusDataForIndex(index)?.isParticipant ? 'View status' : 'View details' }}
                </UButton>
              </div>
            </div>
          </UCard>
        </div>
      </div>
    </template>
  </UDashboardPanel>
</template>
