import { queryOptions, useMutation } from '@tanstack/vue-query'
import type { HackOManiaApiEndpointsOrganizersHackathonOrganizersAddRequest } from '~/api-client/models'

export const organizerQueries = {
  list: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'organizers'],
      async queryFn() {
        return await useNuxtApp()
          .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
          .organizers.get()
      },
    }),
}

export function useAddOrganizerMutation(hackathonId: string) {
  return useMutation({
    mutationFn(data: HackOManiaApiEndpointsOrganizersHackathonOrganizersAddRequest) {
      return useNuxtApp()
        .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
        .organizers.post(data)
    },
  })
}
