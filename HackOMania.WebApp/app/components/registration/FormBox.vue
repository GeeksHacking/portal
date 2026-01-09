<script setup lang="ts">
withDefaults(defineProps<{
  title: string
  iconWidth?: string
  iconHeight?: string
  iconSrc?: string
}>(), {
  iconWidth: '40px',
  iconHeight: '40px',
})
</script>

<template>
  <div class="w-auto">
    <!-- Header with icon and title -->
    <div class="flex items-center">
      <!-- Icon -->
      <img
        v-if="iconSrc"
        :src="iconSrc"
        :alt="title"
        :style="{ width: iconWidth, height: iconHeight }"
      >
      <div
        v-else
        :style="{ width: iconWidth, height: iconHeight }"
      />

      <!-- Title -->
      <h2 class="ml-[12px] font-raleway text-[20px] font-normal text-black">
        {{ title }}
      </h2>
    </div>

    <!-- Form content -->
    <div class="mt-[16px] flex flex-col gap-[12px]">
      <slot />
    </div>
  </div>
</template>
