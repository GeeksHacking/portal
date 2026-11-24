<script setup lang="ts">
/** Workshop settings: the same form as the workspace's Edit page, saved in place, for its organisers. */
const route = useRoute()
const workshopId = computed(() => (route.params.standaloneWorkshopId as string | undefined) ?? '')
</script>

<template>
  <GPanel id="standalone-settings">
    <template #header>
      <GPanelHeader title="Settings" />
    </template>

    <template #body>
      <OrganizersWorkshopForm
        v-if="workshopId"
        :key="workshopId"
        :workshop-id="workshopId"
        settings
      />
    </template>
  </GPanel>
</template>
