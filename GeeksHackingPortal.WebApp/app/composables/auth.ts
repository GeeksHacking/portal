import { queryOptions } from '@tanstack/vue-query'

export const authQueries = {
  whoAmI: queryOptions({
    queryKey: ['whoami'],
    async queryFn() {
      return await useNuxtApp().$apiClient.auth.whoami.get()
    },
    retry: false,
  }),
}
