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
  <GLoading label="Opening the workshop…" />
</template>
