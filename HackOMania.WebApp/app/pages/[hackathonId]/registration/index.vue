<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import { computed } from 'vue'

const config = useRuntimeConfig()
const route = useRoute()
const hackathonId = computed(() => route.params.hackathonId as string)

useHead({
  titleTemplate: title => (title ? `${title} - HackOMania` : 'HackOMania'),
})

// Check if user is authenticated
const { data: user, isLoading, isSuccess } = useQuery({
  ...authQueries.whoAmI,
  retry: false,
  staleTime: 0,
  gcTime: 0,
})

// Only redirect to form if query succeeded and we have user data
watchEffect(() => {
  if (isSuccess.value && user.value) {
    navigateTo(`/${hackathonId.value}/registration/form`, { replace: true })
  }
})
</script>

<template>
  <!-- Show loading while checking auth -->
  <div
    v-if="isLoading"
    class="bg-white min-h-screen font-raleway flex items-center justify-center px-4"
  >
    <p class="text-sm text-muted">
      Checking your session...
    </p>
  </div>

  <!-- Show login UI if not authenticated (error or no user after loading) -->
  <div
    v-else
    class="bg-white min-h-screen font-raleway flex items-center justify-center px-4"
  >
    <div class="w-full flex justify-center">
      <div class="flex flex-col items-center gap-3">
        <p class="font-normal text-base text-black text-center">
          Register to participate in
        </p>
        <img
          src="/logos/logo-hackomania2026-typography.svg"
          alt="HackOMania 2026"
          class="w-full max-w-xl h-auto"
        >
        <div class="flex justify-center mt-8 w-full">
          <Button
            variant="outline"
            class="w-full sm:w-64 h-14 border-2 border-black bg-transparent text-black cursor-pointer flex items-center justify-center gap-2 rounded-lg"
            @click="navigateTo(`${config.public.api}/auth/login`, { external: true })"
          >
            <UIcon
              name="i-lucide-github"
              class="w-6 h-6 sm:w-8 sm:h-8 flex-shrink-0"
            />
            <span class="text-lg sm:text-xl font-normal text-black">
              Sign up with <span class="font-bold">GitHub</span>
            </span>
          </Button>
        </div>
        <div class="mt-30">
          <a
            href="/"
            class="text-base font-normal text-black underline cursor-pointer"
          >
            Exit Registration
          </a>
        </div>
      </div>
    </div>
  </div>
</template>
