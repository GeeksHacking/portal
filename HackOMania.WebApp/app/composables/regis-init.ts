import { fetchQuestions } from '~/composables/question'

interface RegistrationSetupOptions {
  hackathonId: string
  joinMutation: { mutateAsync: (id: string) => Promise<unknown> }
  initQuestionMutation: { mutateAsync: (id: string) => Promise<unknown> }
}

export async function registrationSetup({ hackathonId, joinMutation, initQuestionMutation }: RegistrationSetupOptions) {
  try {
    // Check if user is already a participant
    const statusResponse = await useNuxtApp()
      .$apiClient
      .participants
      .hackathons
      .byHackathonIdOrShortCodeId(hackathonId)
      .status
      .get()

    // Join the hackathon only if not already a participant
    if (!statusResponse?.isParticipant) {
      try {
        await joinMutation.mutateAsync(hackathonId)
      }
      catch (joinError) {
        console.error('[REGIS-INIT] Error joining hackathon:', joinError)
      }
    }

    // Check if registration questions exist, if not initialize them
    const questionsResponse = await fetchQuestions(hackathonId)

    const categories = questionsResponse?.categories ?? []
    const hasQuestions = categories.some(cat => cat.questions && cat.questions.length > 0)

    if (!hasQuestions) {
      await initQuestionMutation.mutateAsync(hackathonId)
    }

    return {
      success: true,
      hackathonId,
    }
  }
  catch (error) {
    console.error('[REGIS-INIT] Registration setup failed:', error)
    console.error('[REGIS-INIT] Error details:', JSON.stringify(error, null, 2))
    return {
      success: false,
      error,
    }
  }
}
