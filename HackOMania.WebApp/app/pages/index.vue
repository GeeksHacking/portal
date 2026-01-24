<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'

definePageMeta({
  // Explicitly mark as public route
  auth: false,
})

useHead({
  titleTemplate: title => (title ? `${title} - HackOMania` : 'HackOMania'),
})

const hasNavigated = ref(false)

const { data: user, isLoading, isError } = useQuery({
  ...authQueries.whoAmI,
  retry: false,
})

// Watch for authentication status and redirect
watch([user, isError, isLoading], ([userData, hasError, loading]) => {
  if (loading || hasNavigated.value) return

  if (userData) {
    // User is authenticated, redirect to dashboard
    hasNavigated.value = true
    navigateTo('/dash')
  } else if (hasError) {
    // User is not authenticated, redirect to login
    hasNavigated.value = true
    navigateTo('/login')
  }
}, { immediate: true })
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-50">
    <div class="flex flex-col items-center gap-4">
      <p class="text-sm font-medium text-gray-600 animate-pulse">
        Checking authentication...
      </p>
      <UIcon name="i-lucide-loader-circle" class="w-8 h-8 animate-spin text-primary" />
    </div>
  </div>
</template>
