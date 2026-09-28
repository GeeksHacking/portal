<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsAdminOAuthDirectoryGetEndpoint,
  useGeeksHackingPortalApiEndpointsAdminOAuthDirectoryHistoryEndpoint,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { computed, ref } from 'vue'

const route = useRoute()
const applicationId = computed(() => route.params.id as string)

const { data: user, isLoading: isLoadingUser } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const isRoot = computed(() => user.value?.isRoot === true)

const { data: detailData, isLoading: isLoadingDetail, isError: isDetailError } = useGeeksHackingPortalApiEndpointsAdminOAuthDirectoryGetEndpoint(
  { path: computed(() => ({ id: applicationId.value })) },
  { query: { enabled: isRoot, retry: false } },
)
const { data: historyData, isLoading: isLoadingHistory } = useGeeksHackingPortalApiEndpointsAdminOAuthDirectoryHistoryEndpoint(
  { path: computed(() => ({ id: applicationId.value })) },
  { query: { enabled: isRoot, retry: false } },
)

const application = computed(() => detailData.value?.application)

useHead({ title: computed(() => application.value?.displayName ?? 'OAuth Application') })

const search = ref('')

const historyItems = computed(() => historyData.value?.items ?? [])

const filteredHistory = computed(() => {
  const term = search.value.trim().toLowerCase()
  if (!term)
    return historyItems.value

  return historyItems.value.filter(item => [
    item.userName,
    item.userEmail,
    item.subject,
  ].some(value => value?.toLowerCase().includes(term)))
})

const stats = computed(() => [
  { label: 'Sign Ins', value: application.value?.totalAuthorizations ?? 0 },
  { label: 'Active Authorizations', value: application.value?.validAuthorizations ?? 0 },
  { label: 'Unique Users', value: application.value?.uniqueUsers ?? 0 },
  { label: 'Tokens Issued', value: application.value?.totalTokens ?? 0 },
])

const columns = [
  { id: 'user', header: 'User' },
  { id: 'creationDate', accessorKey: 'creationDate', header: 'Signed In At' },
  { id: 'status', accessorKey: 'status', header: 'Status' },
  { id: 'scopes', accessorKey: 'scopes', header: 'Scopes' },
  { id: 'tokenCount', accessorKey: 'tokenCount', header: 'Tokens' },
  { id: 'lastTokenIssuedAt', accessorKey: 'lastTokenIssuedAt', header: 'Last Token Issued' },
]

function formatDate(value: string | null | undefined, fallback = '-') {
  if (!value)
    return fallback
  return new Date(value).toLocaleString()
}

function statusBadgeColor(status: string | null | undefined) {
  switch (status) {
    case 'valid':
      return 'success'
    case 'revoked':
      return 'error'
    default:
      return 'neutral'
  }
}
</script>

<template>
  <UDashboardPanel id="admin-oauth-application">
    <template #header>
      <UDashboardNavbar :title="application?.displayName ?? 'OAuth Application'">
        <template #leading>
          <UButton
            variant="ghost"
            icon="i-lucide-arrow-left"
            to="/dash/admin/oauth-applications"
          />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div
        v-if="isLoadingUser || (isRoot && isLoadingDetail)"
        class="text-sm text-(--ui-text-muted)"
      >
        Loading OAuth application...
      </div>

      <UAlert
        v-else-if="!isRoot"
        color="error"
        variant="subtle"
        icon="i-lucide-shield-alert"
        title="Admin access required"
      />

      <UAlert
        v-else-if="isDetailError || !application"
        color="warning"
        variant="subtle"
        icon="i-lucide-search-x"
        title="OAuth application not found"
      />

      <div
        v-else
        class="space-y-6"
      >
        <div class="grid grid-cols-2 gap-4 lg:grid-cols-4">
          <div
            v-for="stat in stats"
            :key="stat.label"
            class="rounded-lg border border-default p-4"
          >
            <div class="text-xs font-medium uppercase text-(--ui-text-muted)">
              {{ stat.label }}
            </div>
            <div class="mt-1 text-2xl font-semibold">
              {{ stat.value }}
            </div>
          </div>
        </div>

        <UCard>
          <template #header>
            <div class="flex items-start justify-between gap-3">
              <div class="min-w-0">
                <h2 class="truncate text-base font-semibold">
                  {{ application.displayName }}
                </h2>
                <code class="truncate text-sm text-(--ui-text-muted)">
                  {{ application.clientId }}
                </code>
              </div>
              <UBadge
                :color="application.platform === 'Native' ? 'info' : 'primary'"
                variant="subtle"
              >
                {{ application.platform }}
              </UBadge>
            </div>
          </template>

          <dl class="grid gap-4 text-sm sm:grid-cols-2">
            <div>
              <dt class="text-xs font-medium uppercase text-(--ui-text-muted)">
                Owner
              </dt>
              <dd class="mt-1">
                <template v-if="application.ownerName || application.ownerEmail">
                  {{ application.ownerName }}
                  <span class="text-(--ui-text-muted)">{{ application.ownerEmail }}</span>
                </template>
                <span
                  v-else
                  class="text-(--ui-text-muted)"
                >
                  {{ application.ownerUserId ? `Unknown user (${application.ownerUserId})` : 'Unowned' }}
                </span>
              </dd>
            </div>
            <div>
              <dt class="text-xs font-medium uppercase text-(--ui-text-muted)">
                Internal ID
              </dt>
              <dd class="mt-1">
                <code class="text-xs">{{ application.id }}</code>
              </dd>
            </div>
            <div>
              <dt class="text-xs font-medium uppercase text-(--ui-text-muted)">
                Client Type
              </dt>
              <dd class="mt-1">
                {{ application.clientType ?? '-' }}
              </dd>
            </div>
            <div>
              <dt class="text-xs font-medium uppercase text-(--ui-text-muted)">
                Consent Type
              </dt>
              <dd class="mt-1">
                {{ application.consentType ?? '-' }}
              </dd>
            </div>
            <div>
              <dt class="text-xs font-medium uppercase text-(--ui-text-muted)">
                Last Sign In
              </dt>
              <dd class="mt-1">
                {{ formatDate(application.lastAuthorizedAt, 'Never') }}
              </dd>
            </div>
            <div>
              <dt class="text-xs font-medium uppercase text-(--ui-text-muted)">
                Last Token Issued
              </dt>
              <dd class="mt-1">
                {{ formatDate(application.lastTokenIssuedAt, 'Never') }}
              </dd>
            </div>
            <div class="sm:col-span-2">
              <dt class="text-xs font-medium uppercase text-(--ui-text-muted)">
                Redirect URIs
              </dt>
              <dd class="mt-1 space-y-1">
                <code
                  v-for="uri in application.redirectUris"
                  :key="uri"
                  class="block truncate rounded-md bg-muted px-2 py-1 text-xs"
                >
                  {{ uri }}
                </code>
              </dd>
            </div>
            <div
              v-if="application.postLogoutRedirectUris?.length"
              class="sm:col-span-2"
            >
              <dt class="text-xs font-medium uppercase text-(--ui-text-muted)">
                Post Logout Redirect URIs
              </dt>
              <dd class="mt-1 space-y-1">
                <code
                  v-for="uri in application.postLogoutRedirectUris"
                  :key="uri"
                  class="block truncate rounded-md bg-muted px-2 py-1 text-xs"
                >
                  {{ uri }}
                </code>
              </dd>
            </div>
            <div class="sm:col-span-2">
              <dt class="text-xs font-medium uppercase text-(--ui-text-muted)">
                Permissions
              </dt>
              <dd class="mt-1 flex flex-wrap gap-1">
                <UBadge
                  v-for="permission in detailData?.permissions ?? []"
                  :key="permission"
                  variant="subtle"
                  color="neutral"
                >
                  {{ permission }}
                </UBadge>
              </dd>
            </div>
            <div
              v-if="detailData?.requirements?.length"
              class="sm:col-span-2"
            >
              <dt class="text-xs font-medium uppercase text-(--ui-text-muted)">
                Requirements
              </dt>
              <dd class="mt-1 flex flex-wrap gap-1">
                <UBadge
                  v-for="requirement in detailData.requirements"
                  :key="requirement"
                  variant="subtle"
                  color="neutral"
                >
                  {{ requirement }}
                </UBadge>
              </dd>
            </div>
          </dl>
        </UCard>

        <div class="space-y-3">
          <div class="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
            <div>
              <h2 class="text-base font-semibold">
                Authentication History
              </h2>
              <p class="text-sm text-(--ui-text-muted)">
                <template v-if="historyData && (historyData.totalCount ?? 0) > historyItems.length">
                  Showing the latest {{ historyItems.length }} of {{ historyData.totalCount }} sign ins.
                </template>
                <template v-else>
                  {{ historyData?.totalCount ?? 0 }} sign ins.
                </template>
              </p>
            </div>
            <UInput
              v-model="search"
              icon="i-lucide-search"
              placeholder="Search by user"
              class="w-full sm:max-w-xs"
            />
          </div>

          <UCard :ui="{ body: 'p-0 sm:p-0' }">
            <UTable
              :data="filteredHistory"
              :columns="columns"
              :loading="isLoadingHistory"
            >
              <template #user-cell="{ row }">
                <div class="min-w-0">
                  <div class="truncate">
                    {{ row.original.userName ?? 'Unknown user' }}
                  </div>
                  <div class="truncate text-xs text-(--ui-text-muted)">
                    {{ row.original.userEmail ?? row.original.subject }}
                  </div>
                </div>
              </template>
              <template #creationDate-cell="{ row }">
                {{ formatDate(row.original.creationDate) }}
              </template>
              <template #status-cell="{ row }">
                <UBadge
                  :color="statusBadgeColor(row.original.status)"
                  variant="subtle"
                >
                  {{ row.original.status ?? 'unknown' }}
                </UBadge>
              </template>
              <template #scopes-cell="{ row }">
                <div class="flex flex-wrap gap-1">
                  <UBadge
                    v-for="scope in row.original.scopes ?? []"
                    :key="scope"
                    variant="subtle"
                    color="neutral"
                  >
                    {{ scope }}
                  </UBadge>
                </div>
              </template>
              <template #lastTokenIssuedAt-cell="{ row }">
                {{ formatDate(row.original.lastTokenIssuedAt) }}
              </template>
              <template #empty>
                <div class="flex flex-col items-center justify-center py-6 text-center">
                  <UIcon
                    name="i-lucide-history"
                    class="mb-4 size-8 text-(--ui-text-muted)"
                  />
                  <p class="text-sm text-(--ui-text-muted)">
                    {{ search ? 'No sign ins match your search.' : 'No sign in history found.' }}
                  </p>
                </div>
              </template>
            </UTable>
          </UCard>
        </div>
      </div>
    </template>
  </UDashboardPanel>
</template>
