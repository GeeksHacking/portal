<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint,
} from '@geekshacking/portal-sdk/hooks'

definePageMeta({
  // Explicitly mark as public route
  auth: false,
})

useHead({
  titleTemplate: title => (title ? `${title} - GeeksHacking Portal` : 'GeeksHacking Portal'),
})

const route = useRoute()
const config = useRuntimeConfig()
const routeHackathonId = computed(() => (route.params.hackathonId as string) ?? '')
const { data: hackathon } = useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint(routeHackathonId)
const resolvedHackathonId = computed(() => hackathon.value?.id ?? '')

// Track if we should show the page
const showPage = ref(false)

// Check if user is authenticated
const { data: user, isLoading, isError } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint({
  query: {
    retry: false,
    staleTime: 0,
    gcTime: 0,
  },
})

// Compute status page path with joinCode if present
const statusPagePath = computed(() => {
  if (!hackathon.value)
    return null
  const basePath = `/${hackathon.value.shortCode}/registration/status`
  if (route.query.joinCode) {
    return { path: basePath, query: { joinCode: route.query.joinCode } }
  }
  return basePath
})

// Handle auth state changes
watchEffect(() => {
  if (!isLoading.value) {
    if (user.value && !isError.value) {
      showPage.value = true
    }
    else if (resolvedHackathonId.value) {
      navigateTo(`${config.public.api}/auth/login?redirect_uri=${encodeURIComponent(route.fullPath)}`, { external: true })
    }
  }
})
</script>

<template>
  <!-- Show loading while checking auth -->
  <div
    v-if="isLoading || !showPage"
    class="min-h-screen flex flex-col items-center justify-center gap-4 bg-(--ui-bg) px-4 text-(--ui-text)"
  >
    <p class="text-sm font-medium text-(--ui-text-muted) animate-pulse">
      Checking your session...
    </p>
    <UIcon name="i-lucide-loader-circle" class="w-8 h-8 animate-spin text-primary" />
  </div>

  <!-- Show content if authenticated -->
  <div
    v-else
    class="min-h-screen bg-(--ui-bg) font-raleway flex items-center justify-center px-4 text-(--ui-text)"
  >
    <div class="w-full flex justify-center">
      <div class="flex flex-col items-center gap-6 max-w-2xl">
        <img
          src="/logos/geekshacking.svg"
          alt="GeeksHacking Portal"
          class="w-full max-w-xl h-auto"
        >

        <div class="flex flex-col items-center gap-4 mt-8">
          <h1 class="font-raleway text-2xl md:text-3xl font-semibold text-(--ui-text-highlighted) text-center">
            Registration Complete!
          </h1>

          <div class="font-raleway text-base md:text-lg font-normal text-(--ui-text) text-center max-w-lg space-y-4">
            <p>Our team will review your details and notify you once your registration is verified.</p>

            <p>You may check your application status below or return to GeeksHacking Portal.</p>
          </div>
        </div>

        <div class="flex flex-col gap-3 mt-8 w-full max-w-sm">
          <UButton
            :to="statusPagePath"
            size="xl"
            color="neutral"
            class="w-full justify-center"
          >
            Check Registration Status
          </UButton>

          <UButton
            to="/"
            size="xl"
            color="neutral"
            variant="outline"
            class="w-full justify-center"
          >
            Return to GeeksHacking Portal
          </UButton>
        </div>
      </div>
    </div>
  </div>
</template>
