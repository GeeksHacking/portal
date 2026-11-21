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

// Track if we should show the page
const showPage = ref(false)

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

const isLoading = computed(() => isLoadingUser.value || isLoadingStatus.value || (statusData.value?.isParticipant && isLoadingSubmissions.value))

// Determine registration state
type RegistrationState = 'not_registered' | 'incomplete' | 'pending_review' | 'rejected' | 'approved'

const registrationState = computed<RegistrationState>(() => {
  if (!statusData.value?.isParticipant) {
    return 'not_registered'
  }

  // Check if approved first.
  if (statusData.value.status === 'Accepted') {
    return 'approved'
  }

  // Check if rejected.
  if (statusData.value.status === 'Rejected') {
    return 'rejected'
  }

  // Incomplete: required answers left, or the event has no questions yet (the form sets them up)
  if (submissionsData.value && (submissionsData.value.requiredQuestionsRemaining > 0 || submissionsData.value.totalQuestions === 0)) {
    return 'incomplete'
  }

  // Otherwise pending review
  return 'pending_review'
})

const teamPath = computed(() => {
  const path = `/${hackathon.value?.shortCode ?? ''}/team`
  return route.query.joinCode ? { path, query: { joinCode: route.query.joinCode } } : path
})

// One entry per state: copy, where the steps stand, and the next action
const stateContent = computed(() => {
  const shortCode = hackathon.value?.shortCode ?? ''

  switch (registrationState.value) {
    case 'not_registered':
      return {
        title: 'You haven\'t registered yet',
        description: 'Sign up to take part in this hackathon. It takes about five minutes.',
        step: 0,
        tone: null,
        primary: { label: 'Start registration', to: `/${shortCode}/registration` },
      }
    case 'incomplete':
      return {
        title: 'Finish your registration',
        description: 'Some required questions are still unanswered. Your answers so far are saved.',
        step: 1,
        tone: null,
        primary: { label: 'Continue registration', to: `/${shortCode}/registration/form` },
      }
    case 'rejected':
      return {
        title: 'Your registration wasn\'t approved',
        description: 'The organisers have reviewed your application and can\'t offer you a place this time.',
        step: 2,
        tone: 'err' as const,
        primary: null,
      }
    case 'approved':
      return {
        title: 'You\'re in!',
        description: 'Your registration has been approved. Head to the event portal to form or join a team.',
        step: 3,
        tone: 'ok' as const,
        primary: { label: 'Go to the event portal', to: teamPath.value },
      }
    case 'pending_review':
    default:
      return {
        title: 'Your registration is under review',
        description: 'Thanks for registering. The organisers are reviewing your answers and will email you once they\'ve decided.',
        step: 2,
        tone: null,
        primary: null,
      }
  }
})

useHead({ title: computed(() => `Registration status`) })

// Handle auth state changes
watchEffect(() => {
  if (isLoadingUser.value || !hackathon.value)
    return

  // Not authenticated - redirect to login
  if (!user.value || isError.value) {
    if (resolvedHackathonId.value) {
      navigateTo(githubLoginUrl(config.public.api, route.fullPath), { external: true })
    }
    return
  }

  // Wait for status and submissions data to load for participants
  if (isLoadingStatus.value)
    return
  if (statusData.value?.isParticipant && isLoadingSubmissions.value)
    return

  // Show the page once auth is confirmed and redirects are handled
  showPage.value = true
})
</script>

<template>
  <!-- Show loading while checking auth -->
  <GLoading
    v-if="isLoading || !showPage"
    page
    label="Checking your registration…"
  />

  <AuthFrame v-else>
    <GLabelStrip
      as="p"
      :label="hackathon?.name ? `Registration · ${hackathon.name}` : 'Registration'"
      class="self-start"
    />
    <div class="flex flex-col gap-3">
      <h1 class="font-display text-[32px] leading-tight font-bold">
        {{ stateContent.title }}
      </h1>
      <p class="text-gh-muted">
        {{ stateContent.description }}
      </p>
    </div>

    <RegistrationSteps
      :current="stateContent.step"
      :rejected="registrationState === 'rejected'"
    />

    <!-- the organiser's note with the decision -->
    <GAlert
      v-if="(registrationState === 'approved' || registrationState === 'rejected') && statusData?.reviewReason"
      :tone="stateContent.tone ?? 'neutral'"
      title="Message from the organisers"
      :description="statusData.reviewReason"
    />

    <div class="flex flex-col gap-3">
      <GButton
        v-if="stateContent.primary"
        :to="stateContent.primary.to"
        variant="primary"
        size="lg"
        block
        :label="stateContent.primary.label"
      />
      <GButton
        to="/dash"
        :variant="stateContent.primary ? 'ghost' : 'secondary'"
        block
        label="Go to the portal"
      />
    </div>
  </AuthFrame>
</template>
