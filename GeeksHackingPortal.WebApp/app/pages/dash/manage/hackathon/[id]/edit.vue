<script setup lang="ts">
/** Edit a hackathon: the full-page form (it used to be a pop-up on the workspace). */
const route = useRoute()
const id = computed(() => (route.params.id as string | undefined) ?? '')
</script>

<template>
  <GPanel id="manage-hackathon-edit">
    <template #header>
      <GPanelHeader title="Workspace" />
    </template>

    <template #body>
      <OrganizersHackathonForm
        :key="id"
        :hackathon-id="id"
      />
    </template>
  </GPanel>
</template>
