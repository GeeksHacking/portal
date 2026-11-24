<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsGetEndpoint,
  useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsHistoryEndpoint,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
} from '@geekshacking/portal-sdk/hooks'

/**
 * Sign-in history for one of your OAuth apps: who allowed the app to use their account, and when.
 * The API returns the latest 100 (newest first) for apps the signed-in admin owns.
 */
const route = useRoute()
const appId = computed(() => (route.params.id as string | undefined) ?? '')

const { data: user, isLoading: isLoadingUser } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const isAdmin = computed(() => user.value?.isRoot === true)
const enabled = { query: { enabled: computed(() => isAdmin.value && !!appId.value) } }
const path = computed(() => ({ id: appId.value }))
const appQuery = useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsGetEndpoint({ path }, enabled)
const historyQuery = useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsHistoryEndpoint({ path }, enabled)

const app = computed(() => appQuery.data.value)
const items = computed(() => historyQuery.data.value?.items ?? [])
const LIMIT = 100

useHead(() => ({ title: `Sign-in history · ${app.value?.displayName ?? 'OAuth app'}` }))

const people = computed(() => new Set(items.value.map(i => i.subject)).size)
const search = ref('')
const shown = computed(() => {
  const q = search.value.trim().toLowerCase()
  if (!q)
    return items.value
  return items.value.filter(i => `${i.userName} ${i.userEmail}`.toLowerCase().includes(q))
})
const when = (value: string | null | undefined) => (value ? formatOrganizerDateTime(value) : '–')
</script>

<template>
  <GPanel id="oauth-history">
    <template #header>
      <GPanelHeader title="Sign-in history" />
    </template>

    <template #body>
      <div class="oh">
        <GButton
          to="/dash/oauth-applications"
          variant="ghost"
          size="sm"
          icon="material-symbols:arrow-back-rounded"
          label="My OAuth apps"
          class="oh-fit"
        />

        <GLoading
          v-if="isLoadingUser || (isAdmin && (appQuery.isLoading.value || historyQuery.isLoading.value))"
          label="Loading sign-ins…"
        />

        <GEmptyState
          v-else-if="!isAdmin"
          icon="material-symbols:lock-rounded"
          title="Only admins can do this"
          description="OAuth apps are managed by portal admins."
          bordered
        />

        <GEmptyState
          v-else-if="!app"
          icon="material-symbols:error-rounded"
          title="Couldn't find this app"
          :description="appQuery.error.value ? 'It may have been deleted, or it belongs to another admin.' : 'Try again in a moment.'"
          bordered
        />

        <template v-else>
          <div class="oh-head">
            <h2 class="oh-title">
              {{ app.displayName }}
            </h2>
            <p class="oh-muted">
              <template v-if="items.length">
                {{ items.length >= LIMIT ? `The latest ${LIMIT} sign-ins` : `${items.length.toLocaleString()} ${items.length === 1 ? 'sign-in' : 'sign-ins'}` }}
                by {{ people.toLocaleString() }} {{ people === 1 ? 'person' : 'people' }}, newest first.
              </template>
              <template v-else>
                Client ID <code>{{ app.clientId }}</code>
              </template>
            </p>
          </div>

          <GAlert
            v-if="historyQuery.error.value"
            tone="err"
            title="Couldn't load sign-ins"
            :description="getApiErrorMessage(historyQuery.error.value, 'Refresh to try again.')"
          />

          <GEmptyState
            v-else-if="!items.length"
            icon="material-symbols:history-rounded"
            title="No sign-ins yet"
            description="Each time someone allows this app to use their GeeksHacking account, it shows here."
            bordered
          />

          <template v-else>
            <GFormField
              label="Find a person"
              hide-label
              class="oh-search"
            >
              <GInput
                v-model="search"
                type="search"
                icon="material-symbols:search-rounded"
                placeholder="Find by name or email"
              />
            </GFormField>

            <p
              v-if="!shown.length"
              class="oh-muted"
            >
              No one matches “{{ search }}”.
            </p>

            <div
              v-else
              class="oh-table-wrap"
            >
              <table class="oh-table">
                <thead>
                  <tr>
                    <th scope="col">
                      Person
                    </th>
                    <th scope="col">
                      When
                    </th>
                    <th scope="col">
                      Allowed
                    </th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="item in shown"
                    :key="item.id"
                  >
                    <th scope="row">
                      <span class="oh-name">{{ item.userName || 'Unknown' }}</span>
                      <span class="oh-email">{{ item.userEmail }}</span>
                    </th>
                    <td class="oh-when">
                      {{ when(item.creationDate) }}
                    </td>
                    <td>
                      <ul
                        v-if="oauthScopesOf(item.scopes).length"
                        class="oh-scopes"
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
                        class="oh-muted"
                      >–</span>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </template>
        </template>
      </div>
    </template>
  </GPanel>
</template>

<style scoped>
.oh {
  display: flex;
  flex-direction: column;
  gap: 16px;
  width: 100%;
  max-width: 64rem;
  margin: 0 auto;
}
.oh-fit { align-self: flex-start; }
.oh-head {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.oh-title {
  margin: 0;
  font-size: 24px;
  font-weight: 700;
  overflow-wrap: anywhere;
}
.oh-muted {
  margin: 0;
  color: var(--gh-text-muted);
}
.oh-search { max-width: 24rem; }
.oh-table-wrap {
  overflow-x: auto;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.oh-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 14px;
}
.oh-table th,
.oh-table td {
  padding: 12px 16px;
  border-top: 1px solid var(--gh-line-soft);
  text-align: left;
  vertical-align: top;
}
.oh-table thead th {
  border-top: 0;
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.oh-name {
  display: block;
  font-weight: 600;
}
.oh-email {
  display: block;
  font-weight: 400;
  color: var(--gh-text-muted);
  overflow-wrap: anywhere;
}
.oh-when { white-space: nowrap; }
.oh-scopes {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.oh-scopes code {
  display: inline-block;
  padding: 1px 6px;
  border: 1px solid var(--gh-line-soft);
  border-radius: var(--radius-gh-pill);
  font-size: 12px;
}
/* phones: each sign-in becomes a small block instead of a wide table */
@media (max-width: 40rem) {
  .oh-table,
  .oh-table tbody { display: block; }
  .oh-table thead { display: none; }
  .oh-table tr {
    display: flex;
    flex-direction: column;
    gap: 4px;
    padding: 12px 16px;
    border-top: 1px solid var(--gh-line-soft);
  }
  .oh-table tbody tr:first-child { border-top: 0; }
  .oh-table th,
  .oh-table td {
    padding: 0;
    border-top: 0;
  }
  .oh-when { white-space: normal; color: var(--gh-text-muted); }
}
</style>
