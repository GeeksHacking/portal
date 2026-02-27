<script setup lang="ts">
import { ref, watch, nextTick, onUnmounted, computed } from 'vue'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { Html5Qrcode } from 'html5-qrcode'
import { useCheckInMutation, useCheckOutMutation, venueHistoryQueries, venueOverviewQueries } from '~/composables/venue'
import { participantOrganizerQueries } from '~/composables/participants'
import type { HackOManiaApiEndpointsOrganizersHackathonVenueOverviewParticipantCheckInDto } from '~/api-client/models'

const route = useRoute()
const props = withDefaults(defineProps<{
  hackathonId?: string
}>(), {
  hackathonId: '',
})
const hackathonId = computed(() => props.hackathonId || (route.params.hackathonId as string | undefined) || '')

const isScannerOpen = ref(false)
const scannedUserId = ref<string>('')
const error = ref<string>('')
const isScanning = ref(false)
const rawQrData = ref<string>('')
const scanResult = ref<{ success: boolean, message: string } | null>(null)
const selectedParticipantUserId = ref<string>('')
const selectedParticipantName = ref<string>('')
const isHistoryModalOpen = ref(false)

const checkInMutation = useCheckInMutation(hackathonId.value)
const checkOutMutation = useCheckOutMutation(hackathonId.value)
const queryClient = useQueryClient()

// Fetch participant details when we have a scanned user ID
const { data: participantDetail } = useQuery(
  computed(() => ({
    ...participantOrganizerQueries.detail(hackathonId.value, scannedUserId.value),
    enabled: !!scannedUserId.value,
  })),
)

const { data: participantHistory, isLoading: isLoadingParticipantHistory } = useQuery(
  computed(() => ({
    ...venueHistoryQueries.participant(hackathonId.value, selectedParticipantUserId.value),
    enabled: !!selectedParticipantUserId.value && !!hackathonId.value,
  })),
)

// Live check-in history
const { data: venueOverview, isLoading: isLoadingOverview, dataUpdatedAt } = useQuery(
  computed(() => ({
    ...venueOverviewQueries.overview(hackathonId.value),
    enabled: !!hackathonId.value,
  })),
)

type ParticipantCheckInDto = HackOManiaApiEndpointsOrganizersHackathonVenueOverviewParticipantCheckInDto
type VenueAuditTrailItem = {
  participantId?: string
  userId?: string
  userName?: string
  action?: string
  timestamp?: string
}

const historySearchQuery = ref('')

const allParticipants = computed<ParticipantCheckInDto[]>(() => venueOverview.value?.participants ?? [])
const auditTrail = computed<VenueAuditTrailItem[]>(
  () => ((venueOverview.value as { auditTrail?: VenueAuditTrailItem[] } | undefined)?.auditTrail ?? []),
)

const checkedInCount = computed(() => allParticipants.value.filter(p => p.isCurrentlyCheckedIn).length)
const checkedOutCount = computed(() => allParticipants.value.length - checkedInCount.value)

const normalizedHistorySearch = computed(() => historySearchQuery.value.trim().toLowerCase())

const filteredParticipants = computed(() => {
  const query = normalizedHistorySearch.value
  const sorted = [...allParticipants.value].sort((a, b) => {
    // Checked-in participants first
    if (a.isCurrentlyCheckedIn !== b.isCurrentlyCheckedIn) {
      return a.isCurrentlyCheckedIn ? -1 : 1
    }
    return (a.userName ?? '').localeCompare(b.userName ?? '', undefined, { sensitivity: 'base' })
  })
  if (!query) return sorted
  return sorted.filter(p => (p.userName ?? '').toLowerCase().includes(query))
})

const selectedParticipant = computed(() =>
  allParticipants.value.find(p => p.userId === selectedParticipantUserId.value),
)

const participantHistoryEvents = computed(() => {
  const entries = participantHistory.value?.history ?? []
  return entries.flatMap((entry) => {
    const events = [
      {
        message: `${selectedParticipantName.value || participantHistory.value?.userName || 'Participant'} checked in`,
        timestamp: entry.checkInTime,
      },
    ]
    if (entry.checkOutTime) {
      events.push({
        message: `${selectedParticipantName.value || participantHistory.value?.userName || 'Participant'} checked out`,
        timestamp: entry.checkOutTime,
      })
    }
    return events
  }).sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())
})

const checkInTimeFormatter = new Intl.DateTimeFormat(undefined, {
  dateStyle: 'medium',
  timeStyle: 'short',
  timeZone: 'Asia/Singapore',
})

function formatCheckInTime(date: Date | null | undefined) {
  if (!date) return '—'
  return `${checkInTimeFormatter.format(date)} SGT`
}

function refreshOverview() {
  queryClient.invalidateQueries({ queryKey: ['hackathons', hackathonId.value, 'venue', 'overview'] })
  if (selectedParticipantUserId.value) {
    queryClient.invalidateQueries({ queryKey: ['hackathons', hackathonId.value, 'venue', 'history', selectedParticipantUserId.value] })
  }
}

let html5QrCode: Html5Qrcode | null = null

const startScanner = async () => {
  error.value = ''
  scannedUserId.value = ''
  scanResult.value = null

  try {
    html5QrCode = new Html5Qrcode('qr-reader')

    await html5QrCode.start(
      { facingMode: 'environment' }, // Use back camera
      {
        fps: 30,
        qrbox: { width: 250, height: 250 },
        aspectRatio: 1.0,
      },
      async (decodedText) => {
        // Handle successful scan
        isScanning.value = false
        rawQrData.value = decodedText

        console.log('QR Code detected! Raw data:', decodedText)

        try {
          console.log('Attempting to parse JSON...')
          const trimmedText = decodedText.trim()
          const data = JSON.parse(trimmedText)
          console.log('Parsed data:', data)

          const userId = data.userId
          console.log('Extracted userId:', userId)

          if (!userId) {
            throw new Error('QR code does not contain a userId')
          }

          scannedUserId.value = userId
          selectedParticipantUserId.value = userId
          const matchedParticipant = allParticipants.value.find(p => p.userId === userId)
          selectedParticipantName.value = matchedParticipant?.userName ?? ''
          await stopScanner()
        }
        catch (parseError) {
          console.error('QR code parse error:', parseError)
          console.error('Failed to parse:', decodedText)
          error.value = `Invalid QR code format. Raw data: ${decodedText.substring(0, 100)}`
          await stopScanner()
        }
      },
      (_errorMessage) => {
        // Handle scan errors (too frequent, can be ignored)
      },
    )
    // Set isScanning to true only after camera has started
    isScanning.value = true
  }
  catch (err) {
    error.value = 'Failed to start camera. Please allow camera access.'
    isScanning.value = false
    console.error(err)
  }
}

const stopScanner = async () => {
  isScanning.value = false
  if (html5QrCode) {
    try {
      await html5QrCode.stop()
      html5QrCode.clear()
    }
    catch (err) {
      console.error('Error stopping scanner:', err)
    }
    finally {
      html5QrCode = null
    }
  }
}

const closeScanner = async () => {
  await stopScanner()
  scannedUserId.value = ''
  error.value = ''
  rawQrData.value = ''
  selectedParticipantUserId.value = ''
  selectedParticipantName.value = ''
  isScannerOpen.value = false
}

const handleCheckIn = async () => {
  if (!selectedParticipantUserId.value)
    return
  scanResult.value = null

  try {
    const result = await checkInMutation.mutateAsync(selectedParticipantUserId.value)
    const participantName = selectedParticipantName.value || participantDetail.value?.name || 'Unknown'

    scanResult.value = {
      success: true,
      message: result?.isCheckedIn
        ? `Participant successfully checked in at ${result.checkInTime?.toLocaleTimeString() || 'now'}`
        : 'Check-in processed',
    }
    selectedParticipantName.value = participantName
    // Refresh the live overview after a successful check-in
    refreshOverview()
  }
  catch (err) {
    console.error('Check-in error:', err)
    const participantName = selectedParticipantName.value || participantDetail.value?.name || 'Unknown'

    scanResult.value = {
      success: false,
      message: (err as Error)?.message || 'Failed to check in participant. Please verify the QR code and try again.',
    }
    selectedParticipantName.value = participantName
  }
}

const handleCheckOut = async () => {
  if (!selectedParticipantUserId.value)
    return
  scanResult.value = null
  try {
    const result = await checkOutMutation.mutateAsync(selectedParticipantUserId.value)
    scanResult.value = {
      success: true,
      message: `Participant checked out at ${new Date(result.checkOutTime).toLocaleTimeString()}.`,
    }
    refreshOverview()
  }
  catch (err) {
    scanResult.value = {
      success: false,
      message: (err as Error)?.message || 'Failed to check out participant.',
    }
  }
}

const openScanner = () => {
  isScannerOpen.value = true
  scanResult.value = null
}

const resetAndScanAgain = async () => {
  scanResult.value = null
  scannedUserId.value = ''
  error.value = ''
  rawQrData.value = ''
  selectedParticipantUserId.value = ''
  selectedParticipantName.value = ''
  await nextTick()
  // Add a small delay to ensure camera is fully released
  await new Promise(resolve => setTimeout(resolve, 300))
  await startScanner()
}

function openHistory(userId: string, name: string) {
  selectedParticipantUserId.value = userId
  selectedParticipantName.value = name
  isHistoryModalOpen.value = true
}

watch(isScannerOpen, (newValue) => {
  if (newValue) {
    nextTick(() => {
      startScanner()
    })
  }
  else {
    stopScanner()
  }
})

onUnmounted(() => {
  stopScanner()
})
</script>

<template>
  <div class="space-y-4">
    <!-- Scanner Card -->
    <UCard>
      <template #header>
        <div class="flex flex-col gap-3">
          <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h3 class="text-sm font-semibold">
                Check-In Scanner
              </h3>
              <p class="text-xs text-(--ui-text-muted) mt-1">
                Scan participant QR codes and choose check-in/check-out actions
              </p>
            </div>
            <UButton
              icon="i-lucide-qr-code"
              size="sm"
              class="w-full sm:w-auto"
              @click="openScanner"
            >
              Open Scanner
            </UButton>
          </div>
        </div>
      </template>

      <div class="text-sm text-(--ui-text-muted)">
        <p class="mt-2">
          Participants should present their unique QR code for check-in.
        </p>
      </div>
    </UCard>

    <!-- Live Check-In History -->
    <UCard>
      <template #header>
        <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h3 class="text-sm font-semibold">
              Live Check-In History
            </h3>
            <p class="text-xs text-(--ui-text-muted) mt-1">
              <span v-if="!isLoadingOverview">
                {{ checkedInCount }} checked in · {{ checkedOutCount }} checked out · {{ allParticipants.length }} total
                <span
                  v-if="dataUpdatedAt"
                  class="ml-1"
                >
                  · Updated {{ new Date(dataUpdatedAt).toLocaleTimeString() }}
                </span>
              </span>
              <span v-else>Loading...</span>
            </p>
          </div>
          <UButton
            icon="i-lucide-refresh-cw"
            size="sm"
            variant="soft"
            color="neutral"
            class="w-full sm:w-auto"
            :loading="isLoadingOverview"
            @click="refreshOverview"
          >
            Refresh
          </UButton>
        </div>
      </template>

      <div class="space-y-3">
        <!-- Search -->
        <UInput
          v-model="historySearchQuery"
          icon="i-lucide-search"
          placeholder="Search by participant name..."
          size="sm"
        />

        <!-- Loading state -->
        <div
          v-if="isLoadingOverview && !allParticipants.length"
          class="text-sm text-(--ui-text-muted) py-4 text-center"
        >
          Loading check-in history...
        </div>

        <!-- Empty state -->
        <div
          v-else-if="!isLoadingOverview && !allParticipants.length"
          class="text-sm text-(--ui-text-muted) py-4 text-center"
        >
          No participants found.
        </div>

        <!-- No search results -->
        <div
          v-else-if="filteredParticipants.length === 0"
          class="text-sm text-(--ui-text-muted) py-4 text-center"
        >
          No participants matching "{{ historySearchQuery }}".
        </div>

        <div
          v-else
          class="overflow-x-auto"
        >
          <table class="w-full text-sm">
            <thead>
              <tr class="text-left text-(--ui-text-muted)">
                <th class="pb-2 pr-3 font-medium">
                  Participant
                </th>
                <th class="pb-2 pr-3 font-medium">
                  Status
                </th>
                <th class="pb-2 pr-3 font-medium">
                  Last activity
                </th>
                <th class="pb-2 text-right font-medium">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody class="divide-y divide-(--ui-border)">
              <tr
                v-for="(participant, index) in filteredParticipants"
                :key="participant.userId ?? participant.participantId ?? index"
              >
                <td class="py-2.5 pr-3 font-medium">
                  {{ participant.userName }}
                </td>
                <td class="py-2.5 pr-3">
                  <UBadge
                    :color="participant.isCurrentlyCheckedIn ? 'success' : 'neutral'"
                    variant="soft"
                    size="xs"
                  >
                    {{ participant.isCurrentlyCheckedIn ? 'Checked In' : 'Checked Out' }}
                  </UBadge>
                </td>
                <td class="py-2.5 pr-3 text-(--ui-text-muted)">
                  {{
                    participant.isCurrentlyCheckedIn
                      ? formatCheckInTime(participant.lastCheckInTime)
                      : formatCheckInTime(participant.lastCheckOutTime)
                  }}
                </td>
                <td class="py-2.5 text-right">
                  <UButton
                    size="xs"
                    variant="ghost"
                    @click="openHistory(participant.userId ?? '', participant.userName ?? 'Participant')"
                  >
                    View history
                  </UButton>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </UCard>

    <UCard>
      <template #header>
        <h3 class="text-sm font-semibold">
          Live Audit Trail
        </h3>
      </template>
      <div
        v-if="!auditTrail.length"
        class="text-sm text-(--ui-text-muted)"
      >
        No check-in events yet.
      </div>
      <ul
        v-else
        class="space-y-2 text-sm"
      >
        <li
          v-for="(event, index) in auditTrail"
          :key="`${event.userId ?? index}-${event.timestamp ?? index}`"
          class="text-(--ui-text-muted)"
        >
          <span class="font-medium text-(--ui-text)">{{ event.userName ?? 'Participant' }}</span>
          {{ event.action ?? 'updated status' }} at {{ formatCheckInTime(event.timestamp ? new Date(event.timestamp) : null) }}
        </li>
      </ul>
    </UCard>

    <!-- Scanner Modal -->
    <UModal v-model:open="isScannerOpen">
      <template #content>
        <UCard>
          <template #header>
            <div class="flex items-center justify-between">
              <h3 class="text-base font-semibold">
                Scan Participant QR Code
              </h3>
              <UButton
                variant="ghost"
                icon="i-lucide-x"
                size="xs"
                @click="closeScanner"
              />
            </div>
          </template>

          <div class="space-y-4">
            <!-- QR Scanner View -->
            <div
              v-if="!scanResult"
              class="space-y-4"
            >
              <div class="relative">
                <div
                  id="qr-reader"
                  class="w-full rounded-lg overflow-hidden"
                />
              </div>

              <div
                v-if="error"
                class="p-4 bg-red-100 dark:bg-red-900/20 rounded-lg space-y-2"
              >
                <p class="text-sm text-red-800 dark:text-red-200">
                  {{ error }}
                </p>
                <details
                  v-if="rawQrData"
                  class="text-xs"
                >
                  <summary class="cursor-pointer text-red-700 dark:text-red-300">
                    Show raw QR data
                  </summary>
                  <pre class="mt-2 p-2 bg-red-50 dark:bg-red-950 rounded overflow-auto text-red-900 dark:text-red-100">{{ rawQrData }}</pre>
                </details>
              </div>

              <div
                v-if="checkInMutation.isPending.value || checkOutMutation.isPending.value"
                class="flex items-center justify-center p-4"
              >
                <div class="flex items-center gap-2">
                  <UIcon
                    name="i-lucide-loader-2"
                    class="animate-spin"
                  />
                  <span class="text-sm text-(--ui-text-muted)">Processing action...</span>
                </div>
              </div>

              <div
                v-if="selectedParticipantUserId && !scanResult"
                class="rounded-lg border border-(--ui-border) p-3 space-y-3"
              >
                <p class="text-sm">
                  <span class="font-medium">{{ selectedParticipantName || participantDetail?.name || selectedParticipant?.userName || 'Participant' }}</span>
                  <span class="text-(--ui-text-muted) ml-1">({{ selectedParticipantUserId }})</span>
                </p>
                <div class="flex flex-wrap items-center gap-2">
                  <UBadge
                    :color="selectedParticipant?.isCurrentlyCheckedIn ? 'success' : 'neutral'"
                    variant="soft"
                    size="xs"
                  >
                    {{ selectedParticipant?.isCurrentlyCheckedIn ? 'Currently checked in' : 'Currently checked out' }}
                  </UBadge>
                  <span class="text-xs text-(--ui-text-muted)">
                    Check-ins: {{ selectedParticipant?.totalCheckIns ?? 0 }}
                  </span>
                </div>
                <div class="flex flex-wrap gap-2">
                  <UButton
                    size="sm"
                    :disabled="selectedParticipant?.isCurrentlyCheckedIn"
                    :loading="checkInMutation.isPending.value"
                    @click="handleCheckIn"
                  >
                    Check In
                  </UButton>
                  <UButton
                    size="sm"
                    color="warning"
                    variant="soft"
                    :disabled="!selectedParticipant?.isCurrentlyCheckedIn"
                    :loading="checkOutMutation.isPending.value"
                    @click="handleCheckOut"
                  >
                    Check Out
                  </UButton>
                  <UButton
                    size="sm"
                    color="neutral"
                    variant="soft"
                    @click="openHistory(selectedParticipantUserId, selectedParticipantName || participantDetail?.name || selectedParticipant?.userName || 'Participant')"
                  >
                    View History
                  </UButton>
                  <UButton
                    size="sm"
                    variant="ghost"
                    @click="resetAndScanAgain"
                  >
                    Scan another
                  </UButton>
                </div>
              </div>
            </div>

            <!-- Scan Result View -->
            <div
              v-else
              class="space-y-4"
            >
              <div
                :class="[
                  'p-4 rounded-lg',
                  scanResult.success
                    ? 'bg-green-100 dark:bg-green-900/20'
                    : 'bg-red-100 dark:bg-red-900/20',
                ]"
              >
                <div class="flex items-start gap-3">
                  <UIcon
                    :name="scanResult.success ? 'i-lucide-check-circle' : 'i-lucide-x-circle'"
                    :class="[
                      'w-5 h-5 flex-shrink-0 mt-0.5',
                      scanResult.success
                        ? 'text-green-800 dark:text-green-200'
                        : 'text-red-800 dark:text-red-200',
                    ]"
                  />
                  <div class="flex-1">
                    <p
                      :class="[
                        'text-sm font-medium',
                        scanResult.success
                          ? 'text-green-800 dark:text-green-200'
                          : 'text-red-800 dark:text-red-200',
                      ]"
                    >
                      {{ scanResult.success ? 'Action Successful!' : 'Action Failed' }}
                    </p>
                    <p
                      :class="[
                        'text-sm mt-1',
                        scanResult.success
                          ? 'text-green-700 dark:text-green-300'
                          : 'text-red-700 dark:text-red-300',
                      ]"
                    >
                      {{ scanResult.message }}
                    </p>
                    <div class="mt-3 flex gap-2">
                      <UButton
                        size="xs"
                        variant="soft"
                        @click="scanResult = null"
                      >
                        Back
                      </UButton>
                      <UButton
                        size="xs"
                        variant="ghost"
                        @click="resetAndScanAgain"
                      >
                        Scan another
                      </UButton>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </UCard>
      </template>
    </UModal>

    <UModal v-model:open="isHistoryModalOpen">
      <template #content>
        <UCard>
          <template #header>
            <div class="flex items-center justify-between">
              <h3 class="text-base font-semibold">
                {{ selectedParticipantName || participantHistory?.userName || 'Participant' }} · Check-In History
              </h3>
              <UButton
                variant="ghost"
                icon="i-lucide-x"
                size="xs"
                @click="isHistoryModalOpen = false"
              />
            </div>
          </template>
          <div
            v-if="isLoadingParticipantHistory"
            class="text-sm text-(--ui-text-muted)"
          >
            Loading history...
          </div>
          <ul
            v-else-if="participantHistoryEvents.length"
            class="space-y-2 text-sm"
          >
            <li
              v-for="(event, index) in participantHistoryEvents"
              :key="`${event.timestamp}-${index}`"
              class="text-(--ui-text-muted)"
            >
              <span class="text-(--ui-text)">{{ event.message }}</span>
              <span class="ml-1">at {{ formatCheckInTime(event.timestamp ? new Date(event.timestamp) : null) }}</span>
            </li>
          </ul>
          <div
            v-else
            class="text-sm text-(--ui-text-muted)"
          >
            No history available.
          </div>
        </UCard>
      </template>
    </UModal>
  </div>
</template>
