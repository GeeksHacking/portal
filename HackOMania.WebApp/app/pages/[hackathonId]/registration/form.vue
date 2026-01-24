<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import { hackathonQueries as participantHackathonQueries } from '~/composables/hackathons'

definePageMeta({
  // Explicitly mark as public route
  auth: false,
})

const route = useRoute()
const config = useRuntimeConfig()
const hackathonId = computed(() => (route.params.hackathonId as string | undefined) ?? null)

// Track if we should show the form
const showForm = ref(false)

// Check if user is authenticated
const { data: user, isLoading: isLoadingUser, isError } = useQuery({
  ...authQueries.whoAmI,
  retry: false,
  staleTime: 0,
  gcTime: 0,
})

// Check participation status
const { data: statusData, isLoading: isLoadingStatus } = useQuery(
  computed(() => ({
    ...participantHackathonQueries.status(hackathonId.value ?? ''),
    enabled: !!hackathonId.value && !!user.value,
  })),
)

// Check registration submissions
const { data: submissionsData, isLoading: isLoadingSubmissions } = useQuery(
  computed(() => ({
    queryKey: ['hackathons', hackathonId.value, 'registration', 'submissions'],
    queryFn: () => useNuxtApp().$apiClient.participants.hackathons
      .byHackathonIdOrShortCodeId(hackathonId.value ?? '')
      .registration.submissions.get(),
    enabled: !!hackathonId.value && statusData.value?.isParticipant === true,
  })),
)

const isLoading = computed(() => isLoadingUser.value || isLoadingStatus.value || isLoadingSubmissions.value)

// Handle auth and registration state
watchEffect(() => {
  if (isLoading.value) return

  // Not authenticated - redirect to login
  if (!user.value || isError.value) {
    if (hackathonId.value) {
      navigateTo(`${config.public.api}/auth/login?redirect_uri=${encodeURIComponent(route.fullPath)}`, { external: true })
    }
    return
  }

  // Registration already complete - redirect to dashboard
  if (submissionsData.value?.requiredQuestionsRemaining === 0) {
    navigateTo('/dash', { replace: true })
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
    :hackathon-id="hackathonId"
  />
</template>
