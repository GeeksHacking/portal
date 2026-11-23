<script setup lang="ts">
/**
 * Hackathon resources: the shared resource desk (pick a resource, scan or pick from the list,
 * redeem) on this hackathon's endpoints. Resources are redeem-only here: the API supports
 * creating them, but the portal has no create/edit UI for hackathons yet.
 */
const props = withDefaults(defineProps<{
  hackathonId?: string
}>(), {
  hackathonId: '',
})
const route = useRoute()
const hackathonId = computed(() => props.hackathonId || (route.params.hackathonId as string | undefined) || '')

useHead({ title: 'Resources' })
</script>

<template>
  <GPanel id="resources">
    <template #header>
      <GPanelHeader
        title="Resources"
        as="h1"
      />
    </template>

    <template #body>
      <OrganizersResourceDesk
        kind="hackathon"
        :activity-id="hackathonId"
      />
    </template>
  </GPanel>
</template>
