<script setup lang="ts">
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint } from '@geekshacking/portal-sdk/hooks'
import { computed, ref } from 'vue'
import { HackOManiaApiEndpointsParticipantsHackathonStatusParticipantStatusObject } from '~/api-client/models'
import { useJoinHackathonMutation } from '~/composables/hackathon'
import { formatParticipantStatus, hackathonQueries as participantHackathonQueries } from '~/composables/hackathons'
import { organizerQueries } from '~/composables/organizers'
import { useReviewParticipantMutation, useWithdrawFromHackathon } from '~/composables/participants'

const route = useRoute()
const toast = useToast()
const queryClient = useQueryClient()

const hackathonIdOrShortCode = computed(() => (route.params.hackathonId as string | undefined) ?? null)
const participantUserId = computed(() => route.query.userId as string | undefined)

const { data: hackathon, isLoading: isLoadingHackathon, error: hackathonError } = useQuery(
  computed(() => ({
    ...participantHackathonQueries.detail(hackathonIdOrShortCode.value ?? ''),
    enabled: !!hackathonIdOrShortCode.value,
  })),
)

const resolvedHackathonId = computed(() => hackathon.value?.id ?? null)

const { data: statusData, isLoading: isLoadingStatus, error: statusError } = useQuery(
  computed(() => ({
    ...participantHackathonQueries.status(resolvedHackathonId.value ?? ''),
    enabled: !!resolvedHackathonId.value,
  })),
)

// Fetch registration submissions to check completion status
const { data: submissionsData } = useQuery(
  computed(() => ({
    queryKey: ['hackathons', resolvedHackathonId.value, 'registration', 'submissions'],
    queryFn: () => useNuxtApp().$apiClient.participants.hackathons.byHackathonIdOrShortCodeId(resolvedHackathonId.value ?? '').registration.submissions.get(),
    enabled: !!resolvedHackathonId.value && statusData.value?.isParticipant === true,
  })),
)

const isRegistrationComplete = computed(() => submissionsData.value?.requiredQuestionsRemaining === 0)

const joinMutation = useJoinHackathonMutation()
const withdrawMutation = useWithdrawFromHackathon(resolvedHackathonId)
const isWithdrawModalOpen = ref(false)

// Organizer check
const { data: user } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()

const { data: organizersData } = useQuery(
  computed(() => ({
    ...organizerQueries.list(resolvedHackathonId.value ?? ''),
    enabled: !!resolvedHackathonId.value,
  })),
)

const isOrganizer = computed(() => {
  if (!user.value?.id) {
    return false
  }

  async function withdrawFromHackathon() {
    try {
      await withdrawMutation.mutateAsync()
      await queryClient.invalidateQueries({ queryKey: participantHackathonQueries.status(resolvedHackathonId.value ?? '').queryKey })
      await queryClient.invalidateQueries({ queryKey: participantHackathonQueries.list.queryKey })
      await queryClient.invalidateQueries({ queryKey: ['hackathons', resolvedHackathonId.value, 'registration', 'submissions'] })
      isWithdrawModalOpen.value = false
      toast.add({
        title: 'Withdrawn',
        description: 'You have withdrawn from this hackathon.',
        color: 'success',
      })
    }
    catch (error) {
      console.error('[DASH] Failed to withdraw from hackathon', error)
      const statusCode = getErrorStatusCode(error)
      const description = statusCode === 400
        ? 'Leave your team before withdrawing from this hackathon.'
        : 'Please try again in a moment.'

      toast.add({
        title: 'Could not withdraw',
        description,
        color: 'error',
      })
    }
  }
  if (user.value.isRoot)
    return true
  if (organizersData.value?.organizers) {
    return organizersData.value.organizers.some(org => org.userId === user.value?.id)
  }
  return false
})

// Review functionality
const reviewMutation = useReviewParticipantMutation(resolvedHackathonId.value ?? '')
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
  if (!participantUserId.value)
    return
  try {
    await reviewMutation.mutateAsync({
      participantUserId: participantUserId.value,
      review: {
        decision: reviewForm.value.decision,
        reason: reviewForm.value.reason || null,
      },
    })
    await queryClient.invalidateQueries({ queryKey: ['hackathons', resolvedHackathonId.value, 'participants'] })
    isReviewModalOpen.value = false
    toast.add({
      title: 'Review submitted',
      description: `Participant has been ${reviewForm.value.decision === 'accept' ? 'approved' : 'rejected'}.`,
      color: 'success',
    })
  }
  catch (error) {
    const statusCode = getErrorStatusCode(error)
    if (statusCode === 409) {
      await queryClient.invalidateQueries({ queryKey: ['hackathons', resolvedHackathonId.value, 'participants'] })
      isReviewModalOpen.value = false
      toast.add({
        title: 'Already reviewed',
        description: 'Another organizer already reviewed this participant.',
        color: 'warning',
      })
      return
    }

    console.error('[DASH] Failed to review participant', error)
    toast.add({
      title: 'Review failed',
      description: 'Please try again.',
      color: 'error',
    })
  }
}

function getErrorStatusCode(error: unknown): number | null {
  if (!error || typeof error !== 'object')
    return null
  const unknownError = error as {
    responseStatusCode?: unknown
    statusCode?: unknown
    response?: { status?: unknown }
  }

  if (typeof unknownError.responseStatusCode === 'number')
    return unknownError.responseStatusCode
  if (typeof unknownError.statusCode === 'number')
    return unknownError.statusCode
  if (typeof unknownError.response?.status === 'number')
    return unknownError.response.status
  return null
}

const statusDisplay = computed(() => {
  // If participant but registration incomplete, show that instead
  if (statusData.value?.isParticipant && !isRegistrationComplete.value) {
    return { label: 'Incomplete registration', color: 'warning' as const }
  }
  return formatParticipantStatus(statusData.value?.status ?? null, statusData.value?.isParticipant)
})

const isParticipant = computed(() => statusData.value?.isParticipant === true)

async function joinHackathon() {
  if (!resolvedHackathonId.value || !hackathon.value)
    return
  try {
    await joinMutation.mutateAsync(resolvedHackathonId.value)
    await queryClient.invalidateQueries({ queryKey: participantHackathonQueries.status(resolvedHackathonId.value).queryKey })
    await queryClient.invalidateQueries({ queryKey: participantHackathonQueries.list.queryKey })
    await navigateTo(`/${hackathon.value.shortCode}/registration`)
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

const reviewedDateFormatter = new Intl.DateTimeFormat(undefined, {
  dateStyle: 'medium',
  timeZone: 'Asia/Singapore',
})

function formatReviewedDate(value: Date | string | null | undefined) {
  if (!value)
    return '—'
  const parsed = value instanceof Date ? value : new Date(value)
  if (Number.isNaN(parsed.getTime()))
    return '—'
  return `${reviewedDateFormatter.format(parsed)} SGT`
}
</script>

<template>
  <div>
    <UDashboardPanel id="hackathon-participant">
      <template #header>
        <UDashboardNavbar :title="hackathon?.name ?? 'Hackathon'">
          <template #leading>
            <UDashboardSidebarCollapse />
          </template>
        </UDashboardNavbar>
      </template>

      <template #body>
        <div class="space-y-3">
          <div
            v-if="isLoadingHackathon || isLoadingStatus"
            class="text-(--ui-text-muted)"
          >
            Loading hackathon details...
          </div>

          <div
            v-else-if="hackathonError || statusError"
            class="text-red-500 dark:text-red-400"
          >
            Unable to load hackathon details. Please try again.
          </div>

          <div
            v-else-if="!hackathon"
            class="text-(--ui-text-muted)"
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
                    <p class="text-xs text-(--ui-text-muted)">
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

              <p class="text-sm text-(--ui-text-muted)">
                {{ hackathon.description ?? 'Details coming soon.' }}
              </p>

              <div class="mt-3 flex flex-wrap gap-2 text-xs text-(--ui-text-muted)">
                <span>
                  Starts: {{ hackathon.eventStartDate ? new Date(hackathon.eventStartDate).toLocaleDateString() : 'TBC' }}
                </span>
                <span>•</span>
                <span>
                  Ends: {{ hackathon.eventEndDate ? new Date(hackathon.eventEndDate).toLocaleDateString() : 'TBC' }}
                </span>
              </div>

              <div
                v-if="statusData?.status === HackOManiaApiEndpointsParticipantsHackathonStatusParticipantStatusObject.Rejected && statusData?.reviewReason"
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
                  color="neutral"
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
              class="bg-gray-50 dark:bg-gray-900"
            >
              <template #header>
                <h3 class="text-sm font-semibold">
                  Application status
                </h3>
              </template>
              <template v-if="!isRegistrationComplete">
                <p class="text-sm text-(--ui-text-muted)">
                  {{ statusDisplay.label }}
                </p>
                <div class="mt-3">
                  <UButton
                    :to="hackathon ? `/${hackathon.shortCode}/registration` : undefined"
                    size="sm"
                    color="neutral"
                    variant="solid"
                  >
                    Continue registration
                  </UButton>
                </div>
              </template>
              <template v-else>
                <p class="text-sm text-(--ui-text-muted)">
                  {{ statusDisplay.label }}
                  <span v-if="statusData?.reviewedAt">
                    • Reviewed {{ formatReviewedDate(statusData.reviewedAt) }}
                  </span>
                </p>
                <p
                  v-if="statusData?.reviewReason"
                  class="text-xs text-red-600 mt-2"
                >
                  Review notes: {{ statusData.reviewReason }}
                </p>
                <div class="mt-3">
                  <UButton
                    size="sm"
                    color="error"
                    variant="soft"
                    icon="i-lucide-user-minus"
                    @click="isWithdrawModalOpen = true"
                  >
                    Withdraw from hackathon
                  </UButton>
                </div>
              </template>
            </UCard>

            <UCard
              v-if="isOrganizer && participantUserId"
              class="border-blue-200 dark:border-blue-800"
            >
              <template #header>
                <h3 class="text-sm font-semibold">
                  Organizer Actions
                </h3>
              </template>
              <p class="text-sm text-(--ui-text-muted) mb-4">
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
        <div class="overflow-auto max-h-[80vh]">
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
        </div>
      </template>
    </UModal>

    <UModal
      v-model:open="isWithdrawModalOpen"
      title="Withdraw from hackathon"
      description="This removes your participant access for this hackathon."
    >
      <template #content>
        <div class="overflow-auto max-h-[80vh]">
          <UCard>
            <p class="text-sm text-(--ui-text-muted)">
              Are you sure you want to withdraw? You must leave your team first if you are currently in one.
            </p>
            <div class="mt-4 flex justify-end gap-2">
              <UButton
                variant="ghost"
                @click="isWithdrawModalOpen = false"
              >
                Cancel
              </UButton>
              <UButton
                color="error"
                :loading="withdrawMutation.isPending.value"
                @click="withdrawFromHackathon"
              >
                Withdraw
              </UButton>
            </div>
          </UCard>
        </div>
      </template>
    </UModal>
  </div>
</template>
