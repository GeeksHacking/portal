<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsParticipantItem,
  GeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsVenueCheckInItem,
} from '@geekshacking/portal-sdk'
import {
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsGetEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsListEndpoint,
} from '@geekshacking/portal-sdk/hooks'

type Participant = GeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsParticipantItem
type CheckIn = GeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsVenueCheckInItem

const route = useRoute()
const standaloneWorkshopId = computed(() => (route.params.standaloneWorkshopId as string | undefined) ?? '')
const searchQuery = ref('')
const selectedUserId = ref<string | null>(null)
const isHistoryOpen = ref(false)

const { data: participantsData, isLoading } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsListEndpoint(
  standaloneWorkshopId,
  { query: { enabled: computed(() => !!standaloneWorkshopId.value) } },
)

const { data: participantDetail, isLoading: isLoadingDetail } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsGetEndpoint(
  standaloneWorkshopId,
  computed(() => selectedUserId.value ?? undefined),
  { query: { enabled: computed(() => !!standaloneWorkshopId.value && !!selectedUserId.value) } },
)

const participants = computed<Participant[]>(() => participantsData.value?.participants ?? [])
const activeParticipants = computed(() => participants.value.filter(participant => participant.status !== 'Withdrawn'))
const selectedCheckIns = computed<CheckIn[]>(() => participantDetail.value?.venueCheckIns ?? [])

const filteredParticipants = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()
  const sorted = [...activeParticipants.value].sort((a, b) => (a.name ?? '').localeCompare(b.name ?? ''))

  if (!query)
    return sorted

  return sorted.filter(participant =>
    [participant.name, participant.email, participant.userId]
      .filter(Boolean)
      .join(' ')
      .toLowerCase()
      .includes(query),
  )
})

function formatDateTime(value: string | null | undefined) {
  if (!value)
    return '—'

  const date = new Date(value)
  if (Number.isNaN(date.getTime()))
    return '—'

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
    timeZone: 'Asia/Singapore',
  }).format(date)
}

function openHistory(userId: string | undefined) {
  if (!userId)
    return

  selectedUserId.value = userId
  isHistoryOpen.value = true
}
</script>

<template>
  <UDashboardPanel id="standalone-checkin">
    <template #header>
      <UDashboardNavbar title="Check Ins">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="space-y-3">
        <UAlert
          color="warning"
          variant="soft"
          title="Scanner actions are not available for standalone workshops yet"
          description="This page exposes participant check-in history that is currently returned by the organizer participant API. Check-in and check-out mutations need standalone workshop API endpoints before the QR scanner can be enabled."
        />

        <UCard>
          <template #header>
            <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h3 class="text-sm font-semibold">
                  Participant Check-In History
                </h3>
                <p class="mt-1 text-xs text-(--ui-text-muted)">
                  {{ activeParticipants.length }} active participant{{ activeParticipants.length === 1 ? '' : 's' }}
                </p>
              </div>
              <UInput
                v-model="searchQuery"
                icon="i-lucide-search"
                placeholder="Search participants"
                class="w-full sm:max-w-xs"
              />
            </div>
          </template>

          <div
            v-if="isLoading"
            class="py-4 text-center text-sm text-(--ui-text-muted)"
          >
            Loading participants...
          </div>
          <div
            v-else-if="!filteredParticipants.length"
            class="py-4 text-center text-sm text-(--ui-text-muted)"
          >
            No participants found.
          </div>
          <div
            v-else
            class="divide-y divide-(--ui-border)"
          >
            <div
              v-for="participant in filteredParticipants"
              :key="participant.userId ?? participant.registrationId"
              class="flex flex-col gap-3 py-3 sm:flex-row sm:items-center sm:justify-between"
            >
              <div class="min-w-0">
                <p class="text-sm font-medium">
                  {{ participant.name || participant.email || 'Participant' }}
                </p>
                <p class="text-xs text-(--ui-text-muted)">
                  {{ participant.email || participant.userId }}
                </p>
              </div>
              <UButton
                size="xs"
                variant="ghost"
                icon="i-lucide-clock"
                @click="openHistory(participant.userId)"
              >
                View history
              </UButton>
            </div>
          </div>
        </UCard>

        <UModal
          v-model:open="isHistoryOpen"
          :title="participantDetail?.name ?? participantDetail?.email ?? 'Check-in history'"
        >
          <template #content>
            <UCard>
              <template #header>
                <div class="flex items-center justify-between gap-3">
                  <div>
                    <h3 class="text-base font-semibold">
                      {{ participantDetail?.name ?? participantDetail?.email ?? 'Participant' }}
                    </h3>
                    <p class="text-xs text-(--ui-text-muted)">
                      {{ participantDetail?.email ?? selectedUserId }}
                    </p>
                  </div>
                  <UButton
                    variant="ghost"
                    icon="i-lucide-x"
                    size="xs"
                    @click="isHistoryOpen = false"
                  />
                </div>
              </template>

              <div
                v-if="isLoadingDetail"
                class="text-sm text-(--ui-text-muted)"
              >
                Loading history...
              </div>
              <div
                v-else-if="selectedCheckIns.length"
                class="space-y-2"
              >
                <div
                  v-for="checkIn in selectedCheckIns"
                  :key="checkIn.id ?? checkIn.checkInTime"
                  class="rounded-lg border border-default px-3 py-2 text-sm"
                >
                  <div class="flex items-center justify-between gap-2">
                    <span>Checked in {{ formatDateTime(checkIn.checkInTime) }}</span>
                    <UBadge
                      :color="checkIn.isCheckedIn ? 'success' : 'neutral'"
                      variant="soft"
                      size="xs"
                    >
                      {{ checkIn.isCheckedIn ? 'Active' : 'Closed' }}
                    </UBadge>
                  </div>
                  <p
                    v-if="checkIn.checkOutTime"
                    class="mt-1 text-xs text-(--ui-text-muted)"
                  >
                    Checked out {{ formatDateTime(checkIn.checkOutTime) }}
                  </p>
                </div>
              </div>
              <p
                v-else
                class="text-sm text-(--ui-text-muted)"
              >
                No check-ins recorded.
              </p>
            </UCard>
          </template>
        </UModal>
      </div>
    </template>
  </UDashboardPanel>
</template>
