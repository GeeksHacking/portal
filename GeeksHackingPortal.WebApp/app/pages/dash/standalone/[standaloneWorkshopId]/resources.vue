<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsAnalyticsEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint,
} from '@geekshacking/portal-sdk/hooks'

const route = useRoute()
const standaloneWorkshopId = computed(() => (route.params.standaloneWorkshopId as string | undefined) ?? '')

const { data: eventsData } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint()
const { data: analytics, isLoading } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsAnalyticsEndpoint(
  standaloneWorkshopId,
  { query: { enabled: computed(() => !!standaloneWorkshopId.value) } },
)

const event = computed(() => eventsData.value?.standaloneWorkshops?.find(item => item.id === standaloneWorkshopId.value))
</script>

<template>
  <UDashboardPanel id="standalone-resources">
    <template #header>
      <UDashboardNavbar title="Resources">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="space-y-3">
        <UCard>
          <template #header>
            <div class="flex flex-col gap-1">
              <h3 class="text-sm font-semibold">
                {{ event?.title ?? 'Standalone Workshop' }}
              </h3>
              <p class="text-xs text-(--ui-text-muted)">
                Resource analytics currently available for this workshop.
              </p>
            </div>
          </template>

          <div
            v-if="isLoading"
            class="text-sm text-(--ui-text-muted)"
          >
            Loading resources...
          </div>
          <div
            v-else
            class="grid gap-4 sm:grid-cols-2"
          >
            <UCard>
              <p class="text-sm text-(--ui-text-muted)">
                Resources
              </p>
              <p class="mt-1 text-2xl font-semibold">
                {{ analytics?.resourceCount ?? 0 }}
              </p>
            </UCard>
            <UCard>
              <p class="text-sm text-(--ui-text-muted)">
                Resource Redemptions
              </p>
              <p class="mt-1 text-2xl font-semibold">
                {{ analytics?.resourceRedemptionCount ?? 0 }}
              </p>
            </UCard>
          </div>
        </UCard>

        <UAlert
          color="warning"
          variant="soft"
          title="Resource management is not available for standalone workshops yet"
          description="The frontend now exposes this section and current analytics. Creating resources, QR redemption, and redemption history need standalone workshop resource API endpoints before they can match the hackathon resource workflow."
        />
      </div>
    </template>
  </UDashboardPanel>
</template>
