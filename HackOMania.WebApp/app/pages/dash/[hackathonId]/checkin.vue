<script setup lang="ts">
import { ref, watch, nextTick, onUnmounted, computed } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import { Html5Qrcode } from 'html5-qrcode'
import { useCheckInMutation } from '~/composables/venue'
import { participantOrganizerQueries } from '~/composables/participants'

const props = defineProps<{
  hackathonId: string
  isOrganizer: boolean
}>()

const isScannerOpen = ref(false)
const scannedUserId = ref<string>('')
const error = ref<string>('')
const isScanning = ref(false)
const rawQrData = ref<string>('')
const scanResult = ref<{ success: boolean, message: string, userId?: string, name?: string } | null>(null)
let autoScanTimer: ReturnType<typeof setTimeout> | null = null

const checkInMutation = useCheckInMutation(props.hackathonId)

// Fetch participant details when we have a scanned user ID
const { data: participantDetail } = useQuery(
  computed(() => ({
    ...participantOrganizerQueries.detail(props.hackathonId, scannedUserId.value),
    enabled: !!scannedUserId.value,
  })),
)

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
          await stopScanner()
          await handleCheckIn(userId)
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
  if (autoScanTimer) {
    clearTimeout(autoScanTimer)
    autoScanTimer = null
  }
  await stopScanner()
  scannedUserId.value = ''
  error.value = ''
  rawQrData.value = ''
  isScannerOpen.value = false
}

const handleCheckIn = async (userId: string) => {
  scanResult.value = null

  try {
    const result = await checkInMutation.mutateAsync(userId)
    const participantName = participantDetail.value?.name || 'Unknown'

    scanResult.value = {
      success: true,
      message: result?.isCheckedIn
        ? `Participant successfully checked in at ${result.checkInTime?.toLocaleTimeString() || 'now'}`
        : 'Check-in processed',
      userId,
      name: participantName,
    }
  }
  catch (err) {
    console.error('Check-in error:', err)
    const participantName = participantDetail.value?.name || 'Unknown'

    scanResult.value = {
      success: false,
      message: (err as Error)?.message || 'Failed to check in participant. Please verify the QR code and try again.',
      userId,
      name: participantName,
    }
  }
}

const openScanner = () => {
  isScannerOpen.value = true
  scanResult.value = null
}

const resetAndScanAgain = async () => {
  if (autoScanTimer) {
    clearTimeout(autoScanTimer)
    autoScanTimer = null
  }
  scanResult.value = null
  scannedUserId.value = ''
  error.value = ''
  rawQrData.value = ''
  await nextTick()
  // Add a small delay to ensure camera is fully released
  await new Promise(resolve => setTimeout(resolve, 300))
  await startScanner()
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

// Auto-scan after showing result for 2 seconds
watch(scanResult, (newValue) => {
  if (newValue) {
    // Clear any existing timer
    if (autoScanTimer) {
      clearTimeout(autoScanTimer)
    }
    // Set new timer to auto-scan after 2 seconds
    autoScanTimer = setTimeout(() => {
      resetAndScanAgain()
    }, 2000)
  }
})

onUnmounted(() => {
  if (autoScanTimer) {
    clearTimeout(autoScanTimer)
  }
  stopScanner()
})
</script>

<template>
  <div>
    <UCard>
      <template #header>
        <div class="flex flex-col gap-3">
          <div class="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <div>
              <h3 class="text-sm font-semibold">
                Check-In Scanner
              </h3>
              <p class="text-xs text-(--ui-text-muted) mt-1">
                Scan participant QR codes to check them in
              </p>
            </div>
            <UButton
              icon="i-lucide-qr-code"
              size="sm"
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
                v-if="checkInMutation.isPending.value"
                class="flex items-center justify-center p-4"
              >
                <div class="flex items-center gap-2">
                  <UIcon
                    name="i-lucide-loader-2"
                    class="animate-spin"
                  />
                  <span class="text-sm text-(--ui-text-muted)">Processing check-in...</span>
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
                      {{ scanResult.success ? 'Check-in Successful!' : 'Check-in Failed' }}
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
                    <div
                      v-if="scanResult.name || scanResult.userId"
                      :class="[
                        'text-xs mt-2',
                        scanResult.success
                          ? 'text-green-600 dark:text-green-400'
                          : 'text-red-600 dark:text-red-400',
                      ]"
                    >
                      <p
                        v-if="scanResult.name"
                        class="font-medium"
                      >
                        {{ scanResult.name }}
                      </p>
                      <p
                        v-if="scanResult.userId"
                        class="font-mono"
                      >
                        User ID: {{ scanResult.userId }}
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </UCard>
      </template>
    </UModal>
  </div>
</template>
