import type { QueryClient } from '@tanstack/vue-query'
import type { ComputedRef, Ref } from 'vue'
import type { HackOManiaApiEndpointsParticipantsHackathonSubmissionsCreateRequest } from '~/api-client/models'
import { queryOptions, useMutation, useQueryClient } from '@tanstack/vue-query'

type MaybeRef<T> = Ref<T> | ComputedRef<T>

function invalidateSubmissionQueries(queryClient: QueryClient, hackathonId: string | null, teamId: string | null) {
  if (hackathonId && teamId) {
    queryClient.invalidateQueries({ queryKey: ['hackathons', hackathonId, 'teams', teamId, 'submissions'] })
  }
}

export const submissionQueries = {
  listForTeam: (hackathonId: string, teamId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'teams', teamId, 'submissions'],
      async queryFn() {
        return await useNuxtApp()
          .$apiClient
          .participants
          .hackathons
          .byHackathonIdOrShortCodeId(hackathonId)
          .teams
          .byTeamId(teamId)
          .submissions
          .get()
      },
    }),
}

export function useCreateSubmission(
  hackathonId: MaybeRef<string | null>,
  teamId: MaybeRef<string | null>,
) {
  const queryClient = useQueryClient()

  return useMutation({
    async mutationFn(data: HackOManiaApiEndpointsParticipantsHackathonSubmissionsCreateRequest) {
      if (!toValue(hackathonId) || !toValue(teamId))
        throw new Error('Missing hackathon or team ID')
      return await useNuxtApp()
        .$apiClient
        .participants
        .hackathons
        .byHackathonIdOrShortCodeId(toValue(hackathonId)!)
        .teams
        .byTeamId(toValue(teamId)!)
        .submissions
        .post(data)
    },
    onSuccess: () => invalidateSubmissionQueries(queryClient, toValue(hackathonId), toValue(teamId)),
  })
}

export const submissionOrganizerQueries = {
  list: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'submissions', 'organizer'],
      async queryFn() {
        return await useNuxtApp()
          .$apiClient
          .organizers
          .hackathons
          .byHackathonId(hackathonId)
          .submissions
          .get()
      },
    }),

  detail: (hackathonId: string, submissionId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'submissions', submissionId, 'organizer'],
      async queryFn() {
        return await useNuxtApp()
          .$apiClient
          .organizers
          .hackathons
          .byHackathonId(hackathonId)
          .submissions
          .bySubmissionId(submissionId)
          .get()
      },
    }),
}
