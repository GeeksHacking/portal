<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import { computed } from 'vue'
import { hackathonQueries as participantHackathonQueries } from '~/composables/hackathons'

const route = useRoute()
const hackathonIdOrShortCode = computed(() => (route.params.hackathonId as string | undefined) ?? null)

// Fetch hackathon to get the actual ID
const { data: hackathon } = useQuery(
  computed(() => ({
    ...participantHackathonQueries.detail(hackathonIdOrShortCode.value ?? ''),
    enabled: !!hackathonIdOrShortCode.value,
  })),
)

const resolvedHackathonId = computed(() => hackathon.value?.id ?? null)
</script>

<template>
  <UDashboardPanel id="registration">
    <template #header>
      <UDashboardNavbar title="Registration">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <RegistrationFormPage :hackathon-id="resolvedHackathonId" />
    </template>
  </UDashboardPanel>
</template>
