import type { HackOManiaApiEndpointsOrganizersHackathonCreateRequest, HackOManiaApiEndpointsOrganizersHackathonUpdateRequest } from '~/api-client/models'
import { queryOptions, useMutation } from '@tanstack/vue-query'

export const hackathonOrganizerQueries = {
  list: queryOptions({
    queryKey: ['hackathons', 'organizer'],
    async queryFn() {
      return await useNuxtApp().$apiClient.organizers.hackathons.get()
    },
  }),
  detail: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'organizer', 'detail'],
      async queryFn() {
        return await useNuxtApp().$apiClient.organizers.hackathons.byHackathonId(hackathonId).get()
      },
    }),
}

export function useCreateHackathonMutation() {
  return useMutation({
    mutationFn(data: HackOManiaApiEndpointsOrganizersHackathonCreateRequest) {
      return useNuxtApp().$apiClient.organizers.hackathons.post(data)
    },
  })
}

export function useUpdateHackathonMutation() {
  return useMutation({
    mutationFn({ hackathonId, data }: { hackathonId: string, data: HackOManiaApiEndpointsOrganizersHackathonUpdateRequest }) {
      return useNuxtApp().$apiClient.organizers.hackathons.byHackathonId(hackathonId).patch(data)
    },
  })
}

export function useJoinHackathonMutation() {
  return useMutation({
    mutationFn(hackathonId: string) { return useNuxtApp().$apiClient.participants.hackathons.byHackathonIdOrShortCodeId(hackathonId).join.post() },
  })
}
