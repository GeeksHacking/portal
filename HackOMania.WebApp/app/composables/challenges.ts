import { queryOptions } from '@tanstack/vue-query'

export const challengeQueries = {
  list: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'challenges'],
      async queryFn() {
        return await useNuxtApp()
          .$apiClient.participants.hackathons.byHackathonIdOrShortCodeId(hackathonId)
          .challenges.get()
      },
    }),

  detail: (hackathonId: string, challengeId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'challenges', challengeId],
      async queryFn() {
        return await useNuxtApp()
          .$apiClient.participants.hackathons.byHackathonIdOrShortCodeId(hackathonId)
          .challenges.byChallengeId(challengeId)
          .get()
      },
    }),
}
