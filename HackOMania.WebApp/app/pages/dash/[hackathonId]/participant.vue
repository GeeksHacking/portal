<script setup lang="ts">
import { computed, ref } from 'vue'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { formatParticipantStatus, hackathonQueries as participantHackathonQueries } from '~/composables/hackathons'
import { useJoinHackathonMutation } from '~/composables/hackathon'
import { organizerQueries } from '~/composables/organizers'
import { authQueries } from '~/composables/auth'
import { useReviewParticipantMutation } from '~/composables/participants'

const route = useRoute()
const toast = useToast()
const queryClient = useQueryClient()

const hackathonId = computed(() => (route.params.hackathonId as string | undefined) ?? null)
const participantUserId = computed(() => route.query.userId as string | undefined)

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

// Organizer check
const { data: user } = useQuery(authQueries.whoAmI)

const { data: organizersData } = useQuery(
  computed(() => ({
    ...organizerQueries.list(hackathonId.value ?? ''),
    enabled: !!hackathonId.value,
  })),
)

const isOrganizer = computed(() => {
  if (!user.value?.id || !organizersData.value?.organizers) {
    return false
  }
  return organizersData.value.organizers.some(org => org.userId === user.value?.id)
})

// Review functionality
const reviewMutation = useReviewParticipantMutation(hackathonId.value ?? '')
const isReviewModalOpen = ref(false)
const reviewForm = ref({
  decision: 'accept',
  reason: '',
})

function openReviewModal(decision: string) {
  reviewForm.value = { decision, reason: '' }
  isReviewModalOpen.value = true
}

async function handleReview() {
  if (!participantUserId.value) return
  try {
    await reviewMutation.mutateAsync({
      participantUserId: participantUserId.value,
      review: {
        decision: reviewForm.value.decision,
        reason: reviewForm.value.reason || null,
      },
    })
    await queryClient.invalidateQueries({ queryKey: ['hackathons', hackathonId.value, 'participants'] })
    isReviewModalOpen.value = false
    toast.add({
      title: 'Review submitted',
      description: `Participant has been ${reviewForm.value.decision === 'accept' ? 'approved' : 'rejected'}.`,
      color: 'success',
    })
  }
  catch (error) {
    console.error('[DASH] Failed to review participant', error)
    toast.add({
      title: 'Review failed',
      description: 'Please try again.',
      color: 'error',
    })
  }
}

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
    await navigateTo(`/${hackathonId.value}/registration`)
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
  <div>
    <UDashboardPanel id="hackathon-participant">
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
          <div
            v-if="isLoadingHackathon || isLoadingStatus"
            class="text-muted"
          >
            Loading hackathon details...
          </div>

          <div
            v-else-if="hackathonError || statusError"
            class="text-red-600"
          >
            Unable to load hackathon details. Please try again.
          </div>

          <div
            v-else-if="!hackathon"
            class="text-muted"
          >
            Hackathon not found.
          </div>

          <div
            v-else
            class="space-y-4"
          >
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
                  <UBadge
                    v-if="statusData"
                    :color="statusDisplay.color"
                    variant="subtle"
                    size="sm"
                  >
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

              <div
                v-if="!isParticipant"
                class="mt-4 flex flex-wrap gap-2"
              >
                <UButton
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

            <UCard
              v-if="statusData?.isParticipant"
              class="bg-gray-50"
            >
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
              <p
                v-if="statusData?.reviewReason"
                class="text-xs text-red-600 mt-2"
              >
                Review notes: {{ statusData.reviewReason }}
              </p>
            </UCard>

            <UCard
              v-if="isOrganizer && participantUserId"
              class="border-blue-200"
            >
              <template #header>
                <h3 class="text-sm font-semibold">
                  Organizer Actions
                </h3>
              </template>
              <p class="text-sm text-muted mb-4">
                Review this participant's application.
              </p>
              <div class="flex gap-2">
                <UButton
                  size="sm"
                  color="success"
                  variant="solid"
                  icon="i-lucide-check"
                  @click="openReviewModal('accept')"
                >
                  Approve
                </UButton>
                <UButton
                  size="sm"
                  color="error"
                  variant="solid"
                  icon="i-lucide-x"
                  @click="openReviewModal('reject')"
                >
                  Reject
                </UButton>
              </div>
            </UCard>
          </div>
        </div>
      </template>
    </UDashboardPanel>

    <UModal
      v-model:open="isReviewModalOpen"
      :title="reviewForm.decision === 'accept' ? 'Approve Participant' : 'Reject Participant'"
      description="Review this participant's application"
    >
      <template #content>
        <UCard>
          <template #header>
            <div class="flex items-center justify-between">
              <h3 class="text-base font-semibold">
                {{ reviewForm.decision === 'accept' ? 'Approve Participant' : 'Reject Participant' }}
              </h3>
              <UButton
                variant="ghost"
                icon="i-lucide-x"
                size="xs"
                @click="isReviewModalOpen = false"
              />
            </div>
          </template>

          <form
            class="space-y-4"
            @submit.prevent="handleReview"
          >
            <UFormField label="Reason (optional)">
              <UTextarea
                v-model="reviewForm.reason"
                :placeholder="reviewForm.decision === 'accept' ? 'Add a note for approval...' : 'Provide a reason for rejection...'"
                :rows="3"
              />
            </UFormField>

            <div class="flex justify-end gap-2">
              <UButton
                variant="ghost"
                @click="isReviewModalOpen = false"
              >
                Cancel
              </UButton>
              <UButton
                type="submit"
                :color="reviewForm.decision === 'accept' ? 'success' : 'error'"
                :loading="reviewMutation.isPending.value"
              >
                {{ reviewForm.decision === 'accept' ? 'Approve' : 'Reject' }}
              </UButton>
            </div>
          </form>
        </UCard>
      </template>
    </UModal>
  </div>
</template>
