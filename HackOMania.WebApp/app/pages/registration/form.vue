<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import { registrationSetup } from '~/regis-init'

useHead({
  titleTemplate: title => (title ? `${title} - HackOMania` : 'HackOMania'),
})

// Run registration setup on page load, idk where else to put this.. also this registration includes the joining hackathon lol
const setupComplete = ref(false)
const setupError = ref<string | null>(null)

onMounted(async () => {
  try {
    await registrationSetup()
    setupComplete.value = true
  }
  catch (error) {
    console.error('[FORM] Registration setup failed:', error)
    setupError.value = 'Failed to initialize registration. Please try again.'
  }
})

// Fetch registration questions (only after setup is complete)
const { data: questions, isLoading, error } = useQuery({
  queryKey: ['registration', 'questions'],
  queryFn: fetchQuestions,
  enabled: setupComplete,
})
</script>

<template>
  <div class="bg-white min-h-screen font-raleway">
    <RegistrationFormHeader />

    <!-- Loading State -->
    <div
      v-if="isLoading"
      class="text-center py-8"
    >
      <p class="text-black">
        Loading registration questions...
      </p>
    </div>

    <!-- Setup Error State -->
    <div
      v-else-if="setupError"
      class="text-center py-8"
    >
      <p class="text-red-600">
        {{ setupError }}
      </p>
    </div>

    <!-- Error State -->
    <div
      v-else-if="error"
      class="text-center py-8"
    >
      <p class="text-red-600">
        Error loading questions: {{ error.message }}
      </p>
    </div>

    <!-- Render Form with Questions -->
    <div v-else-if="questions">
      <RegistrationDynamicForm :questions="questions" />
    </div>
  </div>
</template>
