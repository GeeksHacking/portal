<script setup lang="ts">
import {
  geeksHackingPortalApiEndpointsOrganizersHackathonGetEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesHackathonsEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonGetEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'

/**
 * A hackathon's GitHub checks on submissions. What the API does with these settings
 * (GitHubRepositoryAutomationService, run when a team submits):
 * - checking: looks the repository up on GitHub with the token;
 * - copying (forking): looks it up too, then forks it into the organisation (found by its ID)
 *   and renames the copy `{prefix}-{team}-{repo}`.
 * Any failure stops the submission, and the team sees GitHub's error on the repository field.
 * The token is never sent back, only whether one is saved; a blank token keeps the saved one.
 */
const props = defineProps<{ hackathonId: string }>()

const toast = useGToast()
const queryClient = useQueryClient()
const path = computed(() => ({ hackathonId: props.hackathonId }))
const { data: hackathon, isLoading, error: loadError } = useGeeksHackingPortalApiEndpointsOrganizersHackathonGetEndpoint({ path }, { query: { enabled: computed(() => !!props.hackathonId) } })
const { data: user } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const isAdmin = computed(() => !!user.value?.isRoot)
const mutation = useGeeksHackingPortalApiEndpointsOrganizersActivitiesHackathonsEndpoint()

function emptyForm() {
  return { checking: false, copying: false, token: '', removeToken: false, prefix: '', organizationId: '' }
}
type Form = ReturnType<typeof emptyForm>
const form = ref<Form>(emptyForm())
const snapshot = ref(JSON.stringify(form.value))
const isDirty = computed(() => JSON.stringify(form.value) !== snapshot.value)
const hasSavedToken = ref(false)
// declared before the load watcher below: with cached data it runs at once and calls reset()
const errors = ref<Partial<Record<'token' | 'organizationId', string>>>({})
const saveError = ref('')
const isReplacingToken = ref(false)

function fromServer(): Form {
  const s = hackathon.value?.gitHubRepositorySettings
  return {
    checking: s?.isRepositoryCheckingEnabled ?? false,
    copying: s?.isRepositoryForkingEnabled ?? false,
    token: '',
    removeToken: false,
    prefix: s?.repositoryPrefix ?? '',
    organizationId: s?.organizationId ? String(s.organizationId) : '',
  }
}
function reset() {
  form.value = fromServer()
  snapshot.value = JSON.stringify(form.value)
  hasSavedToken.value = hackathon.value?.gitHubRepositorySettings?.hasApiKey ?? false
  isReplacingToken.value = false
  errors.value = {}
  saveError.value = ''
}
// load once: a background refetch mustn't overwrite what's being typed
let loaded = false
watch(hackathon, (value) => {
  if (value && !loaded) {
    loaded = true
    reset()
  }
}, { immediate: true })

useHead(() => ({ title: `Settings · ${hackathon.value?.name ?? 'Hackathon'}` }))

// ---- token ----
const tokenAfterSave = computed(() => !!form.value.token.trim() || (hasSavedToken.value && !form.value.removeToken))
const showTokenInput = computed(() => !hasSavedToken.value || isReplacingToken.value || form.value.removeToken)
function startReplace() {
  isReplacingToken.value = true
  form.value.removeToken = false
  nextTick(() => document.querySelector<HTMLInputElement>('#gh-token-input')?.focus())
}
function cancelReplace() {
  isReplacingToken.value = false
  form.value.token = ''
}

// ---- fork name preview (same rules as the API's SanitizeRepositoryNamePart) ----
function namePart(value: string) {
  return value.trim().replace(/[^\p{L}\p{N}\-_.]+/gu, '-').replace(/^[-._]+|[-._]+$/g, '')
}
const exampleName = computed(() => [form.value.prefix, 'Byte Me', 'weather-app'].map(namePart).filter(Boolean).join('-'))

// ---- validation (mirrors the API, so mistakes show before saving) ----
watch(() => [form.value.token, form.value.removeToken, form.value.checking, form.value.copying], () => {
  errors.value.token = undefined
})
watch(() => [form.value.organizationId, form.value.copying], () => {
  errors.value.organizationId = undefined
})

function validate() {
  const f = form.value
  const next: typeof errors.value = {}
  if ((f.checking || f.copying) && !tokenAfterSave.value)
    next.token = f.removeToken ? 'Checks need a token. Keep the saved one, add a new one, or turn the checks off.' : 'Add a token: checks need one to reach GitHub.'
  const id = f.organizationId.trim()
  if (id && !/^\d+$/.test(id))
    next.organizationId = 'Use the organisation\'s ID number (digits only), not its name.'
  else if (id && Number(id) <= 0)
    next.organizationId = 'Use the organisation\'s ID number.'
  else if (f.copying && !id)
    next.organizationId = 'Add the ID of the organisation to copy repositories into.'
  errors.value = next
  return Object.keys(next).length === 0
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
  try {
    const result = await mutation.mutateAsync({
      path: path.value,
      body: {
        gitHubRepositorySettings: {
          isRepositoryCheckingEnabled: f.checking,
          isRepositoryForkingEnabled: f.copying,
          apiKey: f.token.trim() || undefined,
          clearApiKey: (f.removeToken && !f.token.trim()) || undefined,
          repositoryPrefix: f.prefix.trim() || undefined,
          organizationId: f.organizationId.trim() ? Number(f.organizationId.trim()) : undefined,
        },
      },
    })
    hasSavedToken.value = result?.gitHubRepositorySettings?.hasApiKey ?? tokenAfterSave.value
    form.value = { ...form.value, token: '', removeToken: false, prefix: f.prefix.trim(), organizationId: f.organizationId.trim() }
    snapshot.value = JSON.stringify(form.value)
    isReplacingToken.value = false
    await queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsOrganizersHackathonGetEndpointQueryKey({ path: path.value }) })
    toast.add({ title: 'GitHub settings saved', tone: 'ok' })
  }
  catch (error) {
    const bag = getApiValidationErrors(error)
    errors.value = {
      token: getApiFieldError(bag, 'GitHubRepositorySettings.ApiKey'),
      organizationId: getApiFieldError(bag, 'GitHubRepositorySettings.OrganizationId'),
    }
    saveError.value = getApiValidationSummary(error) ?? getApiErrorMessage(error, 'The settings weren\'t saved. Try again.')
    focusFirstError()
  }
}

const sections = [
  { id: 'gh-checks', label: 'Checks on submission' },
  { id: 'gh-token', label: 'GitHub token' },
  { id: 'gh-copy', label: 'Copying' },
  { id: 'gh-event', label: 'Event details' },
]
</script>

<template>
  <GLoading
    v-if="isLoading"
    label="Loading settings…"
  />

  <GEmptyState
    v-else-if="!hackathon"
    icon="material-symbols:error-rounded"
    title="Couldn't load settings"
    :description="getApiErrorMessage(loadError, 'Try again in a moment.')"
    bordered
  />

  <OrganizersEventFormShell
    v-else
    eyebrow="Hackathon settings"
    :title="hackathon.name || 'Hackathon'"
    :sections="sections"
    save-label="Save changes"
    :saving="mutation.isPending.value"
    :dirty="isDirty"
    :error="saveError"
    in-place
    :required-note="form.copying"
    @submit="save"
    @discard="reset"
  >
    <OrganizersFormSection
      id="gh-checks"
      title="Checks on submission"
      description="What the portal does with a team's repository link when they submit."
    >
      <GAlert
        tone="neutral"
        title="A failed check stops the submission"
        description="The team sees GitHub's error on their repository link and can't submit until it works. After changing these settings, try a submission with a repository of your own."
      />
      <GSwitch
        v-model="form.checking"
        label="Check the repository exists"
        description="Looks the link up on GitHub. A private repository the token can't see counts as not found."
      />
      <GSwitch
        v-model="form.copying"
        label="Copy it into our GitHub organisation"
        description="Forks the repository into your organisation and renames the copy. Includes the check above."
      />
      <p
        v-if="form.copying && !form.checking"
        class="gh-note"
      >
        The repository is checked anyway while copying is on.
      </p>
    </OrganizersFormSection>

    <OrganizersFormSection
      id="gh-token"
      title="GitHub token"
      description="A GitHub token the portal uses for the checks. It needs to read the submitted repositories and, for copying, create repositories in your organisation. It's kept on the server and never shown again."
    >
      <div class="gh-token">
        <GBadge
          :tone="tokenAfterSave ? 'ok' : (form.checking || form.copying) ? 'err' : 'neutral'"
          variant="outline"
          :icon="tokenAfterSave ? 'material-symbols:key-rounded' : 'material-symbols:key-off-rounded'"
          :label="form.removeToken ? 'Will be removed when you save' : form.token.trim() ? (hasSavedToken ? 'New token, not saved yet' : 'Not saved yet') : hasSavedToken ? 'Token saved' : 'No token'"
        />
        <template v-if="hasSavedToken && !form.removeToken && !isReplacingToken">
          <GButton
            variant="secondary"
            size="sm"
            label="Replace token"
            @click="startReplace"
          />
          <GButton
            variant="ghost"
            size="sm"
            label="Remove token"
            @click="form.removeToken = true"
          />
        </template>
        <GButton
          v-else-if="form.removeToken"
          variant="ghost"
          size="sm"
          label="Keep the saved token"
          @click="form.removeToken = false; form.token = ''"
        />
        <GButton
          v-else-if="isReplacingToken"
          variant="ghost"
          size="sm"
          label="Keep the saved token"
          @click="cancelReplace"
        />
      </div>
      <GFormField
        v-if="showTokenInput"
        :label="hasSavedToken && !form.removeToken ? 'New token' : 'Token'"
        :hint="hasSavedToken && !form.removeToken ? 'Replaces the saved token when you save.' : 'Starts with ghp_ or github_pat_.'"
        :error="errors.token"
      >
        <GInput
          id="gh-token-input"
          v-model="form.token"
          type="password"
          autocomplete="off"
          spellcheck="false"
          placeholder="github_pat_…"
        />
      </GFormField>
      <p
        v-else-if="errors.token"
        class="gh-error"
        role="alert"
      >
        {{ errors.token }}
      </p>
    </OrganizersFormSection>

    <OrganizersFormSection
      id="gh-copy"
      title="Copying"
      description="Where copies go and what they're called. Only used when copying is on."
    >
      <div class="fs-pair">
        <GFormField
          label="Organisation ID"
          :required="form.copying"
          hint="The organisation's number, not its name. It's the &quot;id&quot; at api.github.com/orgs/your-org-name."
          :error="errors.organizationId"
        >
          <GInput
            v-model="form.organizationId"
            inputmode="numeric"
            spellcheck="false"
            placeholder="123456789"
          />
        </GFormField>
        <GFormField
          label="Name prefix"
          hint="Optional. Goes in front of every copy's name."
        >
          <GInput
            v-model="form.prefix"
            spellcheck="false"
            autocapitalize="off"
            placeholder="hackathon-2026"
          />
        </GFormField>
      </div>
      <div class="gh-example">
        <p class="gh-example__label">
          Example: team “Byte Me” submits a repository called “weather-app”. The copy is named
        </p>
        <code class="gh-example__name">{{ exampleName }}</code>
      </div>
    </OrganizersFormSection>

    <OrganizersFormSection
      id="gh-event"
      title="Event details"
      description="The name, dates, venue, emails and publishing are edited from the workspace."
    >
      <GButton
        v-if="isAdmin"
        variant="secondary"
        icon="material-symbols:edit-rounded"
        label="Edit event details"
        :to="`/dash/manage/hackathon/${hackathonId}/edit`"
        class="gh-fit"
      />
      <p
        v-else
        class="gh-note"
      >
        Only portal admins can change these. Ask one if something needs updating.
      </p>
    </OrganizersFormSection>
  </OrganizersEventFormShell>
</template>

<style scoped>
.gh-note {
  margin: 0;
  font-size: 14px;
  color: var(--gh-text-muted);
}
.gh-error {
  margin: 0;
  font-size: 14px;
  color: var(--gh-err);
}
.gh-token {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px 12px;
}
.gh-example {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 12px 16px;
  border: var(--gh-border-width) solid var(--gh-line-soft);
  border-radius: var(--radius-gh);
}
.gh-example__label {
  margin: 0;
  font-size: 14px;
  color: var(--gh-text-muted);
}
.gh-example__name {
  width: fit-content;
  max-width: 100%;
  overflow-wrap: anywhere;
  font-size: 15px;
  font-weight: 600;
}
.gh-fit { align-self: flex-start; }
</style>
