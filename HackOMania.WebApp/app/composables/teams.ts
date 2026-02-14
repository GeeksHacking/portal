import type { QueryClient } from '@tanstack/vue-query'
import { queryOptions, useMutation, useQueryClient } from '@tanstack/vue-query'
import type { ComputedRef, Ref } from 'vue'

type MaybeRef<T> = Ref<T> | ComputedRef<T>

function invalidateTeamQueries(queryClient: QueryClient, hackathonId: string | null) {
  if (hackathonId) {
    queryClient.invalidateQueries({ queryKey: ['hackathons', hackathonId, 'teams', 'me'] })
  }
}

export const teamQueries = {
  me: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'teams', 'me'],
      retry: false,
      async queryFn() {
        try {
          return await useNuxtApp()
            .$apiClient.participants.hackathons.byHackathonIdOrShortCodeId(hackathonId)
            .teams.me.get()
        }
        catch {
          // Return null if user has no team (404)
          return null
        }
      },
    }),
}

export const teamOrganizerQueries = {
  list: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'teams', 'organizer'],
      async queryFn() {
        return await useNuxtApp()
          .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
          .teams.get()
      },
    }),
}

export function useCreateTeam(hackathonId: MaybeRef<string | null>) {
  const queryClient = useQueryClient()

  return useMutation({
    async mutationFn(data: { name: string, description?: string }) {
      if (!hackathonId.value) throw new Error('No hackathon ID')
      return await useNuxtApp()
        .$apiClient.participants.hackathons.byHackathonIdOrShortCodeId(hackathonId.value)
        .teams.post({ name: data.name, description: data.description ?? null })
    },
    onSuccess: () => invalidateTeamQueries(queryClient, hackathonId.value),
  })
}

export function useUpdateTeam(hackathonId: MaybeRef<string | null>, teamId: MaybeRef<string | null>) {
  const queryClient = useQueryClient()

  return useMutation({
    async mutationFn(data: { name?: string, description?: string }) {
      if (!hackathonId.value || !teamId.value) throw new Error('Missing hackathon or team ID')
      return await useNuxtApp()
        .$apiClient.participants.hackathons.byHackathonIdOrShortCodeId(hackathonId.value)
        .teams.byTeamId(teamId.value)
        .patch({ name: data.name ?? null, description: data.description ?? null })
    },
    onSuccess: () => invalidateTeamQueries(queryClient, hackathonId.value),
  })
}

export function useLeaveTeam(hackathonId: MaybeRef<string | null>) {
  const queryClient = useQueryClient()

  return useMutation({
    async mutationFn() {
      if (!hackathonId.value) throw new Error('No hackathon ID')
      return await useNuxtApp()
        .$apiClient.participants.hackathons.byHackathonIdOrShortCodeId(hackathonId.value)
        .teams.leave.post()
    },
    onSuccess: () => invalidateTeamQueries(queryClient, hackathonId.value),
  })
}

export function useJoinTeamByCode() {
  const queryClient = useQueryClient()

  return useMutation({
    async mutationFn(joinCode: string) {
      return await useNuxtApp()
        .$apiClient.participants.teams.join.post({ joinCode })
    },
    onSuccess: data => invalidateTeamQueries(queryClient, data?.hackathonId ?? null),
  })
}

export function useSelectChallenge(
  hackathonId: MaybeRef<string | null>,
  teamId: MaybeRef<string | null>
) {
  const queryClient = useQueryClient()

  return useMutation({
    async mutationFn(challengeId: string) {
      if (!toValue(hackathonId) || !toValue(teamId)) throw new Error('Missing hackathon or team ID')
      return await useNuxtApp()
        .$apiClient.participants.hackathons.byHackathonIdOrShortCodeId(toValue(hackathonId)!)
        .teams.byTeamId(toValue(teamId)!)
        .challenge.put({ challengeId })
    },
    onSuccess: () => invalidateTeamQueries(queryClient, toValue(hackathonId)),
  })
}

export function useRemoveTeamMember(
  hackathonId: MaybeRef<string | null>,
  teamId: MaybeRef<string | null>
) {
  const queryClient = useQueryClient()

  return useMutation({
    async mutationFn(userId: string) {
      if (!toValue(hackathonId) || !toValue(teamId)) throw new Error('Missing hackathon or team ID')
      return await useNuxtApp()
        .$apiClient.participants.hackathons.byHackathonIdOrShortCodeId(toValue(hackathonId)!)
        .teams.byTeamId(toValue(teamId)!)
        .members.byUserId(userId)
        .delete()
    },
    onSuccess: () => invalidateTeamQueries(queryClient, toValue(hackathonId)),
  })
}
