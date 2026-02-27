<script setup lang="ts">
import { computed, ref } from 'vue'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useVirtualList } from '@vueuse/core'
import { participantOrganizerQueries, useReviewParticipantMutation } from '~/composables/participants'
import { registrationQuestionQueries } from '~/composables/question'
import { registrationPageConfig } from '~/config/registration-pages'
import type {
  HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatus,
  HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantItem,
  HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantReviewItem,
  HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantReviewItem_ParticipantReviewStatus,
  HackOManiaApiEndpointsOrganizersHackathonParticipantsListRegistrationSubmissionItem,
  HackOManiaApiEndpointsParticipantsHackathonRegistrationQuestionsListQuestionDto,
} from '~/api-client/models'
import {
  HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject,
  HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantReviewItem_ParticipantReviewStatusObject,
} from '~/api-client/models'

const route = useRoute()
const props = withDefaults(defineProps<{
  hackathonId?: string
}>(), {
  hackathonId: '',
})
const hackathonId = computed(() => props.hackathonId || (route.params.hackathonId as string | undefined) || '')

const toast = useToast()
const queryClient = useQueryClient()

const { data: participantsData, isLoading: isLoadingParticipants } = useQuery(
  computed(() => ({
    ...participantOrganizerQueries.list(hackathonId.value),
    enabled: !!hackathonId.value,
  })),
)

type ParticipantItem = HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantItem
type ParticipantReviewItem = HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantReviewItem
type ParticipantConcludedStatus = HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatus
type ParticipantReviewStatus = HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantReviewItem_ParticipantReviewStatus
type RegistrationQuestion = HackOManiaApiEndpointsParticipantsHackathonRegistrationQuestionsListQuestionDto
type RegistrationSubmission = HackOManiaApiEndpointsOrganizersHackathonParticipantsListRegistrationSubmissionItem

const participants = computed<ParticipantItem[]>(() => participantsData.value?.participants ?? [])
const REVIEW_OVERDUE_DAYS = 5
const REVIEW_OVERDUE_MS = REVIEW_OVERDUE_DAYS * 24 * 60 * 60 * 1000

type ViewMode = 'list' | 'table'
const viewMode = ref<ViewMode>('list')

const viewOptions = [
  { label: 'List', value: 'list', icon: 'i-lucide-list' },
  { label: 'Table', value: 'table', icon: 'i-lucide-table' },
] as const

const searchQuery = ref('')

type SortKey = 'name' | 'team' | 'status' | 'applicationTime'
const sortKey = ref<SortKey>('applicationTime')
const sortDirection = ref<'asc' | 'desc'>('desc')

const sortOptions = [
  { label: 'Name', value: 'name' },
  { label: 'Team', value: 'team' },
  { label: 'Status', value: 'status' },
  { label: 'Application Time', value: 'applicationTime' },
] as const
const tableVirtualizeOptions = { estimateSize: 65, overscan: 12 } as const

// Filter state
type FilterStatus = 'all' | 'incomplete' | 'pending' | 'approved' | 'rejected'
const activeFilter = ref<FilterStatus>('all')

function isIncomplete(p: ParticipantItem) {
  return !p.registrationSubmissions?.length
}

function isPendingStatus(status: ParticipantConcludedStatus | null | undefined) {
  return (
    status === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Pending
    || status === null
    || status === undefined
  )
}

function isPendingParticipant(participant: ParticipantItem) {
  return !isIncomplete(participant) && isPendingStatus(participant.concludedStatus)
}

function getParticipantApplicationTimeEpoch(participant: ParticipantItem) {
  const submissions = participant.registrationSubmissions ?? []
  if (submissions.length === 0) return 0
  return submissions.reduce((max, s) => {
    const t = s.updatedAt?.getTime() ?? 0
    return t > max ? t : max
  }, 0)
}

function getParticipantApplicationDate(participant: ParticipantItem): Date | null {
  const submittedAtEpoch = getParticipantApplicationTimeEpoch(participant)
  return submittedAtEpoch > 0 ? new Date(submittedAtEpoch) : null
}

function isReviewOverdue(participant: ParticipantItem) {
  if (!isPendingParticipant(participant)) return false
  const submittedAtEpoch = getParticipantApplicationTimeEpoch(participant)
  if (!submittedAtEpoch) return false
  return Date.now() - submittedAtEpoch >= REVIEW_OVERDUE_MS
}

function getReviewPriorityBucket(participant: ParticipantItem) {
  if (isReviewOverdue(participant)) return 0
  if (isPendingParticipant(participant)) return 1
  return 2
}

const statusFilteredParticipants = computed(() => {
  const all = participants.value
  const complete = all.filter(p => !isIncomplete(p))
  switch (activeFilter.value) {
    case 'incomplete':
      return all.filter(p => isIncomplete(p))
    case 'pending':
      return complete.filter(p => isPendingStatus(p.concludedStatus))
    case 'approved':
      return complete.filter(
        p =>
          p.concludedStatus
          === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Accepted,
      )
    case 'rejected':
      return complete.filter(
        p =>
          p.concludedStatus
          === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Rejected,
      )
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
    const priorityComparison = getReviewPriorityBucket(a) - getReviewPriorityBucket(b)
    if (priorityComparison !== 0) {
      return priorityComparison
    }

    let result = 0
    if (sortKey.value === 'team') {
      result = compareStrings(a.teamName, b.teamName)
    }
    else if (sortKey.value === 'applicationTime') {
      result = getParticipantApplicationTimeEpoch(a) - getParticipantApplicationTimeEpoch(b)
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
    pending: complete.filter(p => isPendingStatus(p.concludedStatus)).length,
    approved: complete.filter(
      p =>
        p.concludedStatus
        === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Accepted,
    ).length,
    rejected: complete.filter(
      p =>
        p.concludedStatus
        === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Rejected,
    ).length,
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

function getStatusSortValue(status: ParticipantConcludedStatus | null | undefined) {
  if (status === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Accepted) return 2
  if (status === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Rejected) return 3
  return 1
}

const REVIEW_TIME_ZONE = 'Asia/Singapore'
const dateTimeFormatter = new Intl.DateTimeFormat(undefined, {
  dateStyle: 'medium',
  timeStyle: 'short',
  timeZone: REVIEW_TIME_ZONE,
})

function formatDateTime(value: Date | null | undefined) {
  if (!value) return '—'
  return `${dateTimeFormatter.format(value)} SGT`
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
    ...participantOrganizerQueries.detail(hackathonId.value, expandedParticipantId.value ?? ''),
    enabled: !!expandedParticipantId.value,
  })),
)

// Fetch registration questions for ordering
const { data: questionsData } = useQuery(
  computed(() => ({
    ...registrationQuestionQueries.list(hackathonId.value),
    enabled: !!hackathonId.value,
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

function getParticipantListItemHeight(index: number) {
  const participant = sortedParticipants.value[index]
  if (!participant) return 84
  if (expandedParticipantId.value !== participant.id) return 84

  const reviewCount = participantDetail.value?.reviews?.length ?? 0
  const submissionCount = sortedSubmissions.value.length
  return Math.min(1400, 220 + reviewCount * 96 + submissionCount * 92)
}

const {
  list: virtualParticipants,
  containerProps: participantListContainerProps,
  wrapperProps: participantListWrapperProps,
} = useVirtualList(sortedParticipants, {
  itemHeight: getParticipantListItemHeight,
  overscan: 8,
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
const reviewMutation = useReviewParticipantMutation(hackathonId)

// Modal state
const isReviewModalOpen = ref(false)
const reviewingParticipantId = ref<string | null>(null)
const reviewReason = ref('')

const pendingParticipants = computed(() => participants.value.filter(p => isPendingParticipant(p)))
const prioritizedPendingParticipants = computed(() => {
  return [...pendingParticipants.value].sort((a, b) => {
    const priorityComparison = getReviewPriorityBucket(a) - getReviewPriorityBucket(b)
    if (priorityComparison !== 0) {
      return priorityComparison
    }

    // For review queue, older applications should be handled first.
    return getParticipantApplicationTimeEpoch(a) - getParticipantApplicationTimeEpoch(b)
  })
})
const overduePendingCount = computed(() => pendingParticipants.value.filter(p => isReviewOverdue(p)).length)

const reviewingParticipant = computed(() => {
  if (!reviewingParticipantId.value) return null
  return participants.value.find(p => p.id === reviewingParticipantId.value) ?? null
})

const reviewingParticipantName = computed(() => reviewingParticipant.value?.name ?? reviewingParticipant.value?.id ?? '')

const reviewingParticipantSubmissions = computed(() => {
  const submissions = reviewingParticipant.value?.registrationSubmissions ?? []
  const order = questionOrderMap.value
  return [...submissions].sort((a, b) => {
    const orderA = order.get(a.questionId ?? '') ?? Number.MAX_SAFE_INTEGER
    const orderB = order.get(b.questionId ?? '') ?? Number.MAX_SAFE_INTEGER
    return orderA - orderB
  })
})

const reviewingParticipantReviews = computed(() => {
  return reviewingParticipant.value?.reviews ?? []
})

const isReReview = computed(() => {
  return reviewingParticipantReviews.value.length > 0
})

const hasNoExpandedData = computed(() => {
  return !participantDetail.value?.reviews?.length && !sortedSubmissions.value.length
})

const reviewModalTitle = computed(() => {
  return isReReview.value ? 'Re-review Participant' : 'Review Participant'
})

const reviewModalDescription = computed(() => {
  return isReReview.value
    ? "Add a new review for this participant. The latest review will be the final decision."
    : "Review this participant's application and approve or reject it."
})

function openReviewModal(participantId: string) {
  if (!participantId) return
  reviewingParticipantId.value = participantId
  reviewReason.value = ''
  isReviewModalOpen.value = true
}

function openFirstPendingReviewModal() {
  const firstPendingId = prioritizedPendingParticipants.value[0]?.id
  if (!firstPendingId) return
  openReviewModal(firstPendingId)
}

function closeReviewModal() {
  isReviewModalOpen.value = false
  reviewingParticipantId.value = null
  reviewReason.value = ''
}

async function handleReview(decision: 'accept' | 'reject') {
  if (!reviewingParticipantId.value) return
  
  const trimmedReason = reviewReason.value?.trim() || null
  if (decision === 'reject' && !trimmedReason) {
    toast.add({
      title: 'Reason required',
      description: 'Please provide a reason before rejecting this participant.',
      color: 'warning',
    })
    return
  }
  
  try {
    await reviewMutation.mutateAsync({
      participantUserId: reviewingParticipantId.value,
      review: {
        decision,
        reason: trimmedReason,
      },
    })
    await queryClient.invalidateQueries({ queryKey: ['hackathons', hackathonId.value, 'participants', 'organizer'] })
    closeReviewModal()
    toast.add({
      title: 'Review submitted',
      description: `Participant has been ${decision === 'accept' ? 'approved' : 'rejected'}.`,
      color: 'success',
    })
  }
  catch (error) {
    const statusCode = getErrorStatusCode(error)
    if (statusCode === 409) {
      await queryClient.invalidateQueries({ queryKey: ['hackathons', hackathonId.value, 'participants', 'organizer'] })
      closeReviewModal()
      toast.add({
        title: 'Already reviewed',
        description: 'Another organizer already reviewed this participant. The list has been refreshed.',
        color: 'warning',
      })
      return
    }

    toast.add({
      title: 'Review failed',
      description: 'Please try again.',
      color: 'error',
    })
  }
}

function getErrorStatusCode(error: unknown): number | null {
  if (!error || typeof error !== 'object') return null
  const unknownError = error as {
    responseStatusCode?: unknown
    statusCode?: unknown
    response?: { status?: unknown }
  }

  if (typeof unknownError.responseStatusCode === 'number') return unknownError.responseStatusCode
  if (typeof unknownError.statusCode === 'number') return unknownError.statusCode
  if (typeof unknownError.response?.status === 'number') return unknownError.response.status
  return null
}

function getStatusColor(status: ParticipantConcludedStatus | null | undefined): 'success' | 'error' | 'warning' {
  if (status === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Accepted) return 'success'
  if (status === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Rejected) return 'error'
  return 'warning'
}

function getStatusLabel(status: ParticipantConcludedStatus | null | undefined): string {
  if (status === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Accepted) return 'Approved'
  if (status === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantConcludedStatusObject.Rejected) return 'Rejected'
  return 'Pending'
}

function getReviewStatusLabel(status: ParticipantReviewStatus | null | undefined): string {
  if (status === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantReviewItem_ParticipantReviewStatusObject.Accepted) return 'Accepted'
  if (status === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantReviewItem_ParticipantReviewStatusObject.Rejected) return 'Rejected'
  return 'Unknown'
}

function getReviewStatusColor(status: ParticipantReviewStatus | null | undefined): 'success' | 'error' | 'neutral' {
  if (status === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantReviewItem_ParticipantReviewStatusObject.Accepted) return 'success'
  if (status === HackOManiaApiEndpointsOrganizersHackathonParticipantsListParticipantReviewItem_ParticipantReviewStatusObject.Rejected) return 'error'
  return 'neutral'
}
</script>

<template>
  <div>
    <UCard>
      <template #header>
        <div class="flex flex-col gap-3">
          <div class="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
            <h3 class="text-sm font-semibold">
              Participants
            </h3>
            <div class="flex w-full flex-col gap-2 sm:flex-row sm:flex-wrap sm:items-center lg:w-auto">
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
                class="w-full sm:w-44"
                @update:model-value="sortKey = $event as SortKey"
              />
              <UButton
                size="sm"
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
          <div class="flex flex-col gap-2 sm:flex-row sm:flex-wrap sm:items-center sm:justify-between">
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
            <UButton
              size="xs"
              icon="i-lucide-clipboard-check"
              :color="overduePendingCount > 0 ? 'error' : 'warning'"
              variant="soft"
              class="w-full justify-center sm:w-auto"
              :disabled="!pendingParticipants.length"
              @click="openFirstPendingReviewModal"
            >
              Review Pending ({{ pendingParticipants.length }})
              <template v-if="overduePendingCount > 0">
                • Overdue {{ overduePendingCount }}
              </template>
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
          v-bind="participantListContainerProps"
          class="max-h-[42rem] overflow-y-auto"
        >
          <div
            v-bind="participantListWrapperProps"
            class="divide-y divide-(--ui-border)"
          >
            <div
              v-for="{ data: participant, index } in virtualParticipants"
              :key="participant.id ?? index"
              :class="[
                'py-2',
                isReviewOverdue(participant) ? 'rounded-md bg-error/5 px-2 -mx-2' : '',
              ]"
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
                  <p class="text-xs text-(--ui-text-muted)">
                    Applied: {{ formatDateTime(getParticipantApplicationDate(participant)) }}
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
                    <UBadge
                      v-if="isReviewOverdue(participant)"
                      color="error"
                      variant="subtle"
                      size="xs"
                    >
                      Overdue ({{ REVIEW_OVERDUE_DAYS }}d+)
                    </UBadge>
                    <UButton
                      size="xs"
                      :variant="isPendingParticipant(participant) ? 'soft' : 'ghost'"
                      :color="isPendingParticipant(participant) ? 'warning' : 'neutral'"
                      icon="i-lucide-clipboard-check"
                      @click="openReviewModal(participant.id ?? '')"
                    >
                      {{ isPendingParticipant(participant) ? 'Review' : 'Re-review' }}
                    </UButton>
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
                <div v-else>
                  <div v-if="participantDetail?.reviews && participantDetail.reviews.length > 0" class="mb-4">
                    <h4 class="text-sm font-semibold mb-3">
                      Review History ({{ participantDetail.reviews.length }})
                    </h4>
                    <div class="space-y-2">
                      <div
                        v-for="(review, reviewIndex) in participantDetail.reviews"
                        :key="review.id ?? reviewIndex"
                        class="rounded-lg border border-default p-3"
                      >
                        <div class="flex items-start justify-between mb-1">
                          <UBadge
                            :color="getReviewStatusColor(review.status)"
                            variant="subtle"
                            size="xs"
                          >
                            {{ getReviewStatusLabel(review.status) }}
                          </UBadge>
                          <span class="text-xs text-(--ui-text-muted)">
                            {{ formatDateTime(review.createdAt) }}
                          </span>
                        </div>
                        <p
                          v-if="review.reason"
                          class="text-xs text-(--ui-text-muted) mt-1"
                        >
                          {{ review.reason }}
                        </p>
                        <p
                          v-else
                          class="text-xs text-(--ui-text-muted) italic mt-1"
                        >
                          No reason provided
                        </p>
                      </div>
                    </div>
                  </div>

                  <div v-if="sortedSubmissions.length">
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
                </div>

                <div
                  v-if="hasNoExpandedData"
                  class="text-sm text-(--ui-text-muted)"
                >
                  No data found.
                </div>
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
            :virtualize="tableVirtualizeOptions"
            class="min-w-full max-h-[42rem] overflow-auto"
          >
            <template #name-cell="{ row }">
              <div class="flex items-center gap-2">
                <span class="text-sm font-medium">
                  {{ row.original.name ?? row.original.id }}
                </span>
                <UBadge
                  v-if="isReviewOverdue(row.original)"
                  color="error"
                  variant="subtle"
                  size="xs"
                >
                  Overdue ({{ REVIEW_OVERDUE_DAYS }}d+)
                </UBadge>
              </div>
            </template>

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
                <UBadge
                  v-if="isReviewOverdue(row.original)"
                  color="error"
                  variant="subtle"
                  size="xs"
                  class="ml-1"
                >
                  Overdue
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
                  :variant="isPendingParticipant(row.original) ? 'soft' : 'ghost'"
                  :color="isPendingParticipant(row.original) ? 'warning' : 'neutral'"
                  icon="i-lucide-clipboard-check"
                  @click="openReviewModal(row.original.id ?? '')"
                >
                  {{ isPendingParticipant(row.original) ? 'Review' : 'Re-review' }}
                </UButton>
              </div>
              <span
                v-else
                class="text-(--ui-text-muted) text-xs"
              >—</span>
            </template>
          </UTable>
        </div>
      </div>
    </UCard>

    <UModal
      v-model:open="isReviewModalOpen"
      :title="reviewModalTitle"
      :description="reviewModalDescription"
    >
      <template #content>
        <div class="overflow-auto max-h-[80vh]">
          <UCard>
            <template #header>
              <div class="flex items-center justify-between">
                <h3 class="text-base font-semibold">
                  {{ reviewingParticipantName || 'Participant Review' }}
                </h3>
                <UButton
                  variant="ghost"
                  icon="i-lucide-x"
                  size="xs"
                  @click="closeReviewModal"
                />
              </div>
            </template>

            <div class="space-y-4">
              <template v-if="reviewingParticipant">
                <div class="rounded-lg border border-default p-3 text-sm">
                  <p>
                    <strong>Team:</strong> {{ reviewingParticipant.teamName ?? 'No team' }}
                  </p>
                  <p>
                    <strong>Applied:</strong> {{ formatDateTime(getParticipantApplicationDate(reviewingParticipant)) }}
                  </p>
                  <p>
                    <strong>Current Status:</strong>
                    <UBadge
                      :color="getStatusColor(reviewingParticipant.concludedStatus)"
                      variant="subtle"
                      size="xs"
                      class="ml-1"
                    >
                      {{ getStatusLabel(reviewingParticipant.concludedStatus) }}
                    </UBadge>
                  </p>
                </div>

                <div v-if="reviewingParticipantReviews.length > 0">
                  <h4 class="text-sm font-semibold mb-2">
                    Review History ({{ reviewingParticipantReviews.length }})
                  </h4>
                  <div class="max-h-48 overflow-y-auto rounded-lg border border-default p-3 space-y-3">
                    <div
                      v-for="(review, index) in reviewingParticipantReviews"
                      :key="review.id ?? index"
                      class="border-b border-default last:border-b-0 pb-3 last:pb-0"
                    >
                      <div class="flex items-start justify-between mb-1">
                        <UBadge
                          :color="getReviewStatusColor(review.status)"
                          variant="subtle"
                          size="xs"
                        >
                          {{ getReviewStatusLabel(review.status) }}
                        </UBadge>
                        <span class="text-xs text-(--ui-text-muted)">
                          {{ formatDateTime(review.createdAt) }}
                        </span>
                      </div>
                      <p
                        v-if="review.reason"
                        class="text-xs text-(--ui-text-muted) mt-1"
                      >
                        {{ review.reason }}
                      </p>
                      <p
                        v-else
                        class="text-xs text-(--ui-text-muted) italic mt-1"
                      >
                        No reason provided
                      </p>
                    </div>
                  </div>
                </div>

                <div>
                  <h4 class="text-sm font-semibold mb-2">
                    Application Responses
                  </h4>
                  <div
                    v-if="reviewingParticipantSubmissions.length"
                    class="max-h-72 overflow-y-auto rounded-lg border border-default p-3 space-y-3"
                  >
                    <UFormField
                      v-for="submission in reviewingParticipantSubmissions"
                      :key="submission.questionId ?? ''"
                      :label="submission.questionText ?? 'Question'"
                    >
                      <UTextarea
                        :model-value="formatSubmissionAnswer(submission)"
                        disabled
                        autoresize
                        :rows="2"
                      />
                    </UFormField>
                  </div>
                  <p
                    v-else
                    class="text-sm text-(--ui-text-muted)"
                  >
                    No form responses found.
                  </p>
                </div>
              </template>

              <UFormField label="Review note">
                <UTextarea
                  v-model="reviewReason"
                  :rows="3"
                  placeholder="Provide a reason for rejection (optional for approval)..."
                />
              </UFormField>

              <div class="flex justify-end gap-2">
                <UButton
                  variant="ghost"
                  :disabled="reviewMutation.isPending.value"
                  @click="closeReviewModal"
                >
                  Cancel
                </UButton>
                <UButton
                  color="error"
                  :disabled="reviewMutation.isPending.value || !reviewReason.trim()"
                  :loading="reviewMutation.isPending.value"
                  @click="handleReview('reject')"
                >
                  Reject
                </UButton>
                <UButton
                  color="success"
                  :loading="reviewMutation.isPending.value"
                  @click="handleReview('accept')"
                >
                  Approve
                </UButton>
              </div>
            </div>
          </UCard>
        </div>
      </template>
    </UModal>
  </div>
</template>
