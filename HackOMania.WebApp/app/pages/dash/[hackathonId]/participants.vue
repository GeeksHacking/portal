<script setup lang="ts">
import { computed, ref } from 'vue'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { participantOrganizerQueries, useReviewParticipantMutation } from '~/composables/participants'

const props = defineProps<{
  hackathonId: string
  isOrganizer: boolean
}>()

const queryClient = useQueryClient()

const { data: participantsData, isLoading: isLoadingParticipants } = useQuery(
  computed(() => ({
    ...participantOrganizerQueries.list(props.hackathonId),
    enabled: !!props.hackathonId && props.isOrganizer,
  })),
)

const participants = computed(() => participantsData.value?.participants ?? [])

// Filter state
type FilterStatus = 'all' | 'pending' | 'approved' | 'rejected'
const activeFilter = ref<FilterStatus>('all')

const filteredParticipants = computed(() => {
  const all = participants.value
  switch (activeFilter.value) {
    case 'pending':
      return all.filter(p => p.concludedStatus === 0 || p.concludedStatus === null || p.concludedStatus === undefined)
    case 'approved':
      return all.filter(p => p.concludedStatus === 1)
    case 'rejected':
      return all.filter(p => p.concludedStatus === 2)
    default:
      return all
  }
})

const filterCounts = computed(() => ({
  all: participants.value.length,
  pending: participants.value.filter(p => p.concludedStatus === 0 || p.concludedStatus === null || p.concludedStatus === undefined).length,
  approved: participants.value.filter(p => p.concludedStatus === 1).length,
  rejected: participants.value.filter(p => p.concludedStatus === 2).length,
}))

// Review mutation
const reviewMutation = useReviewParticipantMutation(props.hackathonId)

// Modal state
const isModalOpen = ref(false)
const reviewingParticipantId = ref<string | null>(null)
const reviewingParticipantName = ref<string | null>(null)
const reviewForm = ref({
  decision: 'accept',
  reason: '',
})

function openReviewModal(participantId: string, participantName: string, decision: string) {
  reviewingParticipantId.value = participantId
  reviewingParticipantName.value = participantName
  reviewForm.value = { decision, reason: '' }
  isModalOpen.value = true
}

async function handleReview() {
  if (!reviewingParticipantId.value) return
  await reviewMutation.mutateAsync({
    participantUserId: reviewingParticipantId.value,
    review: {
      decision: reviewForm.value.decision,
      reason: reviewForm.value.reason || null,
    },
  })
  await queryClient.invalidateQueries({ queryKey: ['hackathons', props.hackathonId, 'participants', 'organizer'] })
  isModalOpen.value = false
  reviewingParticipantId.value = null
  reviewingParticipantName.value = null
}

function getStatusColor(status: number | null | undefined): 'success' | 'error' | 'warning' {
  if (status === 1) return 'success'
  if (status === 2) return 'error'
  return 'warning'
}

function getStatusLabel(status: number | null | undefined): string {
  if (status === 1) return 'Approved'
  if (status === 2) return 'Rejected'
  return 'Pending'
}
</script>

<template>
  <div>
    <UCard>
      <template #header>
        <div class="flex items-center justify-between gap-2">
          <h3 class="text-sm font-semibold">
            Participants
          </h3>
          <div class="flex items-center gap-2">
            <div class="flex flex-wrap gap-1">
              <UButton
                size="xs"
                :variant="activeFilter === 'all' ? 'solid' : 'ghost'"
                :color="activeFilter === 'all' ? 'primary' : 'neutral'"
                @click="activeFilter = 'all'"
              >
                All ({{ filterCounts.all }})
              </UButton>
              <UButton
                size="xs"
                :variant="activeFilter === 'pending' ? 'solid' : 'ghost'"
                :color="activeFilter === 'pending' ? 'warning' : 'neutral'"
                @click="activeFilter = 'pending'"
              >
                Pending ({{ filterCounts.pending }})
              </UButton>
              <UButton
                size="xs"
                :variant="activeFilter === 'approved' ? 'solid' : 'ghost'"
                :color="activeFilter === 'approved' ? 'success' : 'neutral'"
                @click="activeFilter = 'approved'"
              >
                Approved ({{ filterCounts.approved }})
              </UButton>
              <UButton
                size="xs"
                :variant="activeFilter === 'rejected' ? 'solid' : 'ghost'"
                :color="activeFilter === 'rejected' ? 'error' : 'neutral'"
                @click="activeFilter = 'rejected'"
              >
                Rejected ({{ filterCounts.rejected }})
              </UButton>
            </div>
          </div>
        </div>
      </template>

      <div
        v-if="isLoadingParticipants"
        class="text-(--ui-text-muted) text-sm"
      >
        Loading participants...
      </div>

      <div
        v-else-if="!participants.length"
        class="text-(--ui-text-muted) text-sm"
      >
        No participants yet.
      </div>

      <div
        v-else-if="!filteredParticipants.length"
        class="text-(--ui-text-muted) text-sm"
      >
        No {{ activeFilter }} participants.
      </div>

      <div
        v-else
        class="divide-y divide-(--ui-border)"
      >
        <div
          v-for="participant in filteredParticipants"
          :key="participant.id ?? ''"
          class="py-2 flex items-center justify-between"
        >
          <div class="flex-1 min-w-0">
            <p class="text-sm font-medium">
              {{ participant.name ?? participant.id }}
            </p>
            <p class="text-xs text-(--ui-text-muted)">
              Team: {{ participant.teamName ?? 'No team' }}
            </p>
          </div>
          <div class="flex items-center gap-2 ml-2">
            <UBadge
              :color="getStatusColor(participant.concludedStatus)"
              variant="subtle"
              size="xs"
            >
              {{ getStatusLabel(participant.concludedStatus) }}
            </UBadge>
            <template v-if="participant.concludedStatus === 0 || participant.concludedStatus === null || participant.concludedStatus === undefined">
              <UButton
                size="xs"
                variant="ghost"
                color="success"
                icon="i-lucide-check"
                @click="openReviewModal(participant.id ?? '', participant.name ?? participant.id ?? '', 'accept')"
              />
              <UButton
                size="xs"
                variant="ghost"
                color="error"
                icon="i-lucide-x"
                @click="openReviewModal(participant.id ?? '', participant.name ?? participant.id ?? '', 'reject')"
              />
            </template>
          </div>
        </div>
      </div>
    </UCard>

    <UModal
      v-model:open="isModalOpen"
      :title="reviewForm.decision === 'accept' ? 'Approve Participant' : 'Reject Participant'"
      description="Review this participant's application"
    >
      <template #content>
        <UCard>
          <template #header>
            <div class="flex items-center justify-between">
              <h3 class="text-base font-semibold">
                {{ reviewForm.decision === 'accept' ? 'Approve' : 'Reject' }} Participant
              </h3>
              <UButton
                variant="ghost"
                icon="i-lucide-x"
                size="xs"
                @click="isModalOpen = false"
              />
            </div>
          </template>

          <form
            class="space-y-4"
            @submit.prevent="handleReview"
          >
            <p class="text-sm text-(--ui-text-muted)">
              {{ reviewForm.decision === 'accept' ? 'Approving' : 'Rejecting' }} participant: <strong>{{ reviewingParticipantName }}</strong>
            </p>

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
                @click="isModalOpen = false"
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
