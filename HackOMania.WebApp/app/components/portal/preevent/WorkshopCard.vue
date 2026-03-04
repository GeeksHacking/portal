<script setup lang="ts">
defineProps<{
  logo: string
  logoClass?: string
  title: string
  description: string
  speakerName: string
  speakerBackground: string
}>()
</script>

<template>
  <div>
    <NuxtImg :src="logo" class="w-auto object-contain mb-3" :class="logoClass || 'h-20'" />
    <h4 class="font-['Zalando_Sans_Expanded'] text-2xl lg:text-3xl mb-3">
      {{ title }}
    </h4>
    <p class="font-['Raleway'] text-base leading-snug whitespace-pre-line mb-4">
      {{ description }}
    </p>
    <p class="font-['Zalando_Sans_Expanded'] text-lg">
      {{ speakerName }}
    </p>
    <p class="font-['Raleway'] italic text-black/50 text-lg">
      {{ speakerBackground }}
    </p>
  </div>
</template>
