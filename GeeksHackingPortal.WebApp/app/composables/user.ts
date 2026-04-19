import type { HackOManiaApiEndpointsUsersProfileUpdateRequest } from '~/api-client/models'
import { useMutation } from '@tanstack/vue-query'

export function useUpdateUserMutation() {
  return useMutation({
    async mutationFn(data: HackOManiaApiEndpointsUsersProfileUpdateRequest) {
      return await useNuxtApp()
        .$apiClient
        .users
        .me
        .patch(data)
    },
  })
}
