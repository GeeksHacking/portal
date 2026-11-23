<script setup lang="ts">
import type { GeeksHackingPortalApiEndpointsOrganizersHackathonVenueOverviewParticipantCheckInDto } from '@geekshacking/portal-sdk'
import {
  geeksHackingPortalApiEndpointsOrganizersHackathonVenueHistoryEndpoint1QueryKey,
  geeksHackingPortalApiEndpointsOrganizersHackathonVenueHistoryEndpoint2QueryKey,
  geeksHackingPortalApiEndpointsOrganizersHackathonVenueOverviewEndpoint1QueryKey,
  geeksHackingPortalApiEndpointsOrganizersHackathonVenueOverviewEndpoint2QueryKey,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsGetEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueCheckInEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueCheckInEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueCheckOutEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueCheckOutEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueHistoryEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueHistoryEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueOverviewEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueOverviewEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsGetEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import { HACKATHON_TIME_ZONE, HACKATHON_TIME_ZONE_LABEL, parseHackathonDateTimeValue } from '~/utils/hackathon-date-time'

/**
 * The check-in desk for one event, shared by hackathons and workshops (only the API endpoints
 * differ). Built for a phone at the door:
 *
 * - "Scan QR code" opens the camera; a scan shows the participant with one big Check in / Check out
 *   button, then "Scan next" goes straight back to the camera.
 * - Counts (here now / left / not arrived) and the participant list refresh every 15 seconds and
 *   after every action. Each row has its own Check in / Check out and a history dialog, so the list
 *   doubles as the manual lookup when someone has no QR code.
 * - Recent activity: the event's audit trail, newest first.
 */
const props = defineProps<{
  kind: 'hackathon' | 'workshop'
  /** the hackathon or workshop id */
  activityId: string
}>()

type Participant = GeeksHackingPortalApiEndpointsOrganizersHackathonVenueOverviewParticipantCheckInDto

const toast = useGToast()
const queryClient = useQueryClient()
const isHackathon = computed(() => props.kind === 'hackathon')
const activityId = computed(() => props.activityId)

// ---- data ---------------------------------------------------------------------------------------
// Both endpoint sets are wired up; only the one for this kind is enabled.
const LIVE_REFRESH_MS = 15_000
const overviewPath = computed(() => ({ activityId: activityId.value }))
const overview1 = useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueOverviewEndpoint1({ path: overviewPath }, { query: { enabled: computed(() => isHackathon.value && !!activityId.value), refetchInterval: LIVE_REFRESH_MS } })
const overview2 = useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueOverviewEndpoint2({ path: overviewPath }, { query: { enabled: computed(() => !isHackathon.value && !!activityId.value), refetchInterval: LIVE_REFRESH_MS } })
const overviewQuery = computed(() => isHackathon.value ? overview1 : overview2)
const overview = computed(() => overviewQuery.value.data.value)
const isLoadingOverview = computed(() => overviewQuery.value.isLoading.value)
const isFetchingOverview = computed(() => overviewQuery.value.isFetching.value)
const overviewError = computed(() => overviewQuery.value.error.value)
const updatedAt = computed(() => overviewQuery.value.dataUpdatedAt.value)

const historyUserId = ref('')
const historyPath = computed(() => ({ activityId: activityId.value, participantUserId: historyUserId.value }))
const history1 = useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueHistoryEndpoint1({ path: historyPath }, { query: { enabled: computed(() => isHackathon.value && !!activityId.value && !!historyUserId.value) } })
const history2 = useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueHistoryEndpoint2({ path: historyPath }, { query: { enabled: computed(() => !isHackathon.value && !!activityId.value && !!historyUserId.value) } })
const historyQuery = computed(() => isHackathon.value ? history1 : history2)

const checkIn1 = useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueCheckInEndpoint1()
const checkIn2 = useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueCheckInEndpoint2()
const checkOut1 = useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueCheckOutEndpoint1()
const checkOut2 = useGeeksHackingPortalApiEndpointsOrganizersHackathonVenueCheckOutEndpoint2()

function refresh() {
  const overviewKey = isHackathon.value
    ? geeksHackingPortalApiEndpointsOrganizersHackathonVenueOverviewEndpoint1QueryKey({ path: { activityId: activityId.value } })
    : geeksHackingPortalApiEndpointsOrganizersHackathonVenueOverviewEndpoint2QueryKey({ path: { activityId: activityId.value } })
  queryClient.invalidateQueries({ queryKey: overviewKey })
  if (historyUserId.value) {
    const path = { path: { activityId: activityId.value, participantUserId: historyUserId.value } }
    queryClient.invalidateQueries({
      queryKey: isHackathon.value
        ? geeksHackingPortalApiEndpointsOrganizersHackathonVenueHistoryEndpoint1QueryKey(path)
        : geeksHackingPortalApiEndpointsOrganizersHackathonVenueHistoryEndpoint2QueryKey(path),
    })
  }
}

async function checkIn(userId: string) {
  const path = { activityId: activityId.value, participantUserId: userId }
  const result = isHackathon.value ? await checkIn1.mutateAsync({ path }) : await checkIn2.mutateAsync({ path })
  refresh()
  return result
}

async function checkOut(userId: string) {
  const path = { activityId: activityId.value, participantUserId: userId }
  const result = isHackathon.value ? await checkOut1.mutateAsync({ path }) : await checkOut2.mutateAsync({ path })
  refresh()
  return result
}

// ---- counts and list ----------------------------------------------------------------------------
const participants = computed<Participant[]>(() => overview.value?.participants ?? [])
const auditTrail = computed(() =>
  [...(overview.value?.auditTrail ?? [])].sort((a, b) =>
    (parseHackathonDateTimeValue(b.timestamp)?.getTime() ?? 0) - (parseHackathonDateTimeValue(a.timestamp)?.getTime() ?? 0)),
)

function participantById(userId: string) {
  return participants.value.find(p => p.userId === userId)
}

// a scanned person who isn't in the overview: fetch their name
const scannedUserId = ref('')
const detail1 = useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsGetEndpoint({ path: computed(() => ({ hackathonId: activityId.value, userId: scannedUserId.value })) }, { query: { enabled: computed(() => isHackathon.value && !!scannedUserId.value && !participantById(scannedUserId.value)), retry: false } })
const detail2 = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsGetEndpoint({ path: computed(() => ({ standaloneWorkshopId: activityId.value, userId: scannedUserId.value })) }, { query: { enabled: computed(() => !isHackathon.value && !!scannedUserId.value && !participantById(scannedUserId.value)), retry: false } })
const scannedDetailName = computed(() => (isHackathon.value ? detail1.data.value?.name : detail2.data.value?.name) ?? '')

type Presence = 'here' | 'left' | 'absent'
function presenceOf(p: Participant | undefined): Presence {
  if (p?.isCurrentlyCheckedIn)
    return 'here'
  return p?.totalCheckIns ? 'left' : 'absent'
}

const counts = computed(() => {
  const c = { here: 0, left: 0, absent: 0 }
  for (const p of participants.value)
    c[presenceOf(p)]++
  return c
})

const searchQuery = ref('')
const presenceFilter = ref<'all' | Presence>('all')
const presenceFilters = computed(() => [
  { label: `All ${participants.value.length}`, value: 'all' },
  { label: `Here ${counts.value.here}`, value: 'here' },
  { label: `Left ${counts.value.left}`, value: 'left' },
  { label: `Not arrived ${counts.value.absent}`, value: 'absent' },
])
const presenceFilterModel = computed({
  get: () => presenceFilter.value as string,
  set: value => (presenceFilter.value = value as typeof presenceFilter.value),
})

const visibleParticipants = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()
  const order: Record<Presence, number> = { here: 0, left: 1, absent: 2 }
  return participants.value
    .filter(p => presenceFilter.value === 'all' || presenceOf(p) === presenceFilter.value)
    .filter(p => !query || (p.userName ?? '').toLowerCase().includes(query))
    .sort((a, b) => order[presenceOf(a)] - order[presenceOf(b)]
      || (a.userName ?? '').localeCompare(b.userName ?? '', undefined, { sensitivity: 'base' }))
})

const presenceBadge: Record<Presence, { label: string, tone: 'ok' | 'neutral', icon?: string }> = {
  here: { label: 'Here', tone: 'ok', icon: 'material-symbols:check-circle-rounded' },
  left: { label: 'Left', tone: 'neutral', icon: 'material-symbols:logout-rounded' },
  absent: { label: 'Not arrived', tone: 'neutral' },
}

// event-day times: "Sat 10:32 AM SGT"
const timeFormatter = new Intl.DateTimeFormat(undefined, { weekday: 'short', hour: 'numeric', minute: '2-digit', timeZone: HACKATHON_TIME_ZONE })
function formatTime(value: Date | string | null | undefined) {
  const date = parseHackathonDateTimeValue(value)
  return date ? `${timeFormatter.format(date)} ${HACKATHON_TIME_ZONE_LABEL}` : ''
}
function lastActivity(p: Participant) {
  switch (presenceOf(p)) {
    case 'here': return `In since ${formatTime(p.lastCheckInTime)}`
    case 'left': return `Left ${formatTime(p.lastCheckOutTime)}`
    default: return 'No check-ins yet'
  }
}
const updatedLabel = computed(() => updatedAt.value
  ? new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit', second: '2-digit' }).format(new Date(updatedAt.value))
  : '')

// list actions: only the row being changed shows a spinner
const pendingUserId = ref<string | null>(null)
async function toggleFromList(p: Participant) {
  if (!p.userId || pendingUserId.value)
    return
  pendingUserId.value = p.userId
  const here = p.isCurrentlyCheckedIn
  try {
    if (here)
      await checkOut(p.userId)
    else
      await checkIn(p.userId)
    toast.add({ title: here ? `${p.userName ?? 'Participant'} checked out` : `${p.userName ?? 'Participant'} checked in`, tone: 'ok' })
  }
  catch (error) {
    console.error('Check-in/out from list failed', error)
    toast.add({ title: here ? 'Not checked out' : 'Not checked in', description: getApiErrorMessage(error, 'Try again in a moment.'), tone: 'err' })
  }
  finally {
    pendingUserId.value = null
  }
}

const auditLimit = ref(15)

// ---- history dialog -----------------------------------------------------------------------------
const isHistoryOpen = ref(false)
const historyName = ref('')

function openHistory(userId: string, name: string) {
  historyUserId.value = userId
  historyName.value = name
  isHistoryOpen.value = true
}

const historyEvents = computed(() => {
  const entries = historyQuery.value.data.value?.history ?? []
  return entries.flatMap((entry) => {
    const events = [{ action: 'Checked in', timestamp: entry.checkInTime }]
    if (entry.checkOutTime)
      events.push({ action: 'Checked out', timestamp: entry.checkOutTime })
    return events
  }).sort((a, b) => (parseHackathonDateTimeValue(b.timestamp)?.getTime() ?? 0) - (parseHackathonDateTimeValue(a.timestamp)?.getTime() ?? 0))
})

// ---- scanner ------------------------------------------------------------------------------------
const isScannerOpen = ref(false)
const scannerElementId = `qr-reader-${useId()}`
const isCameraOn = ref(false)
const cameraError = ref('')
const scanError = ref('')
const rawQrData = ref('')
const cameras = ref<Array<{ id: string, label: string }>>([])
const useFrontCamera = ref(false)
const scanAction = ref<{ ok: boolean, title: string, message: string } | null>(null)
const isActing = ref(false)

const scanned = computed(() => participantById(scannedUserId.value))
const scannedName = computed(() => scanned.value?.userName || scannedDetailName.value || 'Participant')
const scannedIsUnknown = computed(() => !!scannedUserId.value && !isLoadingOverview.value && !scanned.value)

let qr: import('html5-qrcode').Html5Qrcode | null = null

/** Pull the participant out of a QR code: our JSON payloads ({ userId, … }) or a bare id. */
function readQr(decodedText: string) {
  const text = decodedText.trim()
  let id = text
  let otherEvent = false
  try {
    const data = JSON.parse(text) as Record<string, unknown>
    id = String(data.userId ?? data.participantUserId ?? data.registrationId ?? data.participantId ?? '').trim()
    // a workshop code carries its workshop: refuse codes for another event
    const eventId = data.workshopId ?? data.hackathonId
    otherEvent = typeof eventId === 'string' && !!eventId && eventId !== activityId.value
  }
  catch {
    // not JSON: treat the whole text as an id
  }
  if (!id)
    throw new Error('This QR code has no participant in it.')
  if (otherEvent)
    throw new Error('This QR code is for a different event.')
  const match = participants.value.find(p => p.userId === id || p.participantId === id)
  return match?.userId ?? id
}

async function waitForElement(id: string) {
  for (let i = 0; i < 60 && !document.getElementById(id); i++)
    await new Promise(resolve => requestAnimationFrame(resolve))
  return document.getElementById(id)
}

async function startCamera() {
  cameraError.value = ''
  scanError.value = ''
  rawQrData.value = ''
  if (!(await waitForElement(scannerElementId)) || !isScannerOpen.value)
    return
  try {
    const { Html5Qrcode } = await import('html5-qrcode')
    qr = new Html5Qrcode(scannerElementId)
    if (!cameras.value.length)
      cameras.value = await Html5Qrcode.getCameras().catch(() => [])
    await qr.start(
      { facingMode: useFrontCamera.value ? 'user' : 'environment' },
      {
        fps: 30,
        // scan most of the picture, not a small square, so codes don't have to be lined up exactly
        qrbox: (width: number, height: number) => {
          const side = Math.max(160, Math.floor(Math.min(width, height) * 0.7))
          return { width: side, height: side }
        },
        aspectRatio: 1,
      },
      (decodedText) => {
        void onScan(decodedText)
      },
      () => {},
    )
    isCameraOn.value = true
  }
  catch (error) {
    console.error('Failed to start camera', error)
    cameraError.value = 'Allow camera access for this site, then try again. You can also check people in from the list.'
    isCameraOn.value = false
  }
}

async function stopCamera() {
  isCameraOn.value = false
  const current = qr
  qr = null
  if (!current)
    return
  try {
    await current.stop()
    current.clear()
  }
  catch (error) {
    console.error('Error stopping scanner', error)
  }
}

async function onScan(decodedText: string) {
  rawQrData.value = decodedText
  await stopCamera()
  try {
    scannedUserId.value = readQr(decodedText)
  }
  catch (error) {
    scanError.value = (error as Error).message
  }
}

async function scanNext() {
  scannedUserId.value = ''
  scanAction.value = null
  await nextTick()
  // give the camera a moment to be released before restarting it
  await new Promise(resolve => setTimeout(resolve, 250))
  await startCamera()
}

async function switchCamera() {
  useFrontCamera.value = !useFrontCamera.value
  await stopCamera()
  await startCamera()
}

async function actOnScanned() {
  const userId = scannedUserId.value
  if (!userId || isActing.value)
    return
  const leaving = !!scanned.value?.isCurrentlyCheckedIn
  isActing.value = true
  scanAction.value = null
  try {
    const result = leaving ? await checkOut(userId) : await checkIn(userId)
    const at = formatTime(leaving
      ? (result as { checkOutTime?: string | null })?.checkOutTime
      : (result as { checkInTime?: string | null })?.checkInTime)
    scanAction.value = {
      ok: true,
      title: leaving ? `${scannedName.value} checked out` : `${scannedName.value} checked in`,
      message: at ? `At ${at}.` : '',
    }
  }
  catch (error) {
    console.error('Check-in/out from scan failed', error)
    scanAction.value = {
      ok: false,
      title: leaving ? 'Not checked out' : 'Not checked in',
      message: getApiErrorMessage(error, 'Check the code and try again.'),
    }
  }
  finally {
    isActing.value = false
  }
}

function openScanner() {
  scannedUserId.value = ''
  scanAction.value = null
  isScannerOpen.value = true
}

watch(isScannerOpen, (open) => {
  if (open) {
    void startCamera()
  }
  else {
    void stopCamera()
    scannedUserId.value = ''
    scanAction.value = null
    scanError.value = ''
    cameraError.value = ''
  }
})

onBeforeUnmount(() => {
  void stopCamera()
})
</script>

<template>
  <div class="cd">
    <!-- scan + live status -->
    <div class="cd-top">
      <GButton
        variant="primary"
        size="lg"
        icon="material-symbols:qr-code-scanner-rounded"
        label="Scan QR code"
        class="cd-top__scan"
        @click="openScanner"
      />
      <div class="cd-top__live">
        <p
          class="cd-top__updated"
          aria-live="polite"
        >
          <span
            class="cd-top__dot"
            :class="{ 'cd-top__dot--busy': isFetchingOverview }"
            aria-hidden="true"
          />
          <template v-if="updatedLabel">
            Live · updated {{ updatedLabel }}
          </template>
          <template v-else>
            Loading…
          </template>
        </p>
        <GButton
          variant="ghost"
          size="sm"
          icon="material-symbols:refresh-rounded"
          label="Refresh"
          :loading="isFetchingOverview"
          loading-label="Refreshing"
          @click="refresh"
        />
      </div>
    </div>

    <GAlert
      v-if="overviewError && !participants.length"
      tone="err"
      title="Couldn't load check-ins"
      :description="getApiErrorMessage(overviewError, 'Refresh to try again.')"
    />

    <div class="cd-stats">
      <GStatTile
        label="Here now"
        :value="counts.here"
        :hint="participants.length ? `of ${participants.length} participants` : undefined"
        icon="material-symbols:how-to-reg-rounded"
        highlight
        :loading="isLoadingOverview"
      />
      <GStatTile
        label="Left"
        :value="counts.left"
        icon="material-symbols:logout-rounded"
        :loading="isLoadingOverview"
      />
      <GStatTile
        label="Not arrived"
        :value="counts.absent"
        icon="material-symbols:schedule-rounded"
        :loading="isLoadingOverview"
      />
    </div>

    <!-- participants -->
    <section
      class="cd-section"
      aria-labelledby="cd-people-title"
    >
      <h2
        id="cd-people-title"
        class="cd-section__title"
      >
        Participants
      </h2>
      <div class="cd-filters">
        <GInput
          v-model="searchQuery"
          type="search"
          icon="material-symbols:search-rounded"
          placeholder="Search by name"
          aria-label="Search participants by name"
          class="cd-filters__search"
        />
        <GTabs
          v-model="presenceFilterModel"
          :items="presenceFilters"
          :content="false"
          size="sm"
          aria-label="Show participants"
        />
      </div>

      <div
        v-if="isLoadingOverview"
        class="cd-list"
        aria-busy="true"
      >
        <GSkeleton
          v-for="i in 5"
          :key="i"
          class="cd-skeleton"
        />
      </div>
      <GEmptyState
        v-else-if="!participants.length"
        icon="material-symbols:group-off-rounded"
        title="No participants yet"
        :description="kind === 'hackathon' ? 'Accepted participants show up here.' : 'People who register show up here.'"
        bordered
      />
      <GEmptyState
        v-else-if="!visibleParticipants.length"
        icon="material-symbols:search-off-rounded"
        title="No one matches"
        description="Try another name, or show everyone."
        bordered
      >
        <template #actions>
          <GButton
            variant="secondary"
            label="Show everyone"
            @click="searchQuery = ''; presenceFilter = 'all'"
          />
        </template>
      </GEmptyState>
      <ul
        v-else
        class="cd-list"
      >
        <li
          v-for="(p, index) in visibleParticipants"
          :key="p.userId ?? p.participantId ?? index"
          class="cd-row"
        >
          <div class="cd-row__who">
            <p class="cd-row__name">
              {{ p.userName || 'Unnamed participant' }}
            </p>
            <div class="cd-row__meta">
              <GBadge
                :tone="presenceBadge[presenceOf(p)].tone"
                :icon="presenceBadge[presenceOf(p)].icon"
                :label="presenceBadge[presenceOf(p)].label"
                size="sm"
              />
              <span>{{ lastActivity(p) }}</span>
            </div>
          </div>
          <div class="cd-row__actions">
            <GButton
              :variant="p.isCurrentlyCheckedIn ? 'secondary' : 'primary'"
              size="sm"
              :icon="p.isCurrentlyCheckedIn ? 'material-symbols:logout-rounded' : 'material-symbols:login-rounded'"
              :label="p.isCurrentlyCheckedIn ? 'Check out' : 'Check in'"
              :aria-label="`${p.isCurrentlyCheckedIn ? 'Check out' : 'Check in'} ${p.userName ?? 'participant'}`"
              :loading="pendingUserId === p.userId"
              :loading-label="p.isCurrentlyCheckedIn ? 'Checking out' : 'Checking in'"
              :disabled="!p.userId || (!!pendingUserId && pendingUserId !== p.userId)"
              @click="toggleFromList(p)"
            />
            <GButton
              variant="ghost"
              size="sm"
              icon="material-symbols:history-rounded"
              :aria-label="`Check-in history for ${p.userName ?? 'participant'}`"
              title="History"
              :disabled="!p.userId"
              @click="openHistory(p.userId ?? '', p.userName ?? 'Participant')"
            />
          </div>
        </li>
      </ul>
    </section>

    <!-- activity -->
    <section
      class="cd-section"
      aria-labelledby="cd-activity-title"
    >
      <h2
        id="cd-activity-title"
        class="cd-section__title"
      >
        Recent activity
      </h2>
      <p
        v-if="!auditTrail.length"
        class="cd-muted"
      >
        Check-ins and check-outs show up here as they happen.
      </p>
      <template v-else>
        <ol class="cd-feed">
          <li
            v-for="(event, index) in auditTrail.slice(0, auditLimit)"
            :key="`${event.userId ?? index}-${event.timestamp ?? index}`"
          >
            <span class="cd-feed__time">{{ formatTime(event.timestamp) }}</span>
            <span><strong>{{ event.userName ?? 'Participant' }}</strong> {{ event.action ?? 'updated' }}</span>
          </li>
        </ol>
        <GButton
          v-if="auditTrail.length > auditLimit"
          variant="ghost"
          size="sm"
          :label="`Show all ${auditTrail.length}`"
          class="self-start"
          @click="auditLimit = auditTrail.length"
        />
      </template>
    </section>
  </div>

  <!-- scanner -->
  <GModal
    v-model:open="isScannerOpen"
    title="Scan a QR code"
    :description="scannedUserId || scanError ? undefined : 'Point the camera at the participant\'s QR code.'"
    size="md"
  >
    <div class="cd-scan">
      <!-- camera -->
      <div
        v-show="!scannedUserId && !scanError"
        class="cd-scan__camera"
      >
        <div
          :id="scannerElementId"
          class="cd-scan__view"
        />
        <GAlert
          v-if="cameraError"
          tone="err"
          title="Camera unavailable"
          :description="cameraError"
        />
        <div class="flex flex-wrap justify-center gap-2">
          <GButton
            v-if="cameraError"
            variant="secondary"
            icon="material-symbols:refresh-rounded"
            label="Try again"
            @click="startCamera"
          />
          <GButton
            v-if="isCameraOn && cameras.length > 1"
            variant="ghost"
            icon="material-symbols:cameraswitch-rounded"
            label="Switch camera"
            @click="switchCamera"
          />
        </div>
      </div>

      <!-- unreadable code -->
      <template v-if="scanError">
        <GAlert
          tone="err"
          title="Can't use this code"
          :description="scanError"
        />
        <details
          v-if="rawQrData"
          class="cd-scan__raw"
        >
          <summary>Show what the code contains</summary>
          <pre>{{ rawQrData }}</pre>
        </details>
        <GButton
          variant="primary"
          size="lg"
          block
          icon="material-symbols:qr-code-scanner-rounded"
          label="Scan again"
          @click="scanNext"
        />
      </template>

      <!-- the scanned participant -->
      <template v-else-if="scannedUserId">
        <div class="cd-scan__person">
          <p class="cd-scan__name">
            {{ scannedName }}
          </p>
          <div class="cd-row__meta">
            <GBadge
              :tone="presenceBadge[presenceOf(scanned)].tone"
              :icon="presenceBadge[presenceOf(scanned)].icon"
              :label="presenceBadge[presenceOf(scanned)].label"
            />
            <span v-if="scanned">{{ lastActivity(scanned) }} · {{ scanned.totalCheckIns ?? 0 }} {{ scanned.totalCheckIns === 1 ? 'check-in' : 'check-ins' }}</span>
          </div>
        </div>

        <GAlert
          v-if="scannedIsUnknown && !scanAction"
          tone="attention"
          title="Not on the participant list"
          :description="kind === 'hackathon' ? 'Only accepted participants can be checked in. Check their status with the team.' : 'They may not be registered for this workshop.'"
        />

        <GAlert
          v-if="scanAction"
          :tone="scanAction.ok ? 'ok' : 'err'"
          :title="scanAction.title"
          :description="scanAction.message || undefined"
        />

        <div class="cd-scan__actions">
          <GButton
            v-if="!scanAction?.ok"
            :variant="scanned?.isCurrentlyCheckedIn ? 'secondary' : 'primary'"
            size="lg"
            block
            :icon="scanned?.isCurrentlyCheckedIn ? 'material-symbols:logout-rounded' : 'material-symbols:login-rounded'"
            :label="scanned?.isCurrentlyCheckedIn ? 'Check out' : 'Check in'"
            :loading="isActing"
            :loading-label="scanned?.isCurrentlyCheckedIn ? 'Checking out' : 'Checking in'"
            @click="actOnScanned"
          />
          <GButton
            :variant="scanAction?.ok ? 'primary' : 'ghost'"
            :size="scanAction?.ok ? 'lg' : 'md'"
            block
            icon="material-symbols:qr-code-scanner-rounded"
            label="Scan next"
            @click="scanNext"
          />
          <GButton
            variant="ghost"
            block
            icon="material-symbols:history-rounded"
            label="History"
            @click="openHistory(scannedUserId, scannedName)"
          />
        </div>
      </template>
    </div>
  </GModal>

  <!-- one participant's history -->
  <GModal
    v-model:open="isHistoryOpen"
    :title="historyName || 'Participant'"
    description="Check-in history, newest first."
    size="sm"
  >
    <GLoading
      v-if="historyQuery.isLoading.value"
      label="Loading history…"
    />
    <GAlert
      v-else-if="historyQuery.error.value"
      tone="err"
      title="Couldn't load the history"
      :description="getApiErrorMessage(historyQuery.error.value, 'Try again in a moment.')"
    />
    <ol
      v-else-if="historyEvents.length"
      class="cd-feed"
    >
      <li
        v-for="(event, index) in historyEvents"
        :key="`${event.timestamp}-${index}`"
      >
        <span class="cd-feed__time">{{ formatTime(event.timestamp) }}</span>
        <span>{{ event.action }}</span>
      </li>
    </ol>
    <p
      v-else
      class="cd-muted"
    >
      No check-ins yet.
    </p>
  </GModal>
</template>

<style scoped>
.cd {
  display: flex;
  flex-direction: column;
  gap: 24px;
  width: 100%;
  max-width: 64rem;
  margin: 0 auto;
}

/* scan button + live status */
.cd-top {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 12px 16px;
}
.cd-top__scan { flex: 1 1 16rem; }
@media (min-width: 640px) {
  .cd-top__scan { flex: 0 0 auto; }
}
.cd-top__live {
  display: flex;
  align-items: center;
  gap: 8px;
}
.cd-top__updated {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  margin: 0;
  font-size: 14px;
  color: var(--gh-text-muted);
  font-variant-numeric: tabular-nums;
}
.cd-top__dot {
  width: 8px;
  height: 8px;
  border-radius: 999px;
  background: var(--gh-ok);
}
.cd-top__dot--busy { animation: cd-pulse 1s ease-in-out infinite; }
@keyframes cd-pulse { 50% { opacity: 0.3; } }
@media (prefers-reduced-motion: reduce) {
  .cd-top__dot--busy { animation: none; }
}

.cd-stats {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
}
@media (max-width: 479px) {
  .cd-stats { grid-template-columns: minmax(0, 1fr) minmax(0, 1fr); }
  .cd-stats > :first-child { grid-column: 1 / -1; }
}

.cd-section {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.cd-section__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 22px;
  font-weight: 700;
}
.cd-muted {
  margin: 0;
  color: var(--gh-text-muted);
}
.cd-filters {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
/* the filter tabs scroll sideways on narrow phones rather than wrap */
.cd-filters :deep(.g-tabs) { max-width: 100%; overflow-x: auto; }

.cd-list {
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0;
  list-style: none;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.cd-list[aria-busy] {
  gap: 8px;
  padding: 12px;
}
.cd-skeleton {
  display: block;
  height: 48px;
}
.cd-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 8px 16px;
  padding: 12px 16px;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.cd-row:first-child { border-top: 0; }
.cd-row__who {
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 0;
  flex: 1 1 14rem;
}
.cd-row__name {
  margin: 0;
  font-weight: 700;
  overflow-wrap: anywhere;
}
.cd-row__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px 8px;
  font-size: 14px;
  color: var(--gh-text-muted);
}
.cd-row__actions {
  display: flex;
  align-items: center;
  gap: 4px;
}

.cd-feed {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.cd-feed li {
  display: grid;
  grid-template-columns: 9.5rem minmax(0, 1fr);
  gap: 12px;
  font-size: 14px;
}
.cd-feed__time {
  color: var(--gh-text-muted);
  font-variant-numeric: tabular-nums;
}
@media (max-width: 479px) {
  .cd-feed li { grid-template-columns: minmax(0, 1fr); gap: 0; }
}

/* scanner dialog */
.cd-scan {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.cd-scan__camera {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.cd-scan__view {
  width: 100%;
  min-height: 240px;
  overflow: hidden;
  border-radius: var(--radius-gh-sm);
  background: #000;
}
.cd-scan__view:empty { aspect-ratio: 1; min-height: 0; }
.cd-scan__person {
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.cd-scan__name {
  margin: 0;
  font-family: var(--font-display);
  font-size: 26px;
  font-weight: 700;
  line-height: 1.15;
  overflow-wrap: anywhere;
}
.cd-scan__actions {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.cd-scan__raw {
  font-size: 14px;
}
.cd-scan__raw summary { cursor: pointer; }
.cd-scan__raw pre {
  margin: 8px 0 0;
  padding: 8px;
  overflow: auto;
  border-radius: var(--radius-gh-sm);
  background: var(--gh-line-soft);
  white-space: pre-wrap;
  word-break: break-all;
}
</style>
