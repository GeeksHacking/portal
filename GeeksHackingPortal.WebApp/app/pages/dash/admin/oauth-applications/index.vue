<script setup lang="ts">
import type { GeeksHackingPortalApiEndpointsAdminOAuthDirectorySharedOAuthDirectoryApplicationResponse } from '@geekshacking/portal-sdk'
import {
  useGeeksHackingPortalApiEndpointsAdminOAuthDirectoryListEndpoint,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { computed, ref } from 'vue'

type DirectoryApplication = GeeksHackingPortalApiEndpointsAdminOAuthDirectorySharedOAuthDirectoryApplicationResponse

useHead({ title: 'All OAuth Applications' })

const { data: user, isLoading: isLoadingUser } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const { data: directoryData, isLoading: isLoadingDirectory } = useGeeksHackingPortalApiEndpointsAdminOAuthDirectoryListEndpoint({
  query: {
    enabled: computed(() => user.value?.isRoot === true),
  },
})

const search = ref('')

const applications = computed(() => directoryData.value?.items ?? [])

const filteredApplications = computed(() => {
  const term = search.value.trim().toLowerCase()
  if (!term)
    return applications.value

  return applications.value.filter(application => [
    application.displayName,
    application.clientId,
    application.ownerName,
    application.ownerEmail,
  ].some(value => value?.toLowerCase().includes(term)))
})

const totals = computed(() => applications.value.reduce(
  (sum, application) => ({
    authorizations: sum.authorizations + (application.totalAuthorizations ?? 0),
    tokens: sum.tokens + (application.totalTokens ?? 0),
  }),
  { authorizations: 0, tokens: 0 },
))

const columns = [
  { id: 'application', header: 'Application' },
  { id: 'platform', accessorKey: 'platform', header: 'Platform' },
  { id: 'owner', header: 'Owner' },
  { id: 'totalAuthorizations', accessorKey: 'totalAuthorizations', header: 'Sign Ins' },
  { id: 'uniqueUsers', accessorKey: 'uniqueUsers', header: 'Unique Users' },
  { id: 'lastAuthorizedAt', accessorKey: 'lastAuthorizedAt', header: 'Last Sign In' },
  { id: 'actions', header: '' },
]

function formatDate(value: string | null | undefined) {
  if (!value)
    return 'Never'
  return new Date(value).toLocaleString()
}

function platformBadgeColor(platform: DirectoryApplication['platform']) {
  return platform === 'Native' ? 'info' : 'primary'
}
</script>

<template>
  <UDashboardPanel id="admin-oauth-applications">
    <template #header>
      <UDashboardNavbar title="All OAuth Applications">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div
        v-if="isLoadingUser || (user?.isRoot && isLoadingDirectory)"
        class="text-sm text-(--ui-text-muted)"
      >
        Loading OAuth applications...
      </div>

      <UAlert
        v-else-if="!user?.isRoot"
        color="error"
        variant="subtle"
        icon="i-lucide-shield-alert"
        title="Admin access required"
      />

      <div
        v-else
        class="space-y-6"
      >
        <div class="grid gap-4 sm:grid-cols-3">
          <div class="rounded-lg border border-default p-4">
            <div class="text-xs font-medium uppercase text-(--ui-text-muted)">
              Applications
            </div>
            <div class="mt-1 text-2xl font-semibold">
              {{ applications.length }}
            </div>
          </div>
          <div class="rounded-lg border border-default p-4">
            <div class="text-xs font-medium uppercase text-(--ui-text-muted)">
              Total Sign Ins
            </div>
            <div class="mt-1 text-2xl font-semibold">
              {{ totals.authorizations }}
            </div>
          </div>
          <div class="rounded-lg border border-default p-4">
            <div class="text-xs font-medium uppercase text-(--ui-text-muted)">
              Tokens Issued
            </div>
            <div class="mt-1 text-2xl font-semibold">
              {{ totals.tokens }}
            </div>
          </div>
        </div>

        <UInput
          v-model="search"
          icon="i-lucide-search"
          placeholder="Search by name, client ID or owner"
          class="w-full sm:max-w-sm"
        />

        <UCard :ui="{ body: 'p-0 sm:p-0' }">
          <UTable
            :data="filteredApplications"
            :columns="columns"
          >
            <template #application-cell="{ row }">
              <div class="min-w-0">
                <NuxtLink
                  :to="`/dash/admin/oauth-applications/${row.original.id}`"
                  class="block truncate font-medium hover:underline"
                >
                  {{ row.original.displayName }}
                </NuxtLink>
                <code class="block truncate text-xs text-(--ui-text-muted)">
                  {{ row.original.clientId }}
                </code>
              </div>
            </template>
            <template #platform-cell="{ row }">
              <UBadge
                :color="platformBadgeColor(row.original.platform)"
                variant="subtle"
              >
                {{ row.original.platform }}
              </UBadge>
            </template>
            <template #owner-cell="{ row }">
              <div
                v-if="row.original.ownerName || row.original.ownerEmail"
                class="min-w-0"
              >
                <div class="truncate">
                  {{ row.original.ownerName }}
                </div>
                <div class="truncate text-xs text-(--ui-text-muted)">
                  {{ row.original.ownerEmail }}
                </div>
              </div>
              <span
                v-else
                class="text-(--ui-text-muted)"
              >
                {{ row.original.ownerUserId ? 'Unknown user' : 'Unowned' }}
              </span>
            </template>
            <template #lastAuthorizedAt-cell="{ row }">
              {{ formatDate(row.original.lastAuthorizedAt) }}
            </template>
            <template #actions-cell="{ row }">
              <div class="flex justify-end">
                <UButton
                  variant="ghost"
                  icon="i-lucide-arrow-right"
                  size="sm"
                  :to="`/dash/admin/oauth-applications/${row.original.id}`"
                >
                  Details
                </UButton>
              </div>
            </template>
            <template #empty>
              <div class="flex flex-col items-center justify-center py-6 text-center">
                <UIcon
                  name="i-lucide-key-round"
                  class="mb-4 size-8 text-(--ui-text-muted)"
                />
                <p class="text-sm text-(--ui-text-muted)">
                  {{ search ? 'No applications match your search.' : 'No OAuth applications have been created yet.' }}
                </p>
              </div>
            </template>
          </UTable>
        </UCard>
      </div>
    </template>
  </UDashboardPanel>
</template>
