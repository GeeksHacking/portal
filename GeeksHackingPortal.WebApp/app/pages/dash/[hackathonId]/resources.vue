<script setup lang="ts">
import type { OrganizerResourceAuditTrailItem, OrganizerResourceItem, OrganizerResourceOverviewParticipant } from '~/composables/resources'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { Html5Qrcode } from 'html5-qrcode'
import { computed, nextTick, onUnmounted, ref, watch } from 'vue'
import { participantOrganizerQueries } from '~/composables/participants'
import {

  resourceOrganizerQueries,
  useRedeemResourceMutation,
} from '~/composables/resources'
import { HACKATHON_TIME_ZONE, HACKATHON_TIME_ZONE_LABEL, parseHackathonDateTimeValue } from '~/utils/hackathon-date-time'

const props = withDefaults(defineProps<{
  hackathonId?: string
}>(), {
  hackathonId: '',
})

const route = useRoute()
const hackathonId = computed(() => props.hackathonId || (route.params.hackathonId as string | undefined) || '')

const selectedResourceId = ref('')
const selectedParticipantUserId = ref('')
const selectedParticipantName = ref('')
const historySearchQuery = ref('')
const isScannerOpen = ref(false)
const isHistoryModalOpen = ref(false)
const scannedUserId = ref('')
const error = ref('')
const rawQrData = ref('')
const scanResult = ref<{ success: boolean, message: string } | null>(null)
const availableCameras = ref<Array<{ id: string, label: string }>>([])
const useFrontCamera = ref(false)
const isScanning = ref(false)

const queryClient = useQueryClient()

const { data: resourcesData, isLoading: isLoadingResources } = useQuery(
  computed(() => ({
    ...resourceOrganizerQueries.list(hackathonId.value),
    enabled: !!hackathonId.value,
  })),
)

const resources = computed<OrganizerResourceItem[]>(() => resourcesData.value?.resources ?? [])

watch(resources, (items) => {
  if (!items.length) {
    selectedResourceId.value = ''
    return
  }

  if (selectedResourceId.value && items.some(item => item.id === selectedResourceId.value))
    return

  selectedResourceId.value = items.find(item => item.isPublished)?.id ?? items[0]?.id ?? ''
}, { immediate: true })

const selectedResource = computed(() =>
  resources.value.find(resource => resource.id === selectedResourceId.value) ?? null,
)

const { data: resourceOverview, isLoading: isLoadingOverview, dataUpdatedAt } = useQuery(
  computed(() => ({
    ...resourceOrganizerQueries.overview(hackathonId.value, selectedResourceId.value),
    enabled: !!hackathonId.value && !!selectedResourceId.value,
  })),
)

const { data: participantDetail } = useQuery(
  computed(() => ({
    ...participantOrganizerQueries.detail(hackathonId.value, scannedUserId.value),
    enabled: !!scannedUserId.value && !!hackathonId.value,
  })),
)

const { data: participantHistory, isLoading: isLoadingParticipantHistory } = useQuery(
  computed(() => ({
    ...resourceOrganizerQueries.participantHistory(
      hackathonId.value,
      selectedParticipantUserId.value,
      selectedResourceId.value,
    ),
    enabled: !!hackathonId.value && !!selectedParticipantUserId.value && !!selectedResourceId.value,
  })),
)

const redeemMutation = useRedeemResourceMutation(hackathonId, selectedResourceId)

const participants = computed<OrganizerResourceOverviewParticipant[]>(() => resourceOverview.value?.participants ?? [])
const auditTrail = computed<OrganizerResourceAuditTrailItem[]>(() => resourceOverview.value?.auditTrail ?? [])
const totalRedemptions = computed(() => resourceOverview.value?.totalRedemptions ?? 0)
const uniqueRedeemers = computed(() => resourceOverview.value?.uniqueRedeemers ?? 0)
const neverRedeemedCount = computed(() => participants.value.filter(participant => !participant.hasRedeemed).length)

const normalizedHistorySearch = computed(() => historySearchQuery.value.trim().toLowerCase())
const filteredParticipants = computed(() => {
  const query = normalizedHistorySearch.value
  const sorted = [...participants.value].sort((a, b) => {
    if (a.hasRedeemed !== b.hasRedeemed)
      return a.hasRedeemed ? -1 : 1

    const timeDiff = (parseHackathonDateTimeValue(b.lastRedeemedAt)?.getTime() ?? 0)
      - (parseHackathonDateTimeValue(a.lastRedeemedAt)?.getTime() ?? 0)
    if (timeDiff !== 0)
      return timeDiff

    return (a.userName ?? '').localeCompare(b.userName ?? '', undefined, { sensitivity: 'base' })
  })

  if (!query)
    return sorted

  return sorted.filter(participant =>
    [participant.userName, participant.userId]
      .filter(Boolean)
      .join(' ')
      .toLowerCase()
      .includes(query),
  )
})

const selectedParticipant = computed(() =>
  participants.value.find(participant => participant.userId === selectedParticipantUserId.value) ?? null,
)

const participantHistoryEvents = computed(() =>
  (participantHistory.value?.history ?? [])
    .map(item => ({
      id: item.redemptionId,
      message: `${selectedParticipantName.value || participantHistory.value?.userName || 'Participant'} redeemed ${participantHistory.value?.resourceName || selectedResource.value?.name || 'resource'}`,
      timestamp: item.createdAt,
    }))
    .sort(
      (a, b) =>
        (parseHackathonDateTimeValue(b.timestamp)?.getTime() ?? 0)
        - (parseHackathonDateTimeValue(a.timestamp)?.getTime() ?? 0),
    ),
)

const resourceOptions = computed(() =>
  resources.value.map(resource => ({
    label: resource.isPublished ? resource.name : `${resource.name} (Unpublished)`,
    value: resource.id,
  })),
)

const redemptionTimeFormatter = new Intl.DateTimeFormat(undefined, {
  dateStyle: 'medium',
  timeStyle: 'short',
  timeZone: HACKATHON_TIME_ZONE,
})

function formatRedemptionTime(value: Date | string | null | undefined) {
  const date = parseHackathonDateTimeValue(value)
  if (!date)
    return '—'
  return `${redemptionTimeFormatter.format(date)} ${HACKATHON_TIME_ZONE_LABEL}`
}

function refreshOverview() {
  if (!selectedResourceId.value)
    return

  queryClient.invalidateQueries({
    queryKey: ['hackathons', hackathonId.value, 'resources', 'organizer', selectedResourceId.value, 'overview'],
  })

  if (selectedParticipantUserId.value) {
    queryClient.invalidateQueries({
      queryKey: ['hackathons', hackathonId.value, 'resources', 'organizer', selectedResourceId.value, 'history', selectedParticipantUserId.value],
    })
  }
}

let html5QrCode: Html5Qrcode | null = null

async function startScanner() {
  error.value = ''
  scannedUserId.value = ''
  scanResult.value = null

  try {
    html5QrCode = new Html5Qrcode('resource-qr-reader')

    if (!availableCameras.value.length) {
      try {
        availableCameras.value = await Html5Qrcode.getCameras()
      }
      catch {
        // If camera enumeration fails, fall back to facingMode constraint
      }
    }

    const cameraConfig = { facingMode: useFrontCamera.value ? 'user' : 'environment' }

    await html5QrCode.start(
      cameraConfig,
      {
        fps: 30,
        qrbox: { width: 250, height: 250 },
        aspectRatio: 1,
      },
      async (decodedText) => {
        rawQrData.value = decodedText

        try {
          const payload = JSON.parse(decodedText.trim())
          const userId = payload.userId

          if (!userId)
            throw new Error('QR code does not contain a userId')

          scannedUserId.value = userId
          selectedParticipantUserId.value = userId
          const matchedParticipant = participants.value.find(participant => participant.userId === userId)
          selectedParticipantName.value = matchedParticipant?.userName ?? ''
          await stopScanner()
        }
        catch (parseError) {
          console.error('QR parse error:', parseError)
          error.value = `Invalid QR code format. Raw data: ${decodedText.substring(0, 100)}`
          await stopScanner()
        }
      },
      () => {},
    )
    isScanning.value = true
  }
  catch (err) {
    error.value = 'Failed to start camera. Please allow camera access.'
    console.error(err)
  }
}

async function switchCamera() {
  if (availableCameras.value.length <= 1)
    return
  useFrontCamera.value = !useFrontCamera.value
  await stopScanner()
  await nextTick()
  await startScanner()
}

async function stopScanner() {
  isScanning.value = false
  if (!html5QrCode)
    return

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

async function closeScanner() {
  await stopScanner()
  scannedUserId.value = ''
  selectedParticipantUserId.value = ''
  selectedParticipantName.value = ''
  error.value = ''
  rawQrData.value = ''
  isScannerOpen.value = false
}

function openScanner() {
  if (!selectedResourceId.value)
    return

  scanResult.value = null
  isScannerOpen.value = true
}

async function resetAndScanAgain() {
  scanResult.value = null
  scannedUserId.value = ''
  selectedParticipantUserId.value = ''
  selectedParticipantName.value = ''
  error.value = ''
  rawQrData.value = ''
  await nextTick()
  await new Promise(resolve => setTimeout(resolve, 300))
  await startScanner()
}

function openHistory(userId: string, name: string) {
  selectedParticipantUserId.value = userId
  selectedParticipantName.value = name
  isHistoryModalOpen.value = true
}

async function redeemForParticipant(userId: string, participantName: string) {
  if (!selectedResourceId.value)
    return

  scanResult.value = null
  selectedParticipantUserId.value = userId
  selectedParticipantName.value = participantName

  try {
    const result = await redeemMutation.mutateAsync(userId)
    scanResult.value = {
      success: true,
      message: `${participantName || 'Participant'} redeemed ${selectedResource.value?.name || 'resource'} at ${formatRedemptionTime(result.createdAt)}.`,
    }
    refreshOverview()
  }
  catch (err) {
    scanResult.value = {
      success: false,
      message: (err as Error)?.message || 'Failed to redeem resource for participant.',
    }
  }
}

async function handleRedeem() {
  const participantName = selectedParticipantName.value || participantDetail.value?.name || selectedParticipant.value?.userName || 'Participant'
  await redeemForParticipant(selectedParticipantUserId.value, participantName)
}

async function handleRedeemFromList(userId: string, participantName: string) {
  await redeemForParticipant(userId, participantName)
}

watch(isScannerOpen, (isOpen) => {
  if (isOpen) {
    nextTick(() => {
      startScanner()
    })
  }
  else {
    stopScanner()
  }
})

watch(selectedResourceId, () => {
  selectedParticipantUserId.value = ''
  selectedParticipantName.value = ''
  scanResult.value = null
})

onUnmounted(() => {
  stopScanner()
})
</script>

<template>
  <UDashboardPanel id="resources">
    <template #header>
      <UDashboardNavbar title="Resources">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="space-y-3">
        <UCard>
          <template #header>
            <div class="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
              <div>
                <h3 class="text-sm font-semibold">
                  Resource Redemption Scanner
                </h3>
                <p class="mt-1 text-xs text-(--ui-text-muted)">
                  Pick a resource, scan a participant QR code, and redeem on their behalf.
                </p>
              </div>
              <UButton
                icon="i-lucide-qr-code"
                size="sm"
                class="w-full lg:w-auto"
                :disabled="!selectedResourceId || !selectedResource?.isPublished"
                @click="openScanner"
              >
                Open Scanner
              </UButton>
            </div>
          </template>

          <div class="grid gap-4 lg:grid-cols-[minmax(0,20rem)_1fr] lg:items-start">
            <UFormField label="Resource">
              <USelect
                :model-value="selectedResourceId"
                :items="resourceOptions"
                size="sm"
                class="w-full"
                :loading="isLoadingResources"
                @update:model-value="selectedResourceId = String($event || '')"
              />
            </UFormField>

            <div
              v-if="selectedResource"
              class="rounded-xl border border-(--ui-border) bg-(--ui-bg-elevated) p-4"
            >
              <div class="flex flex-wrap items-center gap-2">
                <h4 class="text-sm font-semibold">
                  {{ selectedResource.name }}
                </h4>
                <UBadge
                  :color="selectedResource.isPublished ? 'success' : 'warning'"
                  variant="soft"
                  size="xs"
                >
                  {{ selectedResource.isPublished ? 'Published' : 'Unpublished' }}
                </UBadge>
              </div>
              <p class="mt-2 text-sm text-(--ui-text-muted)">
                {{ selectedResource.description || 'No description provided.' }}
              </p>
              <div class="mt-3 flex flex-wrap gap-4 text-xs text-(--ui-text-muted)">
                <span>{{ totalRedemptions }} total redemptions</span>
                <span>{{ uniqueRedeemers }} unique participants</span>
                <span>{{ neverRedeemedCount }} participants have not redeemed</span>
              </div>
              <UAlert
                v-if="!selectedResource.isPublished"
                class="mt-4"
                color="warning"
                variant="soft"
                title="This resource is unpublished"
                description="Organizers can still review redemption history, but new redemptions are blocked until the resource is published."
              />
            </div>
          </div>
        </UCard>

        <UCard>
          <template #header>
            <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h3 class="text-sm font-semibold">
                  Participant Redemption Status
                </h3>
                <p class="mt-1 text-xs text-(--ui-text-muted)">
                  <span v-if="selectedResourceId && !isLoadingOverview">
                    {{ totalRedemptions }} redemptions · {{ uniqueRedeemers }} participants redeemed
                    <span
                      v-if="dataUpdatedAt"
                      class="ml-1"
                    >
                      · Updated {{ new Date(dataUpdatedAt).toLocaleTimeString() }}
                    </span>
                  </span>
                  <span v-else-if="selectedResourceId">Loading...</span>
                  <span v-else>Select a resource to view redemption status.</span>
                </p>
              </div>
              <UButton
                icon="i-lucide-refresh-cw"
                size="sm"
                variant="soft"
                color="neutral"
                class="w-full sm:w-auto"
                :disabled="!selectedResourceId"
                :loading="isLoadingOverview"
                @click="refreshOverview"
              >
                Refresh
              </UButton>
            </div>
          </template>

          <div class="space-y-3">
            <UInput
              v-model="historySearchQuery"
              icon="i-lucide-search"
              placeholder="Search by participant name or user ID..."
              size="sm"
              :disabled="!selectedResourceId"
            />

            <div
              v-if="!selectedResourceId"
              class="py-4 text-center text-sm text-(--ui-text-muted)"
            >
              Select a resource to load the redemption table.
            </div>
            <div
              v-else-if="isLoadingOverview && !participants.length"
              class="py-4 text-center text-sm text-(--ui-text-muted)"
            >
              Loading participant redemption data...
            </div>
            <div
              v-else-if="!participants.length"
              class="py-4 text-center text-sm text-(--ui-text-muted)"
            >
              No participants found for this hackathon.
            </div>
            <div
              v-else-if="!filteredParticipants.length"
              class="py-4 text-center text-sm text-(--ui-text-muted)"
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
                      Last redeemed
                    </th>
                    <th class="pb-2 pr-3 font-medium">
                      Count
                    </th>
                    <th class="pb-2 text-right font-medium">
                      Actions
                    </th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-(--ui-border)">
                  <tr
                    v-for="participant in filteredParticipants"
                    :key="participant.userId"
                  >
                    <td class="py-2.5 pr-3">
                      <div class="font-medium">
                        {{ participant.userName }}
                      </div>
                      <div class="text-xs text-(--ui-text-muted)">
                        {{ participant.userId }}
                      </div>
                    </td>
                    <td class="py-2.5 pr-3">
                      <UBadge
                        :color="participant.hasRedeemed ? 'success' : 'neutral'"
                        variant="soft"
                        size="xs"
                      >
                        {{ participant.hasRedeemed ? 'Redeemed before' : 'Not redeemed yet' }}
                      </UBadge>
                    </td>
                    <td class="py-2.5 pr-3 text-(--ui-text-muted)">
                      {{ formatRedemptionTime(participant.lastRedeemedAt) }}
                    </td>
                    <td class="py-2.5 pr-3">
                      {{ participant.redemptionCount }}
                    </td>
                    <td class="py-2.5 text-right">
                      <div class="flex items-center justify-end gap-1">
                        <UButton
                          size="xs"
                          color="success"
                          variant="soft"
                          :disabled="!selectedResource?.isPublished"
                          :loading="redeemMutation.isPending.value"
                          @click="handleRedeemFromList(participant.userId, participant.userName)"
                        >
                          Redeem
                        </UButton>
                        <UButton
                          size="xs"
                          variant="ghost"
                          @click="openHistory(participant.userId, participant.userName)"
                        >
                          View history
                        </UButton>
                      </div>
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
              Live Redemption Trail
            </h3>
          </template>
          <div
            v-if="!selectedResourceId"
            class="text-sm text-(--ui-text-muted)"
          >
            Select a resource to view recent redemption activity.
          </div>
          <div
            v-else-if="!auditTrail.length"
            class="text-sm text-(--ui-text-muted)"
          >
            No redemption activity yet.
          </div>
          <ul
            v-else
            class="space-y-2 text-sm"
          >
            <li
              v-for="(event, index) in auditTrail"
              :key="`${event.redemptionId}-${index}`"
              class="text-(--ui-text-muted)"
            >
              <span class="font-medium text-(--ui-text)">{{ event.userName || 'Participant' }}</span>
              redeemed <span class="text-(--ui-text)">{{ selectedResource?.name || resourceOverview?.resourceName || 'resource' }}</span>
              at {{ formatRedemptionTime(event.timestamp) }}
            </li>
          </ul>
        </UCard>

        <UModal v-model:open="isScannerOpen">
          <template #content>
            <UCard>
              <template #header>
                <div class="flex items-center justify-between">
                  <div>
                    <h3 class="text-base font-semibold">
                      Scan Participant QR Code
                    </h3>
                    <p class="mt-1 text-xs text-(--ui-text-muted)">
                      Redeeming {{ selectedResource?.name || 'resource' }}
                    </p>
                  </div>
                  <UButton
                    variant="ghost"
                    icon="i-lucide-x"
                    size="xs"
                    @click="closeScanner"
                  />
                </div>
              </template>

              <div class="space-y-4">
                <div
                  v-if="!scanResult"
                  class="space-y-4"
                >
                  <div
                    id="resource-qr-reader"
                    class="w-full overflow-hidden rounded-lg"
                  />

                  <div
                    v-if="isScanning && availableCameras.length > 1"
                    class="flex justify-center"
                  >
                    <UButton
                      variant="ghost"
                      icon="i-lucide-switch-camera"
                      size="sm"
                      @click="switchCamera"
                    >
                      Switch Camera
                    </UButton>
                  </div>

                  <div
                    v-if="error"
                    class="space-y-2 rounded-lg bg-red-100 p-4 dark:bg-red-900/20"
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
                      <pre class="mt-2 overflow-auto rounded bg-red-50 p-2 text-red-900 dark:bg-red-950 dark:text-red-100">{{ rawQrData }}</pre>
                    </details>
                  </div>

                  <div
                    v-if="redeemMutation.isPending.value"
                    class="flex items-center justify-center p-4"
                  >
                    <div class="flex items-center gap-2">
                      <UIcon
                        name="i-lucide-loader-2"
                        class="animate-spin"
                      />
                      <span class="text-sm text-(--ui-text-muted)">Processing redemption...</span>
                    </div>
                  </div>

                  <div
                    v-if="selectedParticipantUserId && !scanResult"
                    class="space-y-3 rounded-lg border border-(--ui-border) p-3"
                  >
                    <p class="text-sm">
                      <span class="font-medium">{{ selectedParticipantName || participantDetail?.name || selectedParticipant?.userName || 'Participant' }}</span>
                      <span class="ml-1 text-(--ui-text-muted)">({{ selectedParticipantUserId }})</span>
                    </p>
                    <div class="flex flex-wrap items-center gap-2">
                      <UBadge
                        :color="selectedParticipant?.hasRedeemed ? 'success' : 'neutral'"
                        variant="soft"
                        size="xs"
                      >
                        {{ selectedParticipant?.hasRedeemed ? 'Redeemed before' : 'First redemption' }}
                      </UBadge>
                      <span class="text-xs text-(--ui-text-muted)">
                        Redemptions: {{ selectedParticipant?.redemptionCount ?? 0 }}
                      </span>
                      <span class="text-xs text-(--ui-text-muted)">
                        Last redeemed: {{ formatRedemptionTime(selectedParticipant?.lastRedeemedAt) }}
                      </span>
                    </div>
                    <div class="flex flex-wrap gap-2">
                      <UButton
                        size="sm"
                        :disabled="!selectedResource?.isPublished"
                        :loading="redeemMutation.isPending.value"
                        @click="handleRedeem"
                      >
                        Redeem Resource
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

                <div
                  v-else
                  class="space-y-4"
                >
                  <div
                    class="rounded-lg p-4"
                    :class="[
                      scanResult.success ? 'bg-green-100 dark:bg-green-900/20' : 'bg-red-100 dark:bg-red-900/20',
                    ]"
                  >
                    <div class="flex items-start gap-3">
                      <UIcon
                        :name="scanResult.success ? 'i-lucide-check-circle' : 'i-lucide-x-circle'"
                        class="mt-0.5 h-5 w-5 flex-shrink-0"
                        :class="[
                          scanResult.success ? 'text-green-800 dark:text-green-200' : 'text-red-800 dark:text-red-200',
                        ]"
                      />
                      <div class="flex-1">
                        <p
                          class="text-sm font-medium"
                          :class="[
                            scanResult.success ? 'text-green-800 dark:text-green-200' : 'text-red-800 dark:text-red-200',
                          ]"
                        >
                          {{ scanResult.success ? 'Redemption Successful' : 'Redemption Failed' }}
                        </p>
                        <p
                          class="mt-1 text-sm"
                          :class="[
                            scanResult.success ? 'text-green-700 dark:text-green-300' : 'text-red-700 dark:text-red-300',
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
                  <div>
                    <h3 class="text-base font-semibold">
                      {{ selectedParticipantName || participantHistory?.userName || 'Participant' }} · {{ participantHistory?.resourceName || selectedResource?.name || 'Resource' }}
                    </h3>
                    <p class="mt-1 text-xs text-(--ui-text-muted)">
                      {{
                        participantHistory?.hasRedeemed
                          ? `${participantHistory.redemptionCount} redemption${participantHistory.redemptionCount === 1 ? '' : 's'}`
                          : 'No prior redemptions'
                      }}
                    </p>
                  </div>
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
                  v-for="event in participantHistoryEvents"
                  :key="event.id"
                  class="text-(--ui-text-muted)"
                >
                  <span class="text-(--ui-text)">{{ event.message }}</span>
                  <span class="ml-1">at {{ formatRedemptionTime(event.timestamp) }}</span>
                </li>
              </ul>
              <div
                v-else
                class="text-sm text-(--ui-text-muted)"
              >
                No redemption history available.
              </div>
            </UCard>
          </template>
        </UModal>
      </div>
    </template>
  </UDashboardPanel>
</template>
