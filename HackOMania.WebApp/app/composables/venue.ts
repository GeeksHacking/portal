import type { MaybeRefOrGetter } from 'vue'
import { queryOptions, useMutation } from '@tanstack/vue-query'
import { toValue } from 'vue'

export const venueOverviewQueries = {
  overview: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'venue', 'overview'],
      refetchInterval: 15_000,
      async queryFn() {
        return await useNuxtApp()
          .$apiClient
          .organizers
          .hackathons
          .byHackathonId(hackathonId)
          .venue
          .overview
          .get()
      },
    }),
}

export function useCheckInMutation(hackathonId: MaybeRefOrGetter<string>) {
  return useMutation({
    mutationFn(participantUserId: string) {
      return useNuxtApp()
        .$apiClient
        .organizers
        .hackathons
        .byHackathonId(toValue(hackathonId))
        .participants
        .byParticipantUserId(participantUserId)
        .venue
        .checkIn
        .post()
    },
  })
}

export function useCheckOutMutation(hackathonId: MaybeRefOrGetter<string>) {
  return useMutation({
    async mutationFn(participantUserId: string) {
      const { public: { api } } = useRuntimeConfig()
      return await $fetch<{ id: string, checkOutTime: string, isCheckedIn: boolean }>(
        `${api}/organizers/hackathons/${toValue(hackathonId)}/participants/${participantUserId}/venue/check-out`,
        {
          method: 'POST',
          credentials: 'include',
        },
      )
    },
  })
}

export const venueHistoryQueries = {
  participant: (hackathonId: string, participantUserId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'venue', 'history', participantUserId],
      async queryFn() {
        const { public: { api } } = useRuntimeConfig()
        return await $fetch<{
          participantId: string
          userId: string
          userName: string
          isCurrentlyCheckedIn: boolean
          history: { checkInTime: string, checkOutTime?: string | null, isCheckedIn: boolean }[]
        }>(
          `${api}/organizers/hackathons/${hackathonId}/participants/${participantUserId}/venue/history`,
          {
            credentials: 'include',
          },
        )
      },
    }),
}
