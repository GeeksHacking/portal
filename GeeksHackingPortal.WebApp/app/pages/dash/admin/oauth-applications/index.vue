<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsAdminOAuthDirectoryListEndpoint,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
} from '@geekshacking/portal-sdk/hooks'

/**
 * Every OAuth app on the portal, from all admins (read-only; owners change their own apps from
 * My OAuth apps). The API lists them most recently signed-in first.
 */
useHead({ title: 'All OAuth apps' })

const { data: user, isLoading: isLoadingUser } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const isAdmin = computed(() => user.value?.isRoot === true)
const directoryQuery = useGeeksHackingPortalApiEndpointsAdminOAuthDirectoryListEndpoint({ query: { enabled: isAdmin } })

const apps = computed(() => directoryQuery.data.value?.items ?? [])
const totals = computed(() => apps.value.reduce(
  (sum, app) => ({ signIns: sum.signIns + (app.totalAuthorizations ?? 0), tokens: sum.tokens + (app.totalTokens ?? 0) }),
  { signIns: 0, tokens: 0 },
))
const mine = computed(() => apps.value.filter(app => app.ownerUserId && app.ownerUserId === user.value?.id).length)

type KindFilter = 'all' | 'Web' | 'Native'
const kind = ref<string>('all' satisfies KindFilter)
const search = ref('')
const kindTabs = computed(() => [
  { value: 'all', label: `All (${apps.value.length})` },
  { value: 'Web', label: `Web (${apps.value.filter(a => a.platform === 'Web').length})` },
  { value: 'Native', label: `Native (${apps.value.filter(a => a.platform === 'Native').length})` },
])
const shown = computed(() => {
  const term = search.value.trim().toLowerCase()
  return apps.value.filter(app => (kind.value === 'all' || app.platform === kind.value)
    && (!term || [app.displayName, app.clientId, app.ownerName, app.ownerEmail].some(v => v?.toLowerCase().includes(term))))
})

const when = (value: string | null | undefined) => (value ? formatOrganizerDateTime(value) : 'Never')
const isMine = (ownerUserId: string | null | undefined) => !!ownerUserId && ownerUserId === user.value?.id
</script>

<template>
  <GPanel id="admin-oauth-applications">
    <template #header>
      <GPanelHeader title="All OAuth apps" />
    </template>

    <template #body>
      <div class="ad">
        <GLoading
          v-if="isLoadingUser || (isAdmin && directoryQuery.isLoading.value)"
          label="Loading apps…"
        />

        <GEmptyState
          v-else-if="!isAdmin"
          icon="material-symbols:lock-rounded"
          title="Only admins can do this"
          description="The list of OAuth apps is for portal admins."
          bordered
        />

        <GAlert
          v-else-if="directoryQuery.error.value && !apps.length"
          tone="err"
          title="Couldn't load apps"
          :description="getApiErrorMessage(directoryQuery.error.value, 'Refresh to try again.')"
        />

        <GEmptyState
          v-else-if="!apps.length"
          icon="material-symbols:apps-rounded"
          title="No OAuth apps yet"
          description="Apps added by any admin show here."
          bordered
        >
          <template #actions>
            <GButton
              variant="secondary"
              label="Go to My OAuth apps"
              to="/dash/oauth-applications"
            />
          </template>
        </GEmptyState>

        <template v-else>
          <p class="ad-intro">
            Every OAuth app on the portal, from all admins. This list is read-only: each app's owner changes it from
            <NuxtLink
              to="/dash/oauth-applications"
              class="ad-link"
            >My OAuth apps</NuxtLink>.
          </p>

          <div class="ad-tiles">
            <GStatTile
              label="Apps"
              :value="apps.length"
              icon="material-symbols:apps-rounded"
              :hint="mine ? `${mine} of them yours` : 'None of them yours'"
            />
            <GStatTile
              label="Sign-ins"
              :value="totals.signIns"
              icon="material-symbols:login-rounded"
              hint="Across all apps"
            />
            <GStatTile
              label="Tokens issued"
              :value="totals.tokens"
              icon="material-symbols:key-rounded"
              hint="Across all apps"
            />
          </div>

          <div class="ad-controls">
            <GTabs
              v-model="kind"
              :items="kindTabs"
              :content="false"
              size="sm"
              aria-label="Kind of app"
            />
            <GFormField
              label="Find an app"
              hide-label
              class="ad-search"
            >
              <GInput
                v-model="search"
                type="search"
                icon="material-symbols:search-rounded"
                placeholder="Find by name, client ID or owner"
              />
            </GFormField>
          </div>

          <p
            v-if="!shown.length"
            class="ad-muted"
          >
            {{ search ? `No app matches “${search}”.` : 'No apps of this kind.' }}
          </p>

          <div
            v-else
            class="ad-table-wrap"
          >
            <table class="ad-table">
              <caption class="sr-only">
                OAuth apps, most recently signed in first
              </caption>
              <thead>
                <tr>
                  <th scope="col">
                    App
                  </th>
                  <th scope="col">
                    Owner
                  </th>
                  <th
                    scope="col"
                    class="ad-num"
                  >
                    Sign-ins
                  </th>
                  <th
                    scope="col"
                    class="ad-num"
                  >
                    People
                  </th>
                  <th scope="col">
                    Last sign-in
                  </th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="app in shown"
                  :key="app.id"
                >
                  <th scope="row">
                    <div class="ad-app">
                      <NuxtLink
                        :to="`/dash/admin/oauth-applications/${app.id}`"
                        class="ad-app__name"
                      >
                        {{ app.displayName || 'Untitled app' }}
                      </NuxtLink>
                      <GBadge
                        variant="outline"
                        size="sm"
                        :icon="app.platform === 'Native' ? 'material-symbols:devices-rounded' : 'material-symbols:dns-rounded'"
                        :label="app.platform ?? 'Web'"
                      />
                    </div>
                    <code class="ad-app__id">{{ app.clientId }}</code>
                  </th>
                  <td>
                    <template v-if="app.ownerName || app.ownerEmail">
                      <span class="ad-owner">
                        {{ app.ownerName }}
                        <GBadge
                          v-if="isMine(app.ownerUserId)"
                          size="sm"
                          label="You"
                        />
                      </span>
                      <span class="ad-sub">{{ app.ownerEmail }}</span>
                    </template>
                    <span
                      v-else
                      class="ad-muted"
                    >{{ app.ownerUserId ? 'Unknown user' : 'No owner' }}</span>
                  </td>
                  <td class="ad-num">
                    <span class="ad-cell-label">Sign-ins</span>{{ (app.totalAuthorizations ?? 0).toLocaleString() }}
                  </td>
                  <td class="ad-num">
                    <span class="ad-cell-label">People</span>{{ (app.uniqueUsers ?? 0).toLocaleString() }}
                  </td>
                  <td :class="{ 'ad-muted': !app.lastAuthorizedAt }">
                    <span class="ad-cell-label">Last sign-in</span>{{ when(app.lastAuthorizedAt) }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </template>
      </div>
    </template>
  </GPanel>
</template>

<style scoped>
.ad {
  display: flex;
  flex-direction: column;
  gap: 20px;
  width: 100%;
  max-width: 80rem;
  margin: 0 auto;
}
.ad-intro,
.ad-muted {
  margin: 0;
  color: var(--gh-text-muted);
}
.ad-link {
  color: var(--gh-text);
  font-weight: 600;
  text-decoration: underline;
  text-underline-offset: 3px;
}
.ad-tiles {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr));
  gap: 16px;
}
.ad-controls {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 12px 16px;
}
.ad-search { flex: 0 1 24rem; }
.ad-table-wrap {
  overflow-x: auto;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.ad-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 14px;
}
.ad-table th,
.ad-table td {
  padding: 12px 16px;
  border-top: 1px solid var(--gh-line-soft);
  text-align: left;
  vertical-align: top;
}
.ad-table thead th {
  border-top: 0;
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.ad-table tbody tr:hover { background: var(--gh-ghost-hover); }
.ad-num {
  text-align: right !important;
  font-variant-numeric: tabular-nums;
}
.ad-app {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px 8px;
}
.ad-app__name {
  font-size: 15px;
  font-weight: 700;
  color: var(--gh-text);
  text-decoration: underline;
  text-decoration-color: var(--gh-line-soft);
  text-underline-offset: 3px;
  overflow-wrap: anywhere;
}
.ad-app__name:hover { text-decoration-color: currentColor; }
/* the whole row opens the app */
.ad-table tbody tr { position: relative; }
.ad-app__name::after {
  content: '';
  position: absolute;
  inset: 0;
}
.ad-app__name:focus-visible {
  outline: none;
}
.ad-app__name:focus-visible::after {
  outline: 2px solid var(--gh-focus);
  outline-offset: -2px;
}
.ad-app__id {
  display: block;
  margin-top: 2px;
  font-size: 13px;
  font-weight: 400;
  color: var(--gh-text-muted);
  overflow-wrap: anywhere;
}
.ad-owner {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px 8px;
}
.ad-sub {
  display: block;
  color: var(--gh-text-muted);
  overflow-wrap: anywhere;
}
.ad-cell-label { display: none; }
/* phones: each app becomes a block with labelled figures */
@media (max-width: 48rem) {
  .ad-table,
  .ad-table tbody { display: block; }
  .ad-table thead { display: none; }
  .ad-table tr {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 8px 12px;
    padding: 14px 16px;
    border-top: 1px solid var(--gh-line-soft);
  }
  .ad-table tbody tr:first-child { border-top: 0; }
  .ad-table th,
  .ad-table td {
    padding: 0;
    border-top: 0;
  }
  .ad-table tr > th,
  .ad-table tr > td:nth-child(2) { grid-column: 1 / -1; }
  .ad-num { text-align: left !important; }
  .ad-cell-label {
    display: block;
    font-size: 12px;
    color: var(--gh-text-muted);
  }
}
</style>
