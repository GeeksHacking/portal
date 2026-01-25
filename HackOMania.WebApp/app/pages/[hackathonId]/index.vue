<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'

const route = useRoute()
const hackathonId = route.params.hackathonId as string

// Middleware handles authentication, just check participant status
const { data: status, isLoading: statusLoading } = useQuery(hackathonQueries.status(hackathonId))

watch(
  [() => status.value, statusLoading],
  ([statusData, statusIsLoading]) => {
    if (statusIsLoading) return
    const query = route.query
    if (!statusData?.isParticipant) {
      navigateTo({ path: `/${hackathonId}/registration`, query }, { replace: true })
    } else {
      navigateTo({ path: `/${hackathonId}/team`, query }, { replace: true })
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
