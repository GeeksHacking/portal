<script setup lang="ts">
/**
 * Hackathon check-ins: the shared check-in desk (scanner, live counts, participant list,
 * activity) pointed at this hackathon's venue endpoints.
 */
const props = withDefaults(defineProps<{
  hackathonId?: string
}>(), {
  hackathonId: '',
})
const route = useRoute()
const hackathonId = computed(() => props.hackathonId || (route.params.hackathonId as string | undefined) || '')

useHead({ title: 'Check-ins' })
</script>

<template>
  <GPanel id="checkin">
    <template #header>
      <GPanelHeader
        title="Check-ins"
        as="h1"
      />
    </template>

    <template #body>
      <OrganizersCheckInDesk
        kind="hackathon"
        :activity-id="hackathonId"
      />
    </template>
  </GPanel>
</template>
