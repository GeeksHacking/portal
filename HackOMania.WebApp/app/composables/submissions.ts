import { queryOptions } from '@tanstack/vue-query'

export const submissionOrganizerQueries = {
  list: (hackathonId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'submissions', 'organizer'],
      async queryFn() {
        return await useNuxtApp()
          .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
          .submissions.get()
      },
    }),

  detail: (hackathonId: string, submissionId: string) =>
    queryOptions({
      queryKey: ['hackathons', hackathonId, 'submissions', submissionId, 'organizer'],
      async queryFn() {
        return await useNuxtApp()
          .$apiClient.organizers.hackathons.byHackathonId(hackathonId)
          .submissions.bySubmissionId(submissionId)
          .get()
      },
    }),
}
