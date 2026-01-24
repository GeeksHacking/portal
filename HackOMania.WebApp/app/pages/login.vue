<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'

definePageMeta({
  // Explicitly mark as public route
  auth: false,
})

const config = useRuntimeConfig()

useHead({
  titleTemplate: title => (title ? `${title} - HackOMania` : 'HackOMania'),
})

const hasNavigated = ref(false)

const { data: user, isLoading, isError } = useQuery({
  ...authQueries.whoAmI,
  retry: false,
})

// Watch for successful authentication and redirect
watch(user, (userData) => {
  if (userData && !hasNavigated.value) {
    hasNavigated.value = true
    navigateTo('/dash')
  }
})

const loginUrl = computed(() => {
  return `${config.public.api}/auth/login?redirect_uri=/dash`
})

const isAuthenticated = computed(() => !!user.value && !isError.value)
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-50">
    <div v-if="isLoading || isAuthenticated" class="flex flex-col items-center gap-4">
      <p class="text-sm font-medium text-gray-600 animate-pulse">
        {{ isAuthenticated ? 'Redirecting to dashboard...' : 'Checking your session...' }}
      </p>
      <UIcon name="i-lucide-loader-circle" class="w-8 h-8 animate-spin text-primary" />
    </div>

    <div v-else-if="isError" class="text-center max-w-md px-6">
      <div class="space-y-6">
        <div class="space-y-2">
          <h1 class="text-4xl font-bold tracking-tight text-gray-900">
            GeeksHacking
          </h1>
          <p class="text-gray-600">
            Giving | Geeks | Grow
          </p>
        </div>

        <UButton
          :to="loginUrl"
          external
          size="lg"
          icon="i-simple-icons-github"
          color="neutral"
        >
          Login with GitHub
        </UButton>
      </div>
    </div>
  </div>
</template>
