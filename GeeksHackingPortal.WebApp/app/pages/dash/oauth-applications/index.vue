<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsAdminOAuthApplicationsSharedOAuthApplicationPlatformKey,
  GeeksHackingPortalApiEndpointsAdminOAuthApplicationsSharedOAuthApplicationResponse,
} from '@geekshacking/portal-sdk'
import {
  geeksHackingPortalApiEndpointsAdminOAuthApplicationsAnalyticsEndpointQueryKey,
  geeksHackingPortalApiEndpointsAdminOAuthApplicationsListEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsAnalyticsEndpoint,
  useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsCreateEndpoint,
  useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsDeleteEndpoint,
  useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsListEndpoint,
  useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsUpdateEndpoint,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import { useClipboard } from '@vueuse/core'

/**
 * The OAuth apps the signed-in admin owns (apps that let people sign in with their portal
 * account). What the API does (Endpoints/Admin/OAuthApplications):
 * - admins only, and each admin sees and changes only the apps they own;
 * - Web apps get a client secret, shown once: on create, when "issue a new secret" is ticked,
 *   or when a Native app is switched to Web. Native apps have none;
 * - deleting an app also deletes its sign-ins and tokens, so everyone using it is signed out.
 */
type App = GeeksHackingPortalApiEndpointsAdminOAuthApplicationsSharedOAuthApplicationResponse
type Platform = GeeksHackingPortalApiEndpointsAdminOAuthApplicationsSharedOAuthApplicationPlatformKey

useHead({ title: 'My OAuth apps' })

const toast = useGToast()
const queryClient = useQueryClient()
const { copy, isSupported: canCopy } = useClipboard({ legacy: true })

const { data: user, isLoading: isLoadingUser } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const isAdmin = computed(() => user.value?.isRoot === true)
const enabled = { query: { enabled: isAdmin } }
const listQuery = useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsListEndpoint(enabled)
const analyticsQuery = useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsAnalyticsEndpoint(enabled)

const apps = computed(() => [...(listQuery.data.value?.items ?? [])]
  .sort((a, b) => (a.displayName ?? '').localeCompare(b.displayName ?? '')))
const usage = computed(() => new Map((analyticsQuery.data.value?.items ?? []).map(item => [item.applicationId, item])))
const plural = (n: number, one: string, many = `${one}s`) => `${n.toLocaleString()} ${n === 1 ? one : many}`

const PLATFORMS: { value: Platform, label: string, icon: string, description: string }[] = [
  { value: 'Web', label: 'Web', icon: 'material-symbols:dns-rounded', description: 'Runs on a server that can keep a secret. Gets a client secret.' },
  { value: 'Native', label: 'Native', icon: 'material-symbols:devices-rounded', description: 'A mobile, desktop or browser-only app that can\'t keep a secret. No client secret.' },
]
const platformOf = (value: Platform | null | undefined) => PLATFORMS.find(p => p.value === value) ?? PLATFORMS[0]!

async function copyText(text: string | null | undefined, what: string) {
  if (!text)
    return
  try {
    await copy(text)
    toast.add({ tone: 'ok', title: `Copied ${what}` })
  }
  catch {
    toast.add({ tone: 'err', title: 'Couldn\'t copy', description: 'Select it and copy it by hand.' })
  }
}

async function refreshLists() {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsAdminOAuthApplicationsListEndpointQueryKey() }),
    queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsAdminOAuthApplicationsAnalyticsEndpointQueryKey() }),
  ])
}

// ---- add / edit dialog ----
const createMutation = useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsCreateEndpoint()
const updateMutation = useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsUpdateEndpoint()
const isSaving = computed(() => createMutation.isPending.value || updateMutation.isPending.value)

const isFormOpen = ref(false)
const editing = ref<App | null>(null)
const form = reactive({
  displayName: '',
  clientId: '',
  platform: 'Web' as Platform,
  redirectUris: [''] as string[],
  postLogoutRedirectUris: [] as string[],
  rotateSecret: false,
})
type FieldErrors = Partial<Record<'displayName' | 'clientId' | 'redirectUris' | 'postLogoutRedirectUris', string>>
const errors = ref<FieldErrors>({})
const redirectRowErrors = ref<(string | undefined)[]>([])
const logoutRowErrors = ref<(string | undefined)[]>([])
const formError = ref('')
/** set after saving when the API issued a client secret: the dialog then shows it, once */
const issued = ref<{ name: string, clientId: string, secret: string, isNew: boolean } | null>(null)

const dialogTitle = computed(() => {
  if (issued.value)
    return issued.value.isNew ? `${issued.value.name} is ready` : `New secret for ${issued.value.name}`
  return editing.value ? `Edit ${editing.value.displayName || 'app'}` : 'Add an app'
})
/** switching a Native app to Web makes the API issue a secret even without the tick */
const getsSecretAnyway = computed(() => !!editing.value && editing.value.platform !== 'Web' && form.platform === 'Web')

function clearErrors() {
  errors.value = {}
  redirectRowErrors.value = []
  logoutRowErrors.value = []
  formError.value = ''
}
function openAdd() {
  editing.value = null
  issued.value = null
  Object.assign(form, { displayName: '', clientId: '', platform: 'Web', redirectUris: [''], postLogoutRedirectUris: [], rotateSecret: false })
  clearErrors()
  isFormOpen.value = true
}
function openEdit(app: App) {
  editing.value = app
  issued.value = null
  Object.assign(form, {
    displayName: app.displayName ?? '',
    clientId: app.clientId ?? '',
    platform: app.platform ?? 'Web',
    redirectUris: app.redirectUris?.length ? [...app.redirectUris] : [''],
    postLogoutRedirectUris: [...(app.postLogoutRedirectUris ?? [])],
    rotateSecret: false,
  })
  clearErrors()
  isFormOpen.value = true
}

// clear a field's error once it's changed
watch(() => form.displayName, () => (errors.value.displayName = undefined))
watch(() => form.clientId, () => (errors.value.clientId = undefined))
watch(() => [...form.redirectUris], () => {
  errors.value.redirectUris = undefined
  redirectRowErrors.value = []
})
watch(() => [...form.postLogoutRedirectUris], () => {
  errors.value.postLogoutRedirectUris = undefined
  logoutRowErrors.value = []
})
watch(() => form.platform, () => {
  redirectRowErrors.value = []
  logoutRowErrors.value = []
})

const cleanUris = (uris: string[]) => uris.map(uri => uri.trim()).filter(Boolean)

/** Same rules as the API's validator; blank rows are ignored. */
function checkUris(uris: string[], platform: Platform) {
  const seen = new Set<string>()
  return uris.map((uri) => {
    const value = uri.trim()
    if (!value)
      return undefined
    let url: URL
    try {
      url = new URL(value)
    }
    catch {
      return 'Use a full address, such as https://app.example.com/callback.'
    }
    if (platform === 'Web' && url.protocol !== 'http:' && url.protocol !== 'https:')
      return 'Web apps must use an http:// or https:// address.'
    if (seen.has(url.href))
      return 'This address is already in the list.'
    seen.add(url.href)
    return undefined
  })
}

function validate() {
  const next: FieldErrors = {}
  if (!form.displayName.trim())
    next.displayName = 'Give the app a name.'
  else if (form.displayName.trim().length > 200)
    next.displayName = 'Use 200 characters or fewer.'
  const clientId = form.clientId.trim()
  if (!clientId)
    next.clientId = 'Add a client ID.'
  else if (clientId.length < 3 || clientId.length > 100)
    next.clientId = 'Use 3 to 100 characters.'
  else if (!/^[\w.:-]+$/.test(clientId))
    next.clientId = 'Use only letters, numbers, dots, underscores, colons and hyphens.'
  if (!cleanUris(form.redirectUris).length)
    next.redirectUris = 'Add at least one redirect URI.'
  errors.value = next
  redirectRowErrors.value = checkUris(form.redirectUris, form.platform)
  logoutRowErrors.value = checkUris(form.postLogoutRedirectUris, form.platform)
  return !Object.keys(next).length && ![...redirectRowErrors.value, ...logoutRowErrors.value].some(Boolean)
}

/** The API names list errors like "redirectUris[2]", counting only the rows it was sent. */
function applyApiErrors(error: unknown) {
  const bag = getApiValidationErrors(error)
  errors.value = {
    displayName: getApiFieldError(bag, 'DisplayName'),
    clientId: getApiFieldError(bag, 'ClientId'),
    redirectUris: getApiFieldError(bag, 'RedirectUris'),
    postLogoutRedirectUris: getApiFieldError(bag, 'PostLogoutRedirectUris'),
  }
  const rowErrors = (uris: string[], name: string) => {
    let sent = -1
    return uris.map((uri) => {
      if (!uri.trim())
        return undefined
      sent++
      return getApiFieldError(bag, `${name}[${sent}]`)
    })
  }
  redirectRowErrors.value = rowErrors(form.redirectUris, 'RedirectUris')
  logoutRowErrors.value = rowErrors(form.postLogoutRedirectUris, 'PostLogoutRedirectUris')
}

function focusFirstError() {
  nextTick(() => document.querySelector<HTMLElement>('#oa-form [aria-invalid="true"]')?.focus())
}

async function save() {
  formError.value = ''
  if (!validate()) {
    focusFirstError()
    return
  }
  const body = {
    displayName: form.displayName.trim(),
    clientId: form.clientId.trim(),
    platform: form.platform,
    redirectUris: cleanUris(form.redirectUris),
    postLogoutRedirectUris: cleanUris(form.postLogoutRedirectUris),
  }
  try {
    const result = editing.value?.id
      ? await updateMutation.mutateAsync({ path: { id: editing.value.id }, body: { ...body, rotateClientSecret: form.platform === 'Web' && form.rotateSecret } })
      : await createMutation.mutateAsync({ body })
    const isNew = !editing.value
    await refreshLists()
    if (result?.clientSecret) {
      issued.value = { name: body.displayName, clientId: body.clientId, secret: result.clientSecret, isNew }
    }
    else {
      isFormOpen.value = false
      toast.add({ tone: 'ok', title: isNew ? 'App added' : 'App saved', description: body.displayName })
    }
  }
  catch (error) {
    applyApiErrors(error)
    formError.value = getApiValidationSummary(error) ?? getApiErrorMessage(error, 'The app wasn\'t saved. Try again.')
    focusFirstError()
  }
}

// ---- delete (inline, one at a time) ----
const deleteMutation = useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsDeleteEndpoint()
const confirmingId = ref<string | null>(null)
const deleteError = ref('')
function askDelete(app: App) {
  confirmingId.value = app.id ?? null
  deleteError.value = ''
}
async function confirmDelete(app: App) {
  if (!app.id)
    return
  deleteError.value = ''
  try {
    await deleteMutation.mutateAsync({ path: { id: app.id } })
    confirmingId.value = null
    await refreshLists()
    toast.add({ tone: 'ok', title: 'App deleted', description: app.displayName ?? undefined })
  }
  catch (error) {
    deleteError.value = getApiErrorMessage(error, 'It wasn\'t deleted. Try again.')
  }
}

const URIS_SHOWN = 2
const expanded = ref(new Set<string>())
function toggleUris(id: string) {
  const next = new Set(expanded.value)
  if (next.has(id))
    next.delete(id)
  else
    next.add(id)
  expanded.value = next
}
</script>

<template>
  <GPanel id="oauth-applications">
    <template #header>
      <GPanelHeader title="My OAuth apps">
        <template #right>
          <GButton
            v-if="isAdmin && apps.length"
            variant="primary"
            icon="material-symbols:add-rounded"
            label="Add app"
            @click="openAdd"
          />
        </template>
      </GPanelHeader>
    </template>

    <template #body>
      <div class="oa">
        <GLoading
          v-if="isLoadingUser || (isAdmin && listQuery.isLoading.value)"
          label="Loading your apps…"
        />

        <GEmptyState
          v-else-if="!isAdmin"
          icon="material-symbols:lock-rounded"
          title="Only admins can do this"
          description="OAuth apps are managed by portal admins."
          bordered
        />

        <GAlert
          v-else-if="listQuery.error.value && !apps.length"
          tone="err"
          title="Couldn't load your apps"
          :description="getApiErrorMessage(listQuery.error.value, 'Refresh to try again.')"
        />

        <GEmptyState
          v-else-if="!apps.length"
          icon="material-symbols:key-rounded"
          title="No apps yet"
          description="Add an app to let people sign in to it with their GeeksHacking account."
          bordered
        >
          <template #actions>
            <GButton
              variant="primary"
              icon="material-symbols:add-rounded"
              label="Add app"
              @click="openAdd"
            />
          </template>
        </GEmptyState>

        <template v-else>
          <p class="oa-intro">
            Apps that let people sign in with their GeeksHacking account. Only you can see and change the apps you own.
          </p>

          <ul class="oa-list">
            <li
              v-for="app in apps"
              :key="app.id"
              class="oa-app"
            >
              <div class="oa-app__head">
                <div class="oa-app__who">
                  <h2 class="oa-app__name">
                    {{ app.displayName || 'Untitled app' }}
                  </h2>
                  <div class="oa-app__id">
                    <span class="oa-muted">Client ID</span>
                    <code>{{ app.clientId }}</code>
                    <GButton
                      v-if="canCopy"
                      variant="ghost"
                      size="sm"
                      icon="material-symbols:content-copy-rounded"
                      :aria-label="`Copy ${app.displayName}'s client ID`"
                      @click="copyText(app.clientId, 'the client ID')"
                    />
                  </div>
                </div>
                <GBadge
                  variant="outline"
                  :icon="platformOf(app.platform).icon"
                  :label="platformOf(app.platform).label"
                />
              </div>

              <dl class="oa-facts">
                <div>
                  <dt>Sign-ins</dt>
                  <dd>
                    <GSkeleton
                      v-if="analyticsQuery.isLoading.value"
                      class="oa-fact-skel"
                    />
                    <template v-else>
                      {{ (usage.get(app.id)?.totalAuthorizations ?? 0).toLocaleString() }}
                    </template>
                  </dd>
                </div>
                <div>
                  <dt>People</dt>
                  <dd>
                    <GSkeleton
                      v-if="analyticsQuery.isLoading.value"
                      class="oa-fact-skel"
                    />
                    <template v-else>
                      {{ (usage.get(app.id)?.uniqueUsers ?? 0).toLocaleString() }}
                    </template>
                  </dd>
                </div>
              </dl>

              <div class="oa-uris">
                <p class="oa-uris__label">
                  Redirect URIs
                </p>
                <ul>
                  <li
                    v-for="uri in expanded.has(app.id ?? '') ? app.redirectUris : app.redirectUris?.slice(0, URIS_SHOWN)"
                    :key="uri"
                  >
                    <code>{{ uri }}</code>
                  </li>
                </ul>
                <template v-if="app.postLogoutRedirectUris?.length && expanded.has(app.id ?? '')">
                  <p class="oa-uris__label">
                    After sign-out
                  </p>
                  <ul>
                    <li
                      v-for="uri in app.postLogoutRedirectUris"
                      :key="uri"
                    >
                      <code>{{ uri }}</code>
                    </li>
                  </ul>
                </template>
                <GButton
                  v-if="(app.redirectUris?.length ?? 0) > URIS_SHOWN || app.postLogoutRedirectUris?.length"
                  variant="ghost"
                  size="sm"
                  class="oa-fit"
                  :aria-expanded="expanded.has(app.id ?? '')"
                  :label="expanded.has(app.id ?? '') ? 'Show fewer' : `Show all addresses (${(app.redirectUris?.length ?? 0) + (app.postLogoutRedirectUris?.length ?? 0)})`"
                  @click="toggleUris(app.id ?? '')"
                />
              </div>

              <div
                v-if="confirmingId === app.id"
                class="oa-confirm"
                role="group"
                :aria-label="`Delete ${app.displayName}?`"
              >
                <p class="oa-confirm__text">
                  <strong>Delete {{ app.displayName }}?</strong>
                  Everyone signed in through it is signed out, and its sign-in history is deleted. This can't be undone.
                </p>
                <p
                  v-if="deleteError"
                  class="oa-error"
                  role="alert"
                >
                  {{ deleteError }}
                </p>
                <div class="oa-actions">
                  <GButton
                    variant="ghost"
                    size="sm"
                    label="Keep it"
                    :disabled="deleteMutation.isPending.value"
                    @click="confirmingId = null"
                  />
                  <GButton
                    variant="danger"
                    size="sm"
                    icon="material-symbols:delete-rounded"
                    label="Delete app"
                    :loading="deleteMutation.isPending.value"
                    loading-label="Deleting"
                    @click="confirmDelete(app)"
                  />
                </div>
              </div>
              <div
                v-else
                class="oa-actions"
              >
                <GButton
                  variant="ghost"
                  size="sm"
                  icon="material-symbols:history-rounded"
                  label="Sign-in history"
                  :to="`/dash/oauth-applications/${app.id}`"
                />
                <GButton
                  variant="secondary"
                  size="sm"
                  icon="material-symbols:edit-rounded"
                  label="Edit"
                  @click="openEdit(app)"
                />
                <GButton
                  variant="ghost"
                  size="sm"
                  icon="material-symbols:delete-rounded"
                  :aria-label="`Delete ${app.displayName}`"
                  @click="askDelete(app)"
                />
              </div>
            </li>
          </ul>
        </template>
      </div>

      <GModal
        v-model:open="isFormOpen"
        :title="dialogTitle"
        :description="issued ? 'Copy the client secret now and keep it somewhere safe. It won\'t be shown again.' : undefined"
        :dismissible="!issued && !isSaving"
        size="lg"
      >
        <!-- the secret, shown once -->
        <div
          v-if="issued"
          class="oa-issued"
        >
          <dl class="oa-issued__pairs">
            <div>
              <dt>Client ID</dt>
              <dd><code>{{ issued.clientId }}</code></dd>
            </div>
            <div>
              <dt>Client secret</dt>
              <dd><code class="oa-issued__secret">{{ issued.secret }}</code></dd>
            </div>
          </dl>
          <GButton
            v-if="canCopy"
            variant="secondary"
            icon="material-symbols:content-copy-rounded"
            label="Copy secret"
            @click="copyText(issued.secret, 'the client secret')"
          />
          <GAlert
            v-if="!issued.isNew"
            tone="attention"
            description="The old secret has stopped working. Update the app with the new one."
          />
        </div>

        <form
          v-else
          id="oa-form"
          class="oa-form"
          novalidate
          @submit.prevent="save"
        >
          <div class="oa-pair">
            <GFormField
              label="Name"
              required
              :error="errors.displayName"
            >
              <GInput
                v-model="form.displayName"
                autocomplete="off"
                placeholder="Hackathon judging app"
                autofocus
              />
            </GFormField>
            <GFormField
              label="Client ID"
              required
              hint="What the app sends when it signs people in. Letters, numbers, dots, underscores, colons or hyphens."
              :error="errors.clientId"
            >
              <GInput
                v-model="form.clientId"
                autocomplete="off"
                spellcheck="false"
                autocapitalize="off"
                placeholder="judging-app"
              />
            </GFormField>
          </div>

          <fieldset class="oa-platform">
            <legend class="oa-platform__legend">
              Kind of app
            </legend>
            <div class="oa-platform__options">
              <label
                v-for="option in PLATFORMS"
                :key="option.value"
                class="oa-option"
                :class="{ 'is-on': form.platform === option.value }"
              >
                <input
                  v-model="form.platform"
                  type="radio"
                  name="oa-platform"
                  :value="option.value"
                  class="sr-only"
                >
                <Icon
                  :name="option.icon"
                  class="oa-option__icon"
                  aria-hidden="true"
                />
                <span>
                  <span class="oa-option__label">{{ option.label }}</span>
                  <span class="oa-option__text">{{ option.description }}</span>
                </span>
              </label>
            </div>
          </fieldset>

          <OauthUriListField
            v-model="form.redirectUris"
            label="Redirect URIs"
            item-label="redirect URI"
            hint="Where people are sent back after signing in. The app must use one of these exactly."
            placeholder="https://app.example.com/callback"
            required
            :error="errors.redirectUris"
            :row-errors="redirectRowErrors"
          />
          <OauthUriListField
            v-model="form.postLogoutRedirectUris"
            label="After sign-out"
            item-label="sign-out address"
            hint="Optional. Where people can be sent after signing out."
            placeholder="https://app.example.com/signed-out"
            :error="errors.postLogoutRedirectUris"
            :row-errors="logoutRowErrors"
          />

          <template v-if="editing && form.platform === 'Web'">
            <GAlert
              v-if="getsSecretAnyway"
              tone="attention"
              description="Switching to Web gives the app a client secret. You'll see it after saving."
            />
            <GCheckbox
              v-else
              v-model="form.rotateSecret"
              label="Issue a new client secret"
              description="The current secret stops working as soon as you save. You'll see the new one once."
            />
          </template>
          <GAlert
            v-if="editing && editing.platform === 'Web' && form.platform === 'Native'"
            tone="attention"
            description="Native apps have no client secret: the current one stops working when you save."
          />

          <GAlert
            v-if="formError"
            tone="err"
            title="Not saved"
            :description="formError"
          />
        </form>

        <template #footer>
          <GButton
            v-if="issued"
            variant="primary"
            label="I've copied it"
            @click="isFormOpen = false"
          />
          <template v-else>
            <GButton
              variant="ghost"
              label="Cancel"
              :disabled="isSaving"
              @click="isFormOpen = false"
            />
            <GButton
              type="submit"
              form="oa-form"
              variant="primary"
              :label="editing ? 'Save changes' : 'Add app'"
              :loading="isSaving"
              loading-label="Saving"
            />
          </template>
        </template>
      </GModal>
    </template>
  </GPanel>
</template>

<style scoped>
.oa {
  display: flex;
  flex-direction: column;
  gap: 20px;
  width: 100%;
  max-width: 80rem;
  margin: 0 auto;
}
.oa-intro {
  margin: 0;
  color: var(--gh-text-muted);
}
.oa-muted { color: var(--gh-text-muted); }
.oa-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 26rem), 1fr));
  gap: 20px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.oa-app {
  display: flex;
  flex-direction: column;
  gap: 16px;
  min-width: 0;
  padding: 20px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.oa-app__head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}
.oa-app__who { min-width: 0; }
.oa-app__name {
  margin: 0;
  font-size: 18px;
  font-weight: 700;
  overflow-wrap: anywhere;
}
.oa-app__id {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 2px 8px;
  margin-top: 2px;
  font-size: 14px;
}
.oa-app__id code { overflow-wrap: anywhere; }
.oa-facts {
  display: flex;
  gap: 32px;
  margin: 0;
}
.oa-facts dt {
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.oa-facts dd {
  margin: 2px 0 0;
  font-size: 24px;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
}
.oa-fact-skel {
  display: block;
  width: 3rem;
  height: 28px;
}
.oa-uris {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 0;
}
.oa-uris__label {
  margin: 0;
  font-size: 13px;
  font-weight: 600;
  color: var(--gh-text-muted);
}
.oa-uris ul {
  display: flex;
  flex-direction: column;
  gap: 4px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.oa-uris code {
  display: block;
  padding: 4px 8px;
  border: 1px solid var(--gh-line-soft);
  border-radius: var(--radius-gh);
  font-size: 13px;
  overflow-wrap: anywhere;
}
.oa-fit { align-self: flex-start; }
.oa-actions {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 8px;
  margin-top: auto;
  padding-top: 12px;
  border-top: 1px solid var(--gh-line-soft);
}
.oa-confirm {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: auto;
  padding: 12px 14px;
  border: var(--gh-border-width) solid var(--gh-err);
  border-radius: var(--radius-gh);
}
.oa-confirm .oa-actions {
  margin-top: 0;
  padding-top: 0;
  border-top: 0;
}
.oa-confirm__text {
  margin: 0;
  font-size: 14px;
}
.oa-confirm__text strong {
  display: block;
  margin-bottom: 2px;
}
.oa-error {
  margin: 0;
  font-size: 14px;
  color: var(--gh-err);
}
.oa-form {
  display: flex;
  flex-direction: column;
  gap: 20px;
}
.oa-pair {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 16px;
}
@media (min-width: 40rem) {
  .oa-pair { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
.oa-platform {
  margin: 0;
  padding: 0;
  border: 0;
}
.oa-platform__legend {
  margin-bottom: 6px;
  font-size: 14px;
  font-weight: 600;
}
.oa-platform__options {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(14rem, 1fr));
  gap: 10px;
}
.oa-option {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 12px 14px;
  border: var(--gh-border-width) solid var(--gh-line-soft);
  border-radius: var(--radius-gh);
  cursor: pointer;
}
.oa-option:hover { background: var(--gh-ghost-hover); }
/* chosen: same as the invite dialog's role cards (yellow fill, dark text) */
.oa-option.is-on {
  border-color: var(--gh-line);
  background: var(--gh-accent);
  color: var(--gh-on-accent);
  box-shadow: 3px 3px 0 var(--gh-line);
}
.oa-option.is-on .oa-option__text { color: inherit; }
.oa-option:has(input:focus-visible) {
  outline: 2px solid var(--gh-focus);
  outline-offset: 2px;
}
.oa-option__icon {
  flex: none;
  margin-top: 2px;
  font-size: 20px;
}
.oa-option__label {
  display: block;
  font-weight: 700;
}
.oa-option__text {
  display: block;
  font-size: 13px;
  color: var(--gh-text-muted);
}
.oa-issued {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 14px;
}
.oa-issued__pairs {
  display: flex;
  flex-direction: column;
  gap: 12px;
  width: 100%;
  margin: 0;
}
.oa-issued__pairs dt {
  font-size: 13px;
  font-weight: 600;
  color: var(--gh-text-muted);
}
.oa-issued__pairs dd { margin: 4px 0 0; overflow-wrap: anywhere; }
.oa-issued__secret {
  display: block;
  padding: 12px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh);
  font-size: 15px;
  overflow-wrap: anywhere;
}
</style>
