<script setup lang="ts">
import { computed } from 'vue'

useHead({
  title: 'Registration complete',
})

const route = useRoute()
const hackathonId = computed(() => (route.params.hackathonId as string | undefined) ?? null)
const dashboardPath = computed(() => (hackathonId.value ? `/dash/${hackathonId.value}` : '/dash'))
</script>

<template>
  <GPanel id="registration-complete">
    <template #header>
      <GPanelHeader title="Registration" />
    </template>

    <template #body>
      <div class="mx-auto flex w-full max-w-xl flex-col gap-5 py-4">
        <div class="flex flex-col gap-3">
          <h2 class="font-display text-[28px] leading-tight font-bold">
            You're registered
          </h2>
          <p class="text-gh-muted">
            Thanks for signing up. The organisers will review your answers and email you once they've decided. Check your spam folder if nothing arrives.
          </p>
        </div>
        <RegistrationSteps :current="2" />
        <p class="gh-small text-gh-muted">
          Your status and any review notes stay on your event page. Once you're approved you can form or join a team.
        </p>
        <div class="flex flex-col gap-3 sm:flex-row">
          <GButton
            :to="dashboardPath"
            variant="primary"
            label="Go to the event page"
          />
          <GButton
            to="/dash"
            variant="ghost"
            label="Back to Explore"
          />
        </div>
      </div>
    </template>
  </GPanel>
</template>
