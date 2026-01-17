import { queryOptions, useQuery } from '@tanstack/vue-query'
import { computed } from 'vue'

export const hackathonQueries = {
  list: queryOptions({
    queryKey: ['hackathons'],
    async queryFn() {
      return await useNuxtApp().$apiClient.participants.hackathons.get()
    },
  }),
  status: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'status'],
      async queryFn() {
        return await useNuxtApp()
          .$apiClient.participants.hackathons.byHackathonIdOrShortCodeId(hackathonId)
          .status.get()
      },
    }),
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
