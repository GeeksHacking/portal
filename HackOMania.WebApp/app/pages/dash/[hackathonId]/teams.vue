<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import { useVirtualList } from '@vueuse/core'
import { computed, ref } from 'vue'
import { participantOrganizerQueries } from '~/composables/participants'
import { teamOrganizerQueries } from '~/composables/teams'

const props = withDefaults(defineProps<{
  hackathonId?: string
}>(), {
  hackathonId: '',
})
const route = useRoute()
const hackathonId = computed(() => props.hackathonId || (route.params.hackathonId as string | undefined) || '')

const { data: teamsData, isLoading: isLoadingTeams } = useQuery(
  computed(() => ({
    ...teamOrganizerQueries.list(hackathonId.value),
    enabled: !!hackathonId.value,
  })),
)

const { data: participantsData } = useQuery(
  computed(() => ({
    ...participantOrganizerQueries.list(hackathonId.value),
    enabled: !!hackathonId.value,
  })),
)

const teams = computed(() => teamsData.value?.teams ?? [])

// Map teamId -> list of participant names
const membersByTeamId = computed(() => {
  const map = new Map<string, string[]>()
  for (const p of participantsData.value?.participants ?? []) {
    if (p.teamId) {
      const list = map.get(p.teamId) ?? []
      list.push(p.name ?? p.id ?? 'Unknown')
      map.set(p.teamId, list)
    }
  }
  return map
})

const expandedTeamId = ref<string | null>(null)

function getTeamListItemHeight(index: number) {
  const team = teams.value[index]
  if (!team)
    return 68
  return expandedTeamId.value === team.id ? 360 : 68
}

const {
  list: virtualTeams,
  containerProps: teamsContainerProps,
  wrapperProps: teamsWrapperProps,
} = useVirtualList(teams, {
  itemHeight: getTeamListItemHeight,
  overscan: 8,
})

function toggleTeam(teamId: string) {
  expandedTeamId.value = expandedTeamId.value === teamId ? null : teamId
}
</script>

<template>
  <UDashboardPanel id="teams">
    <template #header>
      <UDashboardNavbar title="Teams">
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
            v-bind="teamsContainerProps"
            class="max-h-[40rem] overflow-y-auto"
          >
            <div
              v-bind="teamsWrapperProps"
              class="divide-y divide-(--ui-border)"
            >
              <div
                v-for="{ data: team, index } in virtualTeams"
                :key="team.id ?? index"
                class="py-2"
              >
                <div class="flex items-center justify-between gap-2">
                  <div class="flex-1 min-w-0">
                    <button
                      class="text-sm font-medium text-left hover:underline cursor-pointer text-(--ui-text-highlighted)"
                      @click="toggleTeam(team.id ?? '')"
                    >
                      {{ team.name }}
                      <UIcon
                        :name="expandedTeamId === team.id ? 'i-lucide-chevron-up' : 'i-lucide-chevron-down'"
                        class="inline-block w-4 h-4 ml-1 align-middle"
                      />
                    </button>
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

                <!-- Expanded: Team Details -->
                <div>
                  <div
                    v-if="expandedTeamId === team.id"
                    class="mt-3 ml-2 p-4 rounded-lg bg-(--ui-bg-elevated) border border-(--ui-border) max-h-96 overflow-y-auto space-y-3"
                  >
                    <UFormField label="Challenge">
                      <UInput
                        model-value="Not Selected Yet"
                        disabled
                        class="w-full"
                      />
                    </UFormField>

                    <div>
                      <h4 class="text-xs font-semibold mb-2">
                        Members
                      </h4>
                      <div
                        v-if="membersByTeamId.get(team.id ?? '')?.length"
                        class="space-y-1"
                      >
                        <div
                          v-for="(member, idx) in membersByTeamId.get(team.id ?? '')"
                          :key="idx"
                          class="text-sm flex items-center gap-2"
                        >
                          <UIcon
                            name="i-lucide-user"
                            class="w-4 h-4 text-(--ui-text-muted)"
                          />
                          {{ member }}
                        </div>
                      </div>
                      <p
                        v-else
                        class="text-xs text-(--ui-text-muted)"
                      >
                        No members found.
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </UCard>
      </div>
    </template>
  </UDashboardPanel>
</template>
