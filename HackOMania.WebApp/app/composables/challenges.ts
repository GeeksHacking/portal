import { queryOptions, useMutation } from '@tanstack/vue-query'
import type { HackOManiaApiEndpointsOrganizersHackathonChallengesCreateRequest, HackOManiaApiEndpointsOrganizersHackathonChallengesUpdateRequest } from '~/api-client/models'

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

export const challengeOrganizerQueries = {
  list: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'challenges', 'organizer'],
      async queryFn() {
        return await useNuxtApp()
          .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
          .challenges.get()
      },
    }),

  detail: (hackathonId: string, challengeId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'challenges', challengeId, 'organizer'],
      async queryFn() {
        return await useNuxtApp()
          .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
          .challenges.byChallengeId(challengeId)
          .get()
      },
    }),
}

export function useCreateChallengeMutation(hackathonId: string) {
  return useMutation({
    mutationFn(data: HackOManiaApiEndpointsOrganizersHackathonChallengesCreateRequest) {
      return useNuxtApp()
        .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
        .challenges.post(data)
    },
  })
}

export function useDeleteChallengeMutation(hackathonId: string) {
  return useMutation({
    mutationFn(challengeId: string) {
      return useNuxtApp()
        .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
        .challenges.byChallengeId(challengeId)
        .delete()
    },
  })
}

export function useUpdateChallengeMutation(hackathonId: string) {
  return useMutation({
    mutationFn({ challengeId, data }: { challengeId: string, data: HackOManiaApiEndpointsOrganizersHackathonChallengesUpdateRequest }) {
      return useNuxtApp()
        .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
        .challenges.byChallengeId(challengeId)
        .patch(data)
    },
  })
}
