<script setup lang="ts">
/** New workshop: the full-page form (it used to be a pop-up on the workspace). */
</script>

<template>
  <GPanel id="manage-workshop-new">
    <template #header>
      <GPanelHeader title="Workspace" />
    </template>

    <template #body>
      <OrganizersWorkshopForm />
    </template>
  </GPanel>
</template>
