import { queryOptions, useMutation } from '@tanstack/vue-query'
import { toValue } from 'vue'
import type { MaybeRefOrGetter } from 'vue'
import type { HackOManiaApiEndpointsOrganizersHackathonParticipantsReviewRequest } from '~/api-client/models'

export const participantOrganizerQueries = {
  list: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'participants', 'organizer'],
      staleTime: 30_000,
      async queryFn() {
        return await useNuxtApp()
          .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
          .participants.get()
      },
    }),
  detail: (hackathonId: string, participantUserId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'participants', 'organizer', participantUserId],
      staleTime: 30_000,
      async queryFn() {
        return await useNuxtApp()
          .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
          .participants.byParticipantUserId(participantUserId)
          .get()
      },
    }),
}

export function useReviewParticipantMutation(hackathonId: MaybeRefOrGetter<string>) {
  return useMutation({
    mutationFn(data: { participantUserId: string, review: HackOManiaApiEndpointsOrganizersHackathonParticipantsReviewRequest }) {
      return useNuxtApp()
        .$apiClient.organizers.hackathons.byHackathonId(toValue(hackathonId))
        .participants.byParticipantUserId(data.participantUserId)
        .review.post(data.review)
    },
  })
}
