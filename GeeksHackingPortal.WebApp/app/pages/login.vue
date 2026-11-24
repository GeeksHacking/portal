<script setup lang="ts">
import { useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint } from '@geekshacking/portal-sdk/hooks'
import { githubLoginUrl } from '~/utils/login-url'

definePageMeta({
  // Explicitly mark as public route
  auth: false,
})

const config = useRuntimeConfig()
const route = useRoute()
const loginFailed = computed(() => route.query.error === 'github' || route.query.error === 'session')
const sessionMissing = computed(() => route.query.error === 'session')

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

const loginUrl = computed(() => githubLoginUrl(config.public.api, '/dash'))

const isAuthenticated = computed(() => !!user.value && !isError.value)
</script>

<template>
  <GLoading
    v-if="isLoading || isAuthenticated"
    page
    :label="isAuthenticated ? 'Taking you to the portal…' : 'Checking your session…'"
  />

  <AuthFrame v-else>
    <div class="flex flex-col items-start gap-3">
      <GLabelStrip
        as="p"
        label="Sign in"
      />
      <h1 class="font-display text-[32px] leading-tight font-bold">
        Welcome to the GeeksHacking Portal
      </h1>
      <p class="text-gh-muted">
        Register for events, manage your team and run hackathons, all with your GitHub account.
      </p>
    </div>

    <GAlert
      v-if="sessionMissing"
      tone="err"
      title="We couldn't keep you signed in"
      description="GitHub sign-in finished, but this preview couldn't keep the session. Please try again."
    />
    <GAlert
      v-else-if="loginFailed"
      tone="err"
      title="GitHub sign-in didn't finish"
      description="Please try again."
    />

    <GButton
      :href="loginUrl"
      variant="primary"
      size="lg"
      block
      icon="fa6-brands:github"
      :label="loginFailed ? 'Try GitHub sign-in again' : 'Sign in with GitHub'"
    />
    <p class="gh-small text-gh-muted">
      Sign in to GitHub in this browser first, then come back here.
    </p>
  </AuthFrame>
</template>
