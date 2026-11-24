<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListParticipantItem,
  GeeksHackingPortalApiEndpointsOrganizersHackathonTeamsListTeamItem,
} from '@geekshacking/portal-sdk'
import type { GTabItem } from '~/components/g/Tabs.vue'
import {
  useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonSubmissionsListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonTeamsListEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { formatOrganizerDateTime, reviewStateBadge, reviewStateOf } from '~/utils/participant-review'

/**
 * Hackathon teams: search and filter by whether they've picked a challenge; pick a team to see
 * its members (with their review status), challenge and submission in the side panel.
 */
type Team = GeeksHackingPortalApiEndpointsOrganizersHackathonTeamsListTeamItem
type Participant = GeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListParticipantItem
type FilterStatus = 'all' | 'picked' | 'unpicked' | 'submitted'

const props = withDefaults(defineProps<{
  hackathonId?: string
}>(), {
  hackathonId: '',
})
const route = useRoute()
const hackathonId = computed(() => props.hackathonId || (route.params.hackathonId as string | undefined) || '')
const path = computed(() => ({ hackathonId: hackathonId.value }))

const { data: teamsData, isLoading, error: loadError } = useGeeksHackingPortalApiEndpointsOrganizersHackathonTeamsListEndpoint({ path }, { query: { enabled: computed(() => !!hackathonId.value) } })
const { data: participantsData } = useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListEndpoint({ path })
const { data: challengesData } = useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesListEndpoint({ path })
const { data: submissionsData } = useGeeksHackingPortalApiEndpointsOrganizersHackathonSubmissionsListEndpoint({ path })

const teams = computed<Team[]>(() => teamsData.value?.teams ?? [])
const challengeTitleById = computed(() => new Map((challengesData.value?.challenges ?? [])
  .filter(c => c.id)
  .map(c => [c.id as string, c.title || 'Untitled challenge'])))
const membersByTeamId = computed(() => {
  const map = new Map<string, Participant[]>()
  for (const participant of participantsData.value?.participants ?? []) {
    if (!participant.teamId)
      continue
    const list = map.get(participant.teamId) ?? []
    list.push(participant)
    map.set(participant.teamId, list)
  }
  return map
})
const submissionByTeamId = computed(() => new Map((submissionsData.value?.submissions ?? [])
  .filter(s => s.teamId)
  .map(s => [s.teamId as string, s])))

function challengeOf(team: Team) {
  return team.challengeId ? (challengeTitleById.value.get(team.challengeId) ?? 'Unknown challenge') : ''
}
function memberLabel(count: number | undefined) {
  const n = count ?? 0
  return n === 1 ? '1 member' : `${n} members`
}

// ---- filters ----
const searchQuery = ref('')
const activeFilter = ref<FilterStatus>('all')
const counts = computed(() => ({
  all: teams.value.length,
  picked: teams.value.filter(t => !!t.challengeId).length,
  unpicked: teams.value.filter(t => !t.challengeId).length,
  submitted: teams.value.filter(t => submissionByTeamId.value.has(t.id ?? '')).length,
}))
const filterTabs = computed<GTabItem[]>(() => [
  { value: 'all', label: `All · ${counts.value.all}` },
  { value: 'unpicked', label: `No challenge · ${counts.value.unpicked}` },
  { value: 'picked', label: `Picked a challenge · ${counts.value.picked}` },
  { value: 'submitted', label: `Submitted · ${counts.value.submitted}` },
])
const filterModel = computed({
  get: () => activeFilter.value,
  set: (value: string) => { activeFilter.value = value as FilterStatus },
})

const visibleTeams = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()
  return teams.value
    .filter((team) => {
      if (activeFilter.value === 'picked')
        return !!team.challengeId
      if (activeFilter.value === 'unpicked')
        return !team.challengeId
      if (activeFilter.value === 'submitted')
        return submissionByTeamId.value.has(team.id ?? '')
      return true
    })
    .filter((team) => {
      if (!query)
        return true
      const members = (membersByTeamId.value.get(team.id ?? '') ?? []).map(m => m.name ?? '')
      return [team.name, team.description, challengeOf(team), ...members].join(' ').toLowerCase().includes(query)
    })
    .sort((a, b) => (a.name ?? '').localeCompare(b.name ?? '', undefined, { sensitivity: 'base' }))
})

// ---- selection ----
const selectedId = ref<string | null>(null)
const detailOpen = computed({
  get: () => !!selectedId.value,
  set: (value) => {
    if (!value)
      selectedId.value = null
  },
})
const selected = computed(() => teams.value.find(t => t.id === selectedId.value) ?? null)
const selectedMembers = computed(() => [...(membersByTeamId.value.get(selectedId.value ?? '') ?? [])]
  .sort((a, b) => (a.name ?? '').localeCompare(b.name ?? '')))
const selectedSubmission = computed(() => submissionByTeamId.value.get(selectedId.value ?? ''))

// ?team=<id> (e.g. from a submission) opens that team once the list loads
watch([teams, () => route.query.team], ([items, wanted]) => {
  if (typeof wanted === 'string' && wanted && !selectedId.value && items.some(t => t.id === wanted)) {
    searchQuery.value = ''
    activeFilter.value = 'all'
    selectedId.value = wanted
  }
}, { immediate: true })
</script>

<template>
  <GPanel id="teams">
    <template #header>
      <GPanelHeader title="Teams" />
    </template>

    <template #body>
      <div class="tm">
        <GAlert
          v-if="loadError && !teams.length"
          tone="err"
          title="Couldn't load teams"
          :description="getApiErrorMessage(loadError, 'Refresh the page to try again.')"
        />

        <div class="tm-toolbar">
          <GTabs
            v-model="filterModel"
            :items="filterTabs"
            :content="false"
            size="sm"
            aria-label="Show teams"
            class="tm-toolbar__filters"
          />
          <GInput
            v-model="searchQuery"
            type="search"
            icon="material-symbols:search-rounded"
            placeholder="Search teams, members, challenges"
            aria-label="Search teams"
            class="tm-toolbar__search"
          />
        </div>

        <OrganizersListDetail
          v-model:open="detailOpen"
          :label="`Team: ${selected?.name || 'team'}`"
        >
          <template #list>
            <div
              v-if="isLoading"
              class="tm-box tm-box--loading"
              aria-busy="true"
            >
              <GSkeleton
                v-for="i in 5"
                :key="i"
                class="tm-skeleton"
              />
            </div>
            <GEmptyState
              v-else-if="!teams.length && !loadError"
              icon="material-symbols:groups-rounded"
              title="No teams yet"
              description="Participants form teams on their event page. They show up here."
            />
            <GEmptyState
              v-else-if="!visibleTeams.length && teams.length"
              icon="material-symbols:search-off-rounded"
              title="No teams match"
              description="Try another search or filter."
            >
              <template #actions>
                <GButton
                  variant="secondary"
                  label="Show all teams"
                  @click="searchQuery = ''; activeFilter = 'all'"
                />
              </template>
            </GEmptyState>
            <ul
              v-else-if="visibleTeams.length"
              class="tm-box tm-list"
              :aria-label="`${visibleTeams.length} teams`"
            >
              <li
                v-for="team in visibleTeams"
                :key="team.id"
              >
                <button
                  type="button"
                  class="tm-row"
                  :class="{ 'is-selected': team.id === selectedId }"
                  :aria-current="team.id === selectedId ? 'true' : undefined"
                  @click="selectedId = team.id ?? null"
                >
                  <span class="tm-row__main">
                    <span class="tm-row__name">{{ team.name || 'Unnamed team' }}</span>
                    <span class="tm-row__meta">
                      {{ memberLabel(team.memberCount) }} · {{ challengeOf(team) || 'No challenge yet' }}
                    </span>
                  </span>
                  <GBadge
                    v-if="submissionByTeamId.has(team.id ?? '')"
                    tone="ok"
                    size="sm"
                    icon="material-symbols:check-circle-rounded"
                    label="Submitted"
                  />
                </button>
              </li>
            </ul>
          </template>

          <template #detail="{ close }">
            <OrganizersDetailPane
              v-if="selected"
              :title="selected.name || 'Unnamed team'"
              close-label="Close team"
              @close="close"
            >
              <template #meta>
                <GBadge :label="memberLabel(selected.memberCount)" />
                <GBadge
                  v-if="selectedSubmission"
                  tone="ok"
                  icon="material-symbols:check-circle-rounded"
                  label="Submitted"
                />
              </template>

              <p
                v-if="selected.description"
                class="tm-desc"
              >
                {{ selected.description }}
              </p>

              <dl class="tm-facts">
                <div>
                  <dt>Challenge</dt>
                  <dd>{{ challengeOf(selected) || 'Not picked yet' }}</dd>
                </div>
                <div>
                  <dt>Formed</dt>
                  <dd>{{ formatOrganizerDateTime(selected.createdAt) }}</dd>
                </div>
              </dl>

              <section
                class="tm-section"
                aria-labelledby="tm-members"
              >
                <h3
                  id="tm-members"
                  class="tm-section__title"
                >
                  Members
                </h3>
                <ul
                  v-if="selectedMembers.length"
                  class="tm-members"
                >
                  <li
                    v-for="member in selectedMembers"
                    :key="member.id"
                  >
                    <NuxtLink
                      :to="{ path: `/dash/${hackathonId}/participants`, query: { participant: member.id } }"
                      class="tm-link"
                    >
                      {{ member.name || 'Unnamed participant' }}
                    </NuxtLink>
                    <GBadge
                      :tone="reviewStateBadge[reviewStateOf(member)].tone"
                      :variant="reviewStateOf(member) === 'pending' ? 'solid' : 'soft'"
                      size="sm"
                      :label="reviewStateBadge[reviewStateOf(member)].label"
                    />
                  </li>
                </ul>
                <p
                  v-else
                  class="tm-muted"
                >
                  No members found.
                </p>
              </section>

              <section
                class="tm-section"
                aria-labelledby="tm-submission"
              >
                <h3
                  id="tm-submission"
                  class="tm-section__title"
                >
                  Submission
                </h3>
                <div
                  v-if="selectedSubmission"
                  class="tm-submission"
                >
                  <NuxtLink
                    :to="{ path: `/dash/${hackathonId}/submissions`, query: { submission: selectedSubmission.id } }"
                    class="tm-link tm-submission__title"
                  >
                    {{ selectedSubmission.title || 'Untitled project' }}
                  </NuxtLink>
                  <span class="tm-muted">Submitted {{ formatOrganizerDateTime(selectedSubmission.submittedAt) }}</span>
                </div>
                <p
                  v-else
                  class="tm-muted"
                >
                  Not submitted yet.
                </p>
              </section>
            </OrganizersDetailPane>
          </template>

          <template #placeholder>
            <GEmptyState
              icon="material-symbols:groups-rounded"
              title="Pick a team"
              description="Its members, challenge and submission show here."
              :bordered="false"
            />
          </template>
        </OrganizersListDetail>
      </div>
    </template>
  </GPanel>
</template>

<style scoped>
.tm {
  display: flex;
  flex-direction: column;
  gap: 16px;
  width: 100%;
  max-width: 88rem;
  margin: 0 auto;
}
.tm-muted {
  margin: 0;
  color: var(--gh-text-muted);
}
.tm-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px 12px;
}
.tm-toolbar__filters { max-width: 100%; overflow-x: auto; }
.tm-toolbar__search { flex: 1 1 18rem; }
.tm-box {
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.tm-box--loading {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 12px;
}
.tm-skeleton {
  display: block;
  height: 48px;
}
.tm-list {
  margin: 0;
  padding: 0;
  list-style: none;
  overflow: hidden;
}
@media (min-width: 1024px) {
  .tm-list {
    max-height: calc(100dvh - 8.5rem);
    overflow-y: auto;
  }
}
.tm-list > li { border-top: var(--gh-border-width) solid var(--gh-line-soft); }
.tm-list > li:first-child { border-top: 0; }
.tm-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  width: 100%;
  min-height: 68px;
  padding: 10px 16px;
  border: 0;
  border-left: 4px solid transparent;
  background: transparent;
  color: inherit;
  font: inherit;
  text-align: left;
  cursor: pointer;
  transition: background-color var(--gh-dur-fast) var(--ease-gh);
}
.tm-row:hover { background: var(--gh-ghost-hover); }
.tm-row:focus-visible {
  outline: 2px solid var(--gh-focus);
  outline-offset: -2px;
}
.tm-row.is-selected {
  border-left-color: var(--gh-highlight-edge);
  background: var(--gh-highlight);
  color: var(--gh-highlight-ink);
}
.tm-row.is-selected .tm-row__meta { color: inherit; }
.tm-row__main {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}
.tm-row__name { font-weight: 700; overflow-wrap: anywhere; }
.tm-row__meta {
  font-size: 14px;
  color: var(--gh-text-muted);
  overflow-wrap: anywhere;
}
@media (prefers-reduced-motion: reduce) {
  .tm-row { transition: none; }
}

.tm-desc {
  margin: 0;
  white-space: pre-line;
  overflow-wrap: anywhere;
}
.tm-facts {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr));
  gap: 12px 24px;
  margin: 0;
}
.tm-facts dt {
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.tm-facts dd {
  margin: 2px 0 0;
  overflow-wrap: anywhere;
}
.tm-section {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.tm-section__title {
  margin: 0;
  font-size: 16px;
  font-weight: 700;
}
.tm-members {
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0;
  list-style: none;
}
.tm-members li {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  padding: 10px 0;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.tm-members li:first-child {
  padding-top: 0;
  border-top: 0;
}
.tm-link {
  color: inherit;
  font-weight: 600;
  text-decoration: underline;
  text-underline-offset: 3px;
}
.tm-link:focus-visible {
  outline: 2px solid var(--gh-focus);
  outline-offset: 2px;
  border-radius: 2px;
}
.tm-submission {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.tm-submission__title { font-size: 17px; }
</style>
