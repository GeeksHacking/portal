<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsParticipantBreakdownItem,
  GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsResponse,
  GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsTeamBreakdownItem,
} from '@geekshacking/portal-sdk'
import type { GMeterRow } from '~/components/g/MeterList.vue'
import {
  useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesListEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesListEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsEndpoint2,
} from '@geekshacking/portal-sdk/hooks'

/**
 * Resource redemptions (swag, meals...) for one event, all resources or one at a time.
 * Shared by hackathons and workshops (the API's "1" endpoints are hackathons, "2" workshops).
 * Hackathons get a per-team list that flags teams where too few members have redeemed;
 * workshops have no teams, so they get a list of people instead. `teams` is the hackathon's
 * team list, so teams with no redemptions still appear.
 */
type Stats = GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsResponse
type TeamRow = GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsTeamBreakdownItem
type PersonRow = GeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsParticipantBreakdownItem

const props = defineProps<{
  kind: 'hackathon' | 'workshop'
  eventId: string
  teams?: { id?: string, name?: string, memberCount?: number }[]
}>()

const isHackathon = props.kind === 'hackathon'
const path = computed(() => ({ activityId: props.eventId }))
const enabled = { query: { enabled: computed(() => !!props.eventId) } }

const ALL = 'all'
const scope = ref(ALL)
const query = computed(() => (scope.value === ALL ? {} : { resourceId: scope.value }))

const resourcesQuery = isHackathon
  ? useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesListEndpoint1({ path }, enabled)
  : useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesListEndpoint2({ path }, enabled)
const statsQuery = isHackathon
  ? useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsEndpoint1({ path, query }, enabled)
  : useGeeksHackingPortalApiEndpointsOrganizersHackathonResourcesStatisticsEndpoint2({ path, query }, enabled)

const resources = computed(() => resourcesQuery.data.value?.resources ?? [])
const stats = computed<Stats | null>(() => statsQuery.data.value ?? null)
const selected = computed(() => resources.value.find(r => r.id === scope.value) ?? null)
watch(resources, (list) => {
  if (scope.value !== ALL && !list.some(r => r.id === scope.value))
    scope.value = ALL
})
const scopeItems = computed(() => [
  { value: ALL, label: 'All resources' },
  ...resources.value.map(r => ({ value: r.id ?? '', label: r.isPublished ? (r.name ?? 'Untitled') : `${r.name ?? 'Untitled'} (not published)` })),
])
const resourcesPage = computed(() => (isHackathon ? `/dash/${props.eventId}/resources` : `/dash/standalone/${props.eventId}/resources`))

const when = (value: string | null | undefined) => (value ? formatOrganizerDateTime(value) : '–')
const plural = (n: number, one: string, many = `${one}s`) => `${n.toLocaleString()} ${n === 1 ? one : many}`

// ---- headline figures ----
const reachRows = computed<GMeterRow[]>(() => stats.value
  ? [{
      key: 'reach',
      label: 'Redeemed at least once',
      value: stats.value.participantsWithRedemptions ?? 0,
      total: stats.value.totalParticipants ?? 0,
      hint: `${plural(stats.value.participantsWithoutRedemptions ?? 0, 'person', 'people')} haven't redeemed${selected.value ? ` ${selected.value.name}` : ' anything'} yet.`,
    }]
  : [])

const facts = computed(() => {
  const s = stats.value
  if (!s)
    return []
  return [
    { label: 'Redemptions', value: (s.totalRedemptions ?? 0).toLocaleString() },
    ...(isHackathon ? [{ label: 'Teams with a redemption', value: (s.teamsWithRedemptions ?? 0).toLocaleString() }] : []),
    { label: 'Per person who redeemed', value: (s.averageRedemptionsPerRedeemer ?? 0).toFixed(1) },
    { label: 'First', value: when(s.firstRedeemedAt) },
    { label: 'Latest', value: when(s.lastRedeemedAt) },
  ]
})

// ---- by resource ----
const showByResource = computed(() => !selected.value && (stats.value?.resourceCount ?? 0) > 1)
const byResource = computed<GMeterRow[]>(() => [...(stats.value?.resourceSummaries ?? [])]
  .sort((a, b) => (b.uniqueRedeemers ?? 0) - (a.uniqueRedeemers ?? 0) || (a.resourceName ?? '').localeCompare(b.resourceName ?? ''))
  .map(r => ({
    key: r.resourceId ?? r.resourceName ?? '',
    label: `${r.resourceName ?? 'Untitled'}${r.isPublished === false ? ' (not published)' : ''}`,
    value: r.uniqueRedeemers ?? 0,
    total: stats.value?.totalParticipants ?? 0,
    hint: (r.totalRedemptions ?? 0) ? `${plural(r.totalRedemptions ?? 0, 'redemption')} · latest ${when(r.lastRedeemedAt)}` : 'Not redeemed yet',
  })))

// ---- recent ----
const recent = computed(() => stats.value?.recentActivity ?? [])
const showAllRecent = ref(false)
const recentShown = computed(() => (showAllRecent.value ? recent.value : recent.value.slice(0, 6)))

// ---- teams (hackathon) ----
const threshold = ref<number | null>(3)
const minRedeemers = computed(() => Math.max(1, Math.trunc(Number(threshold.value) || 1)))
const search = ref('')

const teamRows = computed<TeamRow[]>(() => {
  const breakdown = stats.value?.teamBreakdown ?? []
  const byId = new Map(breakdown.filter(t => t.teamId).map(t => [t.teamId!, t]))
  const rows: TeamRow[] = (props.teams ?? []).map((team) => {
    const found = team.id ? byId.get(team.id) : undefined
    return {
      teamId: team.id ?? null,
      teamName: team.name ?? 'Unnamed team',
      memberCount: team.memberCount ?? found?.memberCount ?? 0,
      redeemerCount: found?.redeemerCount ?? 0,
      totalRedemptions: found?.totalRedemptions ?? 0,
      distinctResourcesRedeemed: found?.distinctResourcesRedeemed ?? 0,
      lastRedeemedAt: found?.lastRedeemedAt ?? null,
      participants: found?.participants ?? [],
    }
  })
  const noTeam = breakdown.find(t => !t.teamId)
  if (noTeam)
    rows.push({ ...noTeam, teamName: 'Not in a team' })
  // flagged teams first, people not in a team last
  return rows.sort((a, b) => Number(!a.teamId) - Number(!b.teamId)
    || Number(isFlagged(b)) - Number(isFlagged(a))
    || (b.totalRedemptions ?? 0) - (a.totalRedemptions ?? 0)
    || (a.teamName ?? '').localeCompare(b.teamName ?? ''))
})

function isFlagged(team: TeamRow) {
  return !!team.teamId && (team.memberCount ?? 0) > 0 && (team.redeemerCount ?? 0) < Math.min(minRedeemers.value, team.memberCount ?? 0)
}
const missing = (team: TeamRow) => Math.max(0, (team.memberCount ?? 0) - (team.redeemerCount ?? 0))
const flaggedCount = computed(() => teamRows.value.filter(isFlagged).length)

const visibleTeams = computed(() => {
  const q = search.value.trim().toLowerCase()
  if (!q)
    return teamRows.value
  return teamRows.value.flatMap((team) => {
    if ((team.teamName ?? '').toLowerCase().includes(q))
      return [team]
    const people = (team.participants ?? []).filter(p => (p.userName ?? '').toLowerCase().includes(q))
    return people.length ? [{ ...team, participants: people }] : []
  })
})
const openTeams = ref(new Set<string>())
function toggleTeam(key: string) {
  const next = new Set(openTeams.value)
  if (next.has(key))
    next.delete(key)
  else
    next.add(key)
  openTeams.value = next
}

// ---- people (workshop) ----
const people = computed<PersonRow[]>(() => (stats.value?.teamBreakdown ?? [])
  .flatMap(t => t.participants ?? [])
  .filter(p => !search.value.trim() || (p.userName ?? '').toLowerCase().includes(search.value.trim().toLowerCase()))
  .sort((a, b) => (b.lastRedeemedAt ?? '').localeCompare(a.lastRedeemedAt ?? '')))

const multiResource = computed(() => (stats.value?.resourceCount ?? 0) > 1)
function resourcesOf(person: PersonRow) {
  const names = [...new Set((person.redemptions ?? []).map(r => r.resourceName ?? ''))].filter(Boolean)
  return names.length <= 2 ? names.join(', ') : `${names.slice(0, 2).join(', ')} +${names.length - 2} more`
}

// ---- downloads ----
const exportError = ref('')
const isExporting = ref(false)
function fileName(ext: 'xlsx' | 'csv') {
  const slug = (selected.value?.name || 'all-resources').toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '') || 'resources'
  return `resource-redemptions-${slug}-${new Date().toISOString().slice(0, 10)}.${ext}`
}
function personRows() {
  return (stats.value?.teamBreakdown ?? []).flatMap(team => (team.participants ?? []).map(p => ({
    ...(isHackathon ? { Team: team.teamId ? (team.teamName ?? '') : 'Not in a team' } : {}),
    'Name': p.userName ?? '',
    'Redemptions': p.redemptionCount ?? 0,
    'Resources': multiResource.value ? resourcesOf(p) : (selected.value?.name ?? stats.value?.resourceName ?? ''),
    'First': when(p.firstRedeemedAt),
    'Latest': when(p.lastRedeemedAt),
    'Timeline': (p.redemptions ?? []).map(r => `${multiResource.value ? `${r.resourceName}: ` : ''}${when(r.timestamp)}`).join(' | '),
  })))
}
async function download(format: 'xlsx' | 'csv') {
  if (!stats.value || isExporting.value)
    return
  exportError.value = ''
  isExporting.value = true
  try {
    const XLSX = await import('xlsx')
    const s = stats.value
    const people = personRows()
    if (format === 'csv') {
      const csv = XLSX.utils.sheet_to_csv(XLSX.utils.json_to_sheet(people))
      const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }))
      const link = document.createElement('a')
      link.href = url
      link.download = fileName('csv')
      document.body.appendChild(link)
      link.click()
      link.remove()
      URL.revokeObjectURL(url)
      return
    }
    const book = XLSX.utils.book_new()
    const sheet = (name: string, rows: Record<string, unknown>[]) => XLSX.utils.book_append_sheet(book, XLSX.utils.json_to_sheet(rows), name)
    sheet('Summary', [
      { Figure: 'Scope', Value: selected.value?.name || 'All resources' },
      { Figure: 'Resources', Value: s.resourceCount ?? 0 },
      { Figure: 'Resources redeemed', Value: s.resourcesWithRedemptions ?? 0 },
      { Figure: 'Participants', Value: s.totalParticipants ?? 0 },
      { Figure: 'Redeemed at least once', Value: s.participantsWithRedemptions ?? 0 },
      { Figure: 'Not redeemed yet', Value: s.participantsWithoutRedemptions ?? 0 },
      ...(isHackathon ? [{ Figure: 'Teams with a redemption', Value: s.teamsWithRedemptions ?? 0 }, { Figure: 'Redeemers not in a team', Value: s.redeemersWithoutTeam ?? 0 }] : []),
      { Figure: 'Redemptions', Value: s.totalRedemptions ?? 0 },
      { Figure: 'Per person who redeemed', Value: Number((s.averageRedemptionsPerRedeemer ?? 0).toFixed(2)) },
      { Figure: 'First', Value: when(s.firstRedeemedAt) },
      { Figure: 'Latest', Value: when(s.lastRedeemedAt) },
      ...(isHackathon ? [{ Figure: 'Flag teams with fewer redeemers than', Value: minRedeemers.value }, { Figure: 'Flagged teams', Value: flaggedCount.value }] : []),
    ])
    sheet('Resources', (s.resourceSummaries ?? []).map(r => ({
      'Resource': r.resourceName ?? '',
      'Published': r.isPublished ? 'Yes' : 'No',
      'Redemptions': r.totalRedemptions ?? 0,
      'People': r.uniqueRedeemers ?? 0,
      'Latest': when(r.lastRedeemedAt),
    })))
    if (isHackathon) {
      sheet('Teams', teamRows.value.map(t => ({
        'Team': t.teamName ?? '',
        'Members': t.memberCount ?? 0,
        'Members who redeemed': t.redeemerCount ?? 0,
        'Members who haven\'t': t.teamId ? missing(t) : '',
        'Redemptions': t.totalRedemptions ?? 0,
        'Resources redeemed': t.distinctResourcesRedeemed ?? 0,
        'Flagged': isFlagged(t) ? 'Yes' : 'No',
        'Latest': when(t.lastRedeemedAt),
      })))
    }
    sheet('People', people)
    sheet('Recent', recent.value.map(r => ({
      ...(isHackathon ? { Team: r.teamName ?? '' } : {}),
      Name: r.userName ?? '',
      Resource: r.resourceName ?? '',
      When: when(r.timestamp),
    })))
    XLSX.writeFile(book, fileName('xlsx'))
  }
  catch {
    exportError.value = 'The file couldn\'t be made. Try again.'
  }
  finally {
    isExporting.value = false
  }
}
</script>

<template>
  <OrganizersStatsCard
    id="st-resources"
    title="Resources"
    :description="selected?.description || 'Who has redeemed swag, meals and other resources.'"
  >
    <template #actions>
      <GFormField
        v-if="resources.length"
        label="Show"
        class="rs-scope"
      >
        <GSelect
          v-model="scope"
          :items="scopeItems"
          size="sm"
        />
      </GFormField>
      <GButton
        v-if="stats && resources.length"
        variant="secondary"
        size="sm"
        icon="material-symbols:table-view-rounded"
        label="Excel"
        aria-label="Download resource redemptions as Excel"
        :loading="isExporting"
        loading-label="Making file"
        @click="download('xlsx')"
      />
      <GButton
        v-if="stats && resources.length"
        variant="ghost"
        size="sm"
        icon="material-symbols:description-rounded"
        label="CSV"
        aria-label="Download people who redeemed as CSV"
        :disabled="isExporting"
        @click="download('csv')"
      />
    </template>

    <GAlert
      v-if="exportError"
      tone="err"
      title="Download failed"
      :description="exportError"
      dismissible
      @close="exportError = ''"
    />
    <GAlert
      v-if="statsQuery.error.value && !stats"
      tone="err"
      title="Couldn't load redemptions"
      :description="getApiErrorMessage(statsQuery.error.value, 'Refresh to try again.')"
    />

    <GSkeleton
      v-if="resourcesQuery.isLoading.value || (statsQuery.isLoading.value && !stats && resources.length)"
      class="rs-skeleton"
    />

    <GEmptyState
      v-else-if="!resources.length"
      size="sm"
      bordered
      icon="material-symbols:redeem-rounded"
      title="No resources yet"
      description="Add resources such as swag or meals, then track who has redeemed them here."
    >
      <template #actions>
        <GButton
          variant="secondary"
          label="Go to Resources"
          :to="resourcesPage"
        />
      </template>
    </GEmptyState>

    <template v-else-if="stats">
      <div
        class="rs-top"
        :class="{ 'is-refreshing': statsQuery.isFetching.value }"
      >
        <GMeterList
          :rows="reachRows"
          label="People who redeemed"
        />
        <dl class="rs-facts">
          <div
            v-for="fact in facts"
            :key="fact.label"
          >
            <dt>{{ fact.label }}</dt>
            <dd>{{ fact.value }}</dd>
          </div>
        </dl>
      </div>

      <div
        class="rs-split"
        :class="{ 'rs-split--one': !showByResource }"
      >
        <div
          v-if="showByResource"
          class="rs-block"
        >
          <h3 class="rs-block__title">
            By resource
          </h3>
          <p class="rs-muted">
            People who redeemed each one, out of {{ plural(stats.totalParticipants ?? 0, 'participant') }}.
          </p>
          <GMeterList
            :rows="byResource"
            label="People who redeemed, by resource"
          />
        </div>

        <div class="rs-block">
          <h3 class="rs-block__title">
            Recent
          </h3>
          <p
            v-if="!recent.length"
            class="rs-muted"
          >
            Nothing redeemed yet.
          </p>
          <ol
            v-else
            class="rs-recent"
          >
            <li
              v-for="item in recentShown"
              :key="item.redemptionId"
            >
              <p class="rs-recent__what">
                <strong>{{ item.userName || 'Someone' }}</strong> redeemed <strong>{{ item.resourceName }}</strong>
              </p>
              <p class="rs-muted rs-small">
                {{ when(item.timestamp) }}<template v-if="isHackathon && item.teamName">
                  · {{ item.teamName }}
                </template>
              </p>
            </li>
          </ol>
          <GButton
            v-if="recent.length > 6"
            variant="ghost"
            size="sm"
            :label="showAllRecent ? 'Show fewer' : `Show all ${recent.length}`"
            class="rs-fit"
            @click="showAllRecent = !showAllRecent"
          />
        </div>
      </div>

      <!-- per team (hackathons) -->
      <div
        v-if="isHackathon"
        class="rs-block"
      >
        <h3 class="rs-block__title">
          By team
        </h3>
        <div class="rs-controls">
          <GFormField
            label="Flag teams where fewer members than this have redeemed"
            class="rs-threshold"
          >
            <GInput
              v-model.number="threshold"
              type="number"
              min="1"
              step="1"
              inputmode="numeric"
            />
          </GFormField>
          <GFormField
            label="Find a team or person"
            hide-label
            class="rs-search"
          >
            <GInput
              v-model="search"
              type="search"
              icon="material-symbols:search-rounded"
              placeholder="Find a team or person"
            />
          </GFormField>
        </div>
        <GAlert
          :tone="flaggedCount ? 'attention' : 'neutral'"
          :title="flaggedCount ? `${plural(flaggedCount, 'team')} below ${minRedeemers}` : `Every team has at least ${minRedeemers} ${minRedeemers === 1 ? 'member' : 'members'} who redeemed`"
          :description="flaggedCount ? 'Listed first. Smaller teams are flagged only if not every member has redeemed.' : undefined"
        />
        <p
          v-if="!visibleTeams.length"
          class="rs-muted"
        >
          {{ teamRows.length ? `No team or person matches “${search}”.` : 'No teams yet.' }}
        </p>
        <ul
          v-else
          class="rs-teams"
        >
          <li
            v-for="team in visibleTeams"
            :key="team.teamId ?? 'none'"
            class="rs-team"
            :class="{ 'is-flagged': isFlagged(team) }"
          >
            <div class="rs-team__main">
              <div class="rs-team__who">
                <p class="rs-team__name">
                  {{ team.teamName }}
                  <GBadge
                    v-if="isFlagged(team)"
                    tone="pending"
                    variant="solid"
                    size="sm"
                    icon="material-symbols:flag-rounded"
                    :label="`${missing(team)} haven't redeemed`"
                  />
                </p>
                <p class="rs-muted rs-small">
                  <template v-if="team.teamId">
                    {{ (team.redeemerCount ?? 0).toLocaleString() }} of {{ plural(team.memberCount ?? 0, 'member') }} redeemed
                  </template>
                  <template v-else>
                    {{ plural(team.redeemerCount ?? 0, 'person', 'people') }} redeemed
                  </template>
                  · {{ plural(team.totalRedemptions ?? 0, 'redemption') }}<template v-if="multiResource && team.distinctResourcesRedeemed">
                    · {{ plural(team.distinctResourcesRedeemed, 'resource') }}
                  </template><template v-if="team.lastRedeemedAt">
                    · latest {{ when(team.lastRedeemedAt) }}
                  </template>
                </p>
              </div>
              <GButton
                v-if="team.participants?.length"
                variant="ghost"
                size="sm"
                :icon="openTeams.has(team.teamId ?? 'none') ? 'material-symbols:expand-less-rounded' : 'material-symbols:expand-more-rounded'"
                :label="openTeams.has(team.teamId ?? 'none') ? 'Hide people' : `Show people (${team.participants.length})`"
                :aria-expanded="openTeams.has(team.teamId ?? 'none')"
                @click="toggleTeam(team.teamId ?? 'none')"
              />
            </div>
            <OrganizersResourcePeople
              v-if="openTeams.has(team.teamId ?? 'none')"
              :people="team.participants ?? []"
              :multi-resource="multiResource"
            />
          </li>
        </ul>
      </div>

      <!-- people (workshops) -->
      <div
        v-else
        class="rs-block"
      >
        <h3 class="rs-block__title">
          People who redeemed
        </h3>
        <GFormField
          v-if="people.length || search"
          label="Find a person"
          hide-label
          class="rs-search"
        >
          <GInput
            v-model="search"
            type="search"
            icon="material-symbols:search-rounded"
            placeholder="Find a person"
          />
        </GFormField>
        <p
          v-if="!people.length"
          class="rs-muted"
        >
          {{ search ? `No one matches “${search}”.` : 'No one has redeemed yet.' }}
        </p>
        <OrganizersResourcePeople
          v-else
          :people="people"
          :multi-resource="multiResource"
        />
      </div>
    </template>
  </OrganizersStatsCard>
</template>

<style scoped>
.rs-scope { min-width: 14rem; }
.rs-skeleton {
  display: block;
  height: 160px;
}
.rs-muted {
  margin: 0;
  color: var(--gh-text-muted);
}
.rs-small { font-size: 13px; }
.rs-fit { align-self: flex-start; }
.rs-top {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1.2fr);
  gap: 16px 32px;
  align-items: start;
  transition: opacity var(--gh-dur-fast) var(--ease-gh);
}
.rs-top.is-refreshing { opacity: 0.6; }
.rs-facts {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(8.5rem, 1fr));
  gap: 12px 20px;
  margin: 0;
}
.rs-facts dt {
  font-size: 13px;
  color: var(--gh-text-muted);
}
.rs-facts dd {
  margin: 2px 0 0;
  font-weight: 700;
}
.rs-split {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  gap: 24px 32px;
}
.rs-split--one { grid-template-columns: minmax(0, 1fr); }
.rs-block {
  display: flex;
  flex-direction: column;
  gap: 10px;
  min-width: 0;
}
.rs-block__title {
  margin: 0;
  font-size: 15px;
  font-weight: 700;
}
.rs-recent {
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0;
  list-style: none;
}
.rs-recent li {
  padding: 8px 0;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.rs-recent li:first-child { border-top: 0; padding-top: 0; }
.rs-recent__what {
  margin: 0;
  font-size: 14px;
}
.rs-controls {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  gap: 12px 16px;
}
.rs-threshold { width: 20rem; max-width: 100%; }
.rs-threshold :deep(input) { max-width: 7rem; }
.rs-search { flex: 1 1 14rem; max-width: 24rem; }
.rs-teams {
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0;
  list-style: none;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
}
.rs-team {
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 12px 16px;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.rs-team:first-child { border-top: 0; }
.rs-team.is-flagged { box-shadow: inset 4px 0 0 var(--gh-accent); }
.rs-team__main {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 8px 12px;
}
.rs-team__who { min-width: 0; flex: 1 1 16rem; }
.rs-team__name {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px 10px;
  margin: 0 0 2px;
  font-weight: 700;
}
@media (max-width: 56rem) {
  .rs-top,
  .rs-split { grid-template-columns: minmax(0, 1fr); }
}
</style>
