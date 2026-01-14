import { queryOptions } from '@tanstack/vue-query'
import type { HackOManiaApiEndpointsOrganizersHackathonCreateRequest } from '~/api-client/models'

export const hackathonQueries = {
  list: queryOptions({
    queryKey: ['hackathons', 'organizer'],
    async queryFn() {
      return await useNuxtApp().$apiClient.organizers.hackathons.get()
    },
  }),
}

export const hackathonCreate = {
  async create(data: HackOManiaApiEndpointsOrganizersHackathonCreateRequest) {
    return await useNuxtApp().$apiClient.organizers.hackathons.post(data)
  },
}

export const hackathonActions = {
  async join(hackathonId: string) {
    return await useNuxtApp().$apiClient.participants.hackathons.byHackathonIdOrShortCodeId(hackathonId).join.post()
  },
}
