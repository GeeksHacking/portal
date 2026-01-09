<script setup lang="ts">
import { Button } from '~/components/ui/button'

const config = useRuntimeConfig()

useHead({
  titleTemplate: title => (title ? `${title} - HackOMania` : 'HackOMania'),
})
</script>

<template>
  <div class="bg-white min-h-screen font-raleway">
    <div class="absolute top-[399px] left-[452px] right-[451px] bottom-[335px]">
      <div class="flex flex-col items-center gap-[12px]">
        <p class="font-normal text-[16px] text-black">
          Register to participate in
        </p>
        <img
          src="/logos/logo-hackomania2026-typography.svg"
          alt="HackOMania 2026"
          class="w-[537px] h-[32px]"
        >
        <div class="flex gap-[12px] mt-[32px]">
          <Button
            variant="outline"
            class="w-[254px] h-[56px] border-black bg-transparent text-black cursor-pointer"
            @click="navigateTo(`${config.public.api}/auth/login`, { external: true })"
          >
            <img
              src="/logos/github.svg"
              alt="GitHub"
              class="w-[32px] h-[32px]"
            >
            <span class="text-[20px] font-normal text-black">
              Sign up with <span class="font-bold">GitHub</span>
            </span>
          </Button>
          <Button
            variant="outline"
            class="w-[254px] h-[56px] border-black bg-transparent text-black cursor-pointer"
          >
            <img
              src="/logos/google.svg"
              alt="Google"
              class="w-[32px] h-[32px]"
            >
            <span class="text-[20px] font-normal text-black">
              Sign up with <span class="font-bold">Google</span>
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
