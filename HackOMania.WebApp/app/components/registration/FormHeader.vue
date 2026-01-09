<script setup lang="ts">
const route = useRoute()

// Get current index from query params, default to 0
const currentIndex = computed(() => {
  const index = route.query.index
  return index ? Number(index) : 0
})
</script>

<template>
  <div class="relative h-[145px]">
    <!-- Exit Registration link -->
    <a
      href="/"
      class="absolute left-[120px] top-[30px] font-raleway text-[16px] font-normal text-black underline cursor-pointer"
    >
      Exit Registration
    </a>

    <!-- HackOMania 2026 Logo - centered -->
    <img
      src="/logos/logo-hackomania2026-typography.svg"
      alt="HackOMania 2026"
      class="absolute left-1/2 -translate-x-1/2 top-[25px] w-[468px] h-[26px]"
    >

    <!-- First horizontal line - 22px below logo (logo bottom is at 51px, so line at 73px) -->
    <div class="absolute left-0 right-0 top-[73px] h-[0.5px] bg-black" />

    <!-- Button group between the two lines - centered -->
    <div class="absolute left-1/2 -translate-x-1/2 top-[73.5px] flex h-[71px]">
      <!-- Start separator line -->
      <div class="w-[1px] h-full bg-black" />

      <!-- Button 1: Account Details -->
      <div
        class="w-[397px] h-full flex items-center text-[20px] font-normal text-black font-zalando pl-[24px]"
        :class="{ 'bg-gray-200': currentIndex >= 0 }"
      >
        1. Account Details
      </div>

      <!-- Separator line 1 -->
      <div class="w-[1px] h-full bg-black" />

      <!-- Button 2: Skills and Logistics -->
      <div
        class="w-[397px] h-full flex items-center text-[20px] font-normal text-black font-zalando pl-[24px]"
        :class="{ 'bg-gray-200': currentIndex >= 1 }"
      >
        2. Skills and Logistics
      </div>

      <!-- Separator line 2 -->
      <div class="w-[1px] h-full bg-black" />

      <!-- Button 3: Participation -->
      <div
        class="w-[397px] h-full flex items-center text-[20px] font-normal text-black font-zalando pl-[24px]"
        :class="{ 'bg-gray-200': currentIndex >= 2 }"
      >
        3. Participation
      </div>

      <!-- End separator line -->
      <div class="w-[1px] h-full bg-black" />
    </div>

    <!-- Second horizontal line - 72px below first line -->
    <div class="absolute left-0 right-0 top-[145px] h-[0.5px] bg-black" />
  </div>
</template>
