<script setup lang="ts">
import { computed, ref } from 'vue'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { participantOrganizerQueries, useReviewParticipantMutation } from '~/composables/participants'
import { registrationQuestionQueries } from '~/composables/question'
import { registrationPageConfig } from '~/config/registration-pages'
import type {
  HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantItem,
  HackOManiaApiEndpointsOrganizersHackathonParticipantsListRegistrationSubmissionItem,
  HackOManiaApiEndpointsParticipantsHackathonRegistrationQuestionsListQuestionDto,
} from '~/api-client/models'

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

type ParticipantItem = HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantItem
type RegistrationQuestion = HackOManiaApiEndpointsParticipantsHackathonRegistrationQuestionsListQuestionDto
type RegistrationSubmission = HackOManiaApiEndpointsOrganizersHackathonParticipantsListRegistrationSubmissionItem

const participants = computed<ParticipantItem[]>(() => participantsData.value?.participants ?? [])

type ViewMode = 'list' | 'table'
const viewMode = ref<ViewMode>('list')

const viewOptions = [
  { label: 'List', value: 'list', icon: 'i-lucide-list' },
  { label: 'Table', value: 'table', icon: 'i-lucide-table' },
] as const

const searchQuery = ref('')

type SortKey = 'name' | 'team' | 'status'
const sortKey = ref<SortKey>('name')
const sortDirection = ref<'asc' | 'desc'>('asc')

const sortOptions = [
  { label: 'Name', value: 'name' },
  { label: 'Team', value: 'team' },
  { label: 'Status', value: 'status' },
] as const

// Filter state
type FilterStatus = 'all' | 'incomplete' | 'pending' | 'approved' | 'rejected'
const activeFilter = ref<FilterStatus>('all')

function isIncomplete(p: ParticipantItem) {
  return !p.registrationSubmissions?.length
}

const statusFilteredParticipants = computed(() => {
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

const normalizedSearch = computed(() => searchQuery.value.trim().toLowerCase())

const searchFilteredParticipants = computed(() => {
  const query = normalizedSearch.value
  if (!query) return statusFilteredParticipants.value
  return statusFilteredParticipants.value.filter(participant => matchesSearch(participant, query))
})

const sortedParticipants = computed(() => {
  const direction = sortDirection.value === 'asc' ? 1 : -1
  const sorted = [...searchFilteredParticipants.value]
  sorted.sort((a, b) => {
    let result = 0
    if (sortKey.value === 'team') {
      result = compareStrings(a.teamName, b.teamName)
    }
    else if (sortKey.value === 'status') {
      result = getStatusSortValue(a.concludedStatus) - getStatusSortValue(b.concludedStatus)
    }
    else {
      result = compareStrings(a.name, b.name)
    }

    if (result === 0) {
      result = compareStrings(a.name, b.name)
    }

    return result * direction
  })
  return sorted
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

const emptyMessage = computed(() => {
  if (searchQuery.value.trim()) {
    return `No results for "${searchQuery.value.trim()}".`
  }
  if (activeFilter.value === 'all') {
    return 'No participants found.'
  }
  return `No ${activeFilter.value} participants.`
})

function compareStrings(a?: string | null, b?: string | null) {
  return (a ?? '').localeCompare(b ?? '', undefined, { sensitivity: 'base' })
}

function getStatusSortValue(status: number | null | undefined) {
  if (status === 1) return 2
  if (status === 2) return 3
  return 1
}

function matchesSearch(participant: ParticipantItem, query: string) {
  const base = [participant.name, participant.teamName, participant.id]
    .filter(Boolean)
    .join(' ')
    .toLowerCase()

  if (base.includes(query)) return true

  for (const submission of participant.registrationSubmissions ?? []) {
    const text = [submission.questionText, submission.value, submission.followUpValue]
      .filter(Boolean)
      .join(' ')
      .toLowerCase()
    if (text.includes(query)) return true
  }

  return false
}

function formatSubmissionAnswer(submission: RegistrationSubmission) {
  const value = (submission.value ?? '').toString().trim()
  const followUp = (submission.followUpValue ?? '').toString().trim()

  if (!value && !followUp) return '—'
  if (value && followUp) return `${value}\nFollow-up: ${followUp}`
  if (followUp) return `Follow-up: ${followUp}`
  return value
}

function getParticipantAnswer(participantId: string, questionId: string) {
  if (!participantId || !questionId) return '—'
  const answers = participantAnswerMap.value.get(participantId)
  return answers?.[questionId] ?? '—'
}

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

const orderedQuestions = computed<RegistrationQuestion[]>(() => {
  const categories = questionsData.value?.categories ?? []
  const ordered: RegistrationQuestion[] = []
  const seen = new Set<string>()
  const categoryOrder = registrationPageConfig.flatMap(page => [...page.categories])

  for (const categoryName of categoryOrder) {
    const category = categories.find(c => c.name === categoryName)
    for (const question of category?.questions ?? []) {
      if (question.id && !seen.has(question.id)) {
        ordered.push(question)
        seen.add(question.id)
      }
    }
  }

  for (const category of categories) {
    for (const question of category.questions ?? []) {
      if (question.id && !seen.has(question.id)) {
        ordered.push(question)
        seen.add(question.id)
      }
    }
  }

  return ordered
})

// Build question ID -> order index map following registrationPageConfig category order
const questionOrderMap = computed(() => {
  const map = new Map<string, number>()
  for (const [index, question] of orderedQuestions.value.entries()) {
    if (question.id) {
      map.set(question.id, index)
    }
  }
  return map
})

const participantAnswerMap = computed(() => {
  const map = new Map<string, Record<string, string>>()
  for (const participant of participants.value) {
    if (!participant.id) continue
    const answers: Record<string, string> = {}
    for (const submission of participant.registrationSubmissions ?? []) {
      if (!submission.questionId) continue
      answers[submission.questionId] = formatSubmissionAnswer(submission)
    }
    map.set(participant.id, answers)
  }
  return map
})

const tableColumns = computed(() => {
  const baseColumns = [
    {
      accessorKey: 'name',
      header: 'Participant',
      meta: {
        class: {
          th: 'min-w-[12rem]',
          td: 'font-medium',
        },
      },
    },
    {
      accessorKey: 'teamName',
      header: 'Team',
      meta: {
        class: {
          th: 'min-w-[10rem]',
        },
      },
    },
    {
      id: 'status',
      accessorKey: 'concludedStatus',
      header: 'Status',
      meta: {
        class: {
          th: 'w-[7rem]',
        },
      },
    },
    {
      id: 'actions',
      accessorKey: 'id',
      header: 'Actions',
      meta: {
        class: {
          th: 'w-[6rem]',
        },
      },
    },
  ]

  const questionColumns = orderedQuestions.value.map((question, index) => ({
    id: question.id ? `question-${question.id}` : `question-${index}`,
    header: question.questionText ?? 'Question',
    accessorFn: (row: ParticipantItem) => getParticipantAnswer(row.id ?? '', question.id ?? ''),
    meta: {
      class: {
        th: 'min-w-[14rem] max-w-[22rem] text-xs whitespace-normal',
        td: 'text-xs whitespace-pre-wrap align-top',
      },
    },
  }))

  return [...baseColumns, ...questionColumns]
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
        <div class="flex flex-col gap-3">
          <div class="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <h3 class="text-sm font-semibold">
              Participants
            </h3>
            <div class="flex flex-wrap items-center gap-2">
              <UInput
                v-model="searchQuery"
                size="sm"
                icon="i-lucide-search"
                placeholder="Search participants, teams, or answers"
                class="w-full sm:w-60"
              />
              <USelect
                :model-value="sortKey"
                :items="sortOptions"
                size="sm"
                class="w-36"
                @update:model-value="sortKey = $event as SortKey"
              />
              <UButton
                size="xs"
                variant="ghost"
                :icon="sortDirection === 'asc' ? 'i-lucide-arrow-up' : 'i-lucide-arrow-down'"
                @click="sortDirection = sortDirection === 'asc' ? 'desc' : 'asc'"
              />
              <UTabs
                :items="viewOptions"
                :model-value="viewMode"
                size="sm"
                :content="false"
                @update:model-value="viewMode = $event as ViewMode"
              />
            </div>
          </div>
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
        v-else-if="!sortedParticipants.length"
        class="text-(--ui-text-muted) text-sm"
      >
        {{ emptyMessage }}
      </div>

      <div v-else>
        <div
          v-if="viewMode === 'list'"
          class="divide-y divide-(--ui-border)"
        >
          <div
            v-for="participant in sortedParticipants"
            :key="participant.id ?? ''"
            class="py-2"
          >
            <div class="flex items-center justify-between">
              <div class="flex-1 min-w-0">
                <button
                  class="text-sm font-medium text-left hover:underline cursor-pointer text-highlighted"
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
              class="mt-3 ml-2 p-4 rounded-lg bg-elevated border border-default max-h-96 overflow-y-auto"
            >
              <div
                v-if="isLoadingDetail"
                class="text-sm text-muted"
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

        <div
          v-else
          class="overflow-x-auto"
        >
          <UTable
            :data="sortedParticipants"
            :columns="tableColumns"
            sticky="header"
            class="min-w-full"
          >
            <template #teamName-cell="{ row }">
              <span class="text-sm">
                {{ row.original.teamName ?? 'No team' }}
              </span>
            </template>

            <template #status-cell="{ row }">
              <template v-if="!isIncomplete(row.original)">
                <UBadge
                  :color="getStatusColor(row.original.concludedStatus)"
                  variant="subtle"
                  size="xs"
                >
                  {{ getStatusLabel(row.original.concludedStatus) }}
                </UBadge>
              </template>
              <UBadge
                v-else
                color="neutral"
                variant="subtle"
                size="xs"
              >
                Incomplete
              </UBadge>
            </template>

            <template #actions-cell="{ row }">
              <div
                v-if="!isIncomplete(row.original)"
                class="flex items-center gap-1"
              >
                <UButton
                  size="xs"
                  variant="ghost"
                  color="success"
                  icon="i-lucide-check"
                  @click="openReviewModal(row.original.id ?? '', row.original.name ?? row.original.id ?? '', 'accept')"
                />
                <UButton
                  size="xs"
                  variant="ghost"
                  color="error"
                  icon="i-lucide-x"
                  @click="openReviewModal(row.original.id ?? '', row.original.name ?? row.original.id ?? '', 'reject')"
                />
              </div>
              <span v-else class="text-(--ui-text-muted) text-xs">—</span>
            </template>
          </UTable>
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
