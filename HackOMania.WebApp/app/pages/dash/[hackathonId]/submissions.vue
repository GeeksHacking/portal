<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import { useVirtualList } from '@vueuse/core'
import { computed, ref } from 'vue'
import { submissionOrganizerQueries } from '~/composables/submissions'

const props = withDefaults(defineProps<{
  hackathonId?: string
}>(), {
  hackathonId: '',
})
const route = useRoute()
const hackathonId = computed(() => props.hackathonId || (route.params.hackathonId as string | undefined) || '')

const { data: submissionsData, isLoading: isLoadingSubmissions } = useQuery(
  computed(() => ({
    ...submissionOrganizerQueries.list(hackathonId.value),
    enabled: !!hackathonId.value,
  })),
)

const submissions = computed(() => submissionsData.value?.submissions ?? [])

// Search
const searchQuery = ref('')
const normalizedSearch = computed(() => searchQuery.value.trim().toLowerCase())

const filteredSubmissions = computed(() => {
  const query = normalizedSearch.value
  if (!query)
    return submissions.value

  return submissions.value.filter((submission) => {
    const searchableText = [
      submission.title ?? '',
      submission.teamName ?? '',
      submission.challengeTitle ?? '',
    ].join(' ').toLowerCase()

    return searchableText.includes(query)
  })
})

const {
  list: virtualSubmissions,
  containerProps: submissionsContainerProps,
  wrapperProps: submissionsWrapperProps,
} = useVirtualList(filteredSubmissions, {
  itemHeight: 56,
  overscan: 8,
})

// Detail modal
const isDetailOpen = ref(false)
const selectedSubmissionId = ref<string | null>(null)

const { data: submissionDetail, isLoading: isLoadingDetail } = useQuery(
  computed(() => ({
    ...submissionOrganizerQueries.detail(hackathonId.value, selectedSubmissionId.value ?? ''),
    enabled: !!hackathonId.value && !!selectedSubmissionId.value,
  })),
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
      <div class="p-4 space-y-4 overflow-y-auto">
        <UCard>
          <template #header>
            <div class="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
              <div class="flex flex-col gap-2 sm:flex-row sm:items-center sm:gap-3">
                <h3 class="text-sm font-semibold">
                  Submissions
                </h3>
                <UBadge
                  variant="subtle"
                  size="sm"
                >
                  <template v-if="searchQuery">
                    {{ filteredSubmissions.length }} shown · {{ submissions.length }} total
                  </template>
                  <template v-else>
                    {{ submissions.length }} total
                  </template>
                </UBadge>
              </div>
              <UInput
                v-model="searchQuery"
                icon="i-lucide-search"
                placeholder="Search submissions..."
                size="sm"
                class="w-full sm:max-w-xs"
              />
            </div>
          </template>

          <div
            v-if="isLoadingSubmissions"
            class="text-(--ui-text-muted) text-sm"
          >
            Loading submissions...
          </div>

          <div
            v-else-if="!submissions.length"
            class="text-(--ui-text-muted) text-sm"
          >
            No submissions yet.
          </div>

          <div
            v-else-if="!filteredSubmissions.length"
            class="text-(--ui-text-muted) text-sm"
          >
            No submissions matching "{{ searchQuery }}".
          </div>

          <div
            v-else
            v-bind="submissionsContainerProps"
            class="max-h-[40rem] overflow-y-auto"
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
        </UCard>

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
