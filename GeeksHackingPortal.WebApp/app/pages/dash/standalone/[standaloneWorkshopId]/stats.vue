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

const statCards = computed(() => [
  { label: 'Registered', value: analytics.value?.registeredCount ?? 0, icon: 'i-lucide-users', color: 'primary' as const },
  { label: 'Withdrawn', value: analytics.value?.withdrawnCount ?? 0, icon: 'i-lucide-user-x', color: 'warning' as const },
  { label: 'Capacity Remaining', value: analytics.value?.capacityRemaining ?? 0, icon: 'i-lucide-armchair', color: 'success' as const },
  { label: 'Capacity Used', value: `${analytics.value?.capacityUsedPercent ?? 0}%`, icon: 'i-lucide-gauge', color: 'info' as const },
  { label: 'Check-ins', value: analytics.value?.checkInCount ?? 0, icon: 'i-lucide-qr-code', color: 'primary' as const },
  { label: 'Currently Checked In', value: analytics.value?.currentlyCheckedInCount ?? 0, icon: 'i-lucide-map-pin-check', color: 'success' as const },
  { label: 'Resources', value: analytics.value?.resourceCount ?? 0, icon: 'i-lucide-gift', color: 'neutral' as const },
  { label: 'Resource Redemptions', value: analytics.value?.resourceRedemptionCount ?? 0, icon: 'i-lucide-hand-coins', color: 'warning' as const },
  { label: 'Email Templates', value: analytics.value?.emailTemplateCount ?? 0, icon: 'i-lucide-mail', color: 'neutral' as const },
])
</script>

<template>
  <UDashboardPanel id="standalone-stats">
    <template #header>
      <UDashboardNavbar title="Standalone Event Stats">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="space-y-4">
        <UCard>
          <div class="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
            <div class="space-y-1">
              <h2 class="text-lg font-semibold">
                {{ event?.title ?? 'Standalone Event' }}
              </h2>
              <p class="text-sm text-(--ui-text-muted)">
                {{ event?.description ?? 'Summary analytics for this standalone event.' }}
              </p>
            </div>
            <UBadge
              :color="event?.isPublished ? 'success' : 'neutral'"
              variant="subtle"
            >
              {{ event?.isPublished ? 'Published' : 'Draft' }}
            </UBadge>
          </div>
        </UCard>

        <div
          v-if="isLoading"
          class="text-sm text-(--ui-text-muted)"
        >
          Loading analytics...
        </div>

        <div
          v-else
          class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3"
        >
          <UCard
            v-for="card in statCards"
            :key="card.label"
          >
            <div class="flex items-start justify-between gap-3">
              <div>
                <p class="text-sm text-(--ui-text-muted)">
                  {{ card.label }}
                </p>
                <p class="mt-1 text-2xl font-semibold">
                  {{ card.value }}
                </p>
              </div>
              <UIcon
                :name="card.icon"
                class="size-5 text-(--ui-primary)"
              />
            </div>
          </UCard>
        </div>
      </div>
    </template>
  </UDashboardPanel>
</template>
