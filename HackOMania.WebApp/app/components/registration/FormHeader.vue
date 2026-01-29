<script setup lang="ts">
defineProps<{
  hackathonId?: string | null
}>()

const exitTo = 'https://hackomania.geekshacking.com/'
</script>

<template>
  <div class="w-full">
    <div class="py-6 px-4 sm:px-8 lg:px-32">
      <div class="flex flex-col gap-4">
        <!-- Exit Registration link -->
        <NuxtLink
          :to="exitTo"
          external
          class="font-raleway text-sm sm:text-base font-normal text-gray-900 dark:text-gray-100 underline"
        >
          Exit Registration
        </NuxtLink>

        <!-- HackOMania 2026 Logo - centered -->
        <img
          src="/logos/logo-hackomania2026-typography.svg"
          alt="HackOMania 2026"
          class="w-full max-w-xs sm:max-w-md h-auto mx-auto"
        >
      </div>
    </div>
  </div>
</template>
