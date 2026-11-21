<script setup lang="ts">
import {
  geeksHackingPortalApiEndpointsParticipantsHackathonRegistrationQuestionsListEndpointQueryOptions,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsInitializeEndpoint1,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonJoinEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQuery, useQueryClient } from '@tanstack/vue-query'

const props = defineProps<{
  hackathonId?: string | null
  /** inside the portal shell (dash): no standalone header or full-height page */
  embedded?: boolean
}>()

useHead({
  titleTemplate: title => (title ? `${title} - GeeksHacking Portal` : 'GeeksHacking Portal'),
})

const hackathonId = computed(() => props.hackathonId ?? null)

const setupComplete = ref(false)
const setupError = ref<string | null>(null)
const lastSetupHackathonId = ref<string | null>(null)
const canJoinFromHere = ref(false)

// Initialize mutations at component level (must be in setup)
const initQuestionMutation = useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsInitializeEndpoint1()
const joinMutation = useGeeksHackingPortalApiEndpointsParticipantsHackathonJoinEndpoint()
const queryClient = useQueryClient()

// Ensure user is a participant before attempting to load registration data.
const { data: statusData, isLoading: isLoadingStatus, error: statusError } = useGeeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpoint(
  { path: computed(() => ({ hackathonId: hackathonId.value ?? '' })) },
  { query: { enabled: computed(() => !!hackathonId.value) } },
)

watch(
  () => ({ id: hackathonId.value, status: statusData.value }),
  async ({ id, status }) => {
    if (!id || status === undefined)
      return

    // If not a participant, do not auto-join. This prevents withdrawn participants
    // from being silently re-joined by registration initialization.
    if (!status.isParticipant) {
      setupComplete.value = false
      lastSetupHackathonId.value = null
      canJoinFromHere.value = true
      setupError.value = 'You are not currently participating in this hackathon. Please join to continue.'
      return
    }

    canJoinFromHere.value = false

    if (lastSetupHackathonId.value === id && setupComplete.value)
      return

    setupError.value = null
    setupComplete.value = false

    try {
      const questionsResponse = await queryClient.fetchQuery(
        geeksHackingPortalApiEndpointsParticipantsHackathonRegistrationQuestionsListEndpointQueryOptions({ path: { hackathonId: id } }),
      )
      const categories = questionsResponse?.categories ?? []
      const hasQuestions = categories.some(cat => cat.questions && cat.questions.length > 0)

      if (!hasQuestions)
        await initQuestionMutation.mutateAsync({ path: { activityId: id } })

      lastSetupHackathonId.value = id
      setupComplete.value = true
      await queryClient.invalidateQueries({ queryKey: [{ url: '/participants/hackathons/:hackathonId/status', params: { hackathonId: id } }] })
    }
    catch (error) {
      console.error('[FORM] Registration setup failed:', error)
      // only organisers can set up the questions: a participant arriving first just has to wait
      const status = (error as { status?: number } | null)?.status
      setupError.value = status === 401 || status === 403
        ? 'Registration isn\'t open yet: the organisers are still setting up the questions. Try again later.'
        : 'Failed to initialize registration. Please try again.'
    }
  },
  { immediate: true },
)

// Fetch registration questions (only after setup is complete)
const { data: questions, isLoading, error } = useQuery(computed(() => ({
  ...geeksHackingPortalApiEndpointsParticipantsHackathonRegistrationQuestionsListEndpointQueryOptions({ path: { hackathonId: hackathonId.value ?? '' } }),
  enabled: setupComplete.value && !!hackathonId.value,
})))

async function joinHackathonFromRegistration() {
  if (!hackathonId.value)
    return

  try {
    await joinMutation.mutateAsync({ path: { hackathonId: hackathonId.value } })
    await queryClient.invalidateQueries({ queryKey: [{ url: '/participants/hackathons/:hackathonId/status', params: { hackathonId: hackathonId.value } }] })
    setupError.value = null
  }
  catch (joinError) {
    console.error('[FORM] Failed to join hackathon:', joinError)
    setupError.value = 'Unable to join right now. Please try again from the participant dashboard.'
  }
}
</script>

<template>
  <div
    class="reg-page"
    :class="{ 'reg-page--embedded': embedded }"
  >
    <RegistrationFormHeader
      v-if="!embedded"
      :hackathon-id="hackathonId"
    />

    <div
      v-if="!hackathonId"
      class="reg-page__notice"
    >
      <GAlert
        tone="attention"
        title="We couldn't load this registration"
        description="Go back to the portal and open the registration link again."
      />
      <GButton
        to="/"
        variant="secondary"
        label="Back to the portal"
        class="self-start"
      />
    </div>

    <GLoading
      v-else-if="isLoadingStatus || isLoading"
      label="Loading registration questions…"
    />

    <div
      v-else-if="statusError"
      class="reg-page__notice"
    >
      <GAlert
        tone="err"
        title="We couldn't check your registration"
        description="Go back to the portal and try again."
      />
    </div>

    <div
      v-else-if="setupError"
      class="reg-page__notice"
    >
      <GAlert
        :tone="canJoinFromHere ? 'attention' : 'err'"
        :title="canJoinFromHere ? 'You\'re not taking part in this hackathon' : 'Registration unavailable'"
        :description="setupError"
      />
      <GButton
        v-if="canJoinFromHere"
        variant="primary"
        label="Join hackathon"
        :loading="joinMutation.isPending.value"
        loading-label="Joining"
        class="self-start"
        @click="joinHackathonFromRegistration"
      />
    </div>

    <GLoading
      v-else-if="!setupComplete"
      label="Preparing your registration form…"
    />

    <div
      v-else-if="error"
      class="reg-page__notice"
    >
      <GAlert
        tone="err"
        title="We couldn't load the questions"
        :description="error.message"
      />
    </div>

    <RegistrationDynamicForm
      v-else-if="questions"
      :questions="questions"
      :hackathon-id="hackathonId!"
      :embedded="embedded"
    />
  </div>
</template>

<style scoped>
.reg-page {
  display: flex;
  flex-direction: column;
  min-height: 100dvh;
  background: var(--gh-bg);
  color: var(--gh-text);
  font-family: var(--font-body);
  letter-spacing: var(--gh-tracking);
}
.reg-page--embedded {
  min-height: 0;
  background: none;
}
.reg-page__notice {
  display: flex;
  flex-direction: column;
  gap: 12px;
  width: 100%;
  max-width: 36rem;
  margin: 0 auto;
  padding: 32px 16px;
}
</style>
