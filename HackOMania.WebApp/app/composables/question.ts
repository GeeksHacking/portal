import { queryOptions } from '@tanstack/vue-query'

export const questionQueries = {
  list: queryOptions({
    queryKey: ['registration', 'questions'],
    async queryFn() {
      const hackathonId = useHackathonId()
      if (!hackathonId.value) {
        throw new Error('Hackathon ID not found, did you call the regis-init script')
      }
      return await useNuxtApp().$apiClient.participants.hackathons
        .byHackathonIdOrShortCodeId(hackathonId.value)
        .registration.questions.get()
    },
    retry: false,
  }),
}

export const questionActions = {
  async initialize(hackathonId: string) {
    return await useNuxtApp().$apiClient.organizers.hackathons
      .byHackathonId(hackathonId)
      .registration.questions.initialize.post()
  },
}
