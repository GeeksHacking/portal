<script setup lang="ts">
import type {
  HackOManiaApiEndpointsParticipantsHackathonRegistrationSubmissionsListResponse,
  HackOManiaApiEndpointsParticipantsHackathonStatusResponse,
} from '~/api-client/models'
import { useQueries, useQuery, useQueryClient } from '@tanstack/vue-query'
import { computed, ref, unref } from 'vue'
import { HackOManiaApiEndpointsParticipantsHackathonStatusParticipantStatusObject } from '~/api-client/models'
import { useCreateHackathonMutation, useJoinHackathonMutation, useUpdateHackathonMutation } from '~/composables/hackathon'
import { formatParticipantStatus, hackathonQueries as participantHackathonQueries } from '~/composables/hackathons'
import {
  formatHackathonDate,
  formatHackathonDateTimeInput,
  HACKATHON_TIME_ZONE_LABEL,
  serializeHackathonDateTimeInput,
} from '~/utils/hackathon-date-time'

const toast = useToast()
const queryClient = useQueryClient()
const joinMutation = useJoinHackathonMutation()
const createMutation = useCreateHackathonMutation()
const updateMutation = useUpdateHackathonMutation()

// Admin hackathon CRUD
const isHackathonModalOpen = ref(false)
const isEditingHackathon = ref(false)
const editingHackathonId = ref<string | null>(null)

const hackathonForm = ref({
  name: '',
  shortCode: '',
  description: '',
  venue: '',
  homepageUri: '',
  eventStartDate: '',
  eventEndDate: '',
  submissionsStartDate: '',
  challengeSelectionEndDate: '',
  submissionsEndDate: '',
  judgingStartDate: '',
  judgingEndDate: '',
  isPublished: false,
})

function resetHackathonForm() {
  hackathonForm.value = {
    name: '',
    shortCode: '',
    description: '',
    venue: '',
    homepageUri: '',
    eventStartDate: '',
    eventEndDate: '',
    submissionsStartDate: '',
    challengeSelectionEndDate: '',
    submissionsEndDate: '',
    judgingStartDate: '',
    judgingEndDate: '',
    isPublished: false,
  }
  isEditingHackathon.value = false
  editingHackathonId.value = null
}

function openCreateHackathonModal() {
  resetHackathonForm()
  isHackathonModalOpen.value = true
}

function openEditHackathonModal(hackathon: typeof hackathons.value[number]) {
  hackathonForm.value = {
    name: hackathon.name ?? '',
    shortCode: hackathon.shortCode ?? '',
    description: hackathon.description ?? '',
    venue: hackathon.venue ?? '',
    homepageUri: hackathon.homepageUri ?? '',
    eventStartDate: formatHackathonDateTimeInput(hackathon.eventStartDate),
    eventEndDate: formatHackathonDateTimeInput(hackathon.eventEndDate),
    submissionsStartDate: formatHackathonDateTimeInput(hackathon.submissionsStartDate),
    challengeSelectionEndDate: formatHackathonDateTimeInput(hackathon.challengeSelectionEndDate),
    submissionsEndDate: formatHackathonDateTimeInput(hackathon.submissionsEndDate),
    judgingStartDate: formatHackathonDateTimeInput(hackathon.judgingStartDate),
    judgingEndDate: formatHackathonDateTimeInput(hackathon.judgingEndDate),
    isPublished: hackathon.isPublished ?? false,
  }
  isEditingHackathon.value = true
  editingHackathonId.value = hackathon.id ?? null
  isHackathonModalOpen.value = true
}

async function handleHackathonSubmit() {
  const formData = {
    name: hackathonForm.value.name || undefined,
    shortCode: hackathonForm.value.shortCode || undefined,
    description: hackathonForm.value.description || undefined,
    venue: hackathonForm.value.venue || undefined,
    homepageUri: hackathonForm.value.homepageUri || undefined,
    eventStartDate: serializeHackathonDateTimeInput(hackathonForm.value.eventStartDate),
    eventEndDate: serializeHackathonDateTimeInput(hackathonForm.value.eventEndDate),
    submissionsStartDate: serializeHackathonDateTimeInput(hackathonForm.value.submissionsStartDate),
    challengeSelectionEndDate: serializeHackathonDateTimeInput(hackathonForm.value.challengeSelectionEndDate),
    submissionsEndDate: serializeHackathonDateTimeInput(hackathonForm.value.submissionsEndDate),
    judgingStartDate: serializeHackathonDateTimeInput(hackathonForm.value.judgingStartDate),
    judgingEndDate: serializeHackathonDateTimeInput(hackathonForm.value.judgingEndDate),
    isPublished: hackathonForm.value.isPublished,
  }

  try {
    if (isEditingHackathon.value && editingHackathonId.value) {
      await updateMutation.mutateAsync({
        hackathonId: editingHackathonId.value,
        data: formData,
      })
      toast.add({ title: 'Hackathon updated', color: 'success' })
    }
    else {
      await createMutation.mutateAsync(formData)
      toast.add({ title: 'Hackathon created', color: 'success' })
    }
    await queryClient.invalidateQueries({ queryKey: ['hackathons'] })
    isHackathonModalOpen.value = false
    resetHackathonForm()
  }
  catch (error) {
    console.error('Failed to save hackathon', error)
    toast.add({
      title: 'Failed to save hackathon',
      description: 'Please try again.',
      color: 'error',
    })
  }
}

const isHackathonSubmitting = computed(() => createMutation.isPending.value || updateMutation.isPending.value)

const { data: user, isLoading: isLoadingUser } = useQuery(authQueries.whoAmI)

// Fetch participant hackathons (published ones, for everyone)
const { data: participantHackathonsData, isLoading: isLoadingParticipantHackathons } = useQuery(
  participantHackathonQueries.list,
)

// Fetch organizer hackathons (ones user can manage, only for authenticated users)
const { data: organizerHackathonsData, isLoading: isLoadingOrganizerHackathons } = useQuery(
  computed(() => ({
    ...participantHackathonQueries.organizerList,
    enabled: !!user.value?.id,
  })),
)

const isLoadingHackathons = computed(() =>
  isLoadingUser.value || isLoadingParticipantHackathons.value || (!!user.value?.id && isLoadingOrganizerHackathons.value),
)

// Merge and dedupe hackathons from both endpoints
const hackathons = computed(() => {
  const participantList = participantHackathonsData.value?.hackathons ?? []
  const organizerList = organizerHackathonsData.value?.hackathons ?? []

  // Create a map to dedupe by ID
  const hackathonMap = new Map<string, typeof participantList[number]>()

  // Add organizer hackathons first (they may include unpublished ones)
  for (const h of organizerList) {
    if (h.id)
      hackathonMap.set(h.id, h)
  }

  // Add participant hackathons (won't overwrite if already present)
  for (const h of participantList) {
    if (h.id && !hackathonMap.has(h.id))
      hackathonMap.set(h.id, h)
  }

  return Array.from(hackathonMap.values())
})

// Fetch participation status per hackathon
const statusQueries = useQueries({
  queries: computed(() =>
    hackathons.value.map(hackathon => ({
      ...participantHackathonQueries.status(hackathon.id ?? ''),
      enabled: !!hackathon.id,
    })),
  ),
})

// Fetch registration submissions to check completion status
const submissionQueries = useQueries({
  queries: computed(() =>
    hackathons.value.map((hackathon) => {
      const status = statusQueries.value[hackathons.value.indexOf(hackathon)]?.data as HackOManiaApiEndpointsParticipantsHackathonStatusResponse | undefined
      const isParticipant = status?.isParticipant === true
      return {
        queryKey: ['hackathons', hackathon.id, 'registration', 'submissions'],
        queryFn: () => useNuxtApp().$apiClient.participants.hackathons.byHackathonIdOrShortCodeId(hackathon.id ?? '').registration.submissions.get(),
        enabled: !!hackathon.id && isParticipant,
      }
    }),
  ),
})

function statusDataForIndex(index: number): HackOManiaApiEndpointsParticipantsHackathonStatusResponse | undefined {
  return unref(statusQueries.value[index]?.data) as HackOManiaApiEndpointsParticipantsHackathonStatusResponse | undefined
}

function submissionsDataForIndex(index: number): HackOManiaApiEndpointsParticipantsHackathonRegistrationSubmissionsListResponse | undefined {
  return unref(submissionQueries.value[index]?.data) as HackOManiaApiEndpointsParticipantsHackathonRegistrationSubmissionsListResponse | undefined
}

function isRegistrationComplete(index: number): boolean {
  const submissions = submissionsDataForIndex(index)
  return submissions?.requiredQuestionsRemaining === 0
}

async function joinHackathon(hackathon: { id: string, shortCode: string }) {
  try {
    await joinMutation.mutateAsync(hackathon.id)
    await queryClient.invalidateQueries({ queryKey: participantHackathonQueries.status(hackathon.id).queryKey })
    await queryClient.invalidateQueries({ queryKey: participantHackathonQueries.list.queryKey })
    navigateTo(`/${hackathon.shortCode}/registration`)
  }
  catch (error) {
    console.error('[DASH] Failed to join hackathon', error)
    toast.add({
      title: 'Could not join',
      description: 'Please try again in a moment.',
      color: 'error',
    })
  }
}
</script>

<template>
  <div>
    <UDashboardPanel id="dashboard">
      <template #header>
        <UDashboardNavbar title="Dashboard">
          <template #leading>
            <UDashboardSidebarCollapse />
          </template>
        </UDashboardNavbar>
      </template>

      <template #body>
        <div class="space-y-3">
          <div class="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3">
            <div class="flex flex-col gap-1">
              <h2 class="text-lg font-semibold">
                Hackathons
              </h2>
              <p class="text-sm text-(--ui-text-muted)">
                Join a hackathon, complete your registration, and track your application status.
              </p>
            </div>
            <UButton
              v-if="user?.isRoot"
              icon="i-lucide-plus"
              size="sm"
              class="self-start"
              @click="openCreateHackathonModal"
            >
              Add Hackathon
            </UButton>
          </div>

          <div
            v-if="isLoadingHackathons"
            class="text-(--ui-text-muted)"
          >
            Loading hackathons...
          </div>

          <div
            v-else-if="!hackathons.length"
            class="text-(--ui-text-muted)"
          >
            No hackathons available.
          </div>

          <div
            v-else
            class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
          >
            <UCard
              v-for="(hackathon, index) in hackathons"
              :key="hackathon.id ?? index"
            >
              <template #header>
                <div class="flex items-center justify-between gap-2">
                  <h3 class="text-base font-semibold truncate">
                    {{ hackathon.name }}
                  </h3>
                  <div class="flex items-center gap-2 shrink-0">
                    <UButton
                      v-if="user?.isRoot"
                      size="xs"
                      variant="ghost"
                      icon="i-lucide-pencil"
                      @click.stop="openEditHackathonModal(hackathon)"
                    />
                    <UBadge
                      v-if="user?.isRoot"
                      color="warning"
                      variant="subtle"
                      size="sm"
                    >
                      Admin
                    </UBadge>
                    <UBadge
                      v-else-if="statusDataForIndex(index)?.isOrganizer"
                      color="info"
                      variant="subtle"
                      size="sm"
                    >
                      Organizer
                    </UBadge>
                    <UBadge
                      v-else-if="statusDataForIndex(index)"
                      :color="formatParticipantStatus(statusDataForIndex(index)?.status ?? null, statusDataForIndex(index)?.isParticipant).color"
                      variant="subtle"
                      size="sm"
                    >
                      {{ formatParticipantStatus(statusDataForIndex(index)?.status ?? null, statusDataForIndex(index)?.isParticipant).label }}
                    </UBadge>
                  </div>
                </div>
              </template>

              <p class="text-sm text-(--ui-text-muted) min-h-14">
                {{ hackathon.description }}
              </p>

              <div class="mt-3 flex flex-col gap-2">
                <div class="flex flex-wrap items-center gap-2 text-xs text-(--ui-text-muted)">
                  <span>
                    Starts: {{ formatHackathonDate(hackathon.eventStartDate) }}
                  </span>
                  <span>•</span>
                  <span>
                    Ends: {{ formatHackathonDate(hackathon.eventEndDate) }}
                  </span>
                </div>

                <div
                  v-if="statusDataForIndex(index)?.status === HackOManiaApiEndpointsParticipantsHackathonStatusParticipantStatusObject.Rejected && statusDataForIndex(index)?.reviewReason"
                  class="text-xs text-red-500 dark:text-red-400"
                >
                  Reason: {{ statusDataForIndex(index)?.reviewReason }}
                </div>

                <div class="flex flex-wrap items-center gap-2">
                  <!-- Root user: View + Manage -->
                  <template v-if="user?.isRoot">
                    <UButton
                      :to="`/dash/${hackathon.id}`"
                      color="neutral"
                      size="sm"
                      class="w-full sm:w-auto"
                    >
                      Manage
                    </UButton>
                  </template>

                  <!-- Organizer: Manage + Portal -->
                  <template v-else-if="statusDataForIndex(index)?.isOrganizer">
                    <UButton
                      :to="`/dash/${hackathon.id}`"
                      color="neutral"
                      size="sm"
                      class="w-full sm:w-auto"
                    >
                      Manage
                    </UButton>
                    <UButton
                      :to="`/${hackathon.shortCode}/team`"
                      color="neutral"
                      variant="outline"
                      size="sm"
                      class="w-full sm:w-auto"
                    >
                      Go to hackathon portal
                    </UButton>
                  </template>

                  <!-- Not joined: Join event -->
                  <template v-else-if="!statusDataForIndex(index)?.isParticipant">
                    <UButton
                      color="neutral"
                      size="sm"
                      class="w-full sm:w-auto"
                      :loading="joinMutation.isPending.value"
                      @click="joinHackathon({ id: hackathon.id!, shortCode: hackathon.shortCode! })"
                    >
                      Join event
                    </UButton>
                  </template>

                  <!-- Joined but registration incomplete: Continue registration -->
                  <template v-else-if="!isRegistrationComplete(index)">
                    <UButton
                      :to="`/${hackathon.shortCode}/registration`"
                      color="neutral"
                      size="sm"
                      class="w-full sm:w-auto"
                    >
                      Continue registration
                    </UButton>
                  </template>

                  <!-- Registration complete but not approved: View registration status -->
                  <template v-else-if="statusDataForIndex(index)?.status !== 'Accepted'">
                    <UButton
                      :to="`/dash/${hackathon.id}/participant`"
                      color="neutral"
                      size="sm"
                      class="w-full sm:w-auto"
                    >
                      View registration status
                    </UButton>
                  </template>

                  <!-- Approved participant: Portal -->
                  <template v-else>
                    <UButton
                      :to="`/${hackathon.shortCode}/team`"
                      color="neutral"
                      size="sm"
                      class="w-full sm:w-auto"
                    >
                      Go to hackathon portal
                    </UButton>
                  </template>
                </div>
              </div>
            </UCard>
          </div>
        </div>
      </template>
    </UDashboardPanel>

    <!-- Hackathon Create/Edit Modal (Admin only) -->
    <UModal
      v-if="user?.isRoot"
      v-model:open="isHackathonModalOpen"
    >
      <template #content>
        <UCard>
          <template #header>
            <div class="flex items-center justify-between">
              <h3 class="text-base font-semibold">
                {{ isEditingHackathon ? 'Edit Hackathon' : 'Create Hackathon' }}
              </h3>
              <UButton
                variant="ghost"
                icon="i-lucide-x"
                size="xs"
                @click="isHackathonModalOpen = false"
              />
            </div>
          </template>

          <form
            class="space-y-4 max-h-[60vh] sm:max-h-[70vh] overflow-y-auto pr-1"
            @submit.prevent="handleHackathonSubmit"
          >
            <UFormField
              label="Name"
              required
            >
              <UInput
                v-model="hackathonForm.name"
                placeholder="Hackathon name"
              />
            </UFormField>

            <UFormField label="Short Code">
              <UInput
                v-model="hackathonForm.shortCode"
                placeholder="e.g., hackathon-2025"
              />
            </UFormField>

            <UFormField label="Description">
              <UTextarea
                v-model="hackathonForm.description"
                placeholder="Hackathon description"
                :rows="3"
              />
            </UFormField>

            <UFormField label="Venue">
              <UInput
                v-model="hackathonForm.venue"
                placeholder="Event venue"
              />
            </UFormField>

            <UFormField label="Homepage URL">
              <UInput
                v-model="hackathonForm.homepageUri"
                placeholder="https://..."
              />
            </UFormField>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4">
              <p class="text-xs text-(--ui-text-muted) sm:col-span-2">
                Schedule fields use {{ HACKATHON_TIME_ZONE_LABEL }} (UTC+8).
              </p>
              <UFormField label="Event Start">
                <UInput
                  v-model="hackathonForm.eventStartDate"
                  type="datetime-local"
                />
              </UFormField>

              <UFormField label="Event End">
                <UInput
                  v-model="hackathonForm.eventEndDate"
                  type="datetime-local"
                />
              </UFormField>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4">
              <UFormField label="Submissions Start">
                <UInput
                  v-model="hackathonForm.submissionsStartDate"
                  type="datetime-local"
                />
              </UFormField>

              <UFormField label="Challenge Selection Deadline">
                <UInput
                  v-model="hackathonForm.challengeSelectionEndDate"
                  type="datetime-local"
                />
              </UFormField>

              <UFormField label="Project Submissions End">
                <UInput
                  v-model="hackathonForm.submissionsEndDate"
                  type="datetime-local"
                />
              </UFormField>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4">
              <UFormField label="Judging Start">
                <UInput
                  v-model="hackathonForm.judgingStartDate"
                  type="datetime-local"
                />
              </UFormField>

              <UFormField label="Judging End">
                <UInput
                  v-model="hackathonForm.judgingEndDate"
                  type="datetime-local"
                />
              </UFormField>
            </div>

            <UCheckbox
              v-model="hackathonForm.isPublished"
              label="Published"
            />

            <div class="flex flex-col-reverse sm:flex-row sm:justify-end gap-2 pt-4">
              <UButton
                variant="ghost"
                class="w-full sm:w-auto"
                @click="isHackathonModalOpen = false"
              >
                Cancel
              </UButton>
              <UButton
                type="submit"
                class="w-full sm:w-auto"
                :loading="isHackathonSubmitting"
              >
                {{ isEditingHackathon ? 'Update' : 'Create' }}
              </UButton>
            </div>
          </form>
        </UCard>
      </template>
    </UModal>
  </div>
</template>
