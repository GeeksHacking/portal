<script setup lang="ts">
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { hackathonQueries as participantHackathonQueries } from '~/composables/hackathons'

const props = defineProps<{
  hackathonId?: string | null
}>()

useHead({
  titleTemplate: title => (title ? `${title} - HackOMania` : 'HackOMania'),
})

const hackathonId = computed(() => props.hackathonId ?? null)

const setupComplete = ref(false)
const setupError = ref<string | null>(null)
const lastSetupHackathonId = ref<string | null>(null)
const isJoining = ref(false)

// Initialize mutations at component level (must be in setup)
const initQuestionMutation = useInitQuestionMutation()
const joinMutation = useJoinHackathonMutation()
const queryClient = useQueryClient()

// Ensure user is a participant before attempting to load registration data.
const { data: statusData, isLoading: isLoadingStatus, error: statusError } = useQuery(
  computed(() => ({
    ...participantHackathonQueries.status(hackathonId.value ?? ''),
    enabled: !!hackathonId.value,
  })),
)

watch(
  () => ({ id: hackathonId.value, status: statusData.value }),
  async ({ id, status }) => {
    if (!id || status === undefined)
      return

    // If not a participant, auto-join the hackathon
    if (!status.isParticipant) {
      if (isJoining.value)
        return // Prevent duplicate join attempts

      isJoining.value = true
      setupComplete.value = false
      lastSetupHackathonId.value = null

      try {
        await joinMutation.mutateAsync(id)
        // Invalidate status query to refetch participant status
        await queryClient.invalidateQueries({ queryKey: ['hackathons', id, 'status'] })
      }
      catch (error) {
        console.error('[FORM] Failed to join hackathon:', error)
        setupError.value = 'Failed to join hackathon. Please try again.'
        isJoining.value = false
      }
      return
    }

    // Reset joining state once we're a participant
    isJoining.value = false

    if (lastSetupHackathonId.value === id && setupComplete.value)
      return

    setupError.value = null
    setupComplete.value = false

    try {
      await registrationSetup({ hackathonId: id, joinMutation, initQuestionMutation })
      lastSetupHackathonId.value = id
      setupComplete.value = true
    }
    catch (error) {
      console.error('[FORM] Registration setup failed:', error)
      setupError.value = 'Failed to initialize registration. Please try again.'
    }
  },
  { immediate: true },
)

// Fetch registration questions (only after setup is complete)
const { data: questions, isLoading, error } = useQuery(computed(() => ({
  queryKey: ['registration', 'questions', hackathonId.value],
  queryFn: () => fetchQuestions(hackathonId.value ?? ''),
  enabled: setupComplete.value && !!hackathonId.value,
})))
</script>

<template>
  <div class="bg-white dark:bg-gray-900 min-h-screen font-raleway">
    <RegistrationFormHeader :hackathon-id="hackathonId" />

    <div v-if="!hackathonId" class="text-center py-8 space-y-2">
      <p class="text-gray-900 dark:text-gray-100">
        Unable to load registration. Please return to the website and try again.
      </p>
      <NuxtLink to="https://hackomania.geekshacking.com/" external class="text-blue-600 dark:text-blue-400 underline">
        Return to HackOMania
      </NuxtLink>
    </div>

    <div
      v-else-if="isLoadingStatus || isLoading"
      class="text-center py-8"
    >
      <p class="text-gray-900 dark:text-gray-100">
        Loading registration questions...
      </p>
    </div>

    <div
      v-else-if="isJoining"
      class="text-center py-8"
    >
      <p class="text-gray-900 dark:text-gray-100">
        Joining hackathon...
      </p>
    </div>

    <div
      v-else-if="statusError"
      class="text-center py-8"
    >
      <p class="text-red-600 dark:text-red-400">
        Unable to load your participation status. Please return to the dashboard and try again.
      </p>
    </div>

    <div
      v-else-if="!setupComplete"
      class="text-center py-8"
    >
      <p class="text-gray-900 dark:text-gray-100">
        Preparing your registration form...
      </p>
    </div>

    <!-- Setup Error State -->
    <div
      v-else-if="setupError"
      class="text-center py-8"
    >
      <p class="text-red-600 dark:text-red-400">
        {{ setupError }}
      </p>
    </div>

    <!-- Error State -->
    <div
      v-else-if="error"
      class="text-center py-8"
    >
      <p class="text-red-600 dark:text-red-400">
        Error loading questions: {{ error.message }}
      </p>
    </div>

    <!-- Render Form with Questions -->
    <div v-else-if="questions">
      <RegistrationDynamicForm
        :questions="questions"
        :hackathon-id="hackathonId!"
        @section-change="onSectionChange"
      />
    </div>
  </div>
</template>
