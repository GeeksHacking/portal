<script setup lang="ts">
useHead({
  titleTemplate: title => (title ? `${title} - HackOMania` : 'HackOMania'),
})
</script>

<template>
  <div class="bg-white min-h-screen font-raleway flex items-center justify-center px-4">
    <div class="w-full flex justify-center">
      <div class="flex flex-col items-center gap-[24px] max-w-[600px]">
        <img
          src="/logos/logo-hackomania2026-typography.svg"
          alt="HackOMania 2026"
          class="w-full max-w-[537px] h-auto"
        >

        <div class="flex flex-col items-center gap-4 mt-8">
          <h1 class="font-raleway text-2xl md:text-3xl font-semibold text-black text-center">
            Registration Complete!
          </h1>

          <div class="font-raleway text-[16px] md:text-[18px] font-normal text-black text-center max-w-[500px] space-y-4">
            <p>Thank you for registering for HackOMania 2026!</p>

            <p>Our team will review your details and notify you once your registration is verified.</p>

            <p>You will be able to form or join a team after verification.</p>

            <p>Please check your spam folder for the event registration email.</p>
          </div>
        </div>

        <div class="flex flex-col gap-3 mt-8 w-full max-w-[400px]">
          <NuxtLink
            to="/dash"
            class="w-full h-[56px] bg-black text-white rounded-lg flex items-center justify-center font-raleway text-[18px] font-normal hover:bg-gray-800 transition-colors"
          >
            Go to Dashboard
          </NuxtLink>

          <NuxtLink
            to="/"
            class="w-full h-[56px] border border-black bg-transparent text-black rounded-lg flex items-center justify-center font-raleway text-[18px] font-normal hover:bg-gray-50 transition-colors"
          >
            Back to Home
          </NuxtLink>
        </div>
      </div>
    </div>
  </div>
</template>
