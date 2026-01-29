<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import { hackathonQueries as participantHackathonQueries } from '~/composables/hackathons'

definePageMeta({
  // Explicitly mark as public route
  auth: false,
})

const route = useRoute()
const config = useRuntimeConfig()
const hackathon = useRouteHackathon()
const resolvedHackathonId = useResolvedHackathonId()

// Track if we should show the page
const showPage = ref(false)

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
    ...participantHackathonQueries.status(resolvedHackathonId.value ?? ''),
    enabled: !!resolvedHackathonId.value && !!user.value,
  })),
)

// Check registration submissions
const { data: submissionsData, isLoading: isLoadingSubmissions } = useQuery(
  computed(() => ({
    queryKey: ['hackathons', resolvedHackathonId.value, 'registration', 'submissions'],
    queryFn: () => useNuxtApp().$apiClient.participants.hackathons
      .byHackathonIdOrShortCodeId(resolvedHackathonId.value ?? '')
      .registration.submissions.get(),
    enabled: !!resolvedHackathonId.value && statusData.value?.isParticipant === true,
  })),
)

const isLoading = computed(() => isLoadingUser.value || isLoadingStatus.value || (statusData.value?.isParticipant && isLoadingSubmissions.value))

// Determine registration state
type RegistrationState = 'not_registered' | 'incomplete' | 'pending_review' | 'rejected' | 'approved'

const registrationState = computed<RegistrationState>(() => {
  if (!statusData.value?.isParticipant) {
    return 'not_registered'
  }

  // Check if approved first (status === 1)
  if (statusData.value.status === 1) {
    return 'approved'
  }

  // Check if rejected (status === 2)
  if (statusData.value.status === 2) {
    return 'rejected'
  }

  // Check if incomplete (has required questions remaining)
  if (submissionsData.value && submissionsData.value.requiredQuestionsRemaining > 0) {
    return 'incomplete'
  }

  // Otherwise pending review
  return 'pending_review'
})

const stateContent = computed(() => {
  const shortCode = hackathon.value?.shortCode ?? ''

  switch (registrationState.value) {
    case 'not_registered':
      return {
        title: 'Registration Required',
        description: 'You haven\'t registered for HackOMania 2026 yet. Start your registration to participate in the hackathon.',
        primaryButton: {
          label: 'Start Registration',
          to: `/${shortCode}/registration/`,
        },
      }
    case 'incomplete':
      return {
        title: 'Complete Your Registration',
        description: 'Your registration is incomplete. Please continue filling out the required information to complete your registration.',
        primaryButton: {
          label: 'Continue Registration',
          to: `/${shortCode}/registration/form`,
        },
      }
    case 'pending_review':
      return {
        title: 'Registration Under Review',
        description: 'Thank you for registering! Our team is reviewing your application. You will be notified once a decision has been made.',
        primaryButton: {
          label: 'Back to Website',
          to: 'https://hackomania.geekshacking.com/',
          external: true,
        },
      }
    case 'rejected':
      return {
        title: 'Registration Rejected',
        description: 'Unfortunately, your registration has not been approved.',
        primaryButton: {
          label: 'Back to Website',
          to: 'https://hackomania.geekshacking.com/',
          external: true,
        },
      }
    case 'approved':
      return {
        title: 'Registration Approved!',
        description: 'Congratulations! Your registration has been approved. You can now access the team portal to form or join a team.',
        primaryButton: {
          label: 'Go to Team Portal',
          to: route.query.joinCode
            ? { path: `/${shortCode}/team`, query: { joinCode: route.query.joinCode } }
            : `/${shortCode}/team`,
        },
      }
  }
})

// Handle auth state changes
watchEffect(() => {
  if (isLoadingUser.value || !hackathon.value) return

  // Not authenticated - redirect to login
  if (!user.value || isError.value) {
    if (resolvedHackathonId.value) {
      navigateTo(`${config.public.api}/auth/login?redirect_uri=${encodeURIComponent(route.fullPath)}`, { external: true })
    }
    return
  }

  // Wait for status and submissions data to load for participants
  if (isLoadingStatus.value) return
  if (statusData.value?.isParticipant && isLoadingSubmissions.value) return

  // Redirect incomplete registrations to form page
  if (submissionsData.value && submissionsData.value.requiredQuestionsRemaining > 0) {
    navigateTo({ path: `/${hackathon.value.shortCode}/registration/form`, query: route.query }, { replace: true })
    return
  }

  // Show the page once auth is confirmed and redirects are handled
  showPage.value = true
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
            {{ stateContent.title }}
          </h1>

          <div class="font-raleway text-base md:text-lg font-normal text-black text-center max-w-lg space-y-4">
            <p>{{ stateContent.description }}</p>

          </div>
        </div>

        <!-- Show message from approved/rejected status -->
        <div
          v-if="(registrationState === 'approved' || registrationState === 'rejected') && statusData?.reviewReason"
          class="w-full max-w-lg"
        >
          <label class="block font-raleway text-sm font-medium text-gray-700 mb-1">
            Message
          </label>
          <UTextarea
            :model-value="statusData.reviewReason"
            readonly
            :rows="3"
            class="w-full"
          />
        </div>

        <div class="flex flex-col gap-3 mt-8 w-full max-w-sm">
          <UButton
            :to="stateContent.primaryButton.to"
            :external="stateContent.primaryButton.external"
            size="xl"
            color="neutral"
            class="w-full justify-center"
          >
            {{ stateContent.primaryButton.label }}
          </UButton>

          <UButton
            to="https://hackomania.geekshacking.com/"
            external
            size="xl"
            color="neutral"
            variant="outline"
            class="w-full justify-center"
          >
            Visit HackOMania Website
          </UButton>
        </div>
      </div>
    </div>
  </div>
</template>
