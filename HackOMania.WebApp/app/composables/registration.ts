import { queryOptions, useMutation } from '@tanstack/vue-query'
import { computed, toValue, type MaybeRefOrGetter } from 'vue'

export interface RegistrationSubmission {
  questionId: string
  value: string
  followUpValue?: string | null
}

export interface SubmitRegistrationRequest {
  hackathonId: string
  submissions: RegistrationSubmission[]
}

export interface SubmitRegistrationResponse {
  submissionsCount: number
  message: string
}

export const registrationQueries = {
  questions: (hackathonId: MaybeRefOrGetter<string>) =>
    queryOptions({
      queryKey: computed(() => ['registration', 'questions', toValue(hackathonId)] as const),
      async queryFn() {
        const id = toValue(hackathonId)
        return await useNuxtApp().$apiClient.participants.hackathons.byHackathonId(id).registration.questions.get()
      },
      retry: false,
    }),
  submissions: (hackathonId: MaybeRefOrGetter<string>) =>
    queryOptions({
      queryKey: computed(() => ['registration', 'submissions', toValue(hackathonId)] as const),
      async queryFn() {
        const id = toValue(hackathonId)
        return await useNuxtApp().$apiClient.participants.hackathons.byHackathonId(id).registration.submissions.get()
      },
      retry: false,
    }),
}

export function useSubmitRegistration() {
  return useMutation({
    async mutationFn(data: SubmitRegistrationRequest) {
      const { hackathonId, submissions } = data
      // The API expects a body with submissions array
      return await useNuxtApp().$apiClient.participants.hackathons
        .byHackathonId(hackathonId)
        .registration.submissions.post({
          submissions,
        })
    },
  })
}
