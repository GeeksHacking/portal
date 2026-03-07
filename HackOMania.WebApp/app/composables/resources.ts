import type { MaybeRefOrGetter } from 'vue'
import { queryOptions, useMutation } from '@tanstack/vue-query'
import { toValue } from 'vue'

export interface OrganizerResourceItem {
  id: string
  name: string
  description?: string | null
  redemptionStmt?: string | null
  isPublished: boolean
}

export interface OrganizerResourcesListResponse {
  resources?: OrganizerResourceItem[] | null
}

export interface OrganizerResourceOverviewParticipant {
  participantId: string
  userId: string
  userName: string
  hasRedeemed: boolean
  redemptionCount: number
  lastRedeemedAt?: Date | string | null
}

export interface OrganizerResourceAuditTrailItem {
  redemptionId: string
  participantId: string
  userId: string
  userName: string
  timestamp: Date | string
}

export interface OrganizerResourceOverviewResponse {
  resourceId: string
  resourceName: string
  isPublished: boolean
  totalRedemptions: number
  uniqueRedeemers: number
  participants: OrganizerResourceOverviewParticipant[]
  auditTrail: OrganizerResourceAuditTrailItem[]
}

export interface OrganizerResourceHistoryItem {
  redemptionId: string
  createdAt: Date | string
}

export interface OrganizerResourceHistoryResponse {
  participantId: string
  userId: string
  userName: string
  resourceId: string
  resourceName: string
  resourceIsPublished: boolean
  hasRedeemed: boolean
  redemptionCount: number
  history: OrganizerResourceHistoryItem[]
}

export interface OrganizerResourceStatisticsSummaryItem {
  resourceId: string
  resourceName: string
  isPublished: boolean
  totalRedemptions: number
  uniqueRedeemers: number
  lastRedeemedAt?: Date | string | null
}

export interface OrganizerResourceStatisticsParticipantEvent {
  redemptionId: string
  resourceId: string
  resourceName: string
  timestamp: Date | string
}

export interface OrganizerResourceStatisticsParticipantItem {
  participantId: string
  userId: string
  userName: string
  redemptionCount: number
  distinctResourcesRedeemed: number
  firstRedeemedAt?: Date | string | null
  lastRedeemedAt?: Date | string | null
  redemptions: OrganizerResourceStatisticsParticipantEvent[]
}

export interface OrganizerResourceStatisticsTeamItem {
  teamId?: string | null
  teamName: string
  memberCount: number
  redeemerCount: number
  totalRedemptions: number
  distinctResourcesRedeemed: number
  lastRedeemedAt?: Date | string | null
  participants: OrganizerResourceStatisticsParticipantItem[]
}

export interface OrganizerResourceStatisticsRecentActivityItem {
  redemptionId: string
  resourceId: string
  resourceName: string
  participantId: string
  userId: string
  userName: string
  teamId?: string | null
  teamName: string
  timestamp: Date | string
}

export interface OrganizerResourceStatisticsResponse {
  resourceId?: string | null
  resourceName?: string | null
  resourceCount: number
  resourcesWithRedemptions: number
  resourcesWithoutRedemptions: number
  totalParticipants: number
  participantsWithRedemptions: number
  participantsWithoutRedemptions: number
  teamsWithRedemptions: number
  redeemersWithoutTeam: number
  totalRedemptions: number
  averageRedemptionsPerRedeemer: number
  firstRedeemedAt?: Date | string | null
  lastRedeemedAt?: Date | string | null
  resourceSummaries: OrganizerResourceStatisticsSummaryItem[]
  teamBreakdown: OrganizerResourceStatisticsTeamItem[]
  recentActivity: OrganizerResourceStatisticsRecentActivityItem[]
}

export interface ResourceRedeemResponse {
  redemptionId: string
  resourceId: string
  hackathonId: string
  createdAt: Date | string
}

export const resourceOrganizerQueries = {
  list: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'resources', 'organizer'],
      staleTime: 30_000,
      async queryFn() {
        const config = useRuntimeConfig()
        return await $fetch<OrganizerResourcesListResponse>(`/organizers/hackathons/${hackathonId}/resources`, {
          baseURL: config.public.api,
          credentials: 'include',
        })
      },
    }),
  overview: (hackathonId: string, resourceId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'resources', 'organizer', resourceId, 'overview'],
      refetchInterval: 15_000,
      async queryFn() {
        const config = useRuntimeConfig()
        return await $fetch<OrganizerResourceOverviewResponse>(`/organizers/hackathons/${hackathonId}/resources/${resourceId}/overview`, {
          baseURL: config.public.api,
          credentials: 'include',
        })
      },
    }),
  participantHistory: (hackathonId: string, participantUserId: string, resourceId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'resources', 'organizer', resourceId, 'history', participantUserId],
      async queryFn() {
        const config = useRuntimeConfig()
        return await $fetch<OrganizerResourceHistoryResponse>(`/organizers/hackathons/${hackathonId}/participants/${participantUserId}/resources/${resourceId}/history`, {
          baseURL: config.public.api,
          credentials: 'include',
        })
      },
    }),
  statistics: (hackathonId: string, resourceId?: string | null) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'resources', 'organizer', resourceId ?? 'all', 'statistics'],
      refetchInterval: 15_000,
      async queryFn() {
        const config = useRuntimeConfig()
        return await $fetch<OrganizerResourceStatisticsResponse>(`/organizers/hackathons/${hackathonId}/resources/statistics`, {
          baseURL: config.public.api,
          credentials: 'include',
          query: resourceId ? { resourceId } : undefined,
        })
      },
    }),
}

export function useRedeemResourceMutation(
  hackathonId: MaybeRefOrGetter<string>,
  resourceId: MaybeRefOrGetter<string>,
) {
  return useMutation({
    async mutationFn(participantUserId: string) {
      const config = useRuntimeConfig()
      return await $fetch<ResourceRedeemResponse>(
        `/organizers/hackathons/${toValue(hackathonId)}/participants/${participantUserId}/resources/${toValue(resourceId)}/redemptions`,
        {
          baseURL: config.public.api,
          method: 'POST',
          credentials: 'include',
        },
      )
    },
  })
}
