import type { MaybeRefOrGetter } from 'vue'
import type { HackOManiaApiEndpointsOrganizersHackathonParticipantsReviewRequest } from '~/api-client/models'
import { queryOptions, useMutation } from '@tanstack/vue-query'
import { toValue } from 'vue'

export const participantOrganizerQueries = {
  list: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'participants', 'organizer'],
      staleTime: 30_000,
      async queryFn() {
        return await useNuxtApp()
          .$apiClient
          .organizers
          .hackathons
          .byHackathonId(hackathonId)
          .participants
          .get()
      },
    }),
  detail: (hackathonId: string, participantUserId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'participants', 'organizer', participantUserId],
      staleTime: 30_000,
      async queryFn() {
        return await useNuxtApp()
          .$apiClient
          .organizers
          .hackathons
          .byHackathonId(hackathonId)
          .participants
          .byParticipantUserId(participantUserId)
          .get()
      },
    }),
}

export function useReviewParticipantMutation(hackathonId: MaybeRefOrGetter<string>) {
  return useMutation({
    mutationFn(data: { participantUserId: string, review: HackOManiaApiEndpointsOrganizersHackathonParticipantsReviewRequest }) {
      return useNuxtApp()
        .$apiClient
        .organizers
        .hackathons
        .byHackathonId(toValue(hackathonId))
        .participants
        .byParticipantUserId(data.participantUserId)
        .review
        .post(data.review)
    },
  })
}

export interface OrganizerUserBanStateResponse {
  userId: string
  isBanned: boolean
  bannedAt?: string | null
  banReason?: string | null
}

export function useBanUserMutation() {
  return useMutation({
    async mutationFn(data: { userId: string, reason?: string | null }) {
      const config = useRuntimeConfig()
      return await $fetch<OrganizerUserBanStateResponse>(`/organizers/users/${data.userId}/ban`, {
        baseURL: config.public.api,
        method: 'POST',
        body: { reason: data.reason ?? null },
        credentials: 'include',
      })
    },
  })
}

export function useUnbanUserMutation() {
  return useMutation({
    async mutationFn(userId: string) {
      const config = useRuntimeConfig()
      return await $fetch<OrganizerUserBanStateResponse>(`/organizers/users/${userId}/ban`, {
        baseURL: config.public.api,
        method: 'DELETE',
        credentials: 'include',
      })
    },
  })
}

export function useWithdrawFromHackathon(hackathonId: MaybeRefOrGetter<string | null>) {
  return useMutation({
    async mutationFn() {
      const id = toValue(hackathonId)
      if (!id)
        throw new Error('No hackathon ID')
      return await useNuxtApp()
        .$apiClient
        .participants
        .hackathons
        .byHackathonIdOrShortCodeId(id)
        .withdraw
        .post()
    },
  })
}
