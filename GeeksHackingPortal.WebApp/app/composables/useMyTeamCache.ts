import type { MaybeRefOrGetter } from 'vue'
import {
  geeksHackingPortalApiEndpointsParticipantsHackathonChallengesListEndpointQueryKey,
  geeksHackingPortalApiEndpointsParticipantsHackathonTeamsGetMineEndpointQueryKey,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'

/**
 * Keeps the event pages in step after a team change. The SDK's mutations don't touch the cache,
 * so without this the page shows the old team until the next refetch.
 *
 * - `refresh()`: the team still exists (created, joined, edited, member removed, challenge picked).
 * - `reset()`: the user left, so "my team" must be dropped, not kept from the last fetch.
 * Both also refresh the challenge list, whose team counts change with these actions.
 */
export function useMyTeamCache(hackathonId: MaybeRefOrGetter<string | null | undefined>) {
  const queryClient = useQueryClient()
  const keys = () => {
    const id = toValue(hackathonId) ?? ''
    return {
      team: geeksHackingPortalApiEndpointsParticipantsHackathonTeamsGetMineEndpointQueryKey({ path: { hackathonId: id } }),
      challenges: geeksHackingPortalApiEndpointsParticipantsHackathonChallengesListEndpointQueryKey({ path: { hackathonId: id } }),
    }
  }

  async function refresh() {
    const { team, challenges } = keys()
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: team }),
      queryClient.invalidateQueries({ queryKey: challenges }),
    ])
  }

  async function reset() {
    const { team, challenges } = keys()
    await Promise.all([
      queryClient.resetQueries({ queryKey: team }),
      queryClient.invalidateQueries({ queryKey: challenges }),
    ])
  }

  return { refresh, reset }
}
