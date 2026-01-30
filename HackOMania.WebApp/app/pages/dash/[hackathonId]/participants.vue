<script setup lang="ts">
import { computed, ref } from 'vue'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { participantOrganizerQueries, useReviewParticipantMutation } from '~/composables/participants'
import { registrationQuestionQueries } from '~/composables/question'
import { registrationPageConfig } from '~/config/registration-pages'

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
type FilterStatus = 'all' | 'incomplete' | 'pending' | 'approved' | 'rejected'
const activeFilter = ref<FilterStatus>('all')

function isIncomplete(p: { registrationSubmissions?: unknown[] | null }) {
  return !p.registrationSubmissions?.length
}

const filteredParticipants = computed(() => {
  const all = participants.value
  const complete = all.filter(p => !isIncomplete(p))
  switch (activeFilter.value) {
    case 'incomplete':
      return all.filter(p => isIncomplete(p))
    case 'pending':
      return complete.filter(p => p.concludedStatus === 0 || p.concludedStatus === null || p.concludedStatus === undefined)
    case 'approved':
      return complete.filter(p => p.concludedStatus === 1)
    case 'rejected':
      return complete.filter(p => p.concludedStatus === 2)
    default:
      return complete
  }
})

const filterCounts = computed(() => {
  const all = participants.value
  const complete = all.filter(p => !isIncomplete(p))
  return {
    all: complete.length,
    incomplete: all.filter(p => isIncomplete(p)).length,
    pending: complete.filter(p => p.concludedStatus === 0 || p.concludedStatus === null || p.concludedStatus === undefined).length,
    approved: complete.filter(p => p.concludedStatus === 1).length,
    rejected: complete.filter(p => p.concludedStatus === 2).length,
  }
})

// Expanded participant detail
const expandedParticipantId = ref<string | null>(null)

const { data: participantDetail, isLoading: isLoadingDetail } = useQuery(
  computed(() => ({
    ...participantOrganizerQueries.detail(props.hackathonId, expandedParticipantId.value ?? ''),
    enabled: !!expandedParticipantId.value,
  })),
)

// Fetch registration questions for ordering
const { data: questionsData } = useQuery(
  computed(() => ({
    ...registrationQuestionQueries.list(props.hackathonId),
    enabled: !!props.hackathonId && props.isOrganizer,
  })),
)

// Build question ID -> order index map following registrationPageConfig category order
const questionOrderMap = computed(() => {
  const map = new Map<string, number>()
  if (!questionsData.value?.categories) return map

  const categoryOrder = registrationPageConfig.flatMap(page => [...page.categories])
  let index = 0
  for (const categoryName of categoryOrder) {
    const category = questionsData.value.categories.find(c => c.name === categoryName)
    if (!category?.questions) continue
    for (const question of category.questions) {
      if (question.id) {
        map.set(question.id, index++)
      }
    }
  }
  // Append any questions in categories not covered by the config
  for (const category of questionsData.value.categories) {
    for (const question of category.questions ?? []) {
      if (question.id && !map.has(question.id)) {
        map.set(question.id, index++)
      }
    }
  }
  return map
})

const sortedSubmissions = computed(() => {
  const submissions = participantDetail.value?.registrationSubmissions
  if (!submissions) return []
  const order = questionOrderMap.value
  return [...submissions].sort((a, b) => {
    const orderA = order.get(a.questionId ?? '') ?? Number.MAX_SAFE_INTEGER
    const orderB = order.get(b.questionId ?? '') ?? Number.MAX_SAFE_INTEGER
    return orderA - orderB
  })
})

function toggleParticipant(participantId: string) {
  if (expandedParticipantId.value === participantId) {
    expandedParticipantId.value = null
  }
  else {
    expandedParticipantId.value = participantId
  }
}

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
                :variant="activeFilter === 'incomplete' ? 'solid' : 'ghost'"
                :color="activeFilter === 'incomplete' ? 'neutral' : 'neutral'"
                @click="activeFilter = 'incomplete'"
              >
                Incomplete ({{ filterCounts.incomplete }})
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
          class="py-2"
        >
          <div class="flex items-center justify-between">
            <div class="flex-1 min-w-0">
              <button
                class="text-sm font-medium text-left hover:underline cursor-pointer text-(--ui-text-highlighted)"
                @click="toggleParticipant(participant.id ?? '')"
              >
                {{ participant.name ?? participant.id }}
                <UIcon
                  :name="expandedParticipantId === participant.id ? 'i-lucide-chevron-up' : 'i-lucide-chevron-down'"
                  class="inline-block w-4 h-4 ml-1 align-middle"
                />
              </button>
              <p class="text-xs text-(--ui-text-muted)">
                Team: {{ participant.teamName ?? 'No team' }}
              </p>
            </div>
            <div class="flex items-center gap-2 ml-2">
              <template v-if="!isIncomplete(participant)">
                <UBadge
                  :color="getStatusColor(participant.concludedStatus)"
                  variant="subtle"
                  size="xs"
                >
                  {{ getStatusLabel(participant.concludedStatus) }}
                </UBadge>
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
              <UBadge
                v-else
                color="neutral"
                variant="subtle"
                size="xs"
              >
                Incomplete
              </UBadge>
            </div>
          </div>

          <!-- Expanded: Registration Submissions -->
          <div
            v-if="expandedParticipantId === participant.id"
            class="mt-3 ml-2 p-4 rounded-lg bg-(--ui-bg-elevated) border border-(--ui-border) max-h-96 overflow-y-auto"
          >
            <div
              v-if="isLoadingDetail"
              class="text-sm text-(--ui-text-muted)"
            >
              Loading form responses...
            </div>
            <div v-else-if="sortedSubmissions.length">
              <h4 class="text-sm font-semibold mb-3">
                Form Responses
              </h4>
              <div class="space-y-3">
                <UFormField
                  v-for="submission in sortedSubmissions"
                  :key="submission.questionId ?? ''"
                  :label="submission.questionText ?? 'Question'"
                >
                  <UInput
                    :model-value="submission.value || '—'"
                    disabled
                    class="w-full"
                  />
                  <template v-if="submission.followUpValue">
                    <p class="text-xs text-(--ui-text-muted) mt-1">
                      Follow-up:
                    </p>
                    <UInput
                      :model-value="submission.followUpValue"
                      disabled
                      class="w-full"
                    />
                  </template>
                </UFormField>
              </div>
            </div>
            <div
              v-else
              class="text-sm text-(--ui-text-muted)"
            >
              No form responses found.
            </div>
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
