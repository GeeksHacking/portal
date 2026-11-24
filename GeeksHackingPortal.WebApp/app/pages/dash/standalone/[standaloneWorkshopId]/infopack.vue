<script setup lang="ts">
const route = useRoute()
const eventId = computed(() => (route.params.standaloneWorkshopId as string | undefined) ?? '')
</script>

<template>
  <OrganizersDataExport
    kind="workshop"
    :event-id="eventId"
  />
</template>
