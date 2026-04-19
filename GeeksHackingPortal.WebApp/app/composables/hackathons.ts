import type { HackOManiaApiEndpointsParticipantsHackathonStatusParticipantStatus } from '~/api-client/models'
import { queryOptions, useQuery } from '@tanstack/vue-query'
import { computed } from 'vue'
import { HackOManiaApiEndpointsParticipantsHackathonStatusParticipantStatusObject } from '~/api-client/models'

export const hackathonQueries = {
  list: queryOptions({
    queryKey: ['hackathons'],
    async queryFn() {
      return await useNuxtApp().$apiClient.participants.hackathons.get()
    },
  }),
  organizerList: queryOptions({
    queryKey: ['hackathons', 'organizer'],
    async queryFn() {
      return await useNuxtApp().$apiClient.organizers.hackathons.get()
    },
  }),
  status: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'status'],
      async queryFn() {
        return await useNuxtApp()
          .$apiClient
          .participants
          .hackathons
          .byHackathonIdOrShortCodeId(hackathonId)
          .status
          .get()
      },
    }),
  detail: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'detail'],
      async queryFn() {
        return await useNuxtApp()
          .$apiClient
          .participants
          .hackathons
          .byHackathonIdOrShortCodeId(hackathonId)
          .get()
      },
    }),
}

export function formatParticipantStatus(status: HackOManiaApiEndpointsParticipantsHackathonStatusParticipantStatus | null | undefined, isParticipant?: boolean | null) {
  if (!isParticipant)
    return { label: 'Not joined', color: 'neutral' as const }
  switch (status) {
    case HackOManiaApiEndpointsParticipantsHackathonStatusParticipantStatusObject.Accepted:
      return { label: 'Accepted', color: 'success' as const }
    case HackOManiaApiEndpointsParticipantsHackathonStatusParticipantStatusObject.Rejected:
      return { label: 'Rejected', color: 'error' as const }
    default:
      return { label: 'Pending review', color: 'warning' as const }
  }
}

// for now we just take the first hackathon as current
export function useCurrentHackathonId() {
  const { data: hackathonsData } = useQuery(hackathonQueries.list)
  return computed(() => hackathonsData.value?.hackathons?.[0]?.id ?? null)
}

export function useCurrentHackathon() {
  const { data: hackathonsData } = useQuery(hackathonQueries.list)
  return computed(() => hackathonsData.value?.hackathons?.[0] ?? null)
}

// Get hackathonId from route params (could be ID or shortcode)
export function useRouteHackathonId() {
  const route = useRoute()
  return computed(() => (route.params.hackathonId as string) ?? null)
}

// Get full hackathon data based on route param
export function useRouteHackathon() {
  const hackathonId = useRouteHackathonId()
  const { data } = useQuery(
    computed(() => ({
      ...hackathonQueries.detail(hackathonId.value ?? ''),
      enabled: !!hackathonId.value,
    })),
  )
  return computed(() => data.value ?? null)
}

// Get the resolved hackathon ID (UUID) from the route param (which could be shortcode or ID)
// Use this for API calls that need the actual ID
export function useResolvedHackathonId() {
  const hackathon = useRouteHackathon()
  return computed(() => hackathon.value?.id ?? null)
}
