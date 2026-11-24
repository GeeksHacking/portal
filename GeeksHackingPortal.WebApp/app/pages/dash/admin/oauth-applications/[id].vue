<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsAdminOAuthDirectoryGetEndpoint,
  useGeeksHackingPortalApiEndpointsAdminOAuthDirectoryHistoryEndpoint,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useClipboard } from '@vueuse/core'

/**
 * One OAuth app from the admin directory (any owner), read-only: usage, settings in plain words,
 * what it's allowed to do, and its sign-ins. The API returns the latest 500 sign-ins plus the total.
 */
const route = useRoute()
const appId = computed(() => (route.params.id as string | undefined) ?? '')

const { data: user, isLoading: isLoadingUser } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const isAdmin = computed(() => user.value?.isRoot === true)
const options = { query: { enabled: computed(() => isAdmin.value && !!appId.value), retry: false } }
const path = computed(() => ({ id: appId.value }))
const detailQuery = useGeeksHackingPortalApiEndpointsAdminOAuthDirectoryGetEndpoint({ path }, options)
const historyQuery = useGeeksHackingPortalApiEndpointsAdminOAuthDirectoryHistoryEndpoint({ path }, options)

const app = computed(() => detailQuery.data.value?.application)
const isMine = computed(() => !!app.value?.ownerUserId && app.value.ownerUserId === user.value?.id)
const permissions = computed(() => groupOAuthPermissions(detailQuery.data.value?.permissions))
const requirements = computed(() => detailQuery.data.value?.requirements ?? [])

useHead(() => ({ title: app.value?.displayName ?? 'OAuth app' }))

const toast = useGToast()
const { copy, isSupported: canCopy } = useClipboard({ legacy: true })
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

const when = (value: string | null | undefined, fallback = '–') => (value ? formatOrganizerDateTime(value) : fallback)

// ---- sign-ins ----
const items = computed(() => historyQuery.data.value?.items ?? [])
const total = computed(() => historyQuery.data.value?.totalCount ?? items.value.length)
type StatusFilter = 'all' | 'valid' | 'revoked' | 'other'
const status = ref<string>('all' satisfies StatusFilter)
const search = ref('')
const statusTabs = computed(() => {
  const count = (fn: (s: string | null | undefined) => boolean) => items.value.filter(i => fn(i.status)).length
  return [
    { value: 'all', label: `All (${items.value.length})` },
    { value: 'valid', label: `Allowed (${count(s => s === 'valid')})` },
    { value: 'revoked', label: `Revoked (${count(s => s === 'revoked')})` },
    ...(count(s => s !== 'valid' && s !== 'revoked') ? [{ value: 'other', label: `Other (${count(s => s !== 'valid' && s !== 'revoked')})` }] : []),
  ]
})
const shown = computed(() => {
  const term = search.value.trim().toLowerCase()
  return items.value.filter((item) => {
    const s = item.status
    const statusOk = status.value === 'all'
      || (status.value === 'other' ? s !== 'valid' && s !== 'revoked' : s === status.value)
    return statusOk && (!term || [item.userName, item.userEmail, item.subject].some(v => v?.toLowerCase().includes(term)))
  })
})
</script>

<template>
  <GPanel id="admin-oauth-application">
    <template #header>
      <GPanelHeader title="All OAuth apps" />
    </template>

    <template #body>
      <div class="ap">
        <GButton
          to="/dash/admin/oauth-applications"
          variant="ghost"
          size="sm"
          icon="material-symbols:arrow-back-rounded"
          label="All OAuth apps"
          class="ap-fit"
        />

        <GLoading
          v-if="isLoadingUser || (isAdmin && detailQuery.isLoading.value)"
          label="Loading the app…"
        />

        <GEmptyState
          v-else-if="!isAdmin"
          icon="material-symbols:lock-rounded"
          title="Only admins can do this"
          description="The list of OAuth apps is for portal admins."
          bordered
        />

        <GEmptyState
          v-else-if="!app"
          icon="material-symbols:error-rounded"
          title="Couldn't find this app"
          description="It may have been deleted, or the link is wrong."
          bordered
        />

        <template v-else>
          <!-- who and what -->
          <div class="ap-head">
            <div class="ap-head__who">
              <h2 class="ap-title">
                {{ app.displayName }}
              </h2>
              <div class="ap-id">
                <span class="ap-muted">Client ID</span>
                <code>{{ app.clientId }}</code>
                <GButton
                  v-if="canCopy"
                  variant="ghost"
                  size="sm"
                  icon="material-symbols:content-copy-rounded"
                  aria-label="Copy the client ID"
                  @click="copyText(app.clientId, 'the client ID')"
                />
              </div>
              <p class="ap-owner">
                <GBadge
                  variant="outline"
                  :icon="app.platform === 'Native' ? 'material-symbols:devices-rounded' : 'material-symbols:dns-rounded'"
                  :label="app.platform ?? 'Web'"
                />
                <span>
                  Owned by
                  <template v-if="app.ownerName || app.ownerEmail">
                    <strong>{{ app.ownerName || app.ownerEmail }}</strong><template v-if="app.ownerName && app.ownerEmail">
                      ({{ app.ownerEmail }})
                    </template>
                  </template>
                  <span
                    v-else
                    class="ap-muted"
                  >{{ app.ownerUserId ? 'an unknown user' : 'no one' }}</span>
                </span>
                <GBadge
                  v-if="isMine"
                  size="sm"
                  label="You"
                />
              </p>
            </div>
            <GButton
              v-if="isMine"
              variant="secondary"
              icon="material-symbols:edit-rounded"
              label="Edit in My OAuth apps"
              to="/dash/oauth-applications"
            />
          </div>

          <div class="ap-tiles">
            <GStatTile
              label="Sign-ins"
              :value="app.totalAuthorizations ?? 0"
              icon="material-symbols:login-rounded"
              :hint="`Last ${when(app.lastAuthorizedAt, 'never')}`"
            />
            <GStatTile
              label="Still allowed"
              :value="app.validAuthorizations ?? 0"
              icon="material-symbols:verified-user-rounded"
              hint="Sign-ins not revoked"
            />
            <GStatTile
              label="People"
              :value="app.uniqueUsers ?? 0"
              icon="material-symbols:group-rounded"
              hint="Who have signed in"
            />
            <GStatTile
              label="Tokens issued"
              :value="app.totalTokens ?? 0"
              icon="material-symbols:key-rounded"
              :hint="`Last ${when(app.lastTokenIssuedAt, 'never')}`"
            />
          </div>

          <div class="ap-grid">
            <section
              class="ap-card"
              aria-labelledby="ap-settings"
            >
              <h3
                id="ap-settings"
                class="ap-card__title"
              >
                Settings
              </h3>
              <dl class="ap-dl">
                <div>
                  <dt>Client type</dt>
                  <dd>{{ oauthClientTypeLabel(app.clientType) }}</dd>
                </div>
                <div>
                  <dt>Consent</dt>
                  <dd>{{ oauthConsentTypeLabel(app.consentType) }}</dd>
                </div>
                <div>
                  <dt>Redirect URIs</dt>
                  <dd>
                    <ul class="ap-uris">
                      <li
                        v-for="uri in app.redirectUris"
                        :key="uri"
                      >
                        <code>{{ uri }}</code>
                      </li>
                    </ul>
                  </dd>
                </div>
                <div v-if="app.postLogoutRedirectUris?.length">
                  <dt>After sign-out</dt>
                  <dd>
                    <ul class="ap-uris">
                      <li
                        v-for="uri in app.postLogoutRedirectUris"
                        :key="uri"
                      >
                        <code>{{ uri }}</code>
                      </li>
                    </ul>
                  </dd>
                </div>
                <div>
                  <dt>Internal ID</dt>
                  <dd><code class="ap-small">{{ app.id }}</code></dd>
                </div>
              </dl>
            </section>

            <section
              class="ap-card"
              aria-labelledby="ap-allowed"
            >
              <h3
                id="ap-allowed"
                class="ap-card__title"
              >
                What it's allowed to do
              </h3>
              <p
                v-if="!permissions.length && !requirements.length"
                class="ap-muted"
              >
                No permissions recorded.
              </p>
              <dl
                v-else
                class="ap-dl"
              >
                <div
                  v-for="group in permissions"
                  :key="group.group"
                >
                  <dt>{{ group.title }}</dt>
                  <dd>
                    <ul class="ap-perms">
                      <li
                        v-for="item in group.items"
                        :key="item.code"
                      >
                        {{ item.label }} <code>{{ item.code }}</code>
                      </li>
                    </ul>
                  </dd>
                </div>
                <div v-if="requirements.length">
                  <dt>Requirements</dt>
                  <dd>
                    <ul class="ap-perms">
                      <li
                        v-for="code in requirements"
                        :key="code"
                      >
                        {{ oauthRequirementLabel(code) }} <code>{{ code }}</code>
                      </li>
                    </ul>
                  </dd>
                </div>
              </dl>
            </section>
          </div>

          <!-- sign-ins -->
          <section
            class="ap-history"
            aria-labelledby="ap-history"
          >
            <div class="ap-history__head">
              <div>
                <h3
                  id="ap-history"
                  class="ap-card__title"
                >
                  Sign-ins
                </h3>
                <p class="ap-muted">
                  <template v-if="total > items.length">
                    The latest {{ items.length.toLocaleString() }} of {{ total.toLocaleString() }}, newest first.
                  </template>
                  <template v-else-if="items.length">
                    {{ total.toLocaleString() }} {{ total === 1 ? 'sign-in' : 'sign-ins' }}, newest first.
                  </template>
                </p>
              </div>
            </div>

            <GLoading
              v-if="historyQuery.isLoading.value"
              label="Loading sign-ins…"
            />
            <GAlert
              v-else-if="historyQuery.error.value"
              tone="err"
              title="Couldn't load sign-ins"
              :description="getApiErrorMessage(historyQuery.error.value, 'Refresh to try again.')"
            />
            <GEmptyState
              v-else-if="!items.length"
              size="sm"
              icon="material-symbols:history-rounded"
              title="No sign-ins yet"
              description="Each time someone allows this app to use their GeeksHacking account, it shows here."
              bordered
            />
            <template v-else>
              <div class="ap-controls">
                <GTabs
                  v-model="status"
                  :items="statusTabs"
                  :content="false"
                  size="sm"
                  aria-label="Sign-in status"
                />
                <GFormField
                  label="Find a person"
                  hide-label
                  class="ap-search"
                >
                  <GInput
                    v-model="search"
                    type="search"
                    icon="material-symbols:search-rounded"
                    placeholder="Find by name or email"
                  />
                </GFormField>
              </div>

              <p
                v-if="!shown.length"
                class="ap-muted"
              >
                {{ search ? `No one matches “${search}”.` : 'No sign-ins with this status.' }}
              </p>
              <div
                v-else
                class="ap-table-wrap"
              >
                <table class="ap-table">
                  <thead>
                    <tr>
                      <th scope="col">
                        Person
                      </th>
                      <th scope="col">
                        When
                      </th>
                      <th scope="col">
                        Status
                      </th>
                      <th scope="col">
                        Allowed
                      </th>
                      <th scope="col">
                        Tokens
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr
                      v-for="item in shown"
                      :key="item.id"
                    >
                      <th scope="row">
                        <span class="ap-name">{{ item.userName || 'Unknown user' }}</span>
                        <span class="ap-sub">{{ item.userEmail || item.subject }}</span>
                      </th>
                      <td class="ap-when">
                        {{ when(item.creationDate) }}
                      </td>
                      <td>
                        <GBadge
                          :tone="oauthStatusBadge(item.status).tone"
                          variant="outline"
                          size="sm"
                          :label="oauthStatusBadge(item.status).label"
                        />
                      </td>
                      <td>
                        <ul
                          v-if="oauthScopesOf(item.scopes).length"
                          class="ap-scopes"
                        >
                          <li
                            v-for="scope in oauthScopesOf(item.scopes)"
                            :key="scope"
                          >
                            <code>{{ scope }}</code>
                          </li>
                        </ul>
                        <span
                          v-else
                          class="ap-muted"
                        >–</span>
                      </td>
                      <td>
                        <span class="ap-name">{{ (item.tokenCount ?? 0).toLocaleString() }} {{ item.tokenCount === 1 ? 'token' : 'tokens' }}</span>
                        <span
                          v-if="item.lastTokenIssuedAt"
                          class="ap-sub"
                        >last {{ when(item.lastTokenIssuedAt) }}</span>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </template>
          </section>
        </template>
      </div>
    </template>
  </GPanel>
</template>

<style scoped>
.ap {
  display: flex;
  flex-direction: column;
  gap: 20px;
  width: 100%;
  max-width: 80rem;
  margin: 0 auto;
}
.ap-fit { align-self: flex-start; }
.ap-muted {
  margin: 0;
  color: var(--gh-text-muted);
}
.ap-small { font-size: 13px; overflow-wrap: anywhere; }
.ap-head {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px 24px;
}
.ap-head__who {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 0;
}
.ap-title {
  margin: 0;
  font-size: 28px;
  font-weight: 700;
  overflow-wrap: anywhere;
}
.ap-id {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 2px 8px;
}
.ap-id code { overflow-wrap: anywhere; }
.ap-owner {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px 10px;
  margin: 0;
}
.ap-tiles {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}
@media (min-width: 60rem) { .ap-tiles { grid-template-columns: repeat(4, minmax(0, 1fr)); } }
.ap-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 26rem), 1fr));
  gap: 20px;
}
.ap-card,
.ap-history {
  display: flex;
  flex-direction: column;
  gap: 14px;
  min-width: 0;
}
.ap-card {
  padding: 20px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.ap-card__title {
  margin: 0;
  font-size: 18px;
  font-weight: 700;
}
.ap-dl {
  display: flex;
  flex-direction: column;
  gap: 14px;
  margin: 0;
}
.ap-dl dt {
  font-size: 13px;
  font-weight: 600;
  color: var(--gh-text-muted);
}
.ap-dl dd { margin: 4px 0 0; min-width: 0; }
.ap-uris,
.ap-perms {
  display: flex;
  flex-direction: column;
  gap: 4px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.ap-uris code {
  display: block;
  padding: 4px 8px;
  border: 1px solid var(--gh-line-soft);
  border-radius: var(--radius-gh);
  font-size: 13px;
  overflow-wrap: anywhere;
}
.ap-perms li {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 2px 8px;
}
.ap-perms code {
  font-size: 12px;
  color: var(--gh-text-muted);
}
.ap-history__head {
  display: flex;
  justify-content: space-between;
  gap: 12px;
}
.ap-controls {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 12px 16px;
}
.ap-search { flex: 0 1 22rem; }
.ap-table-wrap {
  overflow-x: auto;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.ap-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 14px;
}
.ap-table th,
.ap-table td {
  padding: 12px 16px;
  border-top: 1px solid var(--gh-line-soft);
  text-align: left;
  vertical-align: top;
}
.ap-table thead th {
  border-top: 0;
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.ap-name {
  display: block;
  font-weight: 600;
}
.ap-sub {
  display: block;
  font-weight: 400;
  color: var(--gh-text-muted);
  overflow-wrap: anywhere;
}
.ap-when { white-space: nowrap; }
.ap-scopes {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.ap-scopes code {
  display: inline-block;
  padding: 1px 6px;
  border: 1px solid var(--gh-line-soft);
  border-radius: var(--radius-gh-pill);
  font-size: 12px;
}
@media (max-width: 48rem) {
  .ap-table,
  .ap-table tbody { display: block; }
  .ap-table thead { display: none; }
  .ap-table tr {
    display: flex;
    flex-direction: column;
    gap: 6px;
    padding: 12px 16px;
    border-top: 1px solid var(--gh-line-soft);
  }
  .ap-table tbody tr:first-child { border-top: 0; }
  .ap-table th,
  .ap-table td {
    padding: 0;
    border-top: 0;
  }
  .ap-when { white-space: normal; color: var(--gh-text-muted); }
}
</style>
