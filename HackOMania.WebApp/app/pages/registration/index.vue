<script setup>
const config = useRuntimeConfig()

useHead({
  titleTemplate: title => (title ? `${title} - HackOMania` : 'HackOMania'),
})
</script>

<template>
  <div class="bg-white min-h-screen font-raleway flex items-center justify-center px-4">
    <div class="w-full flex justify-center">
      <div class="flex flex-col items-center gap-[12px]">
        <p class="font-normal text-[16px] text-black text-center">
          Register to participate in
        </p>
        <img
          src="/logos/logo-hackomania2026-typography.svg"
          alt="HackOMania 2026"
          class="w-full max-w-[537px] h-auto"
        >
        <div class="flex justify-center mt-[32px] w-full">
          <Button
            variant="outline"
            class="w-full sm:w-[254px] h-[56px] border-2 border-black bg-transparent text-black cursor-pointer flex items-center justify-center gap-2 rounded-lg"
            @click="navigateTo(`${config.public.api}/auth/login`, { external: true })"
          >
            <img
              src="/logos/github.svg"
              alt="GitHub"
              class="w-[24px] h-[24px] sm:w-[32px] sm:h-[32px] flex-shrink-0"
            >
            <span class="text-[18px] sm:text-[20px] font-normal text-black">
              Sign up with <span class="font-bold">GitHub</span>
            </span>
          </Button>
        </div>
        <div class="mt-[120px]">
          <a
            href="/"
            class="text-[16px] font-normal text-black underline cursor-pointer"
          >
            Exit Registration
          </a>
        </div>
      </div>
    </div>
  </div>
</template>
