import { queryOptions, useMutation } from '@tanstack/vue-query'
import type { HackOManiaApiEndpointsOrganizersHackathonParticipantsReviewRequest } from '~/api-client/models'

export const participantOrganizerQueries = {
  list: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'participants', 'organizer'],
      async queryFn() {
        return await useNuxtApp()
          .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
          .participants.get()
      },
    }),
}

export function useReviewParticipantMutation(hackathonId: string) {
  return useMutation({
    mutationFn(data: { participantUserId: string, review: HackOManiaApiEndpointsOrganizersHackathonParticipantsReviewRequest }) {
      return useNuxtApp()
        .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
        .participants.byParticipantUserId(data.participantUserId)
        .review.post(data.review)
    },
  })
}
