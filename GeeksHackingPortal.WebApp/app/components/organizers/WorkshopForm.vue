<script setup lang="ts">
import {
  geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsCreateEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsUpdateEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import { formatHackathonDateTimeInput, HACKATHON_TIME_ZONE_LABEL, serializeHackathonDateTimeInput } from '~/utils/hackathon-date-time'

/**
 * Create or edit a workshop on its own page (it used to be a pop-up on the workspace).
 * Admins only, as before. Same API calls, body and checks as before; errors now show next to each
 * field and in the save bar instead of a toast. Without `workshopId` it creates; with one it edits
 * that workshop (found in the organiser list, which is where the old pop-up got it from too).
 */
const props = defineProps<{
  workshopId?: string
}>()

const toast = useGToast()
const queryClient = useQueryClient()
const isEditing = computed(() => !!props.workshopId)

const { data: user, isLoading: isLoadingUser } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const isAdmin = computed(() => !!user.value?.isRoot)

const { data: workshopsData, isLoading: isLoadingWorkshops, error: loadError } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint({
  query: { enabled: computed(() => isEditing.value && isAdmin.value) },
})
const workshop = computed(() => workshopsData.value?.standaloneWorkshops?.find(w => w.id === props.workshopId))

const createMutation = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsCreateEndpoint()
const updateMutation = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsUpdateEndpoint()
const isSaving = computed(() => createMutation.isPending.value || updateMutation.isPending.value)

function emptyForm() {
  return {
    title: '',
    shortCode: '',
    homepageUri: '',
    description: '',
    location: '',
    maxParticipants: '' as string | number,
    startTime: '',
    endTime: '',
    isPublished: false,
    emailTemplates: {} as Record<string, string>,
  }
}
type Form = ReturnType<typeof emptyForm>

const form = ref<Form>(emptyForm())
const snapshot = ref(JSON.stringify(form.value))
const isDirty = computed(() => JSON.stringify(form.value) !== snapshot.value)

// load once: the list refetches in the background and mustn't overwrite what's being typed
let loaded = false
watch(workshop, (value) => {
  if (!value || loaded)
    return
  loaded = true
  form.value = {
    title: value.title ?? '',
    shortCode: value.shortCode ?? '',
    homepageUri: value.homepageUri ?? '',
    description: value.description ?? '',
    location: value.location ?? '',
    maxParticipants: value.maxParticipants ?? '',
    startTime: formatHackathonDateTimeInput(value.startTime),
    endTime: formatHackathonDateTimeInput(value.endTime),
    isPublished: value.isPublished ?? false,
    emailTemplates: { ...(value.emailTemplates ?? {}) },
  }
  snapshot.value = JSON.stringify(form.value)
}, { immediate: true })

useHead(() => ({ title: isEditing.value ? `Edit ${workshop.value?.title ?? 'workshop'}` : 'New workshop' }))

// ---- validation (same rules as the old pop-up) --------------------------------------------------
const errors = ref<Partial<Record<keyof Form, string>>>({})
const saveError = ref('')

function normalizeOptionalUrl(value: string | null | undefined) {
  const trimmed = (value ?? '').trim()
  return trimmed === '' ? null : trimmed
}

function validate() {
  const f = form.value
  const next: typeof errors.value = {}
  if (!f.title.trim())
    next.title = 'Give the workshop a title.'
  if (!f.description.trim())
    next.description = 'Add a description.'
  if (!f.location.trim())
    next.location = 'Add where it happens.'
  if (!f.shortCode.trim())
    next.shortCode = 'Add a short code.'
  else if (!/^[A-Z0-9-]{3,16}$/i.test(f.shortCode.trim()))
    next.shortCode = 'Use 3 to 16 letters, numbers or hyphens.'
  const homepageUri = normalizeOptionalUrl(f.homepageUri)
  if (homepageUri && (!URL.canParse(homepageUri) || !['http:', 'https:'].includes(new URL(homepageUri).protocol)))
    next.homepageUri = 'Use a full address starting with http:// or https://.'
  if (!f.startTime)
    next.startTime = 'Add a start time.'
  if (!f.endTime)
    next.endTime = 'Add an end time.'
  else if (f.startTime && f.endTime <= f.startTime)
    next.endTime = 'End after it starts.'
  const max = Number(f.maxParticipants)
  if (f.maxParticipants === '' || !Number.isFinite(max) || max <= 0)
    next.maxParticipants = 'Allow at least 1 participant.'
  errors.value = next
  return Object.keys(next).length === 0
}

const apiFields: Partial<Record<keyof Form, string>> = {
  title: 'Title',
  description: 'Description',
  startTime: 'StartTime',
  endTime: 'EndTime',
  location: 'Location',
  homepageUri: 'HomepageUri',
  shortCode: 'ShortCode',
  maxParticipants: 'MaxParticipants',
  emailTemplates: 'EmailTemplates',
}

function applyApiErrors(error: unknown) {
  const bag = getApiValidationErrors(error)
  errors.value = Object.fromEntries(
    (Object.entries(apiFields) as [keyof Form, string][])
      .map(([field, apiName]) => [field, getApiFieldError(bag, apiName)] as const)
      .filter((entry): entry is [keyof Form, string] => !!entry[1]),
  )
}

function focusFirstError() {
  nextTick(() => document.querySelector<HTMLElement>('.ef-form [aria-invalid="true"]')?.focus())
}

async function save() {
  saveError.value = ''
  if (!validate()) {
    saveError.value = 'Fix the highlighted fields, then save again.'
    focusFirstError()
    return
  }

  const f = form.value
  const body = {
    title: f.title.trim(),
    description: f.description.trim(),
    startTime: serializeHackathonDateTimeInput(f.startTime),
    endTime: serializeHackathonDateTimeInput(f.endTime),
    location: f.location.trim(),
    // null removes the homepage when editing
    homepageUri: normalizeOptionalUrl(f.homepageUri),
    shortCode: f.shortCode.trim(),
    maxParticipants: Number(f.maxParticipants),
    isPublished: f.isPublished,
    emailTemplates: f.emailTemplates,
  }

  try {
    if (isEditing.value && props.workshopId) {
      await updateMutation.mutateAsync({ path: { standaloneWorkshopId: props.workshopId }, body })
      toast.add({ title: 'Workshop saved', description: body.title, tone: 'ok' })
    }
    else {
      await createMutation.mutateAsync({ body })
      toast.add({ title: 'Workshop created', description: body.title, tone: 'ok' })
    }
    await queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpointQueryKey() })
    snapshot.value = JSON.stringify(form.value)
    await navigateTo('/dash/manage')
  }
  catch (error) {
    console.error('Failed to save workshop', error)
    applyApiErrors(error)
    saveError.value = getApiValidationSummary(error) ?? getApiErrorMessage(error, 'The workshop wasn\'t saved. Try again.')
    focusFirstError()
  }
}

const sections = [
  { id: 'wf-basics', label: 'Basics' },
  { id: 'wf-description', label: 'Description' },
  { id: 'wf-venue', label: 'Venue and places' },
  { id: 'wf-schedule', label: 'Schedule' },
  { id: 'wf-emails', label: 'Emails' },
  { id: 'wf-publishing', label: 'Publishing' },
]
</script>

<template>
  <GLoading
    v-if="isLoadingUser || (isEditing && isAdmin && isLoadingWorkshops)"
    :label="isEditing ? 'Loading the workshop…' : 'Loading…'"
  />

  <GEmptyState
    v-else-if="!isAdmin"
    icon="material-symbols:lock-rounded"
    title="Only admins can do this"
    description="Ask a portal admin to create or edit workshops."
    bordered
  >
    <template #actions>
      <GButton
        to="/dash/manage"
        variant="secondary"
        icon="material-symbols:arrow-back-rounded"
        label="Back to workspace"
      />
    </template>
  </GEmptyState>

  <GEmptyState
    v-else-if="isEditing && !workshop"
    icon="material-symbols:error-rounded"
    title="Couldn't find this workshop"
    :description="loadError ? getApiErrorMessage(loadError, 'Try again in a moment.') : 'It may have been removed, or the link is wrong.'"
    bordered
  >
    <template #actions>
      <GButton
        to="/dash/manage"
        variant="secondary"
        icon="material-symbols:arrow-back-rounded"
        label="Back to workspace"
      />
    </template>
  </GEmptyState>

  <OrganizersEventFormShell
    v-else
    eyebrow="Workshop"
    :title="isEditing ? (workshop?.title || 'Edit workshop') : 'New workshop'"
    :sections="sections"
    :save-label="isEditing ? 'Save changes' : 'Create workshop'"
    :saving="isSaving"
    :dirty="isDirty"
    :error="saveError"
    @submit="save"
  >
    <OrganizersFormSection
      id="wf-basics"
      title="Basics"
    >
      <GFormField
        label="Title"
        required
        :error="errors.title"
      >
        <GInput
          v-model="form.title"
          placeholder="Intro to Rust"
        />
      </GFormField>
      <div class="fs-pair">
        <GFormField
          label="Short code"
          required
          hint="3 to 16 letters, numbers or hyphens. Used in the sign-up link."
          :error="errors.shortCode"
        >
          <GInput
            v-model="form.shortCode"
            placeholder="rust-101"
            spellcheck="false"
            autocapitalize="off"
          />
        </GFormField>
        <GFormField
          label="Website"
          hint="Optional."
          :error="errors.homepageUri"
        >
          <GInput
            v-model="form.homepageUri"
            type="url"
            placeholder="https://"
          />
        </GFormField>
      </div>
    </OrganizersFormSection>

    <OrganizersFormSection
      id="wf-description"
      title="Description"
      required
      description="Shown on the workshop's sign-up page."
    >
      <GFormField
        label="Description"
        hide-label
        required
        :error="errors.description"
      >
        <GMarkdownEditor
          v-model="form.description"
          placeholder="What people will learn, who it's for, what to bring…"
        />
      </GFormField>
    </OrganizersFormSection>

    <OrganizersFormSection
      id="wf-venue"
      title="Venue and places"
    >
      <div class="fs-pair">
        <GFormField
          label="Location"
          required
          :error="errors.location"
        >
          <GInput
            v-model="form.location"
            placeholder="Building, room, address"
          />
        </GFormField>
        <GFormField
          label="Places"
          required
          hint="How many people can register."
          :error="errors.maxParticipants"
        >
          <GInput
            v-model="form.maxParticipants"
            type="number"
            min="1"
            inputmode="numeric"
          />
        </GFormField>
      </div>
    </OrganizersFormSection>

    <OrganizersFormSection
      id="wf-schedule"
      title="Schedule"
      :description="`All times are ${HACKATHON_TIME_ZONE_LABEL} (UTC+8).`"
    >
      <div class="fs-pair">
        <GFormField
          label="Starts"
          required
          :error="errors.startTime"
        >
          <GDateTimePicker
            v-model="form.startTime"
          />
        </GFormField>
        <GFormField
          label="Ends"
          required
          :error="errors.endTime"
        >
          <GDateTimePicker
            v-model="form.endTime"
            :min="form.startTime || undefined"
          />
        </GFormField>
      </div>
    </OrganizersFormSection>

    <OrganizersFormSection
      id="wf-emails"
      title="Emails"
      description="Which Postmark template the portal sends for each email."
    >
      <GAlert
        v-if="errors.emailTemplates"
        tone="err"
        :description="errors.emailTemplates"
      />
      <OrganizersEmailTemplateEditor
        v-model="form.emailTemplates"
        event-kind="standalone"
      />
    </OrganizersFormSection>

    <OrganizersFormSection
      id="wf-publishing"
      title="Publishing"
    >
      <GSwitch
        v-model="form.isPublished"
        label="Published"
        description="Published workshops show on Explore, and people can sign up."
      />
    </OrganizersFormSection>
  </OrganizersEventFormShell>
</template>
