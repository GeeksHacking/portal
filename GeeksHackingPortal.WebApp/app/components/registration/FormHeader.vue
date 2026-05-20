<script setup lang="ts">
defineProps<{
  hackathonId?: string | null
}>()

const exitTo = '/'
</script>

<template>
  <div class="w-full">
    <div class="py-6 px-4 sm:px-8 lg:px-32">
      <div class="flex flex-col gap-4">
        <!-- Exit Registration link -->
        <NuxtLink
          :to="exitTo"
          class="font-raleway text-sm sm:text-base font-normal text-(--ui-text-highlighted) underline"
        >
          Exit Registration
        </NuxtLink>

        <!-- GeeksHacking Portal logo - centered -->
        <img
          src="/logos/geekshacking.svg"
          alt="GeeksHacking Portal"
          class="w-full max-w-xs sm:max-w-md h-auto mx-auto"
        >
      </div>
    </div>
  </div>
</template>
