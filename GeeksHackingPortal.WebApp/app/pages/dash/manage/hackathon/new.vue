<script setup lang="ts">
/** New hackathon: the full-page form (it used to be a pop-up on the workspace). */
</script>

<template>
  <GPanel id="manage-hackathon-new">
    <template #header>
      <GPanelHeader title="Workspace" />
    </template>

    <template #body>
      <OrganizersHackathonForm />
    </template>
  </GPanel>
</template>
