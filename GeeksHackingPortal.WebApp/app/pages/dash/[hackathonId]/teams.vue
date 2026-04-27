<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import {
  useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useVirtualList } from '@vueuse/core'
import { computed, ref } from 'vue'
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

const { data: participantsData } = useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListEndpoint(
  computed(() => hackathonId.value),
)

const { data: challengesData } = useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesListEndpoint(
  computed(() => hackathonId.value),
)

const teams = computed(() => teamsData.value?.teams ?? [])
const teamSearchQuery = ref('')
const challengeTitleById = computed(() => {
  const map = new Map<string, string>()
  for (const challenge of challengesData.value?.challenges ?? []) {
    if (challenge.id && challenge.title) {
      map.set(challenge.id, challenge.title)
    }
  }
  return map
})

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

const normalizedTeamSearch = computed(() => teamSearchQuery.value.trim().toLowerCase())

const filteredTeams = computed(() => {
  const query = normalizedTeamSearch.value
  if (!query)
    return teams.value

  return teams.value.filter((team) => {
    const members = membersByTeamId.value.get(team.id ?? '') ?? []
    const searchableText = [
      team.name ?? '',
      team.description ?? '',
      ...members,
    ].join(' ').toLowerCase()

    return searchableText.includes(query)
  })
})

const expandedTeamId = ref<string | null>(null)

function getTeamListItemHeight(index: number) {
  const team = filteredTeams.value[index]
  if (!team)
    return 68
  return expandedTeamId.value === team.id ? 360 : 68
}

const {
  list: virtualTeams,
  containerProps: teamsContainerProps,
  wrapperProps: teamsWrapperProps,
} = useVirtualList(filteredTeams, {
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
      <div class="space-y-3">
        <div class="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
          <UBadge
            variant="subtle"
            size="sm"
            class="w-fit"
          >
            <template v-if="teamSearchQuery">
              {{ filteredTeams.length }} shown · {{ teams.length }} total
            </template>
            <template v-else>
              {{ teams.length }} total
            </template>
          </UBadge>
          <UInput
            v-model="teamSearchQuery"
            icon="i-lucide-search"
            placeholder="Search teams or members..."
            size="sm"
            class="w-full sm:max-w-xs"
          />
        </div>

        <div class="rounded-xl border border-(--ui-border) bg-(--ui-bg)">
          <div
            v-if="isLoadingTeams"
            class="px-4 py-4 text-sm text-(--ui-text-muted)"
          >
            Loading teams...
          </div>

          <div
            v-else-if="!teams.length"
            class="px-4 py-4 text-sm text-(--ui-text-muted)"
          >
            No teams yet.
          </div>

          <div
            v-else-if="!filteredTeams.length"
            class="px-4 py-4 text-sm text-(--ui-text-muted)"
          >
            No teams matching "{{ teamSearchQuery }}".
          </div>

          <div
            v-else
            v-bind="teamsContainerProps"
            class="max-h-[40rem] overflow-y-auto px-4"
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
                    class="mt-2 ml-1.5 max-h-96 space-y-3 overflow-y-auto rounded-lg border border-(--ui-border) bg-(--ui-bg-elevated) p-3"
                  >
                    <UFormField label="Challenge">
                      <UInput
                        :model-value="team.challengeId ? (challengeTitleById.get(team.challengeId) ?? 'Unknown challenge') : 'Not Selected Yet'"
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
        </div>
      </div>
    </template>
  </UDashboardPanel>
</template>
