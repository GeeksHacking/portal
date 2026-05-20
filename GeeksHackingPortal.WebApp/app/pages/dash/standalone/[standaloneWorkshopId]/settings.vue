<script setup lang="ts">
import {
  geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsUpdateEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import { getApiErrorMessage, getApiFieldError, getApiValidationErrors, getApiValidationSummary } from '~/utils/api-errors'
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
const updateMutation = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsUpdateEndpoint()

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
  emailTemplates: {} as Record<string, string>,
})
const fieldErrors = ref<Record<string, string>>({})

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
      emailTemplates: value.emailTemplates ?? {},
    }
  },
  { immediate: true },
)

const isSubmitting = computed(() => updateMutation.isPending.value)

function normalizeOptionalUrl(value: string | null | undefined) {
  const trimmed = (value ?? '').trim()
  return trimmed === '' ? null : trimmed
}

function setFieldError(field: string, message: string) {
  fieldErrors.value = {
    ...fieldErrors.value,
    [field]: message,
  }
}

function validateForm() {
  fieldErrors.value = {}

  if (!form.value.title.trim())
    setFieldError('title', 'Title is required.')

  if (!form.value.description.trim())
    setFieldError('description', 'Description is required.')

  if (!form.value.location.trim())
    setFieldError('location', 'Location is required.')

  if (!form.value.shortCode.trim()) {
    setFieldError('shortCode', 'Short code is required.')
  }
  else if (!/^[A-Z0-9-]{3,16}$/i.test(form.value.shortCode.trim())) {
    setFieldError('shortCode', 'Use 3 to 16 letters, numbers, or hyphens.')
  }

  const homepageUri = normalizeOptionalUrl(form.value.homepageUri)
  if (homepageUri && (!URL.canParse(homepageUri) || !['http:', 'https:'].includes(new URL(homepageUri).protocol)))
    setFieldError('homepageUri', 'Homepage URL must start with http:// or https://.')

  if (!form.value.startTime)
    setFieldError('startTime', 'Start time is required.')

  if (!form.value.endTime) {
    setFieldError('endTime', 'End time is required.')
  }
  else if (form.value.startTime && form.value.endTime <= form.value.startTime) {
    setFieldError('endTime', 'End time must be after start time.')
  }

  if (!Number.isFinite(Number(form.value.maxParticipants)) || Number(form.value.maxParticipants) <= 0)
    setFieldError('maxParticipants', 'Max participants must be greater than 0.')

  return Object.keys(fieldErrors.value).length === 0
}

function applyApiErrors(error: unknown) {
  const errorBag = getApiValidationErrors(error)
  const nextErrors = {
    title: getApiFieldError(errorBag, 'Title'),
    description: getApiFieldError(errorBag, 'Description'),
    startTime: getApiFieldError(errorBag, 'StartTime'),
    endTime: getApiFieldError(errorBag, 'EndTime'),
    location: getApiFieldError(errorBag, 'Location'),
    homepageUri: getApiFieldError(errorBag, 'HomepageUri'),
    shortCode: getApiFieldError(errorBag, 'ShortCode'),
    maxParticipants: getApiFieldError(errorBag, 'MaxParticipants'),
    emailTemplates: getApiFieldError(errorBag, 'EmailTemplates'),
  }

  fieldErrors.value = Object.fromEntries(
    Object.entries(nextErrors).filter((entry): entry is [string, string] => Boolean(entry[1])),
  )
}

async function handleSubmit() {
  if (!standaloneWorkshopId.value)
    return

  if (!validateForm()) {
    toast.add({
      title: 'Review standalone event settings',
      description: Object.values(fieldErrors.value)[0],
      color: 'error',
    })
    return
  }

  try {
    await updateMutation.mutateAsync({
      standaloneWorkshopId: standaloneWorkshopId.value,
      data: {
        title: form.value.title.trim(),
        description: form.value.description.trim(),
        startTime: serializeHackathonDateTimeInput(form.value.startTime),
        endTime: serializeHackathonDateTimeInput(form.value.endTime),
        location: form.value.location.trim(),
        isPublished: form.value.isPublished,
        emailTemplates: form.value.emailTemplates,
        // Keep the key present when clearing; null means "remove homepage URL".
        homepageUri: normalizeOptionalUrl(form.value.homepageUri),
        shortCode: form.value.shortCode.trim(),
        maxParticipants: Number(form.value.maxParticipants),
      },
    })

    await queryClient.invalidateQueries({
      queryKey: geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpointQueryKey(),
    })
    toast.add({ title: 'Standalone event settings updated', color: 'success' })
  }
  catch (error) {
    console.error('Failed to update standalone event settings', error)
    applyApiErrors(error)
    toast.add({
      title: 'Failed to update standalone event settings',
      description: getApiValidationSummary(error) ?? getApiErrorMessage(error, 'Please review the highlighted fields and try again.'),
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
            <UFormField
              label="Title"
              required
              :error="fieldErrors.title"
            >
              <UInput v-model="form.title" />
            </UFormField>

            <UFormField
              label="Short Code"
              required
              :error="fieldErrors.shortCode"
            >
              <UInput v-model="form.shortCode" />
            </UFormField>
          </div>

          <UFormField
            label="Description"
            required
            :error="fieldErrors.description"
          >
            <UTextarea
              v-model="form.description"
              :rows="3"
            />
          </UFormField>

          <div class="grid gap-4 lg:grid-cols-2">
            <UFormField
              label="Location"
              required
              :error="fieldErrors.location"
            >
              <UInput v-model="form.location" />
            </UFormField>

            <UFormField
              label="Homepage URL"
              :error="fieldErrors.homepageUri"
            >
              <UInput v-model="form.homepageUri" />
            </UFormField>
          </div>

          <div class="grid gap-4 lg:grid-cols-2">
            <p class="text-xs text-(--ui-text-muted) lg:col-span-2">
              Schedule fields use {{ HACKATHON_TIME_ZONE_LABEL }} (UTC+8).
            </p>
            <UFormField
              label="Event Start"
              required
              :error="fieldErrors.startTime"
            >
              <UInput
                v-model="form.startTime"
                type="datetime-local"
              />
            </UFormField>

            <UFormField
              label="Event End"
              required
              :error="fieldErrors.endTime"
            >
              <UInput
                v-model="form.endTime"
                type="datetime-local"
              />
            </UFormField>
          </div>

          <UFormField
            label="Max Participants"
            required
            :error="fieldErrors.maxParticipants"
          >
            <UInput
              v-model.number="form.maxParticipants"
              type="number"
              min="0"
            />
          </UFormField>

          <UFormField
            label="Email Templates"
            :error="fieldErrors.emailTemplates"
          >
            <OrganizersEmailTemplateEditor
              v-model="form.emailTemplates"
              event-kind="standalone"
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
