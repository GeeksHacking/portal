<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsListEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import * as XLSX from 'xlsx'

const route = useRoute()
const standaloneWorkshopId = computed(() => (route.params.standaloneWorkshopId as string | undefined) ?? '')

const { data: eventsData } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint()
const { data: participantsData, isLoading } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsListEndpoint(
  standaloneWorkshopId,
  { query: { enabled: computed(() => !!standaloneWorkshopId.value) } },
)

const event = computed(() => eventsData.value?.standaloneWorkshops?.find(item => item.id === standaloneWorkshopId.value))
const participants = computed(() => participantsData.value?.participants ?? [])

function createFilename(name: string, extension: 'json' | 'xlsx') {
  const date = new Date().toISOString().split('T')[0]
  const slug = (event.value?.title ?? 'standalone-workshop')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '')

  return `${slug}-${name}-${date}.${extension}`
}

function downloadJSON() {
  const blob = new Blob([JSON.stringify(participants.value, null, 2)], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = createFilename('participants', 'json')
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}

function flattenParticipants() {
  return participants.value.map((participant) => {
    const row: Record<string, unknown> = {
      registrationId: participant.registrationId,
      userId: participant.userId,
      name: participant.name,
      email: participant.email,
      status: participant.status,
      registeredAt: participant.registeredAt,
      withdrawnAt: participant.withdrawnAt ?? '',
    }

    for (const submission of participant.registrationSubmissions ?? []) {
      const key = submission.questionText ?? submission.questionId ?? 'Question'
      row[`Q: ${key}`] = submission.followUpValue
        ? `${submission.value ?? ''}\nFollow-up: ${submission.followUpValue}`
        : submission.value ?? ''
    }

    return row
  })
}

function downloadExcel() {
  const workbook = XLSX.utils.book_new()
  const worksheet = XLSX.utils.json_to_sheet(flattenParticipants())
  XLSX.utils.book_append_sheet(workbook, worksheet, 'Participants')
  XLSX.writeFile(workbook, createFilename('participants', 'xlsx'))
}

function downloadEmailsExcel() {
  const rows = participants.value.map(participant => ({
    Name: participant.name ?? '',
    Email: participant.email ?? '',
    Status: participant.status ?? '',
  }))
  const workbook = XLSX.utils.book_new()
  const worksheet = XLSX.utils.json_to_sheet(rows)
  XLSX.utils.book_append_sheet(workbook, worksheet, 'Emails')
  XLSX.writeFile(workbook, createFilename('participant-emails', 'xlsx'))
}
</script>

<template>
  <UDashboardPanel id="standalone-infopack">
    <template #header>
      <UDashboardNavbar title="Data Export">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="space-y-3">
        <UCard>
          <template #header>
            <div class="flex flex-col gap-1">
              <h3 class="text-sm font-semibold">
                {{ event?.title ?? 'Standalone Workshop' }}
              </h3>
              <p class="text-xs text-(--ui-text-muted)">
                Export participant registrations and registration responses.
              </p>
            </div>
          </template>

          <div class="space-y-4">
            <div class="rounded-lg border border-default bg-elevated p-4">
              <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <h4 class="text-sm font-medium">
                    Participants
                  </h4>
                  <p class="text-xs text-(--ui-text-muted)">
                    <template v-if="isLoading">
                      Loading...
                    </template>
                    <template v-else>
                      {{ participants.length }} record{{ participants.length === 1 ? '' : 's' }}
                    </template>
                  </p>
                </div>
                <div class="flex flex-wrap gap-2">
                  <UButton
                    size="xs"
                    icon="i-lucide-file-json"
                    :disabled="isLoading || !participants.length"
                    @click="downloadJSON"
                  >
                    JSON
                  </UButton>
                  <UButton
                    size="xs"
                    icon="i-lucide-file-spreadsheet"
                    :disabled="isLoading || !participants.length"
                    @click="downloadExcel"
                  >
                    Excel
                  </UButton>
                  <UButton
                    size="xs"
                    icon="i-lucide-mail"
                    :disabled="isLoading || !participants.length"
                    @click="downloadEmailsExcel"
                  >
                    Emails
                  </UButton>
                </div>
              </div>
            </div>
          </div>
        </UCard>
      </div>
    </template>
  </UDashboardPanel>
</template>
