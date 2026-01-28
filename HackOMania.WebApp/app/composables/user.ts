import { useMutation } from '@tanstack/vue-query'
import type { HackOManiaApiEndpointsUsersProfileUpdateRequest } from '~/api-client/models'

export function useUpdateUserMutation() {
  return useMutation({
    async mutationFn(data: HackOManiaApiEndpointsUsersProfileUpdateRequest) {
      return await useNuxtApp()
        .$apiClient.users.me.patch(data)
    },
  })
}
