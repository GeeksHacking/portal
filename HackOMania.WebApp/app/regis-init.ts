import { useJoinHackathonMutation } from '~/composables/hackathon'
import { dev_hackathonid, fetchQuestions, useInitQuestionMutation } from '~/composables/question'

export const registrationSetup = async () => {
  const joinMutation = useJoinHackathonMutation()
  const initQuestionMutation = useInitQuestionMutation()

  try {
    // Join the hackathon as a participant
    try {
      await joinMutation.mutateAsync(dev_hackathonid)
    }
    catch (joinError) {
      console.error('[REGIS-INIT] Error joining hackathon (may already be joined):', joinError)
    }

    // Check if registration questions exist, if not initialize them
    const questionsResponse = await fetchQuestions()

    const categories = questionsResponse?.categories ?? []
    const hasQuestions = categories.some(cat => cat.questions && cat.questions.length > 0)

    if (!hasQuestions) {
      await initQuestionMutation.mutateAsync(dev_hackathonid)
    }

    return {
      success: true,
      hackathonId: dev_hackathonid,
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
