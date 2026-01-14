<script setup lang="ts">
const route = useRoute()

// Get current index from query params, default to 0
const currentIndex = computed(() => {
  const index = route.query.index
  return index ? Number(index) : 0
})

const steps = [
  { label: 'Personal Details', index: 0 },
  { label: 'Skills and Logistics', index: 1 },
  { label: 'Outreach', index: 2 },
]
</script>

<template>
  <div class="w-full">
    <div class="py-6 px-4 sm:px-8 lg:px-[120px]">
      <div class="flex flex-col gap-4 mb-6">
        <!-- Exit Registration link -->
        <NuxtLink
          to="/"
          class="font-raleway text-[14px] sm:text-[16px] font-normal text-black underline"
        >
          Exit Registration
        </NuxtLink>

        <!-- HackOMania 2026 Logo - centered -->
        <img
          src="/logos/logo-hackomania2026-typography.svg"
          alt="HackOMania 2026"
          class="w-full max-w-[300px] sm:max-w-[468px] h-auto mx-auto"
        >
      </div>

      <!-- top horizontal line -->
      <div class="h-[1px] bg-black mb-0" />

      <!-- Steps - Desktop: horizontal, Mobile: vertical -->
      <div class="hidden lg:flex border-x border-black">
        <div
          v-for="(step, idx) in steps"
          :key="idx"
          class="flex-1 h-[71px] flex items-center text-[18px] xl:text-[20px] font-normal text-black font-zalando pl-[24px] border-r border-black last:border-r-0"
          :class="{ 'bg-gray-200': currentIndex >= step.index }"
        >
          {{ idx + 1 }}. {{ step.label }}
        </div>
      </div>

      <!-- Mobile Steps -->
      <div class="lg:hidden space-y-2 py-4">
        <div
          v-for="(step, idx) in steps"
          :key="idx"
          class="flex items-center gap-3 p-3 border border-black rounded"
          :class="{ 'bg-gray-200': currentIndex >= step.index }"
        >
          <div
            class="flex-shrink-0 w-8 h-8 rounded-full border-2 border-black flex items-center justify-center font-zalando font-bold"
            :class="{ 'bg-black text-white': currentIndex === step.index }"
          >
            {{ idx + 1 }}
          </div>
          <span class="text-[16px] font-normal text-black font-zalando">
            {{ step.label }}
          </span>
        </div>
      </div>

      <!-- bottom horizontal line -->
      <div class="h-[1px] bg-black" />
    </div>
  </div>
</template>
