<script setup lang="ts">
defineProps<{
  hackathonName?: string | null
}>()
</script>

<template>
  <div class="text-center">
    <p class="font-['Raleway'] text-base md:text-xl">
      You are not a participant of {{ hackathonName || 'this hackathon' }}.
    </p>
  </div>
</template>
