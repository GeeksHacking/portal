import { queryOptions } from '@tanstack/vue-query'

export const hackathonQueries = {
  list: queryOptions({
    queryKey: ['hackathons'],
    async queryFn() {
      return await useNuxtApp().$apiClient.participants.hackathons.get()
    },
  }),
}
