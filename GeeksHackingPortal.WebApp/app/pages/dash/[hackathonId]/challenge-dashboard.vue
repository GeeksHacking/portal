<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonOrganizersListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonChallengesListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useLocalStorage } from '@vueuse/core'

/**
 * Live challenge dashboard, for a big screen at the venue: how many teams picked each challenge,
 * refreshed every 10 seconds. Organisers only.
 *
 * - The board is dark by default (projectors and darkened rooms), switchable to light; the choice
 *   is remembered per browser and is separate from the portal's own theme.
 * - Each challenge: its name (readable from the back of the room), team count, share of all teams
 *   as a bar, and a sparkline of the count since the page was opened (kept in memory only).
 * - When a challenge gains teams, a notice slides in for a few seconds (also announced to screen
 *   readers). "Full screen" shows just the board.
 */
const route = useRoute()
const hackathonIdOrShortCode = computed(() => (route.params.hackathonId as string | undefined) ?? '')

const { data: hackathon, isLoading: isLoadingHackathon } = useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint({ path: computed(() => ({ hackathonIdOrShortCode: hackathonIdOrShortCode.value })) }, { query: { enabled: computed(() => !!hackathonIdOrShortCode.value) } })
const hackathonId = computed(() => hackathon.value?.id ?? '')

const { data: user, isLoading: isLoadingUser } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const { data: organizersData, isLoading: isLoadingOrganizers } = useGeeksHackingPortalApiEndpointsOrganizersHackathonOrganizersListEndpoint({ path: computed(() => ({ hackathonId: hackathonId.value })) }, { query: { enabled: computed(() => !!hackathonId.value) } })

const isOrganizer = computed(() => {
  if (!user.value?.id)
    return false
  if (user.value.isRoot)
    return true
  return organizersData.value?.organizers?.some(org => org.userId === user.value?.id) ?? false
})
const isCheckingAccess = computed(() => isLoadingHackathon.value || isLoadingUser.value || (!!hackathonId.value && isLoadingOrganizers.value))

watch([isOrganizer, isCheckingAccess], ([organizer, checking]) => {
  if (!checking && !organizer)
    navigateTo(`/dash/${hackathonIdOrShortCode.value}`)
})

// the layout adds the event's name: "Challenge dashboard · GeekHack · GeeksHacking"
useHead({ title: 'Challenge dashboard' })

const REFRESH_MS = 10_000
const { data: challengesData, dataUpdatedAt, isLoading: isLoadingChallenges, error: challengesError } = useGeeksHackingPortalApiEndpointsParticipantsHackathonChallengesListEndpoint({ path: computed(() => ({ hackathonId: hackathonId.value })) }, {
  query: {
    enabled: computed(() => !!hackathonId.value && isOrganizer.value),
    refetchInterval: REFRESH_MS,
  },
})

const challenges = computed(() => challengesData.value?.challenges ?? [])
// most teams first; ties by name so equal tiles don't swap places on every refresh
const sortedChallenges = computed(() =>
  [...challenges.value].sort((a, b) => (b.teamCount ?? 0) - (a.teamCount ?? 0)
    || (a.title ?? '').localeCompare(b.title ?? '')),
)
const totalTeams = computed(() => challenges.value.reduce((sum, c) => sum + (c.teamCount ?? 0), 0))
const maxTeams = computed(() => Math.max(0, ...challenges.value.map(c => c.teamCount ?? 0)))

const columns = computed(() => {
  const n = sortedChallenges.value.length
  if (n <= 2)
    return Math.max(n, 1)
  if (n <= 4)
    return 2
  if (n <= 9)
    return 3
  return 4
})

function share(count: number | undefined) {
  return totalTeams.value ? Math.round(((count ?? 0) / totalTeams.value) * 100) : 0
}

const updatedLabel = computed(() => dataUpdatedAt.value
  ? new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit', second: '2-digit' }).format(new Date(dataUpdatedAt.value))
  : '')

// ---- board theme + full screen ------------------------------------------------------------------
const boardTheme = useLocalStorage<'dark' | 'light'>('challenge-dashboard-theme', 'dark')
function toggleBoardTheme() {
  boardTheme.value = boardTheme.value === 'dark' ? 'light' : 'dark'
}
const board = ref<HTMLElement | null>(null)
const isFullscreen = ref(false)
const canFullscreen = ref(false)

function syncFullscreen() {
  isFullscreen.value = !!document.fullscreenElement
}

async function toggleFullscreen() {
  try {
    if (document.fullscreenElement)
      await document.exitFullscreen()
    else
      await (board.value ?? document.documentElement).requestFullscreen()
  }
  catch (error) {
    console.error('Failed to toggle full screen', error)
    syncFullscreen()
  }
}

onMounted(() => {
  canFullscreen.value = !!document.fullscreenEnabled
  document.addEventListener('fullscreenchange', syncFullscreen)
})
onBeforeUnmount(() => {
  document.removeEventListener('fullscreenchange', syncFullscreen)
})

// ---- history (sparklines) and "new teams" notices -----------------------------------------------
const MAX_HISTORY = 30
const history = ref(new Map<string, { at: number, count: number }[]>())
const previous = new Map<string, number>()

interface Notice { id: number, title: string, delta: number, count: number }
const notices = ref<Notice[]>([])
let noticeId = 0

watch(dataUpdatedAt, () => {
  const now = Date.now()
  for (const challenge of challenges.value) {
    if (!challenge.id)
      continue
    const count = challenge.teamCount ?? 0
    const points = history.value.get(challenge.id) ?? []
    points.push({ at: now, count })
    if (points.length > MAX_HISTORY)
      points.splice(0, points.length - MAX_HISTORY)
    history.value.set(challenge.id, points)

    const before = previous.get(challenge.id)
    if (before !== undefined && count > before) {
      const notice = { id: noticeId++, title: challenge.title ?? 'A challenge', delta: count - before, count }
      notices.value.push(notice)
      setTimeout(() => {
        notices.value = notices.value.filter(n => n.id !== notice.id)
      }, 6000)
    }
    previous.set(challenge.id, count)
  }
})

// 200×48 viewBox, anchored at zero so a flat line near the bottom means "few teams"
const SPARK_W = 200
const SPARK_H = 48
const SPARK_PAD = 3

function sparkPoints(challengeId: string | undefined) {
  const points = history.value.get(challengeId ?? '') ?? []
  if (points.length < 2)
    return null
  const first = points[0]!.at
  const span = (points.at(-1)!.at - first) || 1
  const top = Math.max(1, ...points.map(p => p.count))
  return points.map(p => ({
    x: SPARK_PAD + ((p.at - first) / span) * (SPARK_W - SPARK_PAD * 2),
    y: SPARK_H - SPARK_PAD - (p.count / top) * (SPARK_H - SPARK_PAD * 2),
  }))
}

function sparkLine(challengeId: string | undefined) {
  const pts = sparkPoints(challengeId)
  return pts ? `M ${pts.map(p => `${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' L ')}` : ''
}

function sparkArea(challengeId: string | undefined) {
  const pts = sparkPoints(challengeId)
  if (!pts)
    return ''
  const base = SPARK_H - SPARK_PAD
  return `${sparkLine(challengeId)} L ${pts.at(-1)!.x.toFixed(1)},${base} L ${pts[0]!.x.toFixed(1)},${base} Z`
}

function sparkSummary(challengeId: string | undefined) {
  const points = history.value.get(challengeId ?? '') ?? []
  if (points.length < 2)
    return ''
  const change = points.at(-1)!.count - points[0]!.count
  const since = new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit' }).format(new Date(points[0]!.at))
  return change === 0
    ? `No change since ${since}`
    : `${change > 0 ? '+' : ''}${change} since ${since}`
}
</script>

<template>
  <GPanel id="challenge-dashboard">
    <template #header>
      <GPanelHeader
        title="Challenge dashboard"
        as="h1"
      >
        <template #right>
          <div class="flex items-center gap-1">
            <span class="hidden sm:contents">
              <GButton
                variant="ghost"
                size="sm"
                :icon="boardTheme === 'dark' ? 'material-symbols:light-mode-rounded' : 'material-symbols:dark-mode-rounded'"
                :label="boardTheme === 'dark' ? 'Light board' : 'Dark board'"
                @click="toggleBoardTheme"
              />
              <!-- full screen is for the venue screen, not phones -->
              <GButton
                v-if="canFullscreen"
                variant="secondary"
                size="sm"
                icon="material-symbols:fullscreen-rounded"
                label="Full screen"
                @click="toggleFullscreen"
              />
            </span>
            <span class="contents sm:hidden">
              <GButton
                variant="ghost"
                size="sm"
                :icon="boardTheme === 'dark' ? 'material-symbols:light-mode-rounded' : 'material-symbols:dark-mode-rounded'"
                :aria-label="boardTheme === 'dark' ? 'Light board' : 'Dark board'"
                @click="toggleBoardTheme"
              />
            </span>
          </div>
        </template>
      </GPanelHeader>
    </template>

    <template #body>
      <div
        ref="board"
        class="cb"
        :data-gh-theme="boardTheme"
      >
        <!-- heading row -->
        <header class="cb-head">
          <div class="cb-head__titles">
            <p
              v-if="hackathon?.name"
              class="cb-head__event"
            >
              {{ hackathon.name }}
            </p>
            <h2 class="cb-head__title">
              Challenge picks
            </h2>
          </div>
          <div class="cb-head__side">
            <p class="cb-head__total">
              <span class="cb-head__total-value">{{ totalTeams }}</span>
              {{ totalTeams === 1 ? 'team has' : 'teams have' }} picked
            </p>
            <p class="cb-head__live">
              <span
                class="cb-head__dot"
                aria-hidden="true"
              />
              Live{{ updatedLabel ? ` · ${updatedLabel}` : '' }}
            </p>
            <GButton
              v-if="isFullscreen"
              variant="ghost"
              size="sm"
              icon="material-symbols:fullscreen-exit-rounded"
              label="Exit full screen"
              @click="toggleFullscreen"
            />
          </div>
        </header>

        <GLoading
          v-if="isCheckingAccess || isLoadingChallenges"
          label="Loading the dashboard…"
        />
        <GAlert
          v-else-if="challengesError && !challenges.length"
          tone="err"
          title="Couldn't load challenges"
          :description="getApiErrorMessage(challengesError, 'It will try again in a few seconds.')"
        />
        <GEmptyState
          v-else-if="!challenges.length"
          icon="material-symbols:flag-rounded"
          title="No challenges yet"
          description="Challenges show up here once they're published."
        />

        <!-- one tile per challenge -->
        <ol
          v-else
          class="cb-grid"
          :style="{ gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` }"
        >
          <li
            v-for="challenge in sortedChallenges"
            :key="challenge.id"
            class="cb-tile"
            :class="{ 'cb-tile--lead': maxTeams > 0 && (challenge.teamCount ?? 0) === maxTeams }"
          >
            <div class="cb-tile__top">
              <h3 class="cb-tile__title">
                {{ challenge.title }}
              </h3>
              <p
                v-if="challenge.sponsor"
                class="cb-tile__sponsor"
              >
                {{ challenge.sponsor }}
              </p>
            </div>

            <p class="cb-tile__count">
              <span class="cb-tile__value">{{ challenge.teamCount ?? 0 }}</span>
              <span class="cb-tile__unit">{{ (challenge.teamCount ?? 0) === 1 ? 'team' : 'teams' }}</span>
            </p>

            <!-- share of all teams -->
            <div
              class="cb-share"
              role="img"
              :aria-label="`${share(challenge.teamCount)}% of all teams`"
            >
              <div class="cb-share__track">
                <div
                  class="cb-share__bar"
                  :style="{ width: `${share(challenge.teamCount)}%` }"
                />
              </div>
              <span class="cb-share__label">{{ share(challenge.teamCount) }}%</span>
            </div>

            <!-- count since the page opened -->
            <div class="cb-spark">
              <svg
                v-if="sparkLine(challenge.id)"
                :viewBox="`0 0 ${SPARK_W} ${SPARK_H}`"
                preserveAspectRatio="none"
                class="cb-spark__svg"
                role="img"
                :aria-label="sparkSummary(challenge.id)"
              >
                <title>{{ sparkSummary(challenge.id) }}</title>
                <path
                  :d="sparkArea(challenge.id)"
                  class="cb-spark__area"
                />
                <path
                  :d="sparkLine(challenge.id)"
                  class="cb-spark__line"
                  vector-effect="non-scaling-stroke"
                />
              </svg>
              <div
                v-else
                class="cb-spark__empty"
              />
              <p class="cb-spark__caption">
                {{ sparkSummary(challenge.id) || 'Trend appears after the next update' }}
              </p>
            </div>
          </li>
        </ol>

        <!-- "new teams" notices -->
        <div
          class="cb-notices"
          aria-live="polite"
        >
          <TransitionGroup name="cb-notice">
            <div
              v-for="notice in notices"
              :key="notice.id"
              class="cb-notice"
            >
              <Icon
                name="material-symbols:celebration-rounded"
                class="cb-notice__icon"
                aria-hidden="true"
              />
              <div class="min-w-0">
                <p class="cb-notice__title">
                  {{ notice.title }}
                </p>
                <p class="cb-notice__text">
                  +{{ notice.delta }} {{ notice.delta === 1 ? 'team' : 'teams' }} · now {{ notice.count }}
                </p>
              </div>
            </div>
          </TransitionGroup>
        </div>
      </div>
    </template>
  </GPanel>
</template>

<style scoped>
/* the board carries its own theme (data-gh-theme), so the tokens below follow it */
.cb {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: clamp(16px, 2.5vw, 32px);
  min-height: 100%;
  padding: clamp(16px, 3vw, 40px);
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-bg);
  color: var(--gh-text);
}
.cb:fullscreen {
  border: 0;
  border-radius: 0;
  overflow: auto;
}

.cb-head {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  justify-content: space-between;
  gap: 12px 32px;
}
.cb-head__titles {
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 0;
}
.cb-head__event {
  margin: 0;
  font-size: clamp(14px, 1.4vw, 20px);
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.cb-head__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: clamp(32px, 5vw, 72px);
  font-weight: 700;
  line-height: 1;
}
.cb-head__side {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px 24px;
}
.cb-head__total {
  margin: 0;
  font-size: clamp(16px, 1.6vw, 22px);
  color: var(--gh-text-muted);
}
.cb-head__total-value {
  margin-right: 4px;
  font-family: var(--font-display);
  font-size: 1.6em;
  font-weight: 700;
  color: var(--gh-text);
  font-variant-numeric: tabular-nums;
}
.cb-head__live {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  margin: 0;
  font-size: 14px;
  font-weight: 700;
  color: var(--gh-text-muted);
  font-variant-numeric: tabular-nums;
}
.cb-head__dot {
  width: 10px;
  height: 10px;
  border-radius: 999px;
  background: var(--gh-ok);
  animation: cb-pulse 2s ease-in-out infinite;
}
@keyframes cb-pulse { 50% { opacity: 0.35; } }

.cb-grid {
  display: grid;
  flex: 1 1 auto;
  grid-auto-rows: minmax(0, 1fr);
  gap: clamp(12px, 1.6vw, 24px);
  margin: 0;
  padding: 0;
  list-style: none;
}
@media (max-width: 767px) {
  .cb-grid { grid-template-columns: minmax(0, 1fr) !important; }
}

.cb-tile {
  display: flex;
  flex-direction: column;
  gap: clamp(8px, 1.2vw, 16px);
  min-width: 0;
  padding: clamp(16px, 2vw, 28px);
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
/* the most-picked challenge (or challenges, on a tie) */
.cb-tile--lead {
  border-color: var(--gh-accent);
  box-shadow: inset 0 6px 0 var(--gh-accent);
}
.cb-tile__top {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}
.cb-tile__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: clamp(20px, 2vw, 32px);
  font-weight: 700;
  line-height: 1.15;
  overflow-wrap: anywhere;
}
.cb-tile__sponsor {
  margin: 0;
  font-size: clamp(13px, 1vw, 16px);
  color: var(--gh-text-muted);
}
.cb-tile__count {
  display: flex;
  align-items: baseline;
  gap: 10px;
  margin: auto 0 0;
}
.cb-tile__value {
  font-family: var(--font-display);
  font-size: clamp(56px, 7vw, 120px);
  font-weight: 700;
  line-height: 0.9;
  font-variant-numeric: tabular-nums;
}
.cb-tile__unit {
  font-size: clamp(16px, 1.4vw, 22px);
  font-weight: 600;
  color: var(--gh-text-muted);
}

.cb-share {
  display: flex;
  align-items: center;
  gap: 12px;
}
.cb-share__track {
  flex: 1 1 auto;
  height: 10px;
  overflow: hidden;
  border-radius: 999px;
  background: var(--gh-viz-track);
}
.cb-share__bar {
  height: 100%;
  border-radius: 999px;
  background: var(--gh-viz-1);
  transition: width 600ms var(--ease-gh, ease);
}
.cb-share__label {
  min-width: 3.5ch;
  font-size: clamp(14px, 1.2vw, 18px);
  font-weight: 700;
  text-align: right;
  font-variant-numeric: tabular-nums;
}

.cb-spark {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.cb-spark__svg,
.cb-spark__empty {
  width: 100%;
  height: clamp(32px, 4vw, 56px);
}
.cb-spark__empty {
  border-bottom: 2px dashed var(--gh-viz-track);
}
.cb-spark__area {
  fill: var(--gh-viz-1);
  opacity: 0.06;
}
.cb-spark__line {
  fill: none;
  stroke: var(--gh-viz-1);
  stroke-width: 2;
  stroke-linecap: round;
  stroke-linejoin: round;
}
.cb-spark__caption {
  margin: 0;
  font-size: 13px;
  color: var(--gh-text-muted);
}

/* notices stay inside the board, so they show in full screen too */
.cb-notices {
  position: fixed;
  right: 24px;
  bottom: 24px;
  z-index: 50;
  display: flex;
  flex-direction: column;
  gap: 8px;
  width: min(22rem, calc(100% - 48px));
  pointer-events: none;
}
.cb-notice {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 14px 16px;
  border: var(--gh-border-width) solid var(--gh-on-accent);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-accent);
  color: var(--gh-on-accent);
  box-shadow: 4px 4px 0 var(--gh-shadow-color, #000);
}
.cb-notice__icon {
  flex: none;
  width: 24px;
  height: 24px;
}
.cb-notice__title {
  margin: 0;
  font-weight: 700;
  overflow-wrap: anywhere;
}
.cb-notice__text {
  margin: 2px 0 0;
  font-size: 14px;
}
.cb-notice-enter-active,
.cb-notice-leave-active {
  transition: transform 300ms ease, opacity 300ms ease;
}
.cb-notice-enter-from,
.cb-notice-leave-to {
  transform: translateX(110%);
  opacity: 0;
}
@media (prefers-reduced-motion: reduce) {
  .cb-head__dot { animation: none; }
  .cb-share__bar { transition: none; }
  .cb-notice-enter-active,
  .cb-notice-leave-active { transition: opacity 150ms linear; }
  .cb-notice-enter-from,
  .cb-notice-leave-to { transform: none; }
}
</style>
