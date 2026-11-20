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

const config = useRuntimeConfig()
const route = useRoute()
const routeHackathonId = computed(() => (route.params.hackathonId as string) ?? '')
const { data: hackathon } = useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint({ path: computed(() => ({ hackathonIdOrShortCode: routeHackathonId.value })) })

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

const loginUrl = computed(() =>
  githubLoginUrl(config.public.api, route.fullPath.replace('/registration', '/registration/form')),
)

// Only redirect to form if query succeeded and we have user data
watchEffect(() => {
  if (isSuccess.value && user.value && hackathon.value) {
    navigateTo({ path: `/${hackathon.value.shortCode}/registration/form`, query: route.query }, { replace: true })
  }
})
</script>

<template>
  <!-- Show loading while checking auth or if authenticated (redirecting) -->
  <GLoading
    v-if="isLoading || user"
    page
    label="Checking your session…"
  />

  <!-- signed out: sign up first, then the form -->
  <AuthFrame v-else>
    <div class="flex flex-col items-start gap-3">
      <GLabelStrip
        as="p"
        label="Registration"
      />
      <h1 class="font-display text-[32px] leading-tight font-bold">
        {{ hackathon?.name ? `Register for ${hackathon.name}` : 'Register to participate' }}
      </h1>
      <p class="text-gh-muted">
        Sign up with your GitHub account, then fill in a short registration form.
      </p>
    </div>

    <GAlert
      tone="attention"
      title="You'll join the waiting list"
      description="We've had a large number of registrations, so every new registration goes on a waiting list. We'll let you know as soon as a slot opens."
    />

    <GButton
      :href="loginUrl"
      variant="primary"
      size="lg"
      block
      icon="fa6-brands:github"
      label="Sign up with GitHub"
    />
    <GButton
      to="/"
      variant="ghost"
      label="Exit registration"
      class="self-center"
    />
  </AuthFrame>
</template>
