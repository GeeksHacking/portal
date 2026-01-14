<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'

const { data: hackathonsData, isLoading } = useQuery(hackathonQueries.list)
</script>

<template>
  <UDashboardPanel
    id="dashboard"
  >
    <template #header>
      <UDashboardNavbar title="Dashboard">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>
    <template #body>
      <div class="p-4">
        <h2 class="text-lg font-semibold mb-4">
          Hackathons
        </h2>

        <div v-if="isLoading" class="text-muted">
          Loading hackathons...
        </div>

        <div v-else-if="!hackathonsData?.hackathons?.length" class="text-muted">
          No hackathons available.
        </div>

        <div v-else class="space-y-4">
          <UCard v-for="hackathon in hackathonsData.hackathons" :key="hackathon.id!">
            <template #header>
              <h3 class="font-semibold">
                {{ hackathon.name }}
              </h3>
            </template>
            <p class="text-sm text-muted">
              {{ hackathon.description }}
            </p>
            <div class="mt-2 text-xs text-muted">
              <span>{{ hackathon.venue }}</span>
            </div>
          </UCard>
        </div>
      </div>
    </template>
  </UDashboardPanel>
</template>
