<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpoint,
} from '@geekshacking/portal-sdk/hooks'

const route = useRoute()
const routeHackathonId = computed(() => (route.params.hackathonId as string) ?? '')
const { data: hackathon } = useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint({ path: computed(() => ({ hackathonIdOrShortCode: routeHackathonId.value })) })
const resolvedHackathonId = computed(() => hackathon.value?.id ?? '')

// Middleware handles authentication, just check participant status
const { data: status, isLoading: statusLoading } = useGeeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpoint({ path: computed(() => ({ hackathonId: resolvedHackathonId.value })) }, { query: { enabled: computed(() => !!resolvedHackathonId.value) } })

watch(
  [() => status.value, statusLoading, hackathon],
  ([statusData, statusIsLoading, hackathonData]) => {
    if (statusIsLoading || !hackathonData)
      return
    const query = route.query
    const shortCode = hackathonData.shortCode
    if (!statusData?.isParticipant) {
      navigateTo({ path: `/${shortCode}/registration`, query }, { replace: true })
    }
    else {
      navigateTo({ path: `/${shortCode}/team`, query }, { replace: true })
    }
  },
  { immediate: true },
)
</script>

<template>
  <!-- shown while checking participant status, then redirected -->
  <GLoading
    page
    label="Checking your session…"
  />
</template>
