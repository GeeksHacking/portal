<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import { useVirtualList } from '@vueuse/core'
import { computed } from 'vue'
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

const {
  list: virtualSubmissions,
  containerProps: submissionsContainerProps,
  wrapperProps: submissionsWrapperProps,
} = useVirtualList(submissions, {
  itemHeight: 56,
  overscan: 8,
})

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
              <h3 class="text-sm font-semibold">
                Submissions
              </h3>
              <UBadge
                variant="subtle"
                size="sm"
              >
                {{ submissions.length }} total
              </UBadge>
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
            v-else
            v-bind="submissionsContainerProps"
            class="max-h-[36rem] overflow-y-auto"
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
                  <p class="text-sm font-medium">
                    {{ submission.title }}
                  </p>
                  <p class="text-xs text-(--ui-text-muted)">
                    {{ submission.teamName }} - {{ submission.challengeTitle }}
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
      </div>
    </template>
  </UDashboardPanel>
</template>
