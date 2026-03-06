<script setup lang="ts">
const route = useRoute()
const props = withDefaults(defineProps<{
  hackathonId?: string
}>(), {
  hackathonId: '',
})
const hackathonId = computed(() => props.hackathonId || (route.params.hackathonId as string | undefined) || '')
</script>

<template>
  <UDashboardPanel id="analytics">
    <template #header>
      <UDashboardNavbar title="Participant Analytics">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="p-4 overflow-y-auto">
        <ParticipantAnalytics
          :hackathon-id="hackathonId"
          :is-organizer="true"
        />
      </div>
    </template>
  </UDashboardPanel>
</template>
