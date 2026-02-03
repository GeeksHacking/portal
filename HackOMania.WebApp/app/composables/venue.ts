import { useMutation } from '@tanstack/vue-query'

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
