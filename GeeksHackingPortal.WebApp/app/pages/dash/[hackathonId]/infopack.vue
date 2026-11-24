<script setup lang="ts">
const route = useRoute()
const eventId = computed(() => (route.params.hackathonId as string | undefined) ?? '')
</script>

<template>
  <OrganizersDataExport
    kind="hackathon"
    :event-id="eventId"
  />
</template>
