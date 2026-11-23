<script setup lang="ts">
/**
 * Workshop resources: the shared resource desk (pick a resource, scan or pick from the list,
 * redeem) on this workshop's endpoints, plus adding, editing and deleting resources.
 */
const route = useRoute()
const standaloneWorkshopId = computed(() => (route.params.standaloneWorkshopId as string | undefined) ?? '')

useHead({ title: 'Resources' })
</script>

<template>
  <GPanel id="standalone-resources">
    <template #header>
      <GPanelHeader
        title="Resources"
        as="h1"
      />
    </template>

    <template #body>
      <OrganizersResourceDesk
        kind="workshop"
        :activity-id="standaloneWorkshopId"
        manage
      />
    </template>
  </GPanel>
</template>
