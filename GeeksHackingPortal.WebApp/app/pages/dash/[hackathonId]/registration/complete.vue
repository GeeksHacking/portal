<script setup lang="ts">
import { computed } from 'vue'

useHead({
  titleTemplate: title => (title ? `${title} - GeeksHacking Portal` : 'GeeksHacking Portal'),
})

const route = useRoute()
const hackathonId = computed(() => (route.params.hackathonId as string | undefined) ?? null)
const dashboardPath = computed(() => (hackathonId.value ? `/dash/${hackathonId.value}` : '/dash'))
</script>

<template>
  <UDashboardPanel id="registration-complete">
    <template #body>
      <div class="min-h-screen bg-(--ui-bg) font-raleway flex items-center justify-center px-4 text-(--ui-text)">
        <div class="w-full flex justify-center">
          <div class="flex flex-col items-center gap-6 max-w-2xl">
            <img
              src="/logos/geekshacking.svg"
              alt="GeeksHacking Portal"
              class="w-full max-w-xl h-auto"
            >

            <div class="flex flex-col items-center gap-4 mt-8">
              <h1 class="font-raleway text-2xl md:text-3xl font-semibold text-(--ui-text-highlighted) text-center">
                Registration Complete!
              </h1>

              <div class="font-raleway text-base md:text-lg font-normal text-(--ui-text) text-center max-w-lg space-y-4">
                <p>Thank you for registering through GeeksHacking Portal.</p>

                <p>Our team will review your details and notify you once your registration is verified.</p>

                <p>You will be able to form or join a team after verification.</p>

                <p>You can check your application status and any review notes anytime from your dashboard.</p>

                <p>Please check your spam folder for the event registration email.</p>
              </div>
            </div>

            <div class="flex flex-col gap-3 mt-8 w-full max-w-sm">
              <UButton
                :to="dashboardPath"
                size="xl"
                color="neutral"
                class="w-full justify-center font-raleway"
              >
                Go to Dashboard
              </UButton>

              <UButton
                to="/"
                size="xl"
                color="neutral"
                variant="outline"
                class="w-full justify-center font-raleway"
              >
                Back to Home
              </UButton>
            </div>
          </div>
        </div>
      </div>
    </template>
  </UDashboardPanel>
</template>
