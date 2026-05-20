<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint,
} from '@geekshacking/portal-sdk/hooks'

definePageMeta({
  // Explicitly mark as public route
  auth: false,
})

const config = useRuntimeConfig()
const route = useRoute()
const routeHackathonId = computed(() => (route.params.hackathonId as string) ?? '')
const { data: hackathon } = useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint(routeHackathonId)

useHead({
  titleTemplate: title => (title ? `${title} - GeeksHacking Portal` : 'GeeksHacking Portal'),
})

// Check if user is authenticated
const { data: user, isLoading, isSuccess } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint({
  query: {
    retry: false,
    staleTime: 0,
    gcTime: 0,
  },
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
    class="min-h-screen flex flex-col items-center justify-center gap-4 bg-(--ui-bg) px-4 text-(--ui-text)"
  >
    <p class="text-sm font-medium text-(--ui-text-muted) animate-pulse">
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
    class="min-h-screen bg-(--ui-bg) font-raleway flex items-center justify-center px-4 text-(--ui-text)"
  >
    <div class="w-full flex justify-center">
      <div class="flex flex-col items-center gap-3">
        <p class="font-normal text-base text-(--ui-text-highlighted) text-center">
          Register to participate in
        </p>
        <img
          src="/logos/geekshacking.svg"
          alt="GeeksHacking Portal"
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
          <p class="font-normal text-base text-(--ui-text-highlighted) text-center">
            Note that due to a large number of registrations, all current registrations will be put on a waiting list.
          </p>
          <p class="font-normal text-base text-(--ui-text-highlighted) text-center">
            You will be notified when a slot is available.
          </p>
        </div>
        <div class="mt-30">
          <NuxtLink
            to="/"
            class="text-base font-normal text-(--ui-text-highlighted) underline"
          >
            Exit Registration
          </NuxtLink>
        </div>
      </div>
    </div>
  </div>
</template>
