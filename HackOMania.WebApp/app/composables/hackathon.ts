import { queryOptions, useMutation } from '@tanstack/vue-query'

interface HackathonMutationPayload {
  name?: string
  description?: string
  venue?: string
  homepageUri?: string
  shortCode?: string
  eventStartDate?: string
  eventEndDate?: string
  submissionsStartDate?: string
  challengeSelectionEndDate?: string
  submissionsEndDate?: string
  judgingStartDate?: string
  judgingEndDate?: string
  isPublished?: boolean
  emailTemplates?: Record<string, string>
}

export const hackathonOrganizerQueries = {
  list: queryOptions({
    queryKey: ['hackathons', 'organizer'],
    async queryFn() {
      return await useNuxtApp().$apiClient.organizers.hackathons.get()
    },
  }),
  detail: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'organizer', 'detail'],
      async queryFn() {
        return await useNuxtApp().$apiClient.organizers.hackathons.byHackathonId(hackathonId).get()
      },
    }),
}

export function useCreateHackathonMutation() {
  const config = useRuntimeConfig()

  return useMutation({
    // Kiota normalizes Date values to UTC via toISOString(), which strips the
    // intended Singapore offset from DateTimeOffset fields.
    mutationFn(data: HackathonMutationPayload) {
      return $fetch('/organizers/hackathons', {
        baseURL: config.public.api,
        method: 'POST',
        credentials: 'include',
        body: data,
      })
    },
  })
}

export function useUpdateHackathonMutation() {
  const config = useRuntimeConfig()

  return useMutation({
    mutationFn({ hackathonId, data }: { hackathonId: string, data: HackathonMutationPayload }) {
      return $fetch(`/organizers/hackathons/${hackathonId}`, {
        baseURL: config.public.api,
        method: 'PATCH',
        credentials: 'include',
        body: data,
      })
    },
  })
}

export function useJoinHackathonMutation() {
  return useMutation({
    mutationFn(hackathonId: string) { return useNuxtApp().$apiClient.participants.hackathons.byHackathonIdOrShortCodeId(hackathonId).join.post() },
  })
}
