<script setup lang="ts">
const route = useRoute()
const eventId = computed(() => (route.params.hackathonId as string | undefined) ?? '')
</script>

<template>
  <OrganizersQuestionsBuilder
    kind="hackathon"
    :event-id="eventId"
  />
</template>
