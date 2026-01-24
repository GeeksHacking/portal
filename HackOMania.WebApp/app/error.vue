<script setup lang="ts">
const props = defineProps({
  error: Object,
})

const is404 = computed(() => props.error?.statusCode === 404)
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-50 px-4">
    <div class="text-center max-w-md">
      <div class="space-y-6">
        <!-- 404 Page Not Found -->
        <div v-if="is404" class="space-y-4">
          <div class="space-y-2">
            <h1 class="text-6xl font-bold text-gray-900">
              404
            </h1>
            <h2 class="text-2xl font-semibold text-gray-700">
              Page not found
            </h2>
          </div>
          <p class="text-gray-600">
            The page you're looking for doesn't exist or has been moved.
          </p>
          <UButton
            to="/"
            size="lg"
            color="neutral"
          >
            Return to home
          </UButton>
        </div>

        <!-- Other errors -->
        <div v-else class="space-y-4">
          <div class="space-y-2">
            <h1 class="text-6xl font-bold text-gray-900">
              {{ error?.statusCode || 'Error' }}
            </h1>
            <h2 class="text-2xl font-semibold text-gray-700">
              Something went wrong
            </h2>
          </div>
          <p class="text-gray-600">
            {{ error?.message || 'An unexpected error occurred' }}
          </p>
          <UButton
            to="/"
            size="lg"
            color="neutral"
          >
            Return to home
          </UButton>
        </div>
      </div>
    </div>
  </div>
</template>
