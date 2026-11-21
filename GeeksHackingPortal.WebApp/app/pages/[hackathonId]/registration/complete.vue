<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { githubLoginUrl } from '~/utils/login-url'

definePageMeta({
  // Explicitly mark as public route
  auth: false,
})

useHead({
  title: 'Registration complete',
})

const route = useRoute()
const config = useRuntimeConfig()
const routeHackathonId = computed(() => (route.params.hackathonId as string) ?? '')
const { data: hackathon } = useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint({ path: computed(() => ({ hackathonIdOrShortCode: routeHackathonId.value })) })
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
      navigateTo(githubLoginUrl(config.public.api, route.fullPath), { external: true })
    }
  }
})
</script>

<template>
  <!-- Show loading while checking auth -->
  <GLoading
    v-if="isLoading || !showPage"
    page
    label="Checking your session…"
  />

  <AuthFrame v-else>
    <GLabelStrip
      as="p"
      :label="hackathon?.name ? `Registration · ${hackathon.name}` : 'Registration'"
      class="self-start"
    />
    <div class="flex flex-col gap-3">
      <h1 class="font-display text-[32px] leading-tight font-bold">
        You're registered
      </h1>
      <p class="text-gh-muted">
        Thanks for signing up. The organisers will review your answers and email you once they've decided. Check your spam folder if nothing arrives.
      </p>
    </div>
    <RegistrationSteps :current="2" />
    <p class="gh-small text-gh-muted">
      Once you're approved you can form or join a team in the portal.
    </p>
    <div class="flex flex-col gap-3">
      <GButton
        :to="statusPagePath ?? undefined"
        variant="primary"
        size="lg"
        block
        label="Check my registration status"
      />
      <GButton
        to="/dash"
        variant="ghost"
        block
        label="Go to the portal"
      />
    </div>
  </AuthFrame>
</template>
