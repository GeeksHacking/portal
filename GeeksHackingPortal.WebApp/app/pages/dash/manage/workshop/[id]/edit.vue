<script setup lang="ts">
/** Edit a workshop: the full-page form (it used to be a pop-up on the workspace). */
const route = useRoute()
const id = computed(() => (route.params.id as string | undefined) ?? '')
</script>

<template>
  <GPanel id="manage-workshop-edit">
    <template #header>
      <GPanelHeader title="Workspace" />
    </template>

    <template #body>
      <OrganizersWorkshopForm
        :key="id"
        :workshop-id="id"
      />
    </template>
  </GPanel>
</template>
