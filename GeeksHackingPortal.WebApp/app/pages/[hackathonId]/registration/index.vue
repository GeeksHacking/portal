<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'

definePageMeta({
  // Explicitly mark as public route
  auth: false,
})

const config = useRuntimeConfig()
const route = useRoute()
const hackathon = useRouteHackathon()

useHead({
  titleTemplate: title => (title ? `${title} - HackOMania` : 'HackOMania'),
})

// Check if user is authenticated
const { data: user, isLoading, isSuccess } = useQuery({
  ...authQueries.whoAmI,
  retry: false,
  staleTime: 0,
  gcTime: 0,
})

// Only redirect to form if query succeeded and we have user data
watchEffect(() => {
  if (isSuccess.value && user.value && hackathon.value) {
    navigateTo({ path: `/${hackathon.value.shortCode}/registration/form`, query: route.query }, { replace: true })
  }
})
</script>

<template>
  <!-- Show loading while checking auth or if authenticated (redirecting) -->
  <div
    v-if="isLoading || user"
    class="bg-white min-h-screen flex flex-col items-center justify-center gap-4 px-4"
  >
    <p class="text-sm font-medium text-gray-600 animate-pulse">
      Checking your session...
    </p>
    <UIcon
      name="i-lucide-loader-circle"
      class="w-8 h-8 animate-spin text-primary"
    />
  </div>

  <!-- Show login UI only if not authenticated -->
  <div
    v-else
    class="bg-white min-h-screen font-raleway flex items-center justify-center px-4"
  >
    <div class="w-full flex justify-center">
      <div class="flex flex-col items-center gap-3">
        <p class="font-normal text-base text-black text-center">
          Register to participate in
        </p>
        <img
          src="/logos/logo-hackomania2026-typography.svg"
          alt="HackOMania 2026"
          class="w-full max-w-xl h-auto"
        >
        <div class="flex justify-center mt-8 w-full">
          <UButton
            :to="`${config.public.api}/auth/login?redirect_uri=${encodeURIComponent(route.fullPath.replace('/registration', '/registration/form'))}`"
            external
            variant="outline"
            color="neutral"
            size="xl"
            icon="i-lucide-github"
          >
            Sign up with GitHub
          </UButton>
        </div>
        <div class="flex flex-col items-center gap-2 mt-6 max-w-md px-4">
          <p class="font-normal text-base text-black text-center">
            Note that due to a large number of registrations, all current registrations will be put on a waiting list.
          </p>
          <p class="font-normal text-base text-black text-center">
            You will be notified when a slot is available.
          </p>
        </div>
        <div class="mt-30">
          <NuxtLink
            to="https://hackomania.geekshacking.com/"
            external
            class="text-base font-normal text-black underline"
          >
            Exit Registration
          </NuxtLink>
        </div>
      </div>
    </div>
  </div>
</template>
