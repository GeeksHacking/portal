<script setup lang="ts">
import { useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint } from '@geekshacking/portal-sdk/hooks'

definePageMeta({
  // Explicitly mark as public route
  auth: false,
})

import { LoaderCircle } from 'lucide-vue-next'

const config = useRuntimeConfig()
const route = useRoute()
const loginFailed = computed(() => route.query.error === 'github' || route.query.error === 'session')

useHead({
  titleTemplate: title => (title ? `${title} - GeeksHacking Portal` : 'GeeksHacking Portal'),
})

const hasNavigated = ref(false)

const { data: user, isLoading, isError } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint({
  query: {
    retry: false,
  },
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
      <LoaderCircle class="w-8 h-8 animate-spin text-primary" />
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

        <Button as-child size="lg">
          <a :href="loginUrl">
            <Icon name="i-simple-icons-github" />
            Login with GitHub
          </a>
        </Button>

        <p v-if="loginFailed" class="text-sm text-red-600">
          GitHub sign-in did not finish. Please try again.
        </p>

        <p class="text-sm text-gray-600">
          Please ensure you are logged into GitHub first!
        </p>
      </div>
    </div>
  </div>
</template>
