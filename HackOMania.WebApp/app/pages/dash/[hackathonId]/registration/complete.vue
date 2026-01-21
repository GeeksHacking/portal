<script setup lang="ts">
import { computed } from 'vue'

useHead({
  titleTemplate: title => (title ? `${title} - HackOMania` : 'HackOMania'),
})

const route = useRoute()
const hackathonId = computed(() => (route.params.hackathonId as string | undefined) ?? null)
const dashboardPath = computed(() => (hackathonId.value ? `/dash/${hackathonId.value}` : '/dash'))
</script>

<template>
  <div class="bg-white min-h-screen font-raleway flex items-center justify-center px-4">
    <div class="w-full flex justify-center">
      <div class="flex flex-col items-center gap-6 max-w-2xl">
        <img
          src="/logos/logo-hackomania2026-typography.svg"
          alt="HackOMania 2026"
          class="w-full max-w-xl h-auto"
        >

        <div class="flex flex-col items-center gap-4 mt-8">
          <h1 class="font-raleway text-2xl md:text-3xl font-semibold text-black text-center">
            Registration Complete!
          </h1>

          <div class="font-raleway text-base md:text-lg font-normal text-black text-center max-w-lg space-y-4">
            <p>Thank you for registering for HackOMania 2026!</p>

            <p>Our team will review your details and notify you once your registration is verified.</p>

            <p>You will be able to form or join a team after verification.</p>

            <p>You can check your application status and any review notes anytime from your dashboard.</p>

            <p>Please check your spam folder for the event registration email.</p>
          </div>
        </div>

        <div class="flex flex-col gap-3 mt-8 w-full max-w-sm">
          <NuxtLink
            :to="dashboardPath"
            class="w-full h-14 bg-black text-white rounded-lg flex items-center justify-center font-raleway text-lg font-normal hover:bg-gray-800 transition-colors"
          >
            Go to Dashboard
          </NuxtLink>

          <NuxtLink
            to="/"
            class="w-full h-14 border border-black bg-transparent text-black rounded-lg flex items-center justify-center font-raleway text-lg font-normal hover:bg-gray-50 transition-colors"
          >
            Back to Home
          </NuxtLink>
        </div>
      </div>
    </div>
  </div>
</template>
