<script setup lang="ts">
defineProps<{
  time: string
  event: string
  bgColor?: string
}>()
</script>

<template>
  <div
    class="w-full h-18.75 pl-6 pr-4 flex flex-col justify-center gap-0.5 shadow-[0_0_4px_rgba(0,0,0,0.2)]"
    :class="bgColor || 'bg-white'"
  >
    <p class="font-['Zalando_Sans_Expanded'] font-bold text-xl leading-tight">
      {{ time }}
    </p>
    <p class="font-['Raleway'] text-sm leading-tight">
      {{ event }}
    </p>
  </div>
</template>
