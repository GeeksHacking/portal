<script setup lang="ts">
/** Hackathon settings: GitHub checks on submissions (event details are edited from the workspace). */
const route = useRoute()
const hackathonId = computed(() => (route.params.hackathonId as string | undefined) ?? '')
</script>

<template>
  <GPanel id="settings">
    <template #header>
      <GPanelHeader title="Settings" />
    </template>

    <template #body>
      <OrganizersGitHubSettings
        v-if="hackathonId"
        :key="hackathonId"
        :hackathon-id="hackathonId"
      />
    </template>
  </GPanel>
</template>
