import { queryOptions, useMutation } from '@tanstack/vue-query'
import type { HackOManiaApiEndpointsOrganizersHackathonCreateRequest } from '~/api-client/models'

export const hackathonQueries = {
  list: queryOptions({
    queryKey: ['hackathons', 'organizer'],
    async queryFn() {
      return await useNuxtApp().$apiClient.organizers.hackathons.get()
    },
  }),
}

export function useCreateHackathonMutation() {
  return useMutation({
    mutationFn(e: HackOManiaApiEndpointsOrganizersHackathonCreateRequest) { return useNuxtApp().$apiClient.organizers.hackathons.post(e) },
  })
}

export function useJoinHackathonMutation() {
  return useMutation({
    mutationFn(hackathonId: string) { return useNuxtApp().$apiClient.participants.hackathons.byHackathonIdOrShortCodeId(hackathonId).join.post() },
  })
}
