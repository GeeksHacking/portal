<script setup lang="ts">
import { computed } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import { teamOrganizerQueries } from '~/composables/teams'

const props = defineProps<{
  hackathonId: string
  isOrganizer: boolean
}>()

const { data: teamsData, isLoading: isLoadingTeams } = useQuery(
  computed(() => ({
    ...teamOrganizerQueries.list(props.hackathonId),
    enabled: !!props.hackathonId && props.isOrganizer,
  })),
)

const teams = computed(() => teamsData.value?.teams ?? [])
</script>

<template>
  <UCard>
    <template #header>
      <div class="flex items-center justify-between">
        <h3 class="text-sm font-semibold">
          Teams
        </h3>
        <UBadge
          variant="subtle"
          size="sm"
        >
          {{ teams.length }} total
        </UBadge>
      </div>
    </template>

    <div
      v-if="isLoadingTeams"
      class="text-(--ui-text-muted) text-sm"
    >
      Loading teams...
    </div>

    <div
      v-else-if="!teams.length"
      class="text-(--ui-text-muted) text-sm"
    >
      No teams yet.
    </div>

    <div
      v-else
      class="divide-y divide-(--ui-border)"
    >
      <div
        v-for="team in teams"
        :key="team.id ?? ''"
        class="py-2 flex items-center justify-between"
      >
        <div>
          <p class="text-sm font-medium">
            {{ team.name }}
          </p>
          <p class="text-xs text-(--ui-text-muted)">
            {{ team.description ?? 'No description' }}
          </p>
        </div>
        <UBadge
          variant="subtle"
          size="xs"
        >
          {{ team.memberCount }} {{ team.memberCount === 1 ? 'member' : 'members' }}
        </UBadge>
      </div>
    </div>
  </UCard>
</template>
