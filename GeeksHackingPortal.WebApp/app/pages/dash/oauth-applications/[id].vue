<script setup lang="ts">
import { useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsHistoryEndpoint } from '@geekshacking/portal-sdk/hooks'
import { computed } from 'vue'

const route = useRoute()
const applicationId = computed(() => route.params.id as string)

const { data: historyData, isLoading } = useGeeksHackingPortalApiEndpointsAdminOAuthApplicationsHistoryEndpoint(applicationId)

const columns = [
  { id: 'userName', accessorKey: 'userName', header: 'Name' },
  { id: 'userEmail', accessorKey: 'userEmail', header: 'Email' },
  { id: 'creationDate', accessorKey: 'creationDate', header: 'Authorized At' },
  { id: 'scopes', accessorKey: 'scopes', header: 'Scopes' },
]

function formatDate(dateStr: string | null | undefined) {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleString()
}
</script>

<template>
  <UDashboardPanel id="oauth-history">
    <template #header>
      <UDashboardNavbar title="Sign In History">
        <template #leading>
          <UButton
            variant="ghost"
            icon="i-lucide-arrow-left"
            to="/dash/oauth-applications"
          />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <UCard
        class="flex-1"
        :ui="{ body: 'p-0 sm:p-0' }"
      >
        <UTable
          :data="historyData?.items ?? []"
          :columns="columns"
          :loading="isLoading"
        >
          <template #creationDate-cell="{ row }">
            {{ formatDate(row.original.creationDate) }}
          </template>
          <template #scopes-cell="{ row }">
            <div class="flex flex-wrap gap-1">
              <LazyUBadge
                v-if="row.original.scopes"
                variant="subtle"
                color="neutral"
              >
                {{ row.original.scopes }}
              </LazyUBadge>
            </div>
          </template>
          <template #empty>
            <div class="flex flex-col items-center justify-center py-6 text-center">
              <UIcon
                name="i-lucide-history"
                class="mb-4 size-8 text-(--ui-text-muted)"
              />
              <p class="text-sm text-(--ui-text-muted)">
                No sign in history found.
              </p>
            </div>
          </template>
        </UTable>
      </UCard>
    </template>
  </UDashboardPanel>
</template>
