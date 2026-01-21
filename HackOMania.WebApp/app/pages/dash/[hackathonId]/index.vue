<script setup lang="ts">
import { computed } from 'vue'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { formatParticipantStatus, hackathonQueries as participantHackathonQueries } from '~/composables/hackathons'
import { useJoinHackathonMutation } from '~/composables/hackathon'

const route = useRoute()
const toast = useToast()
const queryClient = useQueryClient()

const hackathonId = computed(() => (route.params.hackathonId as string | undefined) ?? null)

const { data: hackathon, isLoading: isLoadingHackathon, error: hackathonError } = useQuery(
  computed(() => ({
    ...participantHackathonQueries.detail(hackathonId.value ?? ''),
    enabled: !!hackathonId.value,
  })),
)

const { data: statusData, isLoading: isLoadingStatus, error: statusError } = useQuery(
  computed(() => ({
    ...participantHackathonQueries.status(hackathonId.value ?? ''),
    enabled: !!hackathonId.value,
  })),
)

const joinMutation = useJoinHackathonMutation()

const statusDisplay = computed(() =>
  formatParticipantStatus(statusData.value?.status ?? null, statusData.value?.isParticipant),
)

const isParticipant = computed(() => statusData.value?.isParticipant === true)

const joinHackathon = async () => {
  if (!hackathonId.value) return
  try {
    await joinMutation.mutateAsync(hackathonId.value)
    await queryClient.invalidateQueries({ queryKey: participantHackathonQueries.status(hackathonId.value).queryKey })
    await queryClient.invalidateQueries({ queryKey: participantHackathonQueries.list.queryKey })
    await navigateTo(`/dash/${hackathonId.value}/registration`)
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

const goToRegistration = () => {
  if (!hackathonId.value) return
  navigateTo(`/dash/${hackathonId.value}/registration`)
}
</script>

<template>
  <UDashboardPanel id="hackathon-detail">
    <template #header>
      <UDashboardNavbar :title="hackathon?.name ?? 'Hackathon'">
        <template #leading>
          <UButton
            to="/dash"
            icon="i-lucide-arrow-left"
            color="gray"
            variant="ghost"
            size="sm"
          >
            Back
          </UButton>
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="p-4 space-y-4">
        <div v-if="isLoadingHackathon || isLoadingStatus" class="text-muted">
          Loading hackathon details...
        </div>

        <div v-else-if="hackathonError || statusError" class="text-red-600">
          Unable to load hackathon details. Please try again.
        </div>

        <div v-else-if="!hackathon" class="text-muted">
          Hackathon not found.
        </div>

        <div v-else class="space-y-4">
          <UCard>
            <template #header>
              <div class="flex items-start justify-between gap-2">
                <div>
                  <h2 class="text-lg font-semibold">
                    {{ hackathon.name }}
                  </h2>
                  <p class="text-xs text-muted">
                    {{ hackathon.venue ?? 'Venue TBC' }}
                  </p>
                </div>
                <UBadge v-if="statusData" :color="statusDisplay.color" variant="subtle" size="sm">
                  {{ statusDisplay.label }}
                </UBadge>
              </div>
            </template>

            <p class="text-sm text-muted">
              {{ hackathon.description ?? 'Details coming soon.' }}
            </p>

            <div class="mt-3 flex flex-wrap gap-2 text-xs text-muted">
              <span>
                Starts: {{ hackathon.eventStartDate ? new Date(hackathon.eventStartDate).toLocaleDateString() : 'TBC' }}
              </span>
              <span>•</span>
              <span>
                Ends: {{ hackathon.eventEndDate ? new Date(hackathon.eventEndDate).toLocaleDateString() : 'TBC' }}
              </span>
            </div>

            <div
              v-if="statusData?.status === 2 && statusData?.reviewReason"
              class="mt-3 text-xs text-red-600"
            >
              Reason: {{ statusData?.reviewReason }}
            </div>

            <div class="mt-4 flex flex-wrap gap-2">
              <UButton
                v-if="isParticipant"
                size="sm"
                color="black"
                variant="solid"
                @click="goToRegistration"
              >
                Continue registration
              </UButton>
              <UButton
                v-else
                size="sm"
                color="black"
                variant="solid"
                :loading="joinMutation.isPending.value"
                @click="joinHackathon"
              >
                Join hackathon
              </UButton>
            </div>
          </UCard>

          <UCard v-if="statusData?.isParticipant" class="bg-gray-50">
            <template #header>
              <h3 class="text-sm font-semibold">
                Application status
              </h3>
            </template>
            <p class="text-sm text-muted">
              {{ statusDisplay.label }}
              <span v-if="statusData?.reviewedAt">
                • Reviewed {{ new Date(statusData.reviewedAt).toLocaleDateString() }}
              </span>
            </p>
            <p v-if="statusData?.reviewReason" class="text-xs text-red-600 mt-2">
              Review notes: {{ statusData.reviewReason }}
            </p>
          </UCard>
        </div>
      </div>
    </template>
  </UDashboardPanel>
</template>
