import {
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { githubLoginUrl } from '~/utils/login-url'

/**
 * The participant event pages (/[shortCode]/team, challenges, submission, schedule) are only for
 * accepted participants. Signed out → GitHub login; not registered → registration;
 * registered but not accepted yet → registration status. Returns the event so the page can
 * show its name. `ready` turns true once the visitor is confirmed as an accepted participant.
 */
export function useEventParticipantGuard() {
  const route = useRoute()
  const config = useRuntimeConfig()
  const routeHackathonId = computed(() => (route.params.hackathonId as string) ?? '')

  const { data: hackathon } = useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint({ path: computed(() => ({ hackathonIdOrShortCode: routeHackathonId.value })) })
  const resolvedHackathonId = computed(() => hackathon.value?.id ?? '')

  const { data: user, isLoading: authLoading } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
  const { data: status, isLoading: statusLoading } = useGeeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpoint({ path: computed(() => ({ hackathonId: resolvedHackathonId.value })) }, { query: { enabled: computed(() => !!resolvedHackathonId.value) } })

  const ready = computed(() => !!user.value && status.value?.isParticipant === true && status.value.status === 'Accepted')

  watch(
    [() => user.value, authLoading, () => status.value, statusLoading, hackathon],
    ([userData, authIsLoading, statusData, statusIsLoading, hackathonData]) => {
      if (authIsLoading)
        return
      if (!userData) {
        navigateTo(githubLoginUrl(config.public.api, route.fullPath), { external: true })
        return
      }
      if (statusIsLoading || !hackathonData)
        return
      if (!statusData?.isParticipant) {
        navigateTo({ path: `/${hackathonData.shortCode}/registration`, query: route.query }, { replace: true })
      }
      else if (statusData.status !== 'Accepted') {
        navigateTo({ path: `/${hackathonData.shortCode}/registration/status`, query: route.query }, { replace: true })
      }
    },
    { immediate: true },
  )

  return { hackathon, ready }
}
