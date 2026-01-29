import { queryOptions, useMutation } from '@tanstack/vue-query'
import { unref, type MaybeRef, type Ref } from 'vue'
import type {
  HackOManiaApiEndpointsParticipantsHackathonRegistrationSubmissionsSubmitRequest,
  HackOManiaApiEndpointsParticipantsHackathonRegistrationQuestionsListQuestionDto,
} from '~/api-client/models'

export async function fetchQuestions(hackathonId: string) {
  return await useNuxtApp().$apiClient.participants.hackathons
    .byHackathonIdOrShortCodeId(hackathonId)
    .registration.questions.get()
}

export const registrationQuestionQueries = {
  list: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'registration', 'questions'],
      queryFn: () => fetchQuestions(hackathonId),
    }),
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

export function useSubmitRegistrationMutation(hackathonId: MaybeRef<string>, questions: Ref<Question[]>) {
  const fieldErrors = ref<Record<string, string>>({}) // need to store the field errors because i want them to persist throughout the different pages
  const submissionError = ref(false)

  const mutation = useMutation({
    mutationFn(data: HackOManiaApiEndpointsParticipantsHackathonRegistrationSubmissionsSubmitRequest) {
      // Clear previous errors before submitting
      fieldErrors.value = {}
      submissionError.value = false

      const id = unref(hackathonId)
      return useNuxtApp().$apiClient.participants.hackathons
        .byHackathonIdOrShortCodeId(id)
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

// Map Kiota FastEndpoints errors to field errors by question ID
function parseErrorsToFields(
  error: unknown,
  questions: Question[],
  fieldErrors: Record<string, string>,
) {
  try {
    // Kiota wraps FastEndpoints errors: { errors: { additionalData: { fieldId: [...] } } }
    const err = error as {
      errors?: {
        additionalData?: Record<string, string[] | string>
      }
    }
    const errorBag = err?.errors?.additionalData ?? {}

    const questionById = new Map(
      questions
        .filter(question => question.id)
        .map(question => [question.id as string, question]),
    )

    for (const [fieldId, messages] of Object.entries(errorBag)) {
      const question = questionById.get(fieldId)
      if (!question?.questionKey) continue
      const message = Array.isArray(messages) ? messages[0] : String(messages)
      if (message) {
        fieldErrors[question.questionKey] = message
      }
    }
  }
  catch (parseError) {
    console.error('[QUESTION] Error parsing:', parseError)
  }
}
