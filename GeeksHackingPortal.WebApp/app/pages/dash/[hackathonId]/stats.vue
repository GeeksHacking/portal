<script setup lang="ts">
import type { GBarRow } from '~/components/g/BarList.vue'
import type { GMeterRow } from '~/components/g/MeterList.vue'
import type { GSegmentPart } from '~/components/g/SegmentBar.vue'
import {
  useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonTeamsListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueOverviewEndpoint1,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'

/**
 * Hackathon stats: headline numbers, then registrations, teams, challenges, the venue and
 * resources. Charts use the brand's ink ramp; status colours only where a part means that state
 * (approved / to review / rejected), always named in a list beside the bar.
 */
const route = useRoute()
const hackathonId = computed(() => (route.params.hackathonId as string | undefined) ?? '')
const base = computed(() => `/dash/${hackathonId.value}`)
const hackathonPath = computed(() => ({ hackathonId: hackathonId.value }))
const enabled = { query: { enabled: computed(() => !!hackathonId.value) } }

const participantsQuery = useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListEndpoint({ path: hackathonPath }, enabled)
const teamsQuery = useGeeksHackingPortalApiEndpointsOrganizersHackathonTeamsListEndpoint({ path: hackathonPath }, enabled)
const challengesQuery = useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesListEndpoint({ path: hackathonPath }, enabled)
const venueQuery = useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueOverviewEndpoint1({ path: computed(() => ({ activityId: hackathonId.value })) }, enabled)

const isLoading = computed(() => participantsQuery.isLoading.value || teamsQuery.isLoading.value)
const loadError = computed(() => participantsQuery.error.value ?? teamsQuery.error.value)

// ---- refresh ----
const queryClient = useQueryClient()
const isRefreshing = ref(false)
const updatedAt = computed(() => participantsQuery.dataUpdatedAt.value)
async function refresh() {
  isRefreshing.value = true
  try {
    await queryClient.invalidateQueries()
  }
  finally {
    isRefreshing.value = false
  }
}

// ---- people ----
const everyone = computed(() => participantsQuery.data.value?.participants ?? [])
const active = computed(() => everyone.value.filter(p => !p.isWithdrawn && !p.withdrawnAt))
const withdrew = computed(() => everyone.value.length - active.value.length)
const stateCount = computed(() => {
  const counts = { incomplete: 0, pending: 0, approved: 0, rejected: 0 }
  for (const p of active.value)
    counts[reviewStateOf(p)]++
  return counts
})
const overdue = computed(() => active.value.filter(p => reviewStateOf(p) === 'pending' && isReviewOverdue(p)).length)
const reviewed = computed(() => stateCount.value.approved + stateCount.value.rejected)
const complete = computed(() => active.value.length - stateCount.value.incomplete)
const emailsSent = computed(() => everyone.value.reduce((sum, p) => sum + (p.emailSentCount ?? 0), 0))

const registrationParts = computed<GSegmentPart[]>(() => [
  { key: 'approved', label: 'Approved', value: stateCount.value.approved, tone: 'ok', icon: reviewStateBadge.approved.icon },
  { key: 'pending', label: 'To review', value: stateCount.value.pending, tone: 'pending', icon: reviewStateBadge.pending.icon, note: overdue.value ? `${overdue.value} waiting ${REVIEW_OVERDUE_DAYS}+ days` : undefined },
  { key: 'rejected', label: 'Rejected', value: stateCount.value.rejected, tone: 'err', icon: reviewStateBadge.rejected.icon },
  { key: 'incomplete', label: 'Form not finished', value: stateCount.value.incomplete, tone: 'ink-3', icon: reviewStateBadge.incomplete.icon },
  { key: 'withdrew', label: 'Withdrew', value: withdrew.value, tone: 'ink-2', icon: 'material-symbols:logout-rounded' },
])
const registrationRates = computed<GMeterRow[]>(() => [
  { key: 'complete', label: 'Finished the form', value: complete.value, total: active.value.length },
  { key: 'reviewed', label: 'Reviewed', value: reviewed.value, total: complete.value },
  { key: 'accepted', label: 'Approved, of those reviewed', value: stateCount.value.approved, total: reviewed.value },
])

// ---- teams ----
const teams = computed(() => teamsQuery.data.value?.teams ?? [])
const inTeam = computed(() => active.value.filter(p => p.teamId).length)
const approvedInTeam = computed(() => active.value.filter(p => p.teamId && reviewStateOf(p) === 'approved').length)
const teamRates = computed<GMeterRow[]>(() => [
  { key: 'in', label: 'Participants in a team', value: inTeam.value, total: active.value.length },
  { key: 'approved', label: 'Approved participants in a team', value: approvedInTeam.value, total: stateCount.value.approved, hint: stateCount.value.approved - approvedInTeam.value ? `${stateCount.value.approved - approvedInTeam.value} approved ${stateCount.value.approved - approvedInTeam.value === 1 ? 'person still needs' : 'people still need'} a team.` : undefined },
])
const teamSizes = computed<GBarRow[]>(() => {
  const counts = new Map<number, number>()
  for (const t of teams.value)
    counts.set(t.memberCount ?? 0, (counts.get(t.memberCount ?? 0) ?? 0) + 1)
  return [...counts.keys()].sort((a, b) => a - b).map(size => ({
    key: String(size),
    label: size === 1 ? '1 member' : `${size} members`,
    value: counts.get(size) ?? 0,
    muted: size === 0,
  }))
})
const averageSize = computed(() => (teams.value.length ? teams.value.reduce((s, t) => s + (t.memberCount ?? 0), 0) / teams.value.length : 0))

// ---- challenges ----
const challenges = computed(() => challengesQuery.data.value?.challenges ?? [])
const picked = computed(() => teams.value.filter(t => t.challengeId).length)
const challengeBars = computed<GBarRow[]>(() => {
  const counts = new Map<string, number>()
  for (const t of teams.value) {
    if (t.challengeId)
      counts.set(t.challengeId, (counts.get(t.challengeId) ?? 0) + 1)
  }
  const rows: GBarRow[] = challenges.value
    .map(c => ({ key: c.id ?? '', label: c.title || 'Untitled challenge', value: counts.get(c.id ?? '') ?? 0, note: c.isPublished ? undefined : '(draft)' }))
    .sort((a, b) => b.value - a.value || a.label.localeCompare(b.label))
  if (teams.value.length)
    rows.push({ key: 'none', label: 'No challenge yet', value: teams.value.length - picked.value, muted: true })
  return rows
})
const challengeRates = computed<GMeterRow[]>(() => [
  { key: 'picked', label: 'Teams with a challenge', value: picked.value, total: teams.value.length },
])
const draftCount = computed(() => challenges.value.filter(c => !c.isPublished).length)

// ---- venue ----
const checkIns = computed(() => new Map((venueQuery.data.value?.participants ?? []).filter(v => v.userId).map(v => [v.userId!, v])))
const hereNow = computed(() => active.value.filter(p => checkIns.value.get(p.id ?? '')?.isCurrentlyCheckedIn).length)
const everCame = computed(() => active.value.filter(p => (checkIns.value.get(p.id ?? '')?.totalCheckIns ?? 0) > 0).length)
const totalCheckIns = computed(() => active.value.reduce((s, p) => s + (checkIns.value.get(p.id ?? '')?.totalCheckIns ?? 0), 0))
const venueRates = computed<GMeterRow[]>(() => [
  { key: 'now', label: 'Here now', value: hereNow.value, total: active.value.length },
  { key: 'ever', label: 'Checked in at least once', value: everCame.value, total: active.value.length, hint: `${totalCheckIns.value.toLocaleString()} ${totalCheckIns.value === 1 ? 'check-in' : 'check-ins'} in all.` },
])
const teamPresence = computed<GSegmentPart[]>(() => {
  const byTeam = new Map<string, { members: number, here: number }>()
  for (const p of active.value) {
    if (!p.teamId)
      continue
    const entry = byTeam.get(p.teamId) ?? { members: 0, here: 0 }
    entry.members++
    if (checkIns.value.get(p.id ?? '')?.isCurrentlyCheckedIn)
      entry.here++
    byTeam.set(p.teamId, entry)
  }
  let all = 0
  let some = 0
  let none = 0
  for (const t of teams.value) {
    const entry = byTeam.get(t.id ?? '')
    if (entry && entry.members && entry.here === entry.members)
      all++
    else if (entry?.here)
      some++
    else
      none++
  }
  return [
    { key: 'all', label: 'Whole team here', value: all, tone: 'ink' },
    { key: 'some', label: 'Part of the team here', value: some, tone: 'ink-2' },
    { key: 'none', label: 'Nobody here', value: none, tone: 'ink-3' },
  ]
})
</script>

<template>
  <GPanel id="participant-stats">
    <template #header>
      <GPanelHeader title="Stats">
        <template #right>
          <span
            v-if="updatedAt"
            class="st-updated"
          >Updated {{ new Date(updatedAt).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' }) }}</span>
          <GButton
            variant="secondary"
            icon="material-symbols:refresh-rounded"
            label="Refresh"
            :loading="isRefreshing"
            loading-label="Refreshing"
            @click="refresh"
          />
        </template>
      </GPanelHeader>
    </template>

    <template #body>
      <div class="st">
        <GAlert
          v-if="loadError && !everyone.length"
          tone="err"
          title="Couldn't load stats"
          :description="getApiErrorMessage(loadError, 'Refresh to try again.')"
        />

        <!-- headline numbers -->
        <div class="st-tiles">
          <GStatTile
            label="Participants"
            :value="active.length"
            icon="material-symbols:group-rounded"
            :hint="withdrew ? `${withdrew} withdrew` : 'Signed up, not withdrawn'"
            :loading="isLoading"
            :to="`${base}/participants`"
          />
          <GStatTile
            label="To review"
            :value="stateCount.pending"
            icon="material-symbols:hourglass-top-rounded"
            :hint="overdue ? `${overdue} waiting ${REVIEW_OVERDUE_DAYS}+ days` : 'None overdue'"
            :highlight="stateCount.pending > 0"
            :loading="isLoading"
            :to="`${base}/participants`"
          />
          <GStatTile
            label="Approved"
            :value="stateCount.approved"
            icon="material-symbols:check-circle-rounded"
            :hint="reviewed ? `${Math.round((stateCount.approved / reviewed) * 100)}% of those reviewed` : 'None reviewed yet'"
            :loading="isLoading"
          />
          <GStatTile
            label="Teams"
            :value="teams.length"
            icon="material-symbols:groups-rounded"
            :hint="`${active.length - inTeam} ${active.length - inTeam === 1 ? 'person' : 'people'} without a team`"
            :loading="isLoading"
            :to="`${base}/teams`"
          />
          <GStatTile
            label="Here now"
            :value="hereNow"
            icon="material-symbols:location-on-rounded"
            :hint="`of ${active.length} participants`"
            :loading="isLoading || venueQuery.isLoading.value"
            :to="`${base}/checkin`"
          />
          <GStatTile
            label="Emails sent"
            :value="emailsSent"
            icon="material-symbols:mail-rounded"
            hint="Decision emails, all time"
            :loading="isLoading"
          />
        </div>

        <div
          v-if="!isLoading"
          class="st-grid"
          :class="{ 'is-refreshing': isRefreshing }"
        >
          <OrganizersStatsCard
            id="st-registrations"
            title="Registrations"
            :description="`Everyone who signed up: ${everyone.length.toLocaleString()} ${everyone.length === 1 ? 'person' : 'people'}.`"
          >
            <GSegmentBar
              :parts="registrationParts"
              label="Participants by status"
              unit="people"
            />
            <GMeterList
              :rows="registrationRates"
              label="Registration progress"
            />
          </OrganizersStatsCard>

          <OrganizersStatsCard
            id="st-teams"
            title="Teams"
            :description="teams.length ? `${teams.length} ${teams.length === 1 ? 'team' : 'teams'}, ${averageSize.toFixed(1)} members on average.` : 'No teams yet.'"
          >
            <GMeterList
              :rows="teamRates"
              label="Team formation"
            />
            <div
              v-if="teamSizes.length"
              class="st-sub"
            >
              <h3 class="st-sub__title">
                Teams by size
              </h3>
              <GBarList
                :rows="teamSizes"
                label="Teams by size"
                :describe="(n) => `${n} ${n === 1 ? 'team' : 'teams'}`"
              />
            </div>
          </OrganizersStatsCard>

          <OrganizersStatsCard
            id="st-challenges"
            title="Challenges"
            :description="challenges.length ? `${challenges.length} ${challenges.length === 1 ? 'challenge' : 'challenges'}${draftCount ? `, ${draftCount} not published` : ''}.` : 'No challenges yet.'"
          >
            <GMeterList
              v-if="teams.length"
              :rows="challengeRates"
              label="Challenge picks"
            />
            <div
              v-if="challengeBars.length"
              class="st-sub"
            >
              <h3 class="st-sub__title">
                Teams per challenge
              </h3>
              <GBarList
                :rows="challengeBars"
                label="Teams per challenge"
                :describe="(n) => `${n} ${n === 1 ? 'team' : 'teams'}`"
              />
            </div>
            <GButton
              v-if="!challenges.length"
              variant="secondary"
              label="Add challenges"
              :to="`${base}/challenges`"
              class="st-fit"
            />
          </OrganizersStatsCard>

          <OrganizersStatsCard
            id="st-venue"
            title="At the venue"
            description="Check-ins of current participants."
          >
            <GMeterList
              :rows="venueRates"
              label="Check-ins"
            />
            <div
              v-if="teams.length"
              class="st-sub"
            >
              <h3 class="st-sub__title">
                Teams here now
              </h3>
              <GSegmentBar
                :parts="teamPresence"
                label="Teams by how many members are here"
                unit="teams"
              />
            </div>
          </OrganizersStatsCard>
        </div>

        <OrganizersResourceStats
          v-if="hackathonId"
          kind="hackathon"
          :event-id="hackathonId"
          :teams="teams"
        />
      </div>
    </template>
  </GPanel>
</template>

<style scoped>
.st {
  display: flex;
  flex-direction: column;
  gap: 24px;
  width: 100%;
  max-width: 80rem;
  margin: 0 auto;
}
.st-updated {
  font-size: 13px;
  color: var(--gh-text-muted);
}
.st-tiles {
  display: grid;
  /* 6 across when there's room, else 3 or 2 per row (never 5 + 1) */
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}
@media (min-width: 48rem) { .st-tiles { grid-template-columns: repeat(3, minmax(0, 1fr)); } }
@media (min-width: 90rem) { .st-tiles { grid-template-columns: repeat(6, minmax(0, 1fr)); } }
.st-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 28rem), 1fr));
  gap: 24px;
  transition: opacity var(--gh-dur-fast) var(--ease-gh);
}
.st-grid.is-refreshing { opacity: 0.6; }
.st-sub {
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.st-sub__title {
  margin: 0;
  font-size: 15px;
  font-weight: 700;
}
.st-fit { align-self: flex-start; }
</style>
