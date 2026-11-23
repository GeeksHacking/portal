<script setup lang="ts">
import {
  geeksHackingPortalApiEndpointsOrganizersHackathonGetEndpointQueryKey,
  geeksHackingPortalApiEndpointsOrganizersHackathonListEndpointQueryKey,
  geeksHackingPortalApiEndpointsParticipantsHackathonListEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesHackathonsEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonCreateEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonGetEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import { formatHackathonDateTimeInput, HACKATHON_TIME_ZONE_LABEL, serializeHackathonDateTimeInput } from '~/utils/hackathon-date-time'

/**
 * Create or edit a hackathon on its own page (it used to be a pop-up on the workspace).
 * Admins only, as before. Same API calls and body as before; adds checks the browser can do
 * (name, website address, dates in order) and shows the API's field errors next to each field.
 * Without `hackathonId` it creates; with one it loads that hackathon and edits it.
 */
const props = defineProps<{
  hackathonId?: string
}>()

const toast = useGToast()
const queryClient = useQueryClient()
const isEditing = computed(() => !!props.hackathonId)

const { data: user, isLoading: isLoadingUser } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const isAdmin = computed(() => !!user.value?.isRoot)

const { data: hackathon, isLoading: isLoadingHackathon, error: loadError } = useGeeksHackingPortalApiEndpointsOrganizersHackathonGetEndpoint(
  { path: computed(() => ({ hackathonId: props.hackathonId ?? '' })) },
  { query: { enabled: computed(() => isEditing.value && isAdmin.value) } },
)

const createMutation = useGeeksHackingPortalApiEndpointsOrganizersHackathonCreateEndpoint()
const updateMutation = useGeeksHackingPortalApiEndpointsOrganizersActivitiesHackathonsEndpoint()
const isSaving = computed(() => createMutation.isPending.value || updateMutation.isPending.value)

function emptyForm() {
  return {
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
    emailTemplates: {} as Record<string, string>,
  }
}
type Form = ReturnType<typeof emptyForm>

const form = ref<Form>(emptyForm())
const snapshot = ref(JSON.stringify(form.value))
const isDirty = computed(() => JSON.stringify(form.value) !== snapshot.value)

function load(next: Form) {
  form.value = next
  snapshot.value = JSON.stringify(next)
}

// load once: a background refetch mustn't overwrite what's being typed
let loaded = false
watch(hackathon, (value) => {
  if (!value || loaded)
    return
  loaded = true
  load({
    name: value.name ?? '',
    shortCode: value.shortCode ?? '',
    description: value.description ?? '',
    venue: value.venue ?? '',
    homepageUri: value.homepageUri ?? '',
    eventStartDate: formatHackathonDateTimeInput(value.eventStartDate),
    eventEndDate: formatHackathonDateTimeInput(value.eventEndDate),
    submissionsStartDate: formatHackathonDateTimeInput(value.submissionsStartDate),
    challengeSelectionEndDate: formatHackathonDateTimeInput(value.challengeSelectionEndDate),
    submissionsEndDate: formatHackathonDateTimeInput(value.submissionsEndDate),
    judgingStartDate: formatHackathonDateTimeInput(value.judgingStartDate),
    judgingEndDate: formatHackathonDateTimeInput(value.judgingEndDate),
    isPublished: value.isPublished ?? false,
    emailTemplates: { ...(value.emailTemplates ?? {}) },
  })
}, { immediate: true })

useHead(() => ({ title: isEditing.value ? `Edit ${hackathon.value?.name ?? 'hackathon'}` : 'New hackathon' }))

// ---- validation ---------------------------------------------------------------------------------
const errors = ref<Partial<Record<keyof Form, string>>>({})
const saveError = ref('')

function isHttpUrl(value: string) {
  return URL.canParse(value) && ['http:', 'https:'].includes(new URL(value).protocol)
}

function validate() {
  const f = form.value
  const next: typeof errors.value = {}
  if (!f.name.trim())
    next.name = 'Give the hackathon a name.'
  if (f.homepageUri.trim() && !isHttpUrl(f.homepageUri.trim()))
    next.homepageUri = 'Use a full address starting with http:// or https://.'
  // datetime-local values compare correctly as strings
  if (f.eventStartDate && f.eventEndDate && f.eventEndDate <= f.eventStartDate)
    next.eventEndDate = 'End after the event starts.'
  if (f.submissionsStartDate && f.submissionsEndDate && f.submissionsEndDate <= f.submissionsStartDate)
    next.submissionsEndDate = 'End after submissions open.'
  if (f.judgingStartDate && f.judgingEndDate && f.judgingEndDate <= f.judgingStartDate)
    next.judgingEndDate = 'End after judging starts.'
  errors.value = next
  return Object.keys(next).length === 0
}

const apiFields: Record<keyof Form, string> = {
  name: 'Name',
  shortCode: 'ShortCode',
  description: 'Description',
  venue: 'Venue',
  homepageUri: 'HomepageUri',
  eventStartDate: 'EventStartDate',
  eventEndDate: 'EventEndDate',
  submissionsStartDate: 'SubmissionsStartDate',
  challengeSelectionEndDate: 'ChallengeSelectionEndDate',
  submissionsEndDate: 'SubmissionsEndDate',
  judgingStartDate: 'JudgingStartDate',
  judgingEndDate: 'JudgingEndDate',
  isPublished: 'IsPublished',
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
    name: f.name.trim() || undefined,
    shortCode: f.shortCode.trim() || undefined,
    description: f.description || undefined,
    venue: f.venue.trim() || undefined,
    homepageUri: f.homepageUri.trim(),
    eventStartDate: serializeHackathonDateTimeInput(f.eventStartDate),
    eventEndDate: serializeHackathonDateTimeInput(f.eventEndDate),
    submissionsStartDate: serializeHackathonDateTimeInput(f.submissionsStartDate),
    challengeSelectionEndDate: serializeHackathonDateTimeInput(f.challengeSelectionEndDate),
    submissionsEndDate: serializeHackathonDateTimeInput(f.submissionsEndDate),
    judgingStartDate: serializeHackathonDateTimeInput(f.judgingStartDate),
    judgingEndDate: serializeHackathonDateTimeInput(f.judgingEndDate),
    isPublished: f.isPublished,
    emailTemplates: f.emailTemplates,
  }

  try {
    if (isEditing.value && props.hackathonId) {
      await updateMutation.mutateAsync({ path: { hackathonId: props.hackathonId }, body })
      await queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsOrganizersHackathonGetEndpointQueryKey({ path: { hackathonId: props.hackathonId } }) })
      toast.add({ title: 'Hackathon saved', description: f.name.trim(), tone: 'ok' })
    }
    else {
      await createMutation.mutateAsync({ body })
      toast.add({ title: 'Hackathon created', description: f.name.trim(), tone: 'ok' })
    }
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsParticipantsHackathonListEndpointQueryKey() }),
      queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsOrganizersHackathonListEndpointQueryKey() }),
    ])
    snapshot.value = JSON.stringify(form.value)
    await navigateTo('/dash/manage')
  }
  catch (error) {
    console.error('Failed to save hackathon', error)
    applyApiErrors(error)
    saveError.value = getApiValidationSummary(error) ?? getApiErrorMessage(error, 'The hackathon wasn\'t saved. Try again.')
    focusFirstError()
  }
}

const sections = [
  { id: 'hf-basics', label: 'Basics' },
  { id: 'hf-description', label: 'Description' },
  { id: 'hf-venue', label: 'Venue' },
  { id: 'hf-schedule', label: 'Schedule' },
  { id: 'hf-emails', label: 'Emails' },
  { id: 'hf-publishing', label: 'Publishing' },
]
</script>

<template>
  <GLoading
    v-if="isLoadingUser || (isEditing && isAdmin && isLoadingHackathon)"
    :label="isEditing ? 'Loading the hackathon…' : 'Loading…'"
  />

  <GEmptyState
    v-else-if="!isAdmin"
    icon="material-symbols:lock-rounded"
    title="Only admins can do this"
    description="Ask a portal admin to create or edit hackathons."
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
    v-else-if="isEditing && (loadError || !hackathon)"
    icon="material-symbols:error-rounded"
    title="Couldn't load this hackathon"
    :description="getApiErrorMessage(loadError, 'It may have been removed, or the link is wrong.')"
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
    eyebrow="Hackathon"
    :title="isEditing ? (hackathon?.name || 'Edit hackathon') : 'New hackathon'"
    :sections="sections"
    :save-label="isEditing ? 'Save changes' : 'Create hackathon'"
    :saving="isSaving"
    :dirty="isDirty"
    :error="saveError"
    @submit="save"
  >
    <OrganizersFormSection
      id="hf-basics"
      title="Basics"
    >
      <GFormField
        label="Name"
        required
        :error="errors.name"
      >
        <GInput
          v-model="form.name"
          placeholder="GeeksHacking 2026"
        />
      </GFormField>
      <div class="fs-pair">
        <GFormField
          label="Short code"
          hint="Used in the event's links, e.g. /hack-2026/team."
          :error="errors.shortCode"
        >
          <GInput
            v-model="form.shortCode"
            placeholder="hack-2026"
            spellcheck="false"
            autocapitalize="off"
          />
        </GFormField>
        <GFormField
          label="Website"
          hint="Optional. The event's own site."
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
      id="hf-description"
      title="Description"
      description="What the hackathon is about. Participants see it on Explore and the event pages."
    >
      <GFormField
        label="Description"
        hide-label
        :error="errors.description"
      >
        <GTextarea
          v-model="form.description"
          :rows="6"
          autoresize
          :max-rows="20"
        />
      </GFormField>
    </OrganizersFormSection>

    <OrganizersFormSection
      id="hf-venue"
      title="Venue"
    >
      <GFormField
        label="Venue"
        :error="errors.venue"
      >
        <GInput
          v-model="form.venue"
          placeholder="Building, room, address"
        />
      </GFormField>
    </OrganizersFormSection>

    <OrganizersFormSection
      id="hf-schedule"
      title="Schedule"
      :description="`All times are ${HACKATHON_TIME_ZONE_LABEL} (UTC+8).`"
    >
      <div class="fs-pair">
        <GFormField
          label="Event starts"
          :error="errors.eventStartDate"
        >
          <GDateTimePicker
            v-model="form.eventStartDate"
          />
        </GFormField>
        <GFormField
          label="Event ends"
          :error="errors.eventEndDate"
        >
          <GDateTimePicker
            v-model="form.eventEndDate"
            :min="form.eventStartDate || undefined"
          />
        </GFormField>
      </div>
      <div class="fs-pair">
        <GFormField
          label="Submissions open"
          :error="errors.submissionsStartDate"
        >
          <GDateTimePicker
            v-model="form.submissionsStartDate"
          />
        </GFormField>
        <GFormField
          label="Submissions close"
          :error="errors.submissionsEndDate"
        >
          <GDateTimePicker
            v-model="form.submissionsEndDate"
            :min="form.submissionsStartDate || undefined"
          />
        </GFormField>
      </div>
      <div class="fs-pair">
        <GFormField
          label="Challenge choice closes"
          hint="Teams can't change their challenge after this."
          :error="errors.challengeSelectionEndDate"
        >
          <GDateTimePicker
            v-model="form.challengeSelectionEndDate"
          />
        </GFormField>
      </div>
      <div class="fs-pair">
        <GFormField
          label="Judging starts"
          :error="errors.judgingStartDate"
        >
          <GDateTimePicker
            v-model="form.judgingStartDate"
          />
        </GFormField>
        <GFormField
          label="Judging ends"
          :error="errors.judgingEndDate"
        >
          <GDateTimePicker
            v-model="form.judgingEndDate"
            :min="form.judgingStartDate || undefined"
          />
        </GFormField>
      </div>
    </OrganizersFormSection>

    <OrganizersFormSection
      id="hf-emails"
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
        event-kind="hackathon"
      />
    </OrganizersFormSection>

    <OrganizersFormSection
      id="hf-publishing"
      title="Publishing"
    >
      <GSwitch
        v-model="form.isPublished"
        label="Published"
        description="Published hackathons show on Explore, and people can join them."
      />
    </OrganizersFormSection>
  </OrganizersEventFormShell>
</template>
