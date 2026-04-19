import type { MaybeRefOrGetter } from 'vue'
import type {
  HackOManiaApiEndpointsOrganizersHackathonVenueCheckInResponse,
  HackOManiaApiEndpointsOrganizersHackathonVenueCheckOutResponse,
  HackOManiaApiEndpointsOrganizersHackathonVenueHistoryHistoryItemDto,
  HackOManiaApiEndpointsOrganizersHackathonVenueHistoryResponse,
  HackOManiaApiEndpointsOrganizersHackathonVenueOverviewParticipantCheckInDto,
  HackOManiaApiEndpointsOrganizersHackathonVenueOverviewResponse,
  HackOManiaApiEndpointsOrganizersHackathonVenueOverviewVenueAuditTrailItemDto,
} from '~/api-client/models'
import { queryOptions, useMutation } from '@tanstack/vue-query'
import { toValue } from 'vue'

export type VenueCheckInResponse = HackOManiaApiEndpointsOrganizersHackathonVenueCheckInResponse
export type VenueCheckOutResponse = HackOManiaApiEndpointsOrganizersHackathonVenueCheckOutResponse
export type VenueHistoryItem = HackOManiaApiEndpointsOrganizersHackathonVenueHistoryHistoryItemDto
export type VenueHistoryResponse = HackOManiaApiEndpointsOrganizersHackathonVenueHistoryResponse
export type ParticipantCheckInDto = HackOManiaApiEndpointsOrganizersHackathonVenueOverviewParticipantCheckInDto
export type VenueAuditTrailItem = HackOManiaApiEndpointsOrganizersHackathonVenueOverviewVenueAuditTrailItemDto
export type VenueOverviewResponse = HackOManiaApiEndpointsOrganizersHackathonVenueOverviewResponse

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
    async mutationFn(participantUserId: string) {
      const response = await useNuxtApp()
        .$apiClient
        .organizers
        .hackathons
        .byHackathonId(toValue(hackathonId))
        .participants
        .byParticipantUserId(participantUserId)
        .venue
        .checkIn
        .post()

      if (!response)
        throw new Error('No check-in response returned.')

      return response
    },
  })
}

export function useCheckOutMutation(hackathonId: MaybeRefOrGetter<string>) {
  return useMutation({
    async mutationFn(participantUserId: string) {
      const response = await useNuxtApp()
        .$apiClient
        .organizers
        .hackathons
        .byHackathonId(toValue(hackathonId))
        .participants
        .byParticipantUserId(participantUserId)
        .venue
        .checkOut
        .post()

      if (!response)
        throw new Error('No check-out response returned.')

      return response
    },
  })
}

export const venueHistoryQueries = {
  participant: (hackathonId: string, participantUserId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'venue', 'history', participantUserId],
      async queryFn() {
        return await useNuxtApp()
          .$apiClient
          .organizers
          .hackathons
          .byHackathonId(hackathonId)
          .participants
          .byParticipantUserId(participantUserId)
          .venue
          .history
          .get()
      },
    }),
}
