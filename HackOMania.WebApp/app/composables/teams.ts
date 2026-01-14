import { queryOptions, useMutation, useQueryClient } from '@tanstack/vue-query'
import type { ComputedRef, Ref } from 'vue'

type MaybeRef<T> = Ref<T> | ComputedRef<T>

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

export function useCreateTeam(hackathonId: MaybeRef<string | null>) {
  const queryClient = useQueryClient()

  return useMutation({
    async mutationFn(data: { name: string, description?: string }) {
      if (!hackathonId.value) throw new Error('No hackathon ID')
      return await useNuxtApp()
        .$apiClient.participants.hackathons.byHackathonIdOrShortCodeId(hackathonId.value)
        .teams.post({ name: data.name, description: data.description ?? null })
    },
    onSuccess() {
      if (hackathonId.value) {
        queryClient.invalidateQueries({ queryKey: ['hackathons', hackathonId.value, 'teams', 'me'] })
      }
    },
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
    onSuccess() {
      if (hackathonId.value) {
        queryClient.invalidateQueries({ queryKey: ['hackathons', hackathonId.value, 'teams', 'me'] })
      }
    },
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
    onSuccess() {
      if (hackathonId.value) {
        queryClient.invalidateQueries({ queryKey: ['hackathons', hackathonId.value, 'teams', 'me'] })
      }
    },
  })
}

export function useJoinTeamByCode() {
  const queryClient = useQueryClient()

  return useMutation({
    async mutationFn(joinCode: string) {
      return await useNuxtApp()
        .$apiClient.participants.teams.join.post({ joinCode })
    },
    onSuccess(data) {
      if (data?.hackathonId) {
        queryClient.invalidateQueries({ queryKey: ['hackathons', data.hackathonId, 'teams', 'me'] })
      }
    },
  })
}
