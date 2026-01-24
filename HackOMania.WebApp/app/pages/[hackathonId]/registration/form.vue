<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import { computed, ref } from 'vue'

const route = useRoute()
const config = useRuntimeConfig()
const hackathonId = computed(() => (route.params.hackathonId as string | undefined) ?? null)

// Track if we should show the form
const showForm = ref(false)

// Check if user is authenticated
const { data: user, isLoading, isError } = useQuery({
  ...authQueries.whoAmI,
  retry: false,
  staleTime: 0,
  gcTime: 0,
})

// Handle auth state changes
watchEffect(() => {
  if (!isLoading.value) {
    if (user.value && !isError.value) {
      showForm.value = true
    }
    else if (hackathonId.value) {
      navigateTo(`${config.public.api}/auth/login?redirect_uri=${encodeURIComponent(route.fullPath)}`, { external: true })
    }
  }
})
</script>

<template>
  <!-- Show loading while checking auth -->
  <div
    v-if="isLoading || !showForm"
    class="bg-white min-h-screen font-raleway flex items-center justify-center px-4"
  >
    <p class="text-sm text-muted">
      Checking your session...
    </p>
  </div>

  <!-- Show form if authenticated -->
  <RegistrationFormPage
    v-else
    :hackathon-id="hackathonId"
  />
</template>
