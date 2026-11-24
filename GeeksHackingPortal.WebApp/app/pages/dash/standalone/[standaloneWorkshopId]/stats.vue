<script setup lang="ts">
import type { GMeterRow } from '~/components/g/MeterList.vue'
import type { GSegmentPart } from '~/components/g/SegmentBar.vue'
import {
  useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueOverviewEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsAnalyticsEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'

/** Workshop stats: sign-ups against capacity, the venue, and resources (shared with hackathons). */
const route = useRoute()
const workshopId = computed(() => (route.params.standaloneWorkshopId as string | undefined) ?? '')
const base = computed(() => `/dash/standalone/${workshopId.value}`)
const enabled = { query: { enabled: computed(() => !!workshopId.value) } }

const { data: workshops } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint()
const analyticsQuery = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsAnalyticsEndpoint({ path: computed(() => ({ standaloneWorkshopId: workshopId.value })) }, enabled)
const venueQuery = useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueOverviewEndpoint2({ path: computed(() => ({ activityId: workshopId.value })) }, enabled)

const workshop = computed(() => workshops.value?.standaloneWorkshops?.find(w => w.id === workshopId.value))
const a = computed(() => analyticsQuery.data.value)
const isLoading = computed(() => analyticsQuery.isLoading.value)

const registered = computed(() => a.value?.registeredCount ?? 0)
const withdrawn = computed(() => a.value?.withdrawnCount ?? 0)
const capacity = computed(() => workshop.value?.maxParticipants ?? 0)
const hereNow = computed(() => a.value?.currentlyCheckedInCount ?? 0)
const everCame = computed(() => (venueQuery.data.value?.participants ?? []).filter(p => (p.totalCheckIns ?? 0) > 0).length)

const signUps = computed<GSegmentPart[]>(() => [
  { key: 'registered', label: 'Registered', value: registered.value, tone: 'ink', icon: 'material-symbols:how-to-reg-rounded' },
  { key: 'withdrew', label: 'Withdrew', value: withdrawn.value, tone: 'ink-3', icon: 'material-symbols:logout-rounded' },
])
const capacityRows = computed<GMeterRow[]>(() => capacity.value > 0
  ? [{ key: 'cap', label: 'Places taken', value: registered.value, total: capacity.value, hint: `${(a.value?.capacityRemaining ?? 0).toLocaleString()} ${(a.value?.capacityRemaining ?? 0) === 1 ? 'place' : 'places'} left.` }]
  : [])
const checkInsText = computed(() => {
  const n = a.value?.checkInCount ?? 0
  return `${n.toLocaleString()} ${n === 1 ? 'check-in' : 'check-ins'} in all`
})
const venueRows = computed<GMeterRow[]>(() => [
  { key: 'now', label: 'Here now', value: hereNow.value, total: registered.value },
  { key: 'ever', label: 'Checked in at least once', value: everCame.value, total: registered.value, hint: `${checkInsText.value}.` },
])

const queryClient = useQueryClient()
const isRefreshing = ref(false)
async function refresh() {
  isRefreshing.value = true
  try {
    await queryClient.invalidateQueries()
  }
  finally {
    isRefreshing.value = false
  }
}
</script>

<template>
  <GPanel id="standalone-stats">
    <template #header>
      <GPanelHeader title="Stats">
        <template #right>
          <span
            v-if="analyticsQuery.dataUpdatedAt.value"
            class="ws-updated"
          >Updated {{ new Date(analyticsQuery.dataUpdatedAt.value).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' }) }}</span>
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
      <div class="ws">
        <GAlert
          v-if="analyticsQuery.error.value && !a"
          tone="err"
          title="Couldn't load stats"
          :description="getApiErrorMessage(analyticsQuery.error.value, 'Refresh to try again.')"
        />

        <div class="ws-tiles">
          <GStatTile
            label="Registered"
            :value="registered"
            icon="material-symbols:group-rounded"
            :hint="withdrawn ? `${withdrawn} withdrew` : 'Nobody has withdrawn'"
            :loading="isLoading"
            :to="`${base}/participants`"
            highlight
          />
          <GStatTile
            label="Places left"
            :value="capacity > 0 ? (a?.capacityRemaining ?? 0) : '–'"
            icon="material-symbols:event-seat-rounded"
            :hint="capacity > 0 ? `of ${capacity.toLocaleString()} (${Math.round(a?.capacityUsedPercent ?? 0)}% taken)` : 'No limit set'"
            :loading="isLoading"
          />
          <GStatTile
            label="Here now"
            :value="hereNow"
            icon="material-symbols:location-on-rounded"
            :hint="checkInsText"
            :loading="isLoading"
            :to="`${base}/checkin`"
          />
          <GStatTile
            label="Redemptions"
            :value="a?.resourceRedemptionCount ?? 0"
            icon="material-symbols:redeem-rounded"
            :hint="`across ${(a?.resourceCount ?? 0)} ${(a?.resourceCount ?? 0) === 1 ? 'resource' : 'resources'}`"
            :loading="isLoading"
            :to="`${base}/resources`"
          />
        </div>

        <div
          v-if="!isLoading && a"
          class="ws-grid"
          :class="{ 'is-refreshing': isRefreshing }"
        >
          <OrganizersStatsCard
            id="ws-signups"
            title="Sign-ups"
            :description="`Everyone who signed up: ${(registered + withdrawn).toLocaleString()} ${registered + withdrawn === 1 ? 'person' : 'people'}.`"
          >
            <GSegmentBar
              :parts="signUps"
              label="Sign-ups by status"
              unit="people"
            />
            <GMeterList
              v-if="capacityRows.length"
              :rows="capacityRows"
              label="Capacity"
            />
          </OrganizersStatsCard>

          <OrganizersStatsCard
            id="ws-venue"
            title="At the venue"
            description="Check-ins of registered people."
          >
            <GMeterList
              :rows="venueRows"
              label="Check-ins"
            />
          </OrganizersStatsCard>
        </div>

        <OrganizersResourceStats
          v-if="workshopId"
          kind="workshop"
          :event-id="workshopId"
        />
      </div>
    </template>
  </GPanel>
</template>

<style scoped>
.ws {
  display: flex;
  flex-direction: column;
  gap: 24px;
  width: 100%;
  max-width: 80rem;
  margin: 0 auto;
}
.ws-updated {
  font-size: 13px;
  color: var(--gh-text-muted);
}
.ws-tiles {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}
@media (min-width: 60rem) { .ws-tiles { grid-template-columns: repeat(4, minmax(0, 1fr)); } }
.ws-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 28rem), 1fr));
  gap: 24px;
  transition: opacity var(--gh-dur-fast) var(--ease-gh);
}
.ws-grid.is-refreshing { opacity: 0.6; }
</style>
