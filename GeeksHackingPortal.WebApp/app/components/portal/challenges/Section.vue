<script setup lang="ts">
import {
  geeksHackingPortalApiEndpointsParticipantsHackathonChallengesGetEndpointQueryOptions,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonChallengesListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueries } from '@tanstack/vue-query'
import { useLocalStorage, useMediaQuery } from '@vueuse/core'
import { TabsContent, TabsList, TabsRoot, TabsTrigger } from 'reka-ui'
import { computed, nextTick, watch } from 'vue'

/**
 * Challenge statements: a list of challenges (sponsor, title, how many teams picked it)
 * and the selected one's full statement. Tabs underneath, so arrow keys move through the list.
 * Below 1024px the list is a horizontal row above the statement.
 */
const route = useRoute()
const routeHackathonId = computed(() => (route.params.hackathonId as string) ?? '')
const { data: hackathon } = useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint({ path: computed(() => ({ hackathonIdOrShortCode: routeHackathonId.value })) })
const hackathonId = computed(() => hackathon.value?.id ?? '')

// Team counts move while teams pick challenges, so refresh every 30s
const { data: challengesData, isLoading, error } = useGeeksHackingPortalApiEndpointsParticipantsHackathonChallengesListEndpoint(
  { path: computed(() => ({ hackathonId: hackathonId.value })) },
  { query: { enabled: computed(() => !!hackathonId.value), refetchInterval: 30_000 } },
)
const challenges = computed(() => [...(challengesData.value?.challenges ?? [])].reverse())

// Prefetch every challenge's details so switching is instant
const detailsQueries = useQueries({
  queries: computed(() =>
    challenges.value.map(challenge =>
      geeksHackingPortalApiEndpointsParticipantsHackathonChallengesGetEndpointQueryOptions({ path: { hackathonId: hackathonId.value, challengeId: challenge.id ?? '' } }),
    ),
  ),
})

// Remember the last challenge viewed
const selectedId = useLocalStorage<string>('challenge-selected-id', '')
watch(challenges, (list) => {
  if (list.length && !list.some(c => c.id === selectedId.value))
    selectedId.value = list[0]?.id ?? ''
}, { immediate: true })

function descriptionOf(index: number) {
  return detailsQueries.value[index]?.data?.description ?? challenges.value[index]?.description ?? ''
}

const sponsorLogos: Record<string, string> = {
  'GovTech': '/logos/govtech-colored-logo.png',
  'Ahrefs': '/logos/ahrefs-colored-logo.svg',
  'Interledger': '/logos/Interledger-Foundation.png',
  'SP Group': '/logos/SP_Group_Full_Colour_Logo_Horizontal_RGB.png',
}

function teamLabel(count: number | undefined) {
  const n = count ?? 0
  return n === 1 ? '1 team' : `${n} teams`
}

const isWide = useMediaQuery('(min-width: 1024px)')

// keep the selected challenge visible in the scrolling row on small screens
watch([selectedId, () => challenges.value.length], async () => {
  await nextTick()
  document.querySelector<HTMLElement>('.challenges__tab[data-state=active]')?.scrollIntoView({ block: 'nearest', inline: 'nearest' })
}, { immediate: true })
</script>

<template>
  <section
    class="challenges"
    aria-label="Challenge statements"
  >
    <GLoading
      v-if="isLoading || !hackathonId"
      label="Loading challenges…"
    />

    <GAlert
      v-else-if="error"
      tone="err"
      title="We couldn't load the challenges"
      description="Refresh the page to try again."
    />

    <GEmptyState
      v-else-if="challenges.length === 0"
      icon="material-symbols:flag-rounded"
      title="Challenge statements aren't out yet"
      description="They'll appear here once the organisers release them. Check back closer to the event."
    />

    <TabsRoot
      v-else
      v-model="selectedId"
      :orientation="isWide ? 'vertical' : 'horizontal'"
      activation-mode="automatic"
      class="challenges__layout"
    >
      <div class="challenges__aside">
        <p class="challenges__intro">
          {{ challenges.length }} challenge statements. Team counts update every 30 seconds.
        </p>
        <TabsList
          class="challenges__list"
          aria-label="Challenges"
        >
          <TabsTrigger
            v-for="challenge in challenges"
            :key="challenge.id!"
            :value="challenge.id!"
            class="challenges__tab"
          >
            <span
              v-if="challenge.sponsor"
              class="challenges__sponsor"
            >{{ challenge.sponsor }}</span>
            <span class="challenges__tab-title">{{ challenge.title }}</span>
            <span class="challenges__count">
              <Icon
                name="material-symbols:groups-rounded"
                aria-hidden="true"
              />
              {{ teamLabel(challenge.teamCount) }}
            </span>
          </TabsTrigger>
        </TabsList>
      </div>

      <TabsContent
        v-for="(challenge, index) in challenges"
        :key="challenge.id!"
        :value="challenge.id!"
        class="challenges__detail"
      >
        <div class="challenges__detail-head">
          <div class="flex min-w-0 flex-col items-start gap-2">
            <GLabelStrip
              :label="`Challenge ${index + 1} of ${challenges.length}`"
              size="sm"
            />
            <h2 class="challenges__title">
              {{ challenge.title }}
            </h2>
            <p
              v-if="challenge.sponsor"
              class="challenges__by"
            >
              Set by {{ challenge.sponsor }} · {{ teamLabel(challenge.teamCount) }}
            </p>
          </div>
          <div
            v-if="challenge.sponsor && sponsorLogos[challenge.sponsor]"
            class="challenges__logo"
          >
            <img
              :src="sponsorLogos[challenge.sponsor]"
              :alt="challenge.sponsor"
            >
          </div>
        </div>

        <div class="challenges__block">
          <h3 class="challenges__block-title">
            Challenge statement
          </h3>
          <p class="challenges__text">
            {{ descriptionOf(index) || 'The full statement will be posted soon.' }}
          </p>
        </div>

        <div
          v-if="challenge.selectionCriteriaStmt"
          class="challenges__block"
        >
          <h3 class="challenges__block-title">
            Selection criteria
          </h3>
          <p class="challenges__text">
            {{ challenge.selectionCriteriaStmt }}
          </p>
        </div>
      </TabsContent>
    </TabsRoot>

  </section>
</template>

<style scoped>
.challenges {
  width: 100%;
}
.challenges__layout {
  display: flex;
  flex-direction: column;
  gap: 24px;
}
@media (min-width: 1024px) {
  .challenges__layout {
    display: grid;
    grid-template-columns: minmax(260px, 340px) 1fr;
    align-items: start;
    gap: 32px;
  }
  .challenges__aside {
    position: sticky;
    top: 0;
  }
}
.challenges__aside {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
}
.challenges__intro {
  margin: 0;
  font-size: 13px;
  color: var(--gh-text-muted);
}

/* the list: a scrolling row on small screens, a column on wide ones */
.challenges__list {
  display: flex;
  gap: 10px;
  margin: 0 -16px;
  padding: 4px 16px 8px;
  overflow-x: auto;
  scroll-padding-inline: 16px;
  scroll-snap-type: x mandatory;
}
@media (min-width: 1024px) {
  .challenges__list {
    flex-direction: column;
    margin: 0;
    padding: 0;
    overflow: visible;
  }
}
.challenges__tab {
  display: flex;
  flex: 0 0 min(260px, 78%);
  flex-direction: column;
  align-items: flex-start;
  gap: 6px;
  padding: 14px 16px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh);
  background: var(--gh-surface);
  color: var(--gh-text);
  font: inherit;
  text-align: left;
  cursor: pointer;
  scroll-snap-align: start;
  transition: background-color var(--gh-dur-fast, 120ms) var(--ease-gh);
}
@media (min-width: 1024px) {
  .challenges__tab { flex: none; }
}
.challenges__tab:hover {
  background: var(--gh-ghost-hover);
}
.challenges__tab:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: 2px;
}
.challenges__tab[data-state='active'] {
  border-color: var(--color-gh-ink);
  background: var(--gh-accent);
  color: var(--gh-on-accent);
}
.challenges__sponsor {
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.challenges__tab[data-state='active'] .challenges__sponsor,
.challenges__tab[data-state='active'] .challenges__count {
  color: var(--gh-on-accent);
}
.challenges__tab-title {
  font-family: var(--font-display);
  font-size: 16px;
  font-weight: 700;
  line-height: 1.25;
}
.challenges__count {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 13px;
  font-weight: 600;
  color: var(--gh-text-muted);
}

/* the selected challenge */
.challenges__detail {
  display: flex;
  flex-direction: column;
  gap: 24px;
  min-width: 0;
  padding: 24px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.challenges__detail:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: 2px;
}
.challenges__detail-head {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding-bottom: 20px;
  border-bottom: var(--gh-border-width) solid var(--gh-line-soft);
}
.challenges__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: clamp(24px, 3vw, 32px);
  font-weight: 700;
  line-height: 1.15;
}
.challenges__by {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
  color: var(--gh-text-muted);
}
/* logos are full colour: always on white */
.challenges__logo {
  display: grid;
  flex: none;
  place-items: center;
  width: 160px;
  height: 80px;
  padding: 10px 14px;
  border: var(--gh-border-width) solid var(--gh-line-soft);
  border-radius: var(--radius-gh);
  background: #fff;
}
.challenges__logo img {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
}
.challenges__block {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.challenges__block-title {
  margin: 0;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.challenges__text {
  max-width: 70ch;
  margin: 0;
  font-size: 16px;
  line-height: 1.65;
  white-space: pre-line;
}
</style>
