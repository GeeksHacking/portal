<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import { useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint } from '@geekshacking/portal-sdk/hooks'
import { hackathonQueries as participantHackathonQueries } from '~/composables/hackathons'

definePageMeta({
  // Explicitly mark as public route
  auth: false,
})

const route = useRoute()
const config = useRuntimeConfig()
const hackathon = useRouteHackathon()
const resolvedHackathonId = useResolvedHackathonId()

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
const { data: statusData, isLoading: isLoadingStatus } = useQuery(
  computed(() => ({
    ...participantHackathonQueries.status(resolvedHackathonId.value ?? ''),
    enabled: !!resolvedHackathonId.value && !!user.value,
  })),
)

// Check registration submissions
const { data: submissionsData, isLoading: isLoadingSubmissions } = useQuery(
  computed(() => ({
    queryKey: ['hackathons', resolvedHackathonId.value, 'registration', 'submissions'],
    queryFn: () => useNuxtApp().$apiClient.participants.hackathons.byHackathonIdOrShortCodeId(resolvedHackathonId.value ?? '').registration.submissions.get(),
    enabled: !!resolvedHackathonId.value && statusData.value?.isParticipant === true,
  })),
)

const isLoading = computed(() => isLoadingUser.value || isLoadingStatus.value || isLoadingSubmissions.value)

// Handle auth and registration state
watchEffect(() => {
  if (isLoading.value || !hackathon.value)
    return

  // Not authenticated - redirect to login
  if (!user.value || isError.value) {
    if (resolvedHackathonId.value) {
      navigateTo(`${config.public.api}/auth/login?redirect_uri=${encodeURIComponent(route.fullPath)}`, { external: true })
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
  <div
    v-if="isLoading || !showForm"
    class="bg-white min-h-screen flex flex-col items-center justify-center gap-4 px-4"
  >
    <p class="text-sm font-medium text-gray-600 animate-pulse">
      Checking your session...
    </p>
    <UIcon name="i-lucide-loader-circle" class="w-8 h-8 animate-spin text-primary" />
  </div>

  <!-- Show form if authenticated -->
  <RegistrationFormPage
    v-else
    :hackathon-id="resolvedHackathonId"
  />
</template>
