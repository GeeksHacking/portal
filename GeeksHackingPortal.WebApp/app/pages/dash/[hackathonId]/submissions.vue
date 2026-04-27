<script setup lang="ts">
import type { HackOManiaApiEndpointsOrganizersHackathonSubmissionsListSubmissionItem } from '~/api-client/models'
import {
  geeksHackingPortalApiEndpointsOrganizersHackathonSubmissionsGetEndpointQueryOptions,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonSubmissionsGetEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonSubmissionsListEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueries } from '@tanstack/vue-query'
import { useVirtualList } from '@vueuse/core'
import { computed, ref } from 'vue'
import * as XLSX from 'xlsx'

const props = withDefaults(defineProps<{
  hackathonId?: string
}>(), {
  hackathonId: '',
})
const route = useRoute()
const hackathonId = computed(() => props.hackathonId || (route.params.hackathonId as string | undefined) || '')

const { data: submissionsData, isLoading: isLoadingSubmissions } = useGeeksHackingPortalApiEndpointsOrganizersHackathonSubmissionsListEndpoint(
  computed(() => hackathonId.value),
)

const submissions = computed(() => submissionsData.value?.submissions ?? [])

// Fetch challenges for filter
const { data: challengesData } = useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesListEndpoint(
  computed(() => hackathonId.value),
)
const challenges = computed(() => challengesData.value?.challenges ?? [])

// Fetch submission details for each submission via organizer endpoint
const submissionDetailQueries = useQueries({
  queries: computed(() =>
    submissions.value.map(s =>
      geeksHackingPortalApiEndpointsOrganizersHackathonSubmissionsGetEndpointQueryOptions(
        hackathonId.value,
        s.id ?? '',
      ),
    ),
  ),
})

// Build a map of submissionId -> detail
const submissionDetailsMap = computed(() => {
  const map = new Map<string, {
    description?: string | null
    demoUri?: string | null
    repositoryUri?: string | null
    slidesUri?: string | null
  }>()
  for (const result of submissionDetailQueries.value) {
    const data = result.data
    if (!data?.id)
      continue

    map.set(data.id, {
      description: data.description,
      demoUri: data.demoUri,
      repositoryUri: data.repositoryUri,
      slidesUri: data.slidesUri,
    })
  }
  return map
})

type EnrichedSubmission = HackOManiaApiEndpointsOrganizersHackathonSubmissionsListSubmissionItem & {
  description?: string | null
  demoUri?: string | null
  repositoryUri?: string | null
  slidesUri?: string | null
}

const enrichedSubmissions = computed<EnrichedSubmission[]>(() => {
  return submissions.value.map((s) => {
    const details = s.id ? submissionDetailsMap.value.get(s.id) : undefined
    return { ...s, ...details }
  })
})

// Search
const searchQuery = ref('')
const normalizedSearch = computed(() => searchQuery.value.trim().toLowerCase())

// Challenge filter
const selectedChallengeId = ref<string | null>(null)

const challengeFilterOptions = computed(() => {
  return [
    { label: 'All challenges', value: null },
    ...challenges.value.map(c => ({
      label: c.title ?? 'Untitled',
      value: c.id ?? '',
    })),
  ]
})

const filteredSubmissions = computed(() => {
  let result = enrichedSubmissions.value

  // Filter by challenge
  if (selectedChallengeId.value) {
    result = result.filter(s => s.challengeId === selectedChallengeId.value)
  }

  // Filter by search
  const query = normalizedSearch.value
  if (query) {
    result = result.filter((submission) => {
      const searchableText = [
        submission.title ?? '',
        submission.teamName ?? '',
        submission.challengeTitle ?? '',
        submission.description ?? '',
      ].join(' ').toLowerCase()

      return searchableText.includes(query)
    })
  }

  return result
})

const {
  list: virtualSubmissions,
  containerProps: submissionsContainerProps,
  wrapperProps: submissionsWrapperProps,
} = useVirtualList(filteredSubmissions, {
  itemHeight: 72,
  overscan: 8,
})

// Detail modal
const isDetailOpen = ref(false)
const selectedSubmissionId = ref<string | null>(null)

const { data: submissionDetail, isLoading: isLoadingDetail } = useGeeksHackingPortalApiEndpointsOrganizersHackathonSubmissionsGetEndpoint(
  computed(() => hackathonId.value),
  computed(() => selectedSubmissionId.value ?? ''),
)

function openDetail(submissionId: string) {
  selectedSubmissionId.value = submissionId
  isDetailOpen.value = true
}

function formatDate(date: Date | null | undefined): string {
  if (!date)
    return 'Unknown'
  return new Date(date).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

// Excel export
function exportToExcel() {
  if (typeof window === 'undefined')
    return

  const rows = filteredSubmissions.value.map(s => ({
    'Title': s.title ?? '',
    'Team': s.teamName ?? '',
    'Challenge': s.challengeTitle ?? 'No challenge',
    'Description': s.description ?? '',
    'Repository URL': s.repositoryUri ?? '',
    'Demo URL': s.demoUri ?? '',
    'Slides URL': s.slidesUri ?? '',
    'Submitted At': s.submittedAt ? new Date(s.submittedAt).toISOString() : '',
  }))

  const workbook = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(
    workbook,
    XLSX.utils.json_to_sheet(rows),
    'Submissions',
  )

  const dateStamp = new Date().toISOString().split('T')[0]
  XLSX.writeFile(workbook, `submissions-${dateStamp}.xlsx`)
}
</script>

<template>
  <UDashboardPanel id="submissions">
    <template #header>
      <UDashboardNavbar title="Submissions">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="space-y-3">
        <div class="flex flex-col gap-3 xl:flex-row xl:items-end xl:justify-between">
          <div class="flex flex-col gap-2 sm:flex-row sm:flex-wrap sm:items-center">
            <UBadge
              variant="subtle"
              size="sm"
              class="w-fit"
            >
              <template v-if="searchQuery || selectedChallengeId">
                {{ filteredSubmissions.length }} shown · {{ enrichedSubmissions.length }} total
              </template>
              <template v-else>
                {{ enrichedSubmissions.length }} total
              </template>
            </UBadge>
            <USelect
              :model-value="selectedChallengeId ?? undefined"
              :items="challengeFilterOptions"
              value-key="value"
              label-key="label"
              placeholder="All challenges"
              size="sm"
              class="w-full sm:max-w-xs"
              @update:model-value="selectedChallengeId = $event ?? null"
            />
            <UInput
              v-model="searchQuery"
              icon="i-lucide-search"
              placeholder="Search submissions..."
              size="sm"
              class="w-full sm:max-w-xs"
            />
          </div>
          <UButton
            icon="i-lucide-download"
            size="sm"
            variant="outline"
            class="w-full sm:w-auto"
            :disabled="!filteredSubmissions.length"
            @click="exportToExcel"
          >
            Export Excel
          </UButton>
        </div>

        <div class="rounded-xl border border-(--ui-border) bg-(--ui-bg)">
          <div
            v-if="isLoadingSubmissions"
            class="px-4 py-4 text-sm text-(--ui-text-muted)"
          >
            Loading submissions...
          </div>

          <div
            v-else-if="!enrichedSubmissions.length"
            class="px-4 py-4 text-sm text-(--ui-text-muted)"
          >
            No submissions yet.
          </div>

          <div
            v-else-if="!filteredSubmissions.length"
            class="px-4 py-4 text-sm text-(--ui-text-muted)"
          >
            No submissions matching your filters.
          </div>

          <div
            v-else
            v-bind="submissionsContainerProps"
            class="max-h-[40rem] overflow-y-auto px-4"
          >
            <div
              v-bind="submissionsWrapperProps"
              class="divide-y divide-(--ui-border)"
            >
              <div
                v-for="{ data: submission, index } in virtualSubmissions"
                :key="submission.id ?? index"
                class="py-2 flex items-center justify-between gap-2"
              >
                <div class="flex-1 min-w-0">
                  <button
                    class="text-sm font-medium text-left hover:underline cursor-pointer text-(--ui-text-highlighted)"
                    @click="openDetail(submission.id ?? '')"
                  >
                    {{ submission.title }}
                  </button>
                  <p class="text-xs text-(--ui-text-muted)">
                    {{ submission.teamName }} · {{ submission.challengeTitle ?? 'No challenge' }}
                  </p>
                  <p
                    v-if="submission.description"
                    class="text-xs text-(--ui-text-dimmed) truncate max-w-md"
                  >
                    {{ submission.description }}
                  </p>
                </div>
                <div class="flex items-center gap-2 shrink-0">
                  <div
                    v-if="submission.demoUri || submission.repositoryUri || submission.slidesUri"
                    class="flex gap-1"
                  >
                    <UButton
                      v-if="submission.repositoryUri"
                      size="xs"
                      variant="ghost"
                      icon="i-lucide-github"
                      :to="String(submission.repositoryUri)"
                      target="_blank"
                    />
                    <UButton
                      v-if="submission.demoUri"
                      size="xs"
                      variant="ghost"
                      icon="i-lucide-globe"
                      :to="String(submission.demoUri)"
                      target="_blank"
                    />
                    <UButton
                      v-if="submission.slidesUri"
                      size="xs"
                      variant="ghost"
                      icon="i-lucide-presentation"
                      :to="String(submission.slidesUri)"
                      target="_blank"
                    />
                  </div>
                  <UBadge
                    variant="subtle"
                    size="xs"
                  >
                    {{ formatDate(submission.submittedAt) }}
                  </UBadge>
                </div>
              </div>
            </div>
          </div>
        </div>

        <UModal v-model:open="isDetailOpen">
          <template #content>
            <UCard>
              <template #header>
                <div class="flex items-center justify-between">
                  <h3 class="text-base font-semibold">
                    Submission Details
                  </h3>
                  <UButton
                    variant="ghost"
                    icon="i-lucide-x"
                    size="xs"
                    @click="isDetailOpen = false"
                  />
                </div>
              </template>

              <div
                v-if="isLoadingDetail"
                class="text-(--ui-text-muted) text-sm"
              >
                Loading details...
              </div>

              <div
                v-else-if="submissionDetail"
                class="space-y-4"
              >
                <div>
                  <p class="text-xs font-semibold text-(--ui-text-muted) uppercase">
                    Project Title
                  </p>
                  <p class="text-sm">
                    {{ submissionDetail.title }}
                  </p>
                </div>

                <div>
                  <p class="text-xs font-semibold text-(--ui-text-muted) uppercase">
                    Team
                  </p>
                  <p class="text-sm">
                    {{ submissionDetail.teamName }}
                  </p>
                </div>

                <div>
                  <p class="text-xs font-semibold text-(--ui-text-muted) uppercase">
                    Challenge
                  </p>
                  <p class="text-sm">
                    {{ submissionDetail.challengeTitle }}
                  </p>
                </div>

                <div v-if="submissionDetail.description">
                  <p class="text-xs font-semibold text-(--ui-text-muted) uppercase">
                    Description
                  </p>
                  <p class="text-sm whitespace-pre-wrap">
                    {{ submissionDetail.description }}
                  </p>
                </div>

                <div>
                  <p class="text-xs font-semibold text-(--ui-text-muted) uppercase">
                    Submitted At
                  </p>
                  <p class="text-sm">
                    {{ formatDate(submissionDetail.submittedAt) }}
                  </p>
                </div>

                <div
                  v-if="submissionDetail.repositoryUri || submissionDetail.demoUri || submissionDetail.slidesUri"
                  class="space-y-2"
                >
                  <p class="text-xs font-semibold text-(--ui-text-muted) uppercase">
                    Links
                  </p>
                  <div class="flex flex-wrap gap-2">
                    <UButton
                      v-if="submissionDetail.repositoryUri"
                      size="xs"
                      variant="outline"
                      icon="i-lucide-github"
                      :to="String(submissionDetail.repositoryUri)"
                      target="_blank"
                    >
                      Repository
                    </UButton>
                    <UButton
                      v-if="submissionDetail.demoUri"
                      size="xs"
                      variant="outline"
                      icon="i-lucide-globe"
                      :to="String(submissionDetail.demoUri)"
                      target="_blank"
                    >
                      Demo
                    </UButton>
                    <UButton
                      v-if="submissionDetail.slidesUri"
                      size="xs"
                      variant="outline"
                      icon="i-lucide-presentation"
                      :to="String(submissionDetail.slidesUri)"
                      target="_blank"
                    >
                      Slides
                    </UButton>
                  </div>
                </div>
              </div>
            </UCard>
          </template>
        </UModal>
      </div>
    </template>
  </UDashboardPanel>
</template>
