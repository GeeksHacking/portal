import { queryOptions, useMutation } from '@tanstack/vue-query'
import type { HackOManiaApiEndpointsOrganizersHackathonJudgesCreateRequest, HackOManiaApiEndpointsOrganizersHackathonJudgesUpdateRequest } from '~/api-client/models'

export const judgeQueries = {
  list: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'judges'],
      async queryFn() {
        return await useNuxtApp()
          .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
          .judges.get()
      },
    }),

  detail: (hackathonId: string, judgeId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'judges', judgeId],
      async queryFn() {
        return await useNuxtApp()
          .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
          .judges.byJudgeId(judgeId)
          .get()
      },
    }),
}

export function useCreateJudgeMutation(hackathonId: string) {
  return useMutation({
    mutationFn(data: HackOManiaApiEndpointsOrganizersHackathonJudgesCreateRequest) {
      return useNuxtApp()
        .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
        .judges.post(data)
    },
  })
}

export function useUpdateJudgeMutation(hackathonId: string) {
  return useMutation({
    mutationFn({ judgeId, data }: { judgeId: string, data: HackOManiaApiEndpointsOrganizersHackathonJudgesUpdateRequest }) {
      return useNuxtApp()
        .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
        .judges.byJudgeId(judgeId)
        .patch(data)
    },
  })
}
