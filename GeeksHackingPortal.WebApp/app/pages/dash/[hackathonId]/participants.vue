<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListParticipantItem,
  GeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationQuestionsListQuestionDto,
} from '@geekshacking/portal-sdk'
import type { GTabItem } from '~/components/g/Tabs.vue'
import type { GTableColumn } from '~/utils/g-table'
import type { ReviewState } from '~/utils/participant-review'
import {
  useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsGetEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationQuestionsListEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useVirtualList } from '@vueuse/core'
import { registrationPageConfig } from '~/config/registration-pages'
import {
  applicationTimeOf,
  formatOrganizerDateTime,
  isReviewOverdue,
  REVIEW_OVERDUE_DAYS,
  reviewPriorityOf,
  reviewStateBadge,
  reviewStateOf,
  waitedFor,
} from '~/utils/participant-review'

/**
 * Hackathon participants: the organisers' review queue. Filter by state (with counts), search,
 * sort; pick someone to read their answers and approve or reject them in the side panel (a
 * dialog on narrow screens). The table view shows every answer in columns.
 */
type ParticipantItem = GeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListParticipantItem
type RegistrationQuestion = GeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationQuestionsListQuestionDto
type FilterStatus = 'all' | ReviewState
type SortKey = 'applicationTime' | 'name' | 'team' | 'status'
type ViewMode = 'list' | 'table'

const props = withDefaults(defineProps<{
  hackathonId?: string
}>(), {
  hackathonId: '',
})
const route = useRoute()
const hackathonId = computed(() => props.hackathonId || (route.params.hackathonId as string | undefined) || '')

const toast = useGToast()

const {
  data: participantsData,
  isLoading: isLoadingParticipants,
  error: participantsError,
} = useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListEndpoint({ path: computed(() => ({ hackathonId: hackathonId.value })) })
const participants = computed<ParticipantItem[]>(() => participantsData.value?.participants ?? [])

// ---- filters, search, sort ----
const activeFilter = ref<FilterStatus>('all')
const searchQuery = ref('')
const sortKey = ref<SortKey>('applicationTime')
const sortDirection = ref<'asc' | 'desc'>('desc')
const viewMode = ref<ViewMode>('list')

const counts = computed(() => {
  const result = { all: 0, pending: 0, approved: 0, rejected: 0, incomplete: 0 }
  for (const participant of participants.value) {
    const state = reviewStateOf(participant)
    result[state]++
    if (state !== 'incomplete')
      result.all++
  }
  return result
})

const filterTabs = computed<GTabItem[]>(() => [
  { value: 'all', label: `All submitted · ${counts.value.all}` },
  { value: 'pending', label: `To review · ${counts.value.pending}` },
  { value: 'approved', label: `Approved · ${counts.value.approved}` },
  { value: 'rejected', label: `Rejected · ${counts.value.rejected}` },
  { value: 'incomplete', label: `Incomplete · ${counts.value.incomplete}` },
])
const filterModel = computed({
  get: () => activeFilter.value,
  set: (value: string) => { activeFilter.value = value as FilterStatus },
})

const sortOptions = [
  { label: 'Applied', value: 'applicationTime' },
  { label: 'Name', value: 'name' },
  { label: 'Team', value: 'team' },
  { label: 'Status', value: 'status' },
]
const sortModel = computed({
  get: () => sortKey.value,
  set: (value) => { sortKey.value = value as SortKey },
})

const viewTabs: GTabItem[] = [
  { value: 'list', label: 'Queue', icon: 'material-symbols:view-list-rounded' },
  { value: 'table', label: 'Table', icon: 'material-symbols:table-rows-rounded' },
]
const viewModel = computed({
  get: () => viewMode.value,
  set: (value: string) => {
    viewMode.value = value as ViewMode
    // the table opens people in a dialog: don't pop one up just for switching views
    selectedId.value = null
  },
})

function matchesFilter(participant: ParticipantItem, filter: FilterStatus) {
  const state = reviewStateOf(participant)
  return filter === 'all' ? state !== 'incomplete' : state === filter
}

function matchesSearch(participant: ParticipantItem, query: string) {
  const id = (participant.id ?? '').toLowerCase()
  const compactQuery = query.replace(/[^a-z0-9]/g, '')
  if (id.includes(query) || (compactQuery && id.replace(/[^a-z0-9]/g, '').includes(compactQuery)))
    return true
  if ([participant.name, participant.teamName, participant.email].filter(Boolean).join(' ').toLowerCase().includes(query))
    return true
  return (participant.registrationSubmissions ?? []).some(submission =>
    [submission.questionText, submission.value, submission.followUpValue].filter(Boolean).join(' ').toLowerCase().includes(query))
}

function compareStrings(a?: string | null, b?: string | null) {
  return (a ?? '').localeCompare(b ?? '', undefined, { sensitivity: 'base' })
}

const statusOrder: Record<ReviewState, number> = { pending: 1, approved: 2, rejected: 3, incomplete: 4 }

const visibleParticipants = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()
  const direction = sortDirection.value === 'asc' ? 1 : -1
  return participants.value
    .filter(p => matchesFilter(p, activeFilter.value))
    .filter(p => !query || matchesSearch(p, query))
    .sort((a, b) => {
      // overdue and waiting applications always come first
      const priority = reviewPriorityOf(a) - reviewPriorityOf(b)
      if (priority !== 0)
        return priority
      let result = 0
      if (sortKey.value === 'team')
        result = compareStrings(a.teamName, b.teamName)
      else if (sortKey.value === 'applicationTime')
        result = applicationTimeOf(a) - applicationTimeOf(b)
      else if (sortKey.value === 'status')
        result = statusOrder[reviewStateOf(a)] - statusOrder[reviewStateOf(b)]
      else
        result = compareStrings(a.name, b.name)
      return (result || compareStrings(a.name, b.name)) * direction
    })
})

const hasFilters = computed(() => !!searchQuery.value.trim() || activeFilter.value !== 'all')

function clearFilters() {
  searchQuery.value = ''
  activeFilter.value = 'all'
}

// ---- the queue: oldest waiting first ----
const pendingQueue = computed(() => participants.value
  .filter(p => reviewStateOf(p) === 'pending')
  .sort((a, b) => (reviewPriorityOf(a) - reviewPriorityOf(b)) || (applicationTimeOf(a) - applicationTimeOf(b))))
const overdueCount = computed(() => pendingQueue.value.filter(p => isReviewOverdue(p)).length)

// ---- selection + detail ----
const selectedId = ref<string | null>(null)
const detailOpen = computed({
  get: () => !!selectedId.value,
  set: (value) => {
    if (!value)
      selectedId.value = null
  },
})
const selected = computed(() => participants.value.find(p => p.id === selectedId.value) ?? null)

const { data: participantDetail, isLoading: isLoadingDetail } = useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsGetEndpoint(
  { path: computed(() => ({ hackathonId: hackathonId.value, userId: selectedId.value ?? '' })) },
  { query: { enabled: computed(() => !!hackathonId.value && !!selectedId.value) } },
)

const nextInQueue = computed(() => pendingQueue.value.find(p => p.id !== selectedId.value) ?? null)

const {
  list: virtualRows,
  containerProps,
  wrapperProps,
  scrollTo,
} = useVirtualList(visibleParticipants, { itemHeight: 76, overscan: 8 })

// ?participant=<id> (e.g. from a team's member list) opens that person once the list loads
watch([participants, () => route.query.participant], ([list, wanted]) => {
  if (typeof wanted !== 'string' || !wanted || selectedId.value === wanted)
    return
  const person = list.find(p => p.id === wanted)
  if (!person)
    return
  if (!matchesFilter(person, activeFilter.value))
    activeFilter.value = reviewStateOf(person)
  selectedId.value = wanted
  nextTick(() => {
    const index = visibleParticipants.value.findIndex(p => p.id === wanted)
    if (index >= 0)
      scrollTo(index)
  })
}, { immediate: true })

function select(id: string | undefined) {
  if (id)
    selectedId.value = id
}

function reviewNext() {
  const next = nextInQueue.value
  if (!next?.id)
    return
  // make sure they're in the list being shown
  if (!matchesFilter(next, activeFilter.value))
    activeFilter.value = 'pending'
  if (searchQuery.value.trim() && !matchesSearch(next, searchQuery.value.trim().toLowerCase()))
    searchQuery.value = ''
  selectedId.value = next.id
  nextTick(() => {
    const index = visibleParticipants.value.findIndex(p => p.id === next.id)
    if (index >= 0 && viewMode.value === 'list')
      scrollTo(index)
  })
}

function onReviewed(decision: 'accept' | 'reject') {
  const name = selected.value?.name || 'Participant'
  const next = nextInQueue.value
  toast.add({
    tone: 'ok',
    title: decision === 'accept' ? `Approved ${name}` : `Rejected ${name}`,
    description: next ? `${pendingQueue.value.length} left to review.` : 'Nobody else is waiting for review.',
    action: next ? { label: 'Review next', onClick: reviewNext } : undefined,
  })
}

// ---- answers in the form's question order ----
const { data: questionsData } = useGeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationQuestionsListEndpoint({ path: computed(() => ({ hackathonId: hackathonId.value })) })

const orderedQuestions = computed<RegistrationQuestion[]>(() => {
  const categories = questionsData.value?.categories ?? []
  const ordered: RegistrationQuestion[] = []
  const seen = new Set<string>()
  const add = (question: RegistrationQuestion) => {
    if (question.id && !seen.has(question.id)) {
      ordered.push(question)
      seen.add(question.id)
    }
  }
  for (const categoryName of registrationPageConfig.flatMap(page => [...page.categories]))
    categories.find(c => c.name === categoryName)?.questions?.forEach(add)
  for (const category of categories)
    category.questions?.forEach(add)
  return ordered
})

const questionOrder = computed(() => new Map(orderedQuestions.value.map((q, index) => [q.id ?? '', index])))

const selectedAnswers = computed(() => {
  const order = questionOrder.value
  return [...(selected.value?.registrationSubmissions ?? [])].sort((a, b) =>
    (order.get(a.questionId ?? '') ?? Number.MAX_SAFE_INTEGER) - (order.get(b.questionId ?? '') ?? Number.MAX_SAFE_INTEGER))
})

// ---- table view ----
function answerText(value?: string | null, followUp?: string | null) {
  const main = (value ?? '').trim()
  const extra = (followUp ?? '').trim()
  if (main && extra)
    return `${main}\nFollow-up: ${extra}`
  return main || (extra ? `Follow-up: ${extra}` : '')
}

type TableRow = Record<string, string | number | boolean | undefined> & { id: string }

const tableRows = computed<TableRow[]>(() => visibleParticipants.value.map((p) => {
  const row: TableRow = {
    id: p.id ?? '',
    name: p.name || 'Unnamed participant',
    teamName: p.teamName || '—',
    state: reviewStateOf(p),
    overdue: isReviewOverdue(p),
    applied: formatOrganizerDateTime(applicationTimeOf(p)),
  }
  for (const submission of p.registrationSubmissions ?? []) {
    if (submission.questionId)
      row[`q_${submission.questionId}`] = answerText(submission.value, submission.followUpValue)
  }
  return row
}))

const tableColumns = computed<GTableColumn<TableRow>[]>(() => [
  { accessorKey: 'name', header: 'Participant', meta: { class: { th: 'min-w-[12rem]' } } },
  { accessorKey: 'teamName', header: 'Team', meta: { class: { th: 'min-w-[10rem]' } } },
  { id: 'status', accessorKey: 'state', header: 'Status', meta: { class: { th: 'w-[9rem]' } } },
  { accessorKey: 'applied', header: 'Applied', meta: { class: { th: 'min-w-[11rem]', td: 'whitespace-nowrap' } } },
  ...orderedQuestions.value.map(question => ({
    id: `q_${question.id}`,
    accessorKey: `q_${question.id}`,
    header: question.questionText ?? 'Question',
    meta: { class: { th: 'min-w-[14rem] max-w-[22rem] whitespace-normal', td: 'min-w-[14rem] max-w-[22rem] align-top whitespace-pre-line text-sm' } },
  })),
])
</script>

<template>
  <GPanel id="participants">
    <template #header>
      <GPanelHeader title="Participants">
        <template #right>
          <GButton
            variant="primary"
            icon="material-symbols:fact-check-rounded"
            :label="pendingQueue.length ? `Review next · ${pendingQueue.length}` : 'Nobody to review'"
            :disabled="!nextInQueue"
            @click="reviewNext"
          />
        </template>
      </GPanelHeader>
    </template>

    <template #body>
      <div class="pq">
        <GAlert
          v-if="participantsError && !participants.length"
          tone="err"
          title="Couldn't load participants"
          :description="getApiErrorMessage(participantsError, 'Refresh the page to try again.')"
        />

        <GAlert
          v-if="overdueCount"
          tone="attention"
          :title="overdueCount === 1 ? `1 application has waited ${REVIEW_OVERDUE_DAYS}+ days` : `${overdueCount} applications have waited ${REVIEW_OVERDUE_DAYS}+ days`"
          description="They're at the top of the list. Review next starts with the oldest."
        />

        <!-- toolbar -->
        <div class="pq-toolbar">
          <GTabs
            v-model="filterModel"
            :items="filterTabs"
            :content="false"
            size="sm"
            aria-label="Show participants"
            class="pq-toolbar__filters"
          />
          <div class="pq-toolbar__row">
            <GInput
              v-model="searchQuery"
              type="search"
              icon="material-symbols:search-rounded"
              placeholder="Search names, teams, emails, answers"
              aria-label="Search participants"
              class="pq-toolbar__search"
            />
            <div class="pq-toolbar__sort">
              <GSelect
                v-model="sortModel"
                :items="sortOptions"
                aria-label="Sort by"
                class="pq-toolbar__select"
              />
              <GButton
                variant="ghost"
                :icon="sortDirection === 'asc' ? 'material-symbols:arrow-upward-rounded' : 'material-symbols:arrow-downward-rounded'"
                :aria-label="sortDirection === 'asc' ? 'Sorted oldest or A first. Switch to newest or Z first' : 'Sorted newest or Z first. Switch to oldest or A first'"
                :title="sortDirection === 'asc' ? 'Ascending' : 'Descending'"
                @click="sortDirection = sortDirection === 'asc' ? 'desc' : 'asc'"
              />
            </div>
            <GTabs
              v-model="viewModel"
              :items="viewTabs"
              :content="false"
              size="sm"
              aria-label="View"
            />
          </div>
        </div>

        <OrganizersListDetail
          v-model:open="detailOpen"
          :label="selected ? `Application: ${selected.name || 'participant'}` : 'Application'"
          :dock="viewMode === 'list'"
        >
          <template #list>
            <div
              v-if="isLoadingParticipants"
              class="pq-box pq-box--loading"
              aria-busy="true"
            >
              <GSkeleton
                v-for="i in 6"
                :key="i"
                class="pq-skeleton"
              />
            </div>

            <GEmptyState
              v-else-if="!participants.length"
              icon="material-symbols:group-off-rounded"
              title="No participants yet"
              description="People who sign up for this hackathon show up here."
            />

            <GEmptyState
              v-else-if="!visibleParticipants.length"
              icon="material-symbols:search-off-rounded"
              :title="searchQuery.trim() ? `No one matches “${searchQuery.trim()}”` : 'No one here'"
              :description="activeFilter === 'pending' ? 'Nobody is waiting for review.' : 'Try another search or filter.'"
            >
              <template
                v-if="hasFilters"
                #actions
              >
                <GButton
                  variant="secondary"
                  label="Show all submitted"
                  @click="clearFilters"
                />
              </template>
            </GEmptyState>

            <!-- queue: virtualised; rows are a fixed 76px -->
            <div
              v-else-if="viewMode === 'list'"
              v-bind="containerProps"
              class="pq-box pq-scroll"
            >
              <ul
                v-bind="wrapperProps"
                class="pq-list"
                :aria-label="`${visibleParticipants.length} participants`"
              >
                <li
                  v-for="{ data: p, index } in virtualRows"
                  :key="p.id ?? index"
                >
                  <button
                    type="button"
                    class="pq-row"
                    :class="{ 'is-selected': p.id === selectedId }"
                    :aria-current="p.id === selectedId ? 'true' : undefined"
                    @click="select(p.id)"
                  >
                    <span class="pq-row__main">
                      <span class="pq-row__name">{{ p.name || 'Unnamed participant' }}</span>
                      <span class="pq-row__meta">
                        {{ p.teamName || 'No team' }}
                        <template v-if="applicationTimeOf(p)">
                          · applied {{ waitedFor(applicationTimeOf(p)) }}
                        </template>
                      </span>
                    </span>
                    <span class="pq-row__badges">
                      <GBadge
                        v-if="isReviewOverdue(p)"
                        tone="err"
                        size="sm"
                        icon="material-symbols:schedule-rounded"
                        :label="`${REVIEW_OVERDUE_DAYS}d+`"
                        :title="`Waiting ${REVIEW_OVERDUE_DAYS}+ days`"
                      />
                      <GBadge
                        :tone="reviewStateBadge[reviewStateOf(p)].tone"
                        :variant="reviewStateOf(p) === 'pending' ? 'solid' : 'soft'"
                        size="sm"
                        :label="reviewStateBadge[reviewStateOf(p)].label"
                      />
                    </span>
                  </button>
                </li>
              </ul>
            </div>

            <!-- table: every answer in a column -->
            <GTable
              v-else
              :data="tableRows"
              :columns="tableColumns"
              row-key="id"
              manual-sort
              :virtualize="{ estimateSize: 64, overscan: 12 }"
              caption="Participants and their registration answers"
              class="pq-table"
            >
              <template #name-cell="{ row }">
                <button
                  type="button"
                  class="pq-name-link"
                  @click="select(row.original.id)"
                >
                  {{ row.original.name }}
                </button>
              </template>
              <template #status-cell="{ row }">
                <span class="pq-table__badges">
                  <GBadge
                    :tone="reviewStateBadge[row.original.state as ReviewState].tone"
                    :variant="row.original.state === 'pending' ? 'solid' : 'soft'"
                    size="sm"
                    :label="reviewStateBadge[row.original.state as ReviewState].label"
                  />
                  <GBadge
                    v-if="row.original.overdue"
                    tone="err"
                    size="sm"
                    :label="`${REVIEW_OVERDUE_DAYS}d+`"
                    :title="`Waiting ${REVIEW_OVERDUE_DAYS}+ days`"
                  />
                </span>
              </template>
            </GTable>
          </template>

          <template #detail="{ close }">
            <OrganizersParticipantReview
              v-if="selected"
              :hackathon-id="hackathonId"
              :participant="selected"
              :answers="selectedAnswers"
              :emails="participantDetail?.emailDeliveries ?? []"
              :loading-emails="isLoadingDetail"
              :next-name="nextInQueue?.name || (nextInQueue ? 'next applicant' : null)"
              @close="close"
              @reviewed="onReviewed"
              @next="reviewNext"
            />
          </template>

          <template #placeholder>
            <GEmptyState
              icon="material-symbols:fact-check-rounded"
              :title="pendingQueue.length ? 'Pick an application' : 'All caught up'"
              :description="pendingQueue.length ? `${pendingQueue.length} waiting for review. Choose one from the list, or press Review next.` : 'Nobody is waiting for review. Pick anyone to see their answers.'"
              :bordered="false"
            />
          </template>
        </OrganizersListDetail>
      </div>
    </template>
  </GPanel>
</template>

<style scoped>
.pq {
  display: flex;
  flex-direction: column;
  gap: 16px;
  width: 100%;
  max-width: 88rem;
  margin: 0 auto;
}

.pq-toolbar {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.pq-toolbar__filters { max-width: 100%; overflow-x: auto; }
.pq-toolbar__row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px 12px;
}
.pq-toolbar__search { flex: 1 1 18rem; }
.pq-toolbar__sort {
  display: flex;
  align-items: center;
  gap: 4px;
}
.pq-toolbar__select { width: 9rem; }

.pq-box {
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.pq-box--loading {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 12px;
}
.pq-skeleton {
  display: block;
  height: 52px;
}
.pq-scroll {
  max-height: min(70vh, 42rem);
  overflow-y: auto;
}
@media (min-width: 1024px) {
  .pq-scroll { max-height: calc(100dvh - 8.5rem); }
}
.pq-list {
  margin: 0;
  padding: 0;
  list-style: none;
}
.pq-list > li {
  height: 76px;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.pq-list > li:first-child { border-top: 0; }

.pq-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  width: 100%;
  height: 100%;
  padding: 0 16px;
  border: 0;
  border-left: 4px solid transparent;
  background: transparent;
  color: inherit;
  font: inherit;
  text-align: left;
  cursor: pointer;
  transition: background-color var(--gh-dur-fast) var(--ease-gh);
}
.pq-row:hover { background: var(--gh-ghost-hover); }
.pq-row:focus-visible {
  outline: 2px solid var(--gh-focus);
  outline-offset: -2px;
}
/* picked: the highlight surface + edge (pale yellow in light, yellow edge in dark) */
.pq-row.is-selected {
  border-left-color: var(--gh-highlight-edge);
  background: var(--gh-highlight);
  color: var(--gh-highlight-ink);
}
.pq-row.is-selected .pq-row__meta { color: inherit; }
.pq-row__main {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}
.pq-row__name,
.pq-row__meta {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.pq-row__name { font-weight: 700; }
.pq-row__meta {
  font-size: 14px;
  color: var(--gh-text-muted);
}
.pq-row__badges,
.pq-table__badges {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  gap: 4px;
}
@media (prefers-reduced-motion: reduce) {
  .pq-row { transition: none; }
}

.pq-table { max-height: calc(100dvh - 12rem); }
.pq-name-link {
  padding: 0;
  border: 0;
  background: none;
  color: inherit;
  font: inherit;
  font-weight: 700;
  text-align: left;
  text-decoration: underline;
  text-underline-offset: 3px;
  cursor: pointer;
}
.pq-name-link:focus-visible {
  outline: 2px solid var(--gh-focus);
  outline-offset: 2px;
  border-radius: 2px;
}
</style>
