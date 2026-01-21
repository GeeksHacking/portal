<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useQuery } from '@tanstack/vue-query'
import { fetchQuestions } from '~/composables/question'
import { registrationSetup } from '~/regis-init'
import { hackathonQueries as participantHackathonQueries } from '~/composables/hackathons'

useHead({
  titleTemplate: title => (title ? `${title} - HackOMania` : 'HackOMania'),
})

const props = defineProps<{
  hackathonId?: string | null
}>()

const hackathonId = computed(() => props.hackathonId ?? null)

const setupComplete = ref(false)
const setupError = ref<string | null>(null)
const lastSetupHackathonId = ref<string | null>(null)

// Ensure user is a participant before attempting to load registration data.
const { data: statusData, isLoading: isLoadingStatus, error: statusError } = useQuery(
  computed(() => ({
    ...participantHackathonQueries.status(hackathonId.value ?? ''),
    enabled: !!hackathonId.value,
  })),
)

const isNotParticipant = computed(() => statusData.value && statusData.value.isParticipant === false)

watch(
  () => ({ id: hackathonId.value, status: statusData.value }),
  async ({ id, status }) => {
    if (!id || status === undefined) return
    if (!status.isParticipant) {
      setupComplete.value = false
      lastSetupHackathonId.value = null
      return
    }

    if (lastSetupHackathonId.value === id && setupComplete.value) return

    setupError.value = null
    setupComplete.value = false

    try {
      await registrationSetup(id)
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
        Select a hackathon from the dashboard to start your registration.
      </p>
      <NuxtLink to="/dash" class="text-blue-600 dark:text-blue-400 underline">
        Go to dashboard
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
      v-else-if="isNotParticipant"
      class="text-center py-8 space-y-2"
    >
      <p class="text-gray-900 dark:text-gray-100">
        You need to join the hackathon before completing the registration form.
      </p>
      <NuxtLink to="/dash" class="text-blue-600 dark:text-blue-400 underline">
        Join from dashboard
      </NuxtLink>
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
      <RegistrationDynamicForm :questions="questions" :hackathon-id="hackathonId!" />
    </div>
  </div>
</template>
