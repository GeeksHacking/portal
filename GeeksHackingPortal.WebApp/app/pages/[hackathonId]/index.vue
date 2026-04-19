<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'

const route = useRoute()
const hackathon = useRouteHackathon()
const resolvedHackathonId = useResolvedHackathonId()

// Middleware handles authentication, just check participant status
const { data: status, isLoading: statusLoading } = useQuery(
  computed(() => ({
    ...hackathonQueries.status(resolvedHackathonId.value ?? ''),
    enabled: !!resolvedHackathonId.value,
  })),
)

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
  <!-- Loading state while checking participant status -->
  <div class="min-h-screen flex flex-col items-center justify-center gap-4">
    <p class="text-sm font-medium text-gray-600 animate-pulse">
      Authenticating...
    </p>
    <UIcon name="i-lucide-loader-circle" class="w-8 h-8 animate-spin text-primary" />
  </div>
</template>
