<script setup lang="ts">
import { computed } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import { submissionOrganizerQueries } from '~/composables/submissions'

const props = defineProps<{
  hackathonId: string
  isOrganizer: boolean
}>()

const { data: submissionsData, isLoading: isLoadingSubmissions } = useQuery(
  computed(() => ({
    ...submissionOrganizerQueries.list(props.hackathonId),
    enabled: !!props.hackathonId && props.isOrganizer,
  })),
)

const submissions = computed(() => submissionsData.value?.submissions ?? [])

function formatDate(date: Date | null | undefined): string {
  if (!date) return 'Unknown'
  return new Date(date).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}
</script>

<template>
  <UCard>
    <template #header>
      <div class="flex items-center justify-between">
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
      class="divide-y divide-(--ui-border)"
    >
      <div
        v-for="submission in submissions"
        :key="submission.id ?? ''"
        class="py-2 flex items-center justify-between"
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
  </UCard>
</template>
