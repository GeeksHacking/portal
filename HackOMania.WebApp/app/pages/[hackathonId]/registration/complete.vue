<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'

definePageMeta({
  // Explicitly mark as public route
  auth: false,
})

useHead({
  titleTemplate: title => (title ? `${title} - HackOMania` : 'HackOMania'),
})

const route = useRoute()
const config = useRuntimeConfig()
const hackathonId = computed(() => route.params.hackathonId as string)

// Track if we should show the page
const showPage = ref(false)

// Check if user is authenticated
const { data: user, isLoading, isError } = useQuery({
  ...authQueries.whoAmI,
  retry: false,
  staleTime: 0,
  gcTime: 0,
})

// Handle auth state changes
watchEffect(() => {
  if (!isLoading.value) {
    if (user.value && !isError.value) {
      showPage.value = true
    }
    else if (hackathonId.value) {
      navigateTo(`${config.public.api}/auth/login?redirect_uri=${encodeURIComponent(route.fullPath)}`, { external: true })
    }
  }
})
</script>

<template>
  <!-- Show loading while checking auth -->
  <div
    v-if="isLoading || !showPage"
    class="bg-white min-h-screen flex flex-col items-center justify-center gap-4 px-4"
  >
    <p class="text-sm font-medium text-gray-600 animate-pulse">
      Checking your session...
    </p>
    <UIcon name="i-lucide-loader-circle" class="w-8 h-8 animate-spin text-primary" />
  </div>

  <!-- Show content if authenticated -->
  <div
    v-else
    class="bg-white min-h-screen font-raleway flex items-center justify-center px-4"
  >
    <div class="w-full flex justify-center">
      <div class="flex flex-col items-center gap-6 max-w-2xl">
        <img
          src="/logos/logo-hackomania2026-typography.svg"
          alt="HackOMania 2026"
          class="w-full max-w-xl h-auto"
        >

        <div class="flex flex-col items-center gap-4 mt-8">
          <h1 class="font-raleway text-2xl md:text-3xl font-semibold text-black text-center">
            Registration Complete!
          </h1>

          <div class="font-raleway text-base md:text-lg font-normal text-black text-center max-w-lg space-y-4">
            <p>Thank you for registering for HackOMania 2026!</p>

            <p>Our team will review your details and notify you once your registration is verified.</p>

            <p>You will be able to form or join a team after verification.</p>

            <p>You can check your application status and any review notes anytime from your dashboard.</p>

            <p>Please check your spam folder for the event registration email.</p>
          </div>
        </div>

        <div class="flex flex-col gap-3 mt-8 w-full max-w-sm">
          <UButton
            to="/dash"
            size="xl"
            color="neutral"
            class="w-full justify-center"
          >
            Go to Dashboard
          </UButton>
        </div>
      </div>
    </div>
  </div>
</template>
