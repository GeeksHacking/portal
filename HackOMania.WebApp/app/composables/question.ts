import { useMutation } from '@tanstack/vue-query'
import type {
  HackOManiaApiEndpointsParticipantsHackathonRegistrationSubmissionsSubmitRequest,
  HackOManiaApiEndpointsParticipantsHackathonRegistrationQuestionsListQuestionDto,
} from '~/api-client/models'

// Hackathon ID from DatabaseInitBackgroundService
export const dev_hackathonid = '1e2beba8-0dd2-484f-b5b2-4b1b71a084e4'

export async function fetchQuestions() {
  return await useNuxtApp().$apiClient.participants.hackathons
    .byHackathonIdOrShortCodeId(dev_hackathonid)
    .registration.questions.get()
}

export function useInitQuestionMutation() {
  return useMutation({
    mutationFn(hackathonId: string) {
      return useNuxtApp().$apiClient.organizers.hackathons
        .byHackathonId(hackathonId)
        .registration.questions.initialize.post()
    },
  })
}

type Question = HackOManiaApiEndpointsParticipantsHackathonRegistrationQuestionsListQuestionDto

export function useSubmitRegistrationMutation(questions: Ref<Question[]>) {
  const fieldErrors = ref<Record<string, string>>({}) // need to store the field errors because i want them to persist throughout the different pages
  const submissionError = ref(false)

  const mutation = useMutation({
    mutationFn(data: HackOManiaApiEndpointsParticipantsHackathonRegistrationSubmissionsSubmitRequest) {
      // Clear previous errors before submitting
      fieldErrors.value = {}
      submissionError.value = false

      return useNuxtApp().$apiClient.participants.hackathons
        .byHackathonIdOrShortCodeId(dev_hackathonid)
        .registration.submissions.post(data)
    },
    onError(error: unknown) {
      submissionError.value = true
      parseErrorsToFields(error, questions.value, fieldErrors.value)
    },
  })

  return {
    ...mutation,
    fieldErrors,
    submissionError,
  }
}

// reduce time complexity of looping through everytime there's an error (thx copilot)
function buildQuestionMaps(questions: Question[]) {
  const keywords = ['phone', 'telegram', 'age', 'experience', 'email', 'github']
  const questionsByText = new Map<string, Question>()
  const keywordToQuestion = new Map<string, Question>()

  for (const question of questions) {
    const questionText = question.questionText?.toLowerCase() ?? ''
    const questionKey = question.questionKey?.toLowerCase() ?? ''

    if (questionText.length > 5) {
      questionsByText.set(questionText, question)
    }

    for (const keyword of keywords) {
      if (questionKey.includes(keyword)) {
        keywordToQuestion.set(keyword, question)
      }
    }
  }

  return { questionsByText, keywordToQuestion, keywords }
}

// make the error go according to the question field, instead of showing all errors at once
function parseErrorsToFields(
  error: unknown,
  questions: Question[],
  fieldErrors: Record<string, string>,
) {
  try {
    // Error is thrown directly from response.json(), so structure is { errors: { generalErrors: [...] } }
    const err = error as { errors?: { generalErrors?: string[] } }
    const errors = err?.errors?.generalErrors ?? []
    if (errors.length === 0) return

    // Build maps once - O(m)
    const { questionsByText, keywordToQuestion, keywords } = buildQuestionMaps(questions)

    for (const errorMessage of errors) {
      const errorLower = errorMessage.toLowerCase()
      let matched = false

      // Match by question text
      for (const [questionText, question] of questionsByText) {
        if (errorLower.includes(`'${questionText}'`) || errorLower.includes(questionText)) {
          fieldErrors[question.questionKey ?? ''] = errorMessage
          matched = true
          break
        }
      }

      // match by keywords
      if (!matched) {
        for (const keyword of keywords) {
          if (errorLower.includes(keyword)) {
            const question = keywordToQuestion.get(keyword)
            if (question) {
              fieldErrors[question.questionKey ?? ''] = errorMessage
              break
            }
          }
        }
      }
    }
  }
  catch (parseError) {
    console.error('[QUESTION] Error parsing:', parseError)
  }
}
