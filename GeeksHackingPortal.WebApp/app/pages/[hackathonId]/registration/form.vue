<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { githubLoginUrl } from '~/utils/login-url'

definePageMeta({
  // Explicitly mark as public route
  auth: false,
})

const route = useRoute()
const config = useRuntimeConfig()
const routeHackathonId = computed(() => (route.params.hackathonId as string) ?? '')
const { data: hackathon } = useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint({ path: computed(() => ({ hackathonIdOrShortCode: routeHackathonId.value })) })
const resolvedHackathonId = computed(() => hackathon.value?.id ?? '')

// Track if we should show the form
const showForm = ref(false)

// Check if user is authenticated
const { data: user, isLoading: isLoadingUser, isError } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint({
  query: {
    retry: false,
    staleTime: 0,
    gcTime: 0,
  },
})

// Check participation status
const { data: statusData, isLoading: isLoadingStatus } = useGeeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpoint({ path: computed(() => ({ hackathonId: resolvedHackathonId.value })) }, { query: { enabled: computed(() => !!resolvedHackathonId.value && !!user.value) } })

// Check registration submissions
const { data: submissionsData, isLoading: isLoadingSubmissions } = useGeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListEndpoint({ path: computed(() => ({ hackathonId: resolvedHackathonId.value })) }, { query: { enabled: computed(() => !!resolvedHackathonId.value && statusData.value?.isParticipant === true) } })

const isLoading = computed(() => isLoadingUser.value || isLoadingStatus.value || isLoadingSubmissions.value)

// Handle auth and registration state
watchEffect(() => {
  if (isLoading.value || !hackathon.value)
    return

  // Not authenticated - redirect to login
  if (!user.value || isError.value) {
    if (resolvedHackathonId.value) {
      navigateTo(githubLoginUrl(config.public.api, route.fullPath), { external: true })
    }
    return
  }

  // Registration already complete - redirect to team page
  // Guard with current participant status to avoid redirect loops when cached
  // submissions exist for users who have withdrawn.
  if (statusData.value?.isParticipant && submissionsData.value?.requiredQuestionsRemaining === 0) {
    navigateTo({ path: `/${hackathon.value.shortCode}/team`, query: route.query }, { replace: true })
    return
  }

  // Show the form
  showForm.value = true
})
</script>

<template>
  <!-- Show loading while checking auth -->
  <GLoading
    v-if="isLoading || !showForm"
    page
    label="Checking your session…"
  />

  <!-- Show form if authenticated -->
  <RegistrationFormPage
    v-else
    :hackathon-id="resolvedHackathonId"
  />
</template>
