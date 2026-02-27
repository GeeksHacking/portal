import { queryOptions, useMutation } from '@tanstack/vue-query'

export const venueOverviewQueries = {
  overview: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'venue', 'overview'],
      refetchInterval: 15_000,
      async queryFn() {
        return await useNuxtApp()
          .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
          .venue.overview.get()
      },
    }),
}

export function useCheckInMutation(hackathonId: string) {
  return useMutation({
    mutationFn(participantUserId: string) {
      return useNuxtApp()
        .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
        .participants.byParticipantUserId(participantUserId)
        .venue.checkIn.post()
    },
  })
}
