<script setup lang="ts">
import { useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint } from '@geekshacking/portal-sdk/hooks'

definePageMeta({
  // Explicitly mark as public route
  auth: false,
})

useHead({
  titleTemplate: title => (title ? `${title} - GeeksHacking Portal` : 'GeeksHacking Portal'),
})

const hasNavigated = ref(false)

const { data: user, isLoading, isError } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint({
  query: {
    retry: false,
  },
})

// Watch for authentication status and redirect
watch([user, isError, isLoading], ([userData, _hasError, loading]) => {
  if (loading || hasNavigated.value)
    return

  if (userData) {
    // User is authenticated, redirect to dashboard
    hasNavigated.value = true
    navigateTo('/dash')
  }
  else {
    // User is not authenticated, redirect to login
    hasNavigated.value = true
    navigateTo('/login')
  }
}, { immediate: true })
</script>

<template>
  <GLoading
    page
    label="Checking your session…"
  />
</template>
