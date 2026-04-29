<script setup lang="ts">
const route = useRoute()
const standaloneWorkshopId = computed(() => route.params.standaloneWorkshopId as string | undefined)

watch(
  standaloneWorkshopId,
  (value) => {
    if (value)
      navigateTo(`/dash/standalone/${value}/participants`)
  },
  { immediate: true },
)
</script>

<template>
  <div class="p-4 text-sm text-(--ui-text-muted)">
    Redirecting...
  </div>
</template>
