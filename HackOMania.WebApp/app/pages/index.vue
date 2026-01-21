<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'

const config = useRuntimeConfig()

useHead({
  titleTemplate: title => (title ? `${title} - HackOMania` : 'HackOMania'),
})

const hasNavigated = ref(false)

const { isLoading } = useQuery({
  ...authQueries.whoAmI,
  retry: false,
  onSuccess: async () => {
    if (!hasNavigated.value) {
      hasNavigated.value = true
      await navigateTo('/dash')
    }
  },
})
</script>

<template>
  <div class="min-h-screen flex items-center justify-center">
    <p class="text-sm text-muted">
      {{ isLoading ? 'Checking your session…' : 'Redirecting…' }}
    </p>
  </div>
</template>
