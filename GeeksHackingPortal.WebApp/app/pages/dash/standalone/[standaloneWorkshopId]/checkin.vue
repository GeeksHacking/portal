<script setup lang="ts">
/**
 * Workshop check-ins: the shared check-in desk (scanner, live counts, participant list,
 * activity) pointed at this workshop's venue endpoints.
 */
const route = useRoute()
const standaloneWorkshopId = computed(() => (route.params.standaloneWorkshopId as string | undefined) ?? '')

useHead({ title: 'Check-ins' })
</script>

<template>
  <GPanel id="standalone-checkin">
    <template #header>
      <GPanelHeader
        title="Check-ins"
        as="h1"
      />
    </template>

    <template #body>
      <OrganizersCheckInDesk
        kind="workshop"
        :activity-id="standaloneWorkshopId"
      />
    </template>
  </GPanel>
</template>
