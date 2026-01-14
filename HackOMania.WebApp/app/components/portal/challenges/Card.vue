<script setup lang="ts">
defineProps<{
  challengeName: string
  teamCount: number
  headerColor: string
}>()
</script>

<template>
  <div class="rounded-lg overflow-hidden shadow-md min-w-48">
    <div class="px-4 py-2 text-white font-semibold" :style="{ backgroundColor: headerColor }">
      {{ challengeName }}
    </div>
    <div class="px-4 py-3 bg-white">
      <span class="text-2xl font-bold">{{ teamCount }}</span>
      <span class="text-gray-600 ml-1">Teams</span>
    </div>
  </div>
</template>
