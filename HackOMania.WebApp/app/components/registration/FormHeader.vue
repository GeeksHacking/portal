<script setup lang="ts">
const props = defineProps<{
  hackathonId?: string | null
}>()

const route = useRoute()

// Get current index from query params, default to 0
const currentIndex = computed(() => {
  const index = route.query.index
  return index ? Number(index) : 0
})

const exitTo = computed(() => {
  if (props.hackathonId) return `/dash/${props.hackathonId}`
  return '/dash'
})

const steps = [
  { label: 'Personal Details', index: 0 },
  { label: 'Skills and Logistics', index: 1 },
  { label: 'Outreach', index: 2 },
]
</script>

<template>
  <div class="w-full">
    <div class="py-6 px-4 sm:px-8 lg:px-32">
      <div class="flex flex-col gap-4 mb-6">
        <!-- Exit Registration link -->
        <NuxtLink
          :to="exitTo"
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

      <!-- top horizontal line -->
      <div class="h-px bg-gray-900 dark:bg-gray-100 mb-0" />

      <!-- Steps - Desktop: horizontal, Mobile: vertical -->
      <div class="hidden lg:flex border-x border-gray-900 dark:border-gray-100">
        <div
          v-for="(step, idx) in steps"
          :key="idx"
          class="flex-1 h-18 flex items-center text-lg xl:text-xl font-normal text-gray-900 dark:text-gray-100 font-zalando pl-6 border-r border-gray-900 dark:border-gray-100 last:border-r-0"
          :class="{ 'bg-gray-200 dark:bg-gray-700': currentIndex >= step.index }"
        >
          {{ idx + 1 }}. {{ step.label }}
        </div>
      </div>

      <!-- Mobile Steps -->
      <div class="lg:hidden space-y-2 py-4">
        <div
          v-for="(step, idx) in steps"
          :key="idx"
          class="flex items-center gap-3 p-3 border border-gray-900 dark:border-gray-100 rounded"
          :class="{ 'bg-gray-200 dark:bg-gray-700': currentIndex >= step.index }"
        >
          <div
            class="flex-shrink-0 w-8 h-8 rounded-full border-2 border-gray-900 dark:border-gray-100 flex items-center justify-center font-zalando font-bold"
            :class="{ 'bg-gray-900 dark:bg-gray-100 text-white dark:text-gray-900': currentIndex === step.index }"
          >
            {{ idx + 1 }}
          </div>
          <span class="text-base font-normal text-gray-900 dark:text-gray-100 font-zalando">
            {{ step.label }}
          </span>
        </div>
      </div>

      <!-- bottom horizontal line -->
      <div class="h-px bg-gray-900 dark:bg-gray-100" />
    </div>
  </div>
</template>
