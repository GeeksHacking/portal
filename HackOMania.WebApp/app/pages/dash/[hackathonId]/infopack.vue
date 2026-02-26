<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import * as XLSX from 'xlsx'
import { participantOrganizerQueries } from '~/composables/participants'
import { teamOrganizerQueries } from '~/composables/teams'

const props = defineProps<{
  hackathonId: string
  isOrganizer: boolean
}>()

// Cache keys for localStorage
const CACHE_KEY_PREFIX = 'infopack-count'
function getCacheKey(dataType: string) {
  return `${CACHE_KEY_PREFIX}-${props.hackathonId}-${dataType}`
}

// Get cached count from localStorage
function getCachedCount(dataType: string): number | null {
  if (typeof window === 'undefined') return null
  const cached = localStorage.getItem(getCacheKey(dataType))
  return cached ? Number.parseInt(cached, 10) : null
}

// Set cached count in localStorage
function setCachedCount(dataType: string, count: number) {
  if (typeof window === 'undefined') return
  localStorage.setItem(getCacheKey(dataType), count.toString())
}

// Fetch participants data
const { data: participantsData, isLoading: isLoadingParticipants } = useQuery(
  computed(() => ({
    ...participantOrganizerQueries.list(props.hackathonId),
    enabled: !!props.hackathonId && props.isOrganizer,
  })),
)

// Fetch teams data
const { data: teamsData, isLoading: isLoadingTeams } = useQuery(
  computed(() => ({
    ...teamOrganizerQueries.list(props.hackathonId),
    enabled: !!props.hackathonId && props.isOrganizer,
  })),
)

const participants = computed(() => participantsData.value?.participants ?? [])
const teams = computed(() => teamsData.value?.teams ?? [])

// Track if there's new data
const hasNewParticipants = ref(false)
const hasNewTeams = ref(false)

// Watch for changes in participants count
watch(() => participants.value.length, (newCount) => {
  if (newCount === 0) return
  const cachedCount = getCachedCount('participants')
  if (cachedCount !== null && cachedCount !== newCount) {
    hasNewParticipants.value = true
  }
}, { immediate: true })

// Watch for changes in teams count
watch(() => teams.value.length, (newCount) => {
  if (newCount === 0) return
  const cachedCount = getCachedCount('teams')
  if (cachedCount !== null && cachedCount !== newCount) {
    hasNewTeams.value = true
  }
}, { immediate: true })

function downloadJSON<T>(data: T[], filename: string) {
  const dataStr = JSON.stringify(data, null, 2)
  const blob = new Blob([dataStr], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = `${filename}-${new Date().toISOString().split('T')[0]}.json`
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}

interface SummaryStats {
  total: number
  approved: number
  rejected: number
  pending: number
}

// List of container fields that should be flattened without prefix
const CONTAINER_FIELDS = ['additionalData', 'reviews', 'metadata']

function downloadExcel<T>(data: T[], filename: string, stats?: SummaryStats) {
  const processedData: Record<string, unknown>[] = data.map((item) => {
    const record = item as Record<string, unknown>
    const processed: Record<string, unknown> = {}

    for (const [key, value] of Object.entries(record)) {
      // Check if this is a container field that should be flattened
      const isContainer = CONTAINER_FIELDS.includes(key)

      if (value === null || value === undefined) {
        if (!isContainer) {
          processed[key] = ''
        }
      }
      else if (Array.isArray(value)) {
        // Handle arrays
        if (value.length === 0) {
          if (!isContainer) {
            processed[key] = '[]'
          }
        }
        else if (key === 'registrationSubmissions') {
          // Special handling for registration submissions
          value.forEach((submission: unknown) => {
            const sub = submission as Record<string, unknown>
            const questionText = (sub.questionText as string) ?? 'Unknown Question'
            const questionId = (sub.questionId as string) ?? ''
            const rowName = `Q: ${questionText}${questionId ? ` (${questionId})` : ''}`

            const mainValue = (sub.value as string) ?? ''
            const followUp = (sub.followUpValue as string) ?? ''
            processed[rowName] = followUp
              ? `${mainValue}${mainValue ? '\n' : ''}Follow-up: ${followUp}`
              : mainValue
          })
        }
        else {
          // Generic array handling
          value.forEach((item: unknown, index: number) => {
            if (typeof item === 'object' && item !== null) {
              const obj = item as Record<string, unknown>
              // Look for identifier fields
              const identifier = (obj.name as string) || (obj.title as string) || (obj.id as string) || (obj.type as string) || `${index + 1}`

              // Add each property of the array item
              for (const [subKey, subValue] of Object.entries(obj)) {
                // Skip the identifier field itself
                if (subKey !== 'name' && subKey !== 'title' && subKey !== 'id' && subKey !== 'type') {
                  // Simplify field name for container fields
                  const fieldName = isContainer
                    ? `${subKey}[${identifier}]`
                    : `${key}[${identifier}].${subKey}`
                  processed[fieldName] = subValue ?? ''
                }
              }
            }
            else {
              const fieldName = isContainer ? `[${index}]` : `${key}[${index}]`
              processed[fieldName] = item
            }
          })
        }
      }
      else if (typeof value === 'object') {
        // Flatten nested objects
        if (isContainer) {
          // For container fields, flatten directly without prefix
          const obj = value as Record<string, unknown>
          for (const [subKey, subValue] of Object.entries(obj)) {
            if (subValue === null || subValue === undefined) {
              processed[subKey] = ''
            }
            else if (typeof subValue === 'object' && !Array.isArray(subValue)) {
              // Nested object - still flatten with dot notation
              processed[subKey] = JSON.stringify(subValue)
            }
            else if (Array.isArray(subValue)) {
              processed[subKey] = JSON.stringify(subValue)
            }
            else {
              processed[subKey] = subValue
            }
          }
        }
        else {
          // For non-container fields, use dot notation
          const obj = value as Record<string, unknown>
          for (const [subKey, subValue] of Object.entries(obj)) {
            processed[`${key}.${subKey}`] = subValue ?? ''
          }
        }
      }
      else {
        processed[key] = value
      }
    }

    return processed
  })

  const workbook = XLSX.utils.book_new()

  // Only transpose for participants, use normal layout for teams
  if (filename === 'participants') {
    // Transpose data: fields as rows, participants as columns
    const allFields = new Set<string>()
    processedData.forEach((record) => {
      Object.keys(record).forEach(key => allFields.add(key))
    })

    const transposedData: Record<string, unknown>[] = []

    // Add summary statistics if provided
    if (stats) {
      transposedData.push({ Field: '=== SUMMARY STATISTICS ===' })
      transposedData.push({ Field: 'Total Records', Value: stats.total })
      transposedData.push({ Field: 'Approved', Value: stats.approved })
      transposedData.push({ Field: 'Rejected', Value: stats.rejected })
      transposedData.push({ Field: 'Pending', Value: stats.pending })
      transposedData.push({ Field: '' }) // Empty row for spacing
      transposedData.push({ Field: '=== DATA ===' })
    }

    allFields.forEach((field) => {
      const row: Record<string, unknown> = { Field: field }
      processedData.forEach((record, index) => {
        row[`Participant ${index + 1}`] = record[field] ?? ''
      })
      transposedData.push(row)
    })

    const worksheet = XLSX.utils.json_to_sheet(transposedData)
    XLSX.utils.book_append_sheet(workbook, worksheet, 'Sheet1')
  }
  else {
    // Normal layout for teams: columns for fields, rows for teams
    const worksheet = XLSX.utils.json_to_sheet(processedData)
    XLSX.utils.book_append_sheet(workbook, worksheet, 'Sheet1')
  }

  XLSX.writeFile(workbook, `${filename}-${new Date().toISOString().split('T')[0]}.xlsx`)
}

function downloadParticipantsJSON() {
  downloadJSON(participants.value, 'participants')
  setCachedCount('participants', participants.value.length)
  hasNewParticipants.value = false
}

function downloadParticipantsExcel() {
  // Calculate statistics for participants
  const stats: SummaryStats = {
    total: participants.value.length,
    approved: participants.value.filter(p => p.concludedStatus === 'Accepted').length,
    rejected: participants.value.filter(p => p.concludedStatus === 'Rejected').length,
    pending: participants.value.filter(p => p.concludedStatus === 'Pending' || p.concludedStatus === null || p.concludedStatus === undefined).length,
  }

  downloadExcel(participants.value, 'participants', stats)
  setCachedCount('participants', participants.value.length)
  hasNewParticipants.value = false
}

function downloadTeamsJSON() {
  downloadJSON(teams.value, 'teams')
  setCachedCount('teams', teams.value.length)
  hasNewTeams.value = false
}

function downloadTeamsExcel() {
  downloadExcel(teams.value, 'teams')
  setCachedCount('teams', teams.value.length)
  hasNewTeams.value = false
}

function downloadParticipantEmailsExcel() {
  const data = participants.value.map(p => ({
    'Name': p.name ?? '',
    'Telegram Handle': p.registrationSubmissions?.find(s =>
      s.questionText?.toLowerCase().includes('telegram'),
    )?.value ?? '',
    'Status': p.concludedStatus ?? 'Pending',
    'Email': p.email ?? '',
  }))

  const workbook = XLSX.utils.book_new()
  const worksheet = XLSX.utils.json_to_sheet(data)
  XLSX.utils.book_append_sheet(workbook, worksheet, 'Participant Emails')
  XLSX.writeFile(workbook, `participant-emails-${new Date().toISOString().split('T')[0]}.xlsx`)
}
</script>

<template>
  <div>
    <UCard>
      <template #header>
        <div class="flex flex-col gap-2">
          <h3 class="text-sm font-semibold">
            Info Pack
          </h3>
          <p class="text-xs text-(--ui-text-muted)">
            Export useful data
          </p>
        </div>
      </template>

      <div class="space-y-4">
        <!-- Participants Section -->
        <div class="p-4 rounded-lg bg-elevated border border-default">
          <div class="flex items-center justify-between mb-3">
            <div class="flex items-center gap-2">
              <div>
                <div class="flex items-center gap-2">
                  <h4 class="text-sm font-medium">
                    Participants
                  </h4>
                  <UBadge
                    v-if="hasNewParticipants"
                    color="primary"
                    variant="subtle"
                    size="xs"
                  >
                    New
                  </UBadge>
                </div>
                <p class="text-xs text-(--ui-text-muted)">
                  <template v-if="isLoadingParticipants">
                    Loading...
                  </template>
                  <template v-else>
                    {{ participants.length }} record{{ participants.length !== 1 ? 's' : '' }}
                  </template>
                </p>
              </div>
            </div>
            <div class="flex gap-2">
              <UButton
                size="xs"
                icon="i-lucide-file-json"
                :disabled="isLoadingParticipants || !participants.length"
                @click="downloadParticipantsJSON"
              >
                JSON
              </UButton>
              <UButton
                size="xs"
                icon="i-lucide-file-spreadsheet"
                :disabled="isLoadingParticipants || !participants.length"
                @click="downloadParticipantsExcel"
              >
                Excel
              </UButton>
            </div>
          </div>
        </div>

        <!-- Participant Emails Section -->
        <div class="p-4 rounded-lg bg-elevated border border-default">
          <div class="flex items-center justify-between mb-3">
            <div class="flex items-center gap-2">
              <div>
                <h4 class="text-sm font-medium">
                  Participant Emails
                </h4>
                <p class="text-xs text-(--ui-text-muted)">
                  <template v-if="isLoadingParticipants">
                    Loading...
                  </template>
                  <template v-else>
                    {{ participants.length }} record{{ participants.length !== 1 ? 's' : '' }}
                  </template>
                </p>
              </div>
            </div>
            <UButton
              size="xs"
              icon="i-lucide-file-spreadsheet"
              :disabled="isLoadingParticipants || !participants.length"
              @click="downloadParticipantEmailsExcel"
            >
              Excel
            </UButton>
          </div>
        </div>

        <!-- Teams Section -->
        <div class="p-4 rounded-lg bg-elevated border border-default">
          <div class="flex items-center justify-between mb-3">
            <div class="flex items-center gap-2">
              <div>
                <div class="flex items-center gap-2">
                  <h4 class="text-sm font-medium">
                    Teams
                  </h4>
                  <UBadge
                    v-if="hasNewTeams"
                    color="primary"
                    variant="subtle"
                    size="xs"
                  >
                    New
                  </UBadge>
                </div>
                <p class="text-xs text-(--ui-text-muted)">
                  <template v-if="isLoadingTeams">
                    Loading...
                  </template>
                  <template v-else>
                    {{ teams.length }} record{{ teams.length !== 1 ? 's' : '' }}
                  </template>
                </p>
              </div>
            </div>
            <div class="flex gap-2">
              <UButton
                size="xs"
                icon="i-lucide-file-json"
                :disabled="isLoadingTeams || !teams.length"
                @click="downloadTeamsJSON"
              >
                JSON
              </UButton>
              <UButton
                size="xs"
                icon="i-lucide-file-spreadsheet"
                :disabled="isLoadingTeams || !teams.length"
                @click="downloadTeamsExcel"
              >
                Excel
              </UButton>
            </div>
          </div>
        </div>
      </div>
    </UCard>
  </div>
</template>
