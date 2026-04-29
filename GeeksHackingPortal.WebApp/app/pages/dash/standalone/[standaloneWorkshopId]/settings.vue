<script setup lang="ts">
import {
  geeksHackingPortalApiEndpointsOrganizersActivitiesListEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesStandaloneWorkshopsEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesUpdateEndpoint,
  geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import {
  formatHackathonDateTimeInput,
  HACKATHON_TIME_ZONE_LABEL,
  serializeHackathonDateTimeInput,
} from '~/utils/hackathon-date-time'

const route = useRoute()
const toast = useToast()
const queryClient = useQueryClient()
const standaloneWorkshopId = computed(() => (route.params.standaloneWorkshopId as string | undefined) ?? '')

const { data: eventsData, isLoading } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint()
const updateActivityMutation = useGeeksHackingPortalApiEndpointsOrganizersActivitiesUpdateEndpoint()
const updateMutation = useGeeksHackingPortalApiEndpointsOrganizersActivitiesStandaloneWorkshopsEndpoint()

const event = computed(() => eventsData.value?.standaloneWorkshops?.find(item => item.id === standaloneWorkshopId.value))

const form = ref({
  title: '',
  description: '',
  startTime: '',
  endTime: '',
  location: '',
  homepageUri: '',
  shortCode: '',
  maxParticipants: 0,
  isPublished: false,
  emailTemplates: '{}',
})

watch(
  event,
  (value) => {
    if (!value)
      return

    form.value = {
      title: value.title ?? '',
      description: value.description ?? '',
      startTime: formatHackathonDateTimeInput(value.startTime),
      endTime: formatHackathonDateTimeInput(value.endTime),
      location: value.location ?? '',
      homepageUri: value.homepageUri ?? '',
      shortCode: value.shortCode ?? '',
      maxParticipants: value.maxParticipants ?? 0,
      isPublished: value.isPublished ?? false,
      emailTemplates: JSON.stringify(value.emailTemplates ?? {}, null, 2),
    }
  },
  { immediate: true },
)

const isSubmitting = computed(() => updateActivityMutation.isPending.value || updateMutation.isPending.value)

function parseEmailTemplates() {
  try {
    const value = JSON.parse(form.value.emailTemplates || '{}')
    if (value === null || Array.isArray(value) || typeof value !== 'object')
      throw new Error('Expected object')

    return Object.fromEntries(
      Object.entries(value)
        .filter((entry): entry is [string, string] => typeof entry[1] === 'string')
        .map(([key, value]) => [key, value.trim()]),
    )
  }
  catch {
    toast.add({
      title: 'Invalid email templates',
      description: 'Use a JSON object like { "registration-confirmed": "template-id" }.',
      color: 'error',
    })
    return null
  }
}

async function handleSubmit() {
  const emailTemplates = parseEmailTemplates()
  if (emailTemplates === null || !standaloneWorkshopId.value)
    return

  try {
    await Promise.all([
      updateActivityMutation.mutateAsync({
        activityId: standaloneWorkshopId.value,
        data: {
          title: form.value.title || undefined,
          description: form.value.description || undefined,
          startTime: serializeHackathonDateTimeInput(form.value.startTime),
          endTime: serializeHackathonDateTimeInput(form.value.endTime),
          location: form.value.location || undefined,
          isPublished: form.value.isPublished,
          emailTemplates,
        },
      }),
      updateMutation.mutateAsync({
        activityId: standaloneWorkshopId.value,
        data: {
          homepageUri: form.value.homepageUri || undefined,
          shortCode: form.value.shortCode || undefined,
          maxParticipants: Number(form.value.maxParticipants) || undefined,
        },
      }),
    ])

    await queryClient.invalidateQueries({
      queryKey: geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpointQueryKey(),
    })
    await queryClient.invalidateQueries({
      queryKey: geeksHackingPortalApiEndpointsOrganizersActivitiesListEndpointQueryKey(),
    })

    toast.add({ title: 'Standalone event settings updated', color: 'success' })
  }
  catch (error) {
    console.error('Failed to update standalone event settings', error)
    toast.add({
      title: 'Failed to update standalone event settings',
      description: 'Please review the settings and try again.',
      color: 'error',
    })
  }
}
</script>

<template>
  <UDashboardPanel id="standalone-settings">
    <template #header>
      <UDashboardNavbar title="Standalone Event Settings">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <UCard>
        <template #header>
          <div class="space-y-1">
            <h3 class="text-sm font-semibold">
              Event Details
            </h3>
            <p class="text-sm text-(--ui-text-muted)">
              Customize the standalone event details, schedule, capacity, publishing state, and notification templates.
            </p>
          </div>
        </template>

        <div
          v-if="isLoading"
          class="text-sm text-(--ui-text-muted)"
        >
          Loading settings...
        </div>

        <form
          v-else
          class="space-y-4"
          @submit.prevent="handleSubmit"
        >
          <div class="grid gap-4 lg:grid-cols-2">
            <UFormField label="Title">
              <UInput v-model="form.title" />
            </UFormField>

            <UFormField label="Short Code">
              <UInput v-model="form.shortCode" />
            </UFormField>
          </div>

          <UFormField label="Description">
            <UTextarea
              v-model="form.description"
              :rows="3"
            />
          </UFormField>

          <div class="grid gap-4 lg:grid-cols-2">
            <UFormField label="Location">
              <UInput v-model="form.location" />
            </UFormField>

            <UFormField label="Homepage URL">
              <UInput v-model="form.homepageUri" />
            </UFormField>
          </div>

          <div class="grid gap-4 lg:grid-cols-2">
            <p class="text-xs text-(--ui-text-muted) lg:col-span-2">
              Schedule fields use {{ HACKATHON_TIME_ZONE_LABEL }} (UTC+8).
            </p>
            <UFormField label="Event Start">
              <UInput
                v-model="form.startTime"
                type="datetime-local"
              />
            </UFormField>

            <UFormField label="Event End">
              <UInput
                v-model="form.endTime"
                type="datetime-local"
              />
            </UFormField>
          </div>

          <UFormField label="Max Participants">
            <UInput
              v-model.number="form.maxParticipants"
              type="number"
              min="0"
            />
          </UFormField>

          <UFormField
            label="Email Templates"
            help="JSON object keyed by event name, e.g. registration-confirmed."
          >
            <UTextarea
              v-model="form.emailTemplates"
              :rows="5"
              class="font-mono text-xs"
            />
          </UFormField>

          <UCheckbox
            v-model="form.isPublished"
            label="Published"
          />

          <div class="flex justify-end">
            <UButton
              type="submit"
              :loading="isSubmitting"
            >
              Save Settings
            </UButton>
          </div>
        </form>
      </UCard>
    </template>
  </UDashboardPanel>
</template>
