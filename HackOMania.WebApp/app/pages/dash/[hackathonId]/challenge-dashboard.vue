<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'
import { authQueries } from '~/composables/auth'
import { challengeQueries } from '~/composables/challenges'
import { hackathonQueries as participantHackathonQueries } from '~/composables/hackathons'
import { organizerQueries } from '~/composables/organizers'

const route = useRoute()
const hackathonIdOrShortCode = computed(() => (route.params.hackathonId as string | undefined) ?? null)

const { data: hackathon, isLoading: isLoadingHackathon } = useQuery(
  computed(() => ({
    ...participantHackathonQueries.detail(hackathonIdOrShortCode.value ?? ''),
    enabled: !!hackathonIdOrShortCode.value,
  })),
)

const resolvedHackathonId = computed(() => hackathon.value?.id ?? null)

const { data: user, isLoading: isLoadingUser } = useQuery(authQueries.whoAmI)

const { data: organizersData, isLoading: isLoadingOrganizers } = useQuery(
  computed(() => ({
    ...organizerQueries.list(resolvedHackathonId.value ?? ''),
    enabled: !!resolvedHackathonId.value,
  })),
)

const isOrganizer = computed(() => {
  if (!user.value?.id)
    return false
  if (user.value.isRoot)
    return true
  return organizersData.value?.organizers?.some(org => org.userId === user.value?.id) ?? false
})

const isLoadingOrganizerCheck = computed(() => isLoadingHackathon.value || isLoadingUser.value || isLoadingOrganizers.value)

watch([isOrganizer, isLoadingOrganizerCheck], ([org, loading]) => {
  if (!loading && !org) {
    navigateTo(`/dash/${hackathonIdOrShortCode.value}`)
  }
})

// Poll challenges every 10 seconds for live updates
const { data: challengesData, dataUpdatedAt, isLoading: isLoadingChallenges } = useQuery(
  computed(() => ({
    ...challengeQueries.list(resolvedHackathonId.value ?? ''),
    enabled: !!resolvedHackathonId.value && isOrganizer.value,
    refetchInterval: 10_000,
  })),
)

const challenges = computed(() => challengesData.value?.challenges ?? [])

// Sort challenges by team count descending
const sortedChallenges = computed(() =>
  [...challenges.value].sort((a, b) => (b.teamCount ?? 0) - (a.teamCount ?? 0)),
)

const gridCols = computed(() => {
  const n = sortedChallenges.value.length
  if (n <= 2)
    return n || 1
  if (n <= 4)
    return 2
  if (n <= 9)
    return 3
  return 4
})

const totalTeams = computed(() =>
  challenges.value.reduce((sum, c) => sum + (c.teamCount ?? 0), 0),
)

// Time-series history: challengeId → [{timestamp, count}]
const MAX_HISTORY = 30
const historyMap = ref(new Map<string, { timestamp: number, count: number }[]>())

// Notifications
interface Notification {
  id: number
  challengeTitle: string
  delta: number
  newCount: number
}
const notifications = ref<Notification[]>([])
let _notifId = 0

// Previous counts for change detection
const prevCounts = ref(new Map<string, number>())
const dashboardElement = ref<HTMLElement | null>(null)
const isFullscreen = ref(false)

function syncFullscreenState() {
  isFullscreen.value = !!document.fullscreenElement
}

async function toggleFullscreen() {
  if (!import.meta.client)
    return
  if (!document.fullscreenEnabled)
    return
  try {
    if (!document.fullscreenElement) {
      await (dashboardElement.value ?? document.documentElement).requestFullscreen()
    }
    else {
      await document.exitFullscreen()
    }
  }
  catch (error) {
    console.error('Failed to toggle fullscreen mode', error)
    syncFullscreenState()
  }
}

onMounted(() => {
  document.addEventListener('fullscreenchange', syncFullscreenState)
})

onUnmounted(() => {
  document.removeEventListener('fullscreenchange', syncFullscreenState)
})

// When data updates, accumulate history and emit notifications
watch(dataUpdatedAt, () => {
  const now = Date.now()
  for (const challenge of challenges.value) {
    if (!challenge.id)
      continue
    const count = challenge.teamCount ?? 0
    const prev = prevCounts.value.get(challenge.id)

    // Accumulate history
    const history = historyMap.value.get(challenge.id) ?? []
    history.push({ timestamp: now, count })
    if (history.length > MAX_HISTORY)
      history.splice(0, history.length - MAX_HISTORY)
    historyMap.value.set(challenge.id, history)

    // Emit notification when count increases
    if (prev !== undefined && count > prev) {
      const notif: Notification = {
        id: _notifId++,
        challengeTitle: challenge.title ?? 'Unknown',
        delta: count - prev,
        newCount: count,
      }
      notifications.value.push(notif)
      setTimeout(() => {
        const idx = notifications.value.findIndex(n => n.id === notif.id)
        if (idx !== -1)
          notifications.value.splice(idx, 1)
      }, 5000)
    }

    prevCounts.value.set(challenge.id, count)
  }
})

// Build SVG sparkline path for a challenge
function getSparklinePath(challengeId: string): string {
  const history = historyMap.value.get(challengeId) ?? []
  if (history.length < 2)
    return ''

  const W = 200
  const H = 48
  const PAD = 4

  const minT = history[0]!.timestamp
  const maxT = history[history.length - 1]!.timestamp
  const counts = history.map(h => h.count)
  const minC = Math.min(...counts)
  const maxC = Math.max(...counts)

  const rangeT = maxT - minT || 1
  const rangeC = maxC - minC || 1

  const pts = history.map((h) => {
    const x = PAD + ((h.timestamp - minT) / rangeT) * (W - PAD * 2)
    const y = (H - PAD) - ((h.count - minC) / rangeC) * (H - PAD * 2)
    return `${x.toFixed(1)},${y.toFixed(1)}`
  })

  return `M ${pts.join(' L ')}`
}

// Build SVG fill path (area under sparkline)
function getSparklineFill(challengeId: string): string {
  const history = historyMap.value.get(challengeId) ?? []
  if (history.length < 2)
    return ''

  const W = 200
  const H = 48
  const PAD = 4

  const minT = history[0]!.timestamp
  const maxT = history[history.length - 1]!.timestamp
  const counts = history.map(h => h.count)
  const minC = Math.min(...counts)
  const maxC = Math.max(...counts)

  const rangeT = maxT - minT || 1
  const rangeC = maxC - minC || 1

  const xs: number[] = []
  const pts = history.map((h) => {
    const x = PAD + ((h.timestamp - minT) / rangeT) * (W - PAD * 2)
    const y = (H - PAD) - ((h.count - minC) / rangeC) * (H - PAD * 2)
    xs.push(x)
    return `${x.toFixed(1)},${y.toFixed(1)}`
  })

  const firstX = xs[0]?.toFixed(1) ?? PAD
  const lastX = xs[xs.length - 1]?.toFixed(1) ?? (W - PAD)

  return `M ${pts.join(' L ')} L ${lastX},${H - PAD} L ${firstX},${H - PAD} Z`
}

// Whether a challenge has enough history to show the chart
function hasChart(challengeId: string | null | undefined): boolean {
  return (historyMap.value.get(challengeId ?? '') ?? []).length >= 2
}
</script>

<template>
  <UDashboardPanel id="challenge-dashboard">
    <template #header>
      <UDashboardNavbar :title="hackathon?.name ? `${hackathon.name} — Challenges` : 'Challenge Dashboard'">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>

        <template #trailing>
          <div class="flex items-center gap-3">
            <UButton
              :icon="isFullscreen ? 'i-lucide-shrink' : 'i-lucide-expand'"
              color="neutral"
              variant="ghost"
              size="sm"
              :aria-label="isFullscreen ? 'Exit full screen' : 'View full screen'"
              @click="toggleFullscreen"
            >
              {{ isFullscreen ? 'Exit full screen' : 'Full screen' }}
            </UButton>
            <div class="text-sm text-(--ui-text-muted)">
              <span class="font-semibold text-(--ui-text)">{{ totalTeams }}</span> teams total
            </div>
            <div class="flex items-center gap-1.5">
              <span class="relative flex h-2 w-2">
                <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75" />
                <span class="relative inline-flex h-2 w-2 rounded-full bg-emerald-500" />
              </span>
              <span class="text-xs font-semibold text-emerald-500 uppercase tracking-wider">Live</span>
            </div>
          </div>
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <!-- Full-screen light dashboard -->
      <div
        ref="dashboardElement"
        class="h-full min-h-0 flex flex-col bg-gray-50 p-6"
      >
        <!-- Loading state -->
        <div
          v-if="isLoadingOrganizerCheck || isLoadingChallenges"
          class="flex items-center justify-center h-64 text-gray-600 text-sm"
        >
          Loading dashboard…
        </div>
        <div
          v-else-if="!challenges.length"
          class="flex items-center justify-center h-64 text-gray-600 text-sm"
        >
          No challenges available yet.
        </div>

        <!-- Challenge grid -->
        <template v-else>
          <p class="text-gray-900 font-bold text-7xl uppercase tracking-widest mb-6 text-center">
            Challenge Distribution
          </p>
          <div
            class="flex-1 min-h-0 grid gap-5 [grid-auto-rows:1fr]"
            :style="{ gridTemplateColumns: `repeat(${gridCols}, minmax(0, 1fr))` }"
          >
            <div
              v-for="challenge in sortedChallenges"
              :key="challenge.id ?? ''"
              class="relative bg-white border border-gray-200 rounded-2xl p-6 flex flex-col gap-3 overflow-hidden group transition-all duration-300 hover:border-orange-500/40 hover:shadow-[0_0_32px_-4px_rgba(249,115,22,0.15)]"
            >
              <!-- Subtle gradient accent -->
              <div class="absolute inset-0 bg-gradient-to-br from-orange-500/10 via-transparent to-transparent pointer-events-none" />

              <!-- Challenge title -->
              <p class="text-gray-500 text-xs font-semibold uppercase tracking-widest">
                {{ challenge.title }}
              </p>

              <!-- Team count -->
              <div class="flex items-end gap-3">
                <span
                  class="text-gray-900 font-bold tabular-nums leading-none"
                  style="font-size: clamp(3rem, 5vw, 5rem)"
                >
                  {{ challenge.teamCount ?? 0 }}
                </span>
                <span class="text-gray-500 text-base font-medium pb-2">
                  {{ (challenge.teamCount ?? 0) === 1 ? 'team' : 'teams' }}
                </span>
              </div>

              <!-- Sparkline chart -->
              <div class="mt-1 h-12">
                <svg
                  v-if="hasChart(challenge.id)"
                  viewBox="0 0 200 48"
                  class="w-full h-full"
                  preserveAspectRatio="none"
                >
                  <!-- Area fill -->
                  <path
                    :d="getSparklineFill(challenge.id ?? '')"
                    fill="url(#sparkGrad)"
                    opacity="0.3"
                  />
                  <!-- Line -->
                  <path
                    :d="getSparklinePath(challenge.id ?? '')"
                    fill="none"
                    stroke="#f97316"
                    stroke-width="2"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                  />
                  <!-- Gradient definition -->
                  <defs>
                    <linearGradient
                      id="sparkGrad"
                      x1="0"
                      y1="0"
                      x2="0"
                      y2="1"
                    >
                      <stop
                        offset="0%"
                        stop-color="#f97316"
                      />
                      <stop
                        offset="100%"
                        stop-color="#f97316"
                        stop-opacity="0"
                      />
                    </linearGradient>
                  </defs>
                </svg>
                <!-- Placeholder when history insufficient -->
                <div
                  v-else
                  class="w-full h-full flex items-center"
                >
                  <div class="w-full h-px bg-gray-200" />
                </div>
              </div>
            </div>
          </div>
        </template>

        <!-- Notifications overlay -->
        <div class="fixed bottom-6 right-6 flex flex-col gap-2 z-50 max-w-xs w-full pointer-events-none">
          <TransitionGroup
            enter-active-class="transition-all duration-300 ease-out"
            enter-from-class="translate-x-full opacity-0"
            leave-active-class="transition-all duration-300 ease-in"
            leave-to-class="translate-x-full opacity-0"
          >
            <div
              v-for="notif in notifications"
              :key="notif.id"
              class="pointer-events-auto bg-orange-500 text-white rounded-xl px-4 py-3 shadow-2xl flex items-start gap-3"
            >
              <UIcon
                name="i-lucide-bell-ring"
                class="w-5 h-5 mt-0.5 shrink-0 animate-bounce"
              />
              <div class="min-w-0">
                <p class="font-semibold text-sm leading-tight truncate">
                  {{ notif.challengeTitle }}
                </p>
                <p class="text-xs text-orange-100 mt-0.5">
                  +{{ notif.delta }} {{ notif.delta === 1 ? 'team' : 'teams' }} selected — now {{ notif.newCount }} total
                </p>
              </div>
            </div>
          </TransitionGroup>
        </div>
      </div>
    </template>
  </UDashboardPanel>
</template>
