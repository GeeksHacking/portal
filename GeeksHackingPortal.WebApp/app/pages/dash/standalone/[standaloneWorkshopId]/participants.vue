<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsParticipantItem,
  GeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsParticipantResponse,
  GeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsRegistrationSubmissionItem,
} from '@geekshacking/portal-sdk'
import {
  geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsGetEndpointQueryKey,
  geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsListEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsGetEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsWithdrawEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'

type ParticipantItem = GeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsParticipantItem
type ParticipantDetail = GeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsParticipantResponse
type RegistrationSubmission = GeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsRegistrationSubmissionItem
type FilterStatus = 'all' | 'active' | 'withdrawn'

const route = useRoute()
const toast = useToast()
const queryClient = useQueryClient()

const standaloneWorkshopId = computed(() => (route.params.standaloneWorkshopId as string | undefined) ?? '')
const searchQuery = ref('')
const activeFilter = ref<FilterStatus>('all')
const selectedUserId = ref<string | null>(null)
const isDetailOpen = ref(false)

const { data: eventsData } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint()
const { data: participantsData, isLoading } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsListEndpoint(
  standaloneWorkshopId,
)
const withdrawMutation = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsWithdrawEndpoint()

const event = computed(() => eventsData.value?.standaloneWorkshops?.find(item => item.id === standaloneWorkshopId.value))
const participants = computed<ParticipantItem[]>(() => participantsData.value?.participants ?? [])

const { data: participantDetail, isLoading: isLoadingDetail } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsGetEndpoint(
  standaloneWorkshopId,
  computed(() => selectedUserId.value ?? undefined),
  { query: { enabled: computed(() => !!standaloneWorkshopId.value && !!selectedUserId.value) } },
)

const filteredParticipants = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()

  return participants.value
    .filter((participant) => {
      if (activeFilter.value === 'active')
        return !participant.withdrawnAt
      if (activeFilter.value === 'withdrawn')
        return !!participant.withdrawnAt
      return true
    })
    .filter((participant) => {
      if (!query)
        return true

      const haystack = [
        participant.name,
        participant.email,
        participant.status,
        participant.userId,
      ]
        .filter(Boolean)
        .join(' ')
        .toLowerCase()

      if (haystack.includes(query))
        return true

      return (participant.registrationSubmissions ?? []).some(submission =>
        [submission.questionText, submission.value, submission.followUpValue]
          .filter(Boolean)
          .join(' ')
          .toLowerCase()
          .includes(query),
      )
    })
    .sort((a, b) => {
      const aTime = a.registeredAt ? new Date(a.registeredAt).getTime() : 0
      const bTime = b.registeredAt ? new Date(b.registeredAt).getTime() : 0
      return bTime - aTime
    })
})

const stats = computed(() => ({
  total: participants.value.length,
  active: participants.value.filter(participant => !participant.withdrawnAt).length,
  withdrawn: participants.value.filter(participant => !!participant.withdrawnAt).length,
  withResponses: participants.value.filter(participant => (participant.registrationSubmissions?.length ?? 0) > 0).length,
}))

const detailSubmissions = computed<RegistrationSubmission[]>(() => participantDetail.value?.registrationSubmissions ?? [])
const selectedParticipant = computed<ParticipantDetail | null>(() => participantDetail.value ?? null)

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

function getSubmissionValue(submission: RegistrationSubmission) {
  const value = submission.value?.trim()
  const followUpValue = submission.followUpValue?.trim()

  if (value && followUpValue)
    return `${value}\nFollow-up: ${followUpValue}`
  if (followUpValue)
    return `Follow-up: ${followUpValue}`
  return value || '—'
}

function openParticipant(userId: string | undefined) {
  if (!userId)
    return

  selectedUserId.value = userId
  isDetailOpen.value = true
}

function closeParticipant() {
  isDetailOpen.value = false
  selectedUserId.value = null
}

async function invalidateParticipants() {
  await queryClient.invalidateQueries({
    queryKey: geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsListEndpointQueryKey(standaloneWorkshopId.value),
  })

  if (selectedUserId.value) {
    await queryClient.invalidateQueries({
      queryKey: geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsGetEndpointQueryKey(
        standaloneWorkshopId.value,
        selectedUserId.value,
      ),
    })
  }
}

async function withdrawParticipant(participant: ParticipantItem) {
  if (!standaloneWorkshopId.value || !participant.userId || participant.withdrawnAt)
    return

  if (!confirm(`Withdraw ${participant.name ?? participant.email ?? 'this participant'} from the workshop?`))
    return

  try {
    await withdrawMutation.mutateAsync({
      standaloneWorkshopId: standaloneWorkshopId.value,
      userId: participant.userId,
    })
    await invalidateParticipants()
    toast.add({
      title: 'Participant withdrawn',
      color: 'success',
    })
  }
  catch (error) {
    console.error('Failed to withdraw participant', error)
    toast.add({
      title: 'Failed to withdraw participant',
      description: 'Please try again.',
      color: 'error',
    })
  }
}
</script>

<template>
  <UDashboardPanel id="standalone-participants">
    <template #header>
      <UDashboardNavbar title="Participants">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="space-y-4">
        <UCard>
          <div class="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
            <div class="space-y-1">
              <h2 class="text-lg font-semibold">
                {{ event?.title ?? 'Standalone Workshop' }}
              </h2>
              <p class="text-sm text-(--ui-text-muted)">
                Review registrations and manage workshop attendees.
              </p>
            </div>
            <UBadge
              :color="event?.isPublished ? 'success' : 'neutral'"
              variant="subtle"
            >
              {{ event?.isPublished ? 'Published' : 'Draft' }}
            </UBadge>
          </div>
        </UCard>

        <div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <UCard>
            <p class="text-sm text-(--ui-text-muted)">
              Total
            </p>
            <p class="mt-1 text-2xl font-semibold">
              {{ stats.total }}
            </p>
          </UCard>
          <UCard>
            <p class="text-sm text-(--ui-text-muted)">
              Active
            </p>
            <p class="mt-1 text-2xl font-semibold">
              {{ stats.active }}
            </p>
          </UCard>
          <UCard>
            <p class="text-sm text-(--ui-text-muted)">
              Withdrawn
            </p>
            <p class="mt-1 text-2xl font-semibold">
              {{ stats.withdrawn }}
            </p>
          </UCard>
          <UCard>
            <p class="text-sm text-(--ui-text-muted)">
              With Responses
            </p>
            <p class="mt-1 text-2xl font-semibold">
              {{ stats.withResponses }}
            </p>
          </UCard>
        </div>

        <UCard>
          <template #header>
            <div class="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
              <div class="flex flex-1 flex-col gap-3 sm:flex-row sm:items-center">
                <UInput
                  v-model="searchQuery"
                  icon="i-lucide-search"
                  placeholder="Search by name, email, status, or responses"
                  class="w-full sm:max-w-md"
                />
                <div class="flex flex-wrap gap-2">
                  <UButton
                    size="xs"
                    :variant="activeFilter === 'all' ? 'solid' : 'ghost'"
                    @click="activeFilter = 'all'"
                  >
                    All ({{ stats.total }})
                  </UButton>
                  <UButton
                    size="xs"
                    :variant="activeFilter === 'active' ? 'solid' : 'ghost'"
                    @click="activeFilter = 'active'"
                  >
                    Active ({{ stats.active }})
                  </UButton>
                  <UButton
                    size="xs"
                    :variant="activeFilter === 'withdrawn' ? 'solid' : 'ghost'"
                    color="warning"
                    @click="activeFilter = 'withdrawn'"
                  >
                    Withdrawn ({{ stats.withdrawn }})
                  </UButton>
                </div>
              </div>
            </div>
          </template>

          <div
            v-if="isLoading"
            class="text-sm text-(--ui-text-muted)"
          >
            Loading participants...
          </div>

          <div
            v-else-if="!participants.length"
            class="text-sm text-(--ui-text-muted)"
          >
            No participants yet.
          </div>

          <div
            v-else-if="!filteredParticipants.length"
            class="text-sm text-(--ui-text-muted)"
          >
            No participants match the current filters.
          </div>

          <div
            v-else
            class="divide-y divide-(--ui-border)"
          >
            <div
              v-for="participant in filteredParticipants"
              :key="participant.userId ?? participant.registrationId ?? participant.email ?? ''"
              class="flex flex-col gap-3 py-4 lg:flex-row lg:items-center lg:justify-between"
            >
              <div class="min-w-0 flex-1">
                <button
                  class="text-left text-sm font-medium text-highlighted hover:underline"
                  @click="openParticipant(participant.userId)"
                >
                  {{ participant.name ?? participant.email ?? participant.userId ?? 'Participant' }}
                </button>
                <div class="mt-1 flex flex-wrap gap-2">
                  <UBadge
                    variant="subtle"
                    :color="participant.withdrawnAt ? 'warning' : 'success'"
                    size="xs"
                  >
                    {{ participant.withdrawnAt ? 'Withdrawn' : (participant.status ?? 'Registered') }}
                  </UBadge>
                  <UBadge
                    v-if="participant.email"
                    variant="outline"
                    size="xs"
                  >
                    {{ participant.email }}
                  </UBadge>
                  <UBadge
                    variant="outline"
                    size="xs"
                  >
                    Registered {{ formatDateTime(participant.registeredAt) }}
                  </UBadge>
                  <UBadge
                    v-if="participant.registrationSubmissions?.length"
                    variant="outline"
                    size="xs"
                  >
                    {{ participant.registrationSubmissions.length }} responses
                  </UBadge>
                </div>
              </div>

              <div class="flex gap-2">
                <UButton
                  size="xs"
                  variant="ghost"
                  icon="i-lucide-eye"
                  @click="openParticipant(participant.userId)"
                >
                  View
                </UButton>
                <UButton
                  v-if="!participant.withdrawnAt"
                  size="xs"
                  color="warning"
                  variant="ghost"
                  icon="i-lucide-user-x"
                  :loading="withdrawMutation.isPending.value"
                  @click="withdrawParticipant(participant)"
                >
                  Withdraw
                </UButton>
              </div>
            </div>
          </div>
        </UCard>

        <UModal
          v-model:open="isDetailOpen"
          :title="selectedParticipant?.name ?? selectedParticipant?.email ?? 'Participant details'"
        >
          <template #content>
            <div class="overflow-auto max-h-[80vh]">
              <UCard>
                <template #header>
                  <div class="flex items-center justify-between gap-3">
                    <div>
                      <h3 class="text-base font-semibold">
                        {{ selectedParticipant?.name ?? selectedParticipant?.email ?? 'Participant details' }}
                      </h3>
                      <p class="text-sm text-(--ui-text-muted)">
                        {{ selectedParticipant?.userId ?? '' }}
                      </p>
                    </div>
                    <UButton
                      variant="ghost"
                      icon="i-lucide-x"
                      size="xs"
                      @click="closeParticipant"
                    />
                  </div>
                </template>

                <div
                  v-if="isLoadingDetail"
                  class="text-sm text-(--ui-text-muted)"
                >
                  Loading participant details...
                </div>

                <div
                  v-else-if="selectedParticipant"
                  class="space-y-4"
                >
                  <div class="grid gap-4 sm:grid-cols-2">
                    <UFormField label="Name">
                      <UInput
                        :model-value="selectedParticipant.name ?? '—'"
                        disabled
                      />
                    </UFormField>
                    <UFormField label="Email">
                      <UInput
                        :model-value="selectedParticipant.email ?? '—'"
                        disabled
                      />
                    </UFormField>
                    <UFormField label="Status">
                      <UInput
                        :model-value="selectedParticipant.withdrawnAt ? 'Withdrawn' : (selectedParticipant.status ?? 'Registered')"
                        disabled
                      />
                    </UFormField>
                    <UFormField label="Registered At">
                      <UInput
                        :model-value="formatDateTime(selectedParticipant.registeredAt)"
                        disabled
                      />
                    </UFormField>
                  </div>

                  <div>
                    <h4 class="mb-2 text-sm font-semibold">
                      Registration Responses
                    </h4>
                    <div
                      v-if="detailSubmissions.length"
                      class="space-y-3"
                    >
                      <UFormField
                        v-for="submission in detailSubmissions"
                        :key="submission.questionId ?? submission.questionText ?? ''"
                        :label="submission.questionText ?? 'Question'"
                      >
                        <UTextarea
                          :model-value="getSubmissionValue(submission)"
                          disabled
                          autoresize
                          :rows="2"
                        />
                      </UFormField>
                    </div>
                    <p
                      v-else
                      class="text-sm text-(--ui-text-muted)"
                    >
                      No registration responses found.
                    </p>
                  </div>

                  <div>
                    <h4 class="mb-2 text-sm font-semibold">
                      Venue Check-ins
                    </h4>
                    <div
                      v-if="selectedParticipant.venueCheckIns?.length"
                      class="space-y-2"
                    >
                      <div
                        v-for="checkIn in selectedParticipant.venueCheckIns"
                        :key="checkIn.id ?? checkIn.checkInTime ?? ''"
                        class="rounded-lg border border-default px-3 py-2 text-sm"
                      >
                        <div>{{ formatDateTime(checkIn.checkInTime) }}</div>
                        <div
                          v-if="checkIn.checkOutTime"
                          class="text-xs text-(--ui-text-muted)"
                        >
                          Checked out {{ formatDateTime(checkIn.checkOutTime) }}
                        </div>
                      </div>
                    </div>
                    <p
                      v-else
                      class="text-sm text-(--ui-text-muted)"
                    >
                      No check-ins recorded.
                    </p>
                  </div>
                </div>
              </UCard>
            </div>
          </template>
        </UModal>
      </div>
    </template>
  </UDashboardPanel>
</template>
