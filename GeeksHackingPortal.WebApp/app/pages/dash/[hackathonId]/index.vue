<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonOrganizersListEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { computed, watch } from 'vue'

const route = useRoute()
const hackathonIdOrShortCode = computed(() => (route.params.hackathonId as string | undefined) ?? null)

const { data: hackathon } = useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint({ path: computed(() => ({ hackathonIdOrShortCode: hackathonIdOrShortCode.value ?? '' })) }, { query: { enabled: computed(() => !!hackathonIdOrShortCode.value) } })

const resolvedHackathonId = computed(() => hackathon.value?.id ?? null)
const { data: user, isLoading: isLoadingUser } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const { data: organizersData, isLoading: isLoadingOrganizers } = useGeeksHackingPortalApiEndpointsOrganizersHackathonOrganizersListEndpoint({ path: computed(() => ({ hackathonId: resolvedHackathonId.value ?? '' })) }, { query: { enabled: computed(() => !!resolvedHackathonId.value) } })

const isOrganizer = computed(() => {
  if (!user.value?.id)
    return false
  if (user.value.isRoot)
    return true
  return organizersData.value?.organizers?.some(org => org.userId === user.value?.id) ?? false
})

watch([isOrganizer, isLoadingUser, isLoadingOrganizers], ([organizer, loadingUser, loadingOrganizers]) => {
  if (loadingUser || loadingOrganizers || !hackathonIdOrShortCode.value)
    return

  if (organizer) {
    navigateTo(`/dash/${hackathonIdOrShortCode.value}/checkin`)
    return
  }

  navigateTo(`/dash/${hackathonIdOrShortCode.value}/participant`)
}, { immediate: true })
</script>

<template>
  <div class="p-4 text-sm text-(--ui-text-muted)">
    Redirecting...
  </div>
</template>
