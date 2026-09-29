import type { GeeksHackingPortalApiEndpointsAuthWhoAmIResponse } from '@geekshacking/portal-sdk'
import type { MaybeRefOrGetter } from 'vue'
import {
  geeksHackingPortalApiEndpointsAuthWhoAmIEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsUsersProfileUpdateEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'

/**
 * A participant's name belongs to their user profile, not to a single registration.
 *
 * Hackathon and workshop sign-up flows use this to show the profile name for confirmation.
 * Saving writes any edit back to the profile, so it applies to every activity the user has
 * joined or will join. The name is never stored as a registration answer.
 *
 * Takes the caller's `whoami` data so the caller keeps control of that query's options.
 */
export function useProfileName(user: MaybeRefOrGetter<GeeksHackingPortalApiEndpointsAuthWhoAmIResponse | undefined>) {
  const queryClient = useQueryClient()
  const updateProfileMutation = useGeeksHackingPortalApiEndpointsUsersProfileUpdateEndpoint()

  const profileName = reactive({ firstName: '', lastName: '' })

  // Prefill once so a background refetch never overwrites what the user is typing.
  let isPrefilled = false
  watch(() => toValue(user), (value) => {
    if (!value || isPrefilled)
      return

    profileName.firstName = value.firstName ?? ''
    profileName.lastName = value.lastName ?? ''
    isPrefilled = true
  }, { immediate: true })

  const isProfileNameValid = computed(() =>
    profileName.firstName.trim() !== '' && profileName.lastName.trim() !== '',
  )

  // Compared against the last save because whoami is not refetched after saving:
  // that would re-run the sign-up forms' prefill watchers mid-submit.
  let savedName: { firstName: string, lastName: string } | undefined

  async function saveProfileName() {
    const firstName = profileName.firstName.trim()
    const lastName = profileName.lastName.trim()
    const current = savedName ?? toValue(user)

    if (firstName === current?.firstName && lastName === current?.lastName)
      return

    await updateProfileMutation.mutateAsync({ body: { firstName, lastName } })
    savedName = { firstName, lastName }
    await queryClient.invalidateQueries({
      queryKey: geeksHackingPortalApiEndpointsAuthWhoAmIEndpointQueryKey(),
      refetchType: 'none',
    })
  }

  return {
    profileName,
    isProfileNameValid,
    saveProfileName,
    isSavingProfileName: updateProfileMutation.isPending,
  }
}
