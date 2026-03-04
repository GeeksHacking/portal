<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import QRCode from 'qrcode'

const items = [
  { label: 'home', target: 'home' },
  { label: 'challenges', target: 'challenges' },
  { label: 'team', target: 'team' },
  { label: 'submission', target: 'submission' },
  { label: 'contacts', target: 'contacts' },
]

const expanded = ref(false)
const showQrModal = ref(false)
const qrCodeDataUrl = ref<string>('')

function scrollTo(targetId: string) {
  document.getElementById(targetId)?.scrollIntoView({ behavior: 'smooth' })
  expanded.value = false
}
const config = useRuntimeConfig()
const loginUrl = `${config.public.api}/auth/login`

const { data: user, isLoading } = useQuery(authQueries.whoAmI)

async function generateQrCode() {
  if (!user.value?.id)
    return

  const qrData = JSON.stringify({ userId: user.value.id })

  try {
    const dataUrl = await QRCode.toDataURL(qrData, {
      width: 300,
      margin: 2,
      color: {
        dark: '#000000',
        light: '#FFFFFF',
      },
    })
    qrCodeDataUrl.value = dataUrl
    showQrModal.value = true
  }
  catch (error) {
    console.error('Error generating QR code:', error)
  }
}

function closeQrModal() {
  showQrModal.value = false
}
</script>

<template>
  <nav class="h-12 lg:h-18 border-b border-black sticky top-0 bg-white z-50 font-[Raleway] font-medium text-black">
    <!-- Desktop -->
    <div class="hidden lg:flex items-center justify-between h-full px-32">
      <div class="flex items-center gap-20">
        <button v-for="item in items" :key="item.label" @click="scrollTo(item.target)">
          {{ item.label }}
        </button>
      </div>
      <div class="flex items-center gap-3">
        <span v-if="isLoading">...</span>
        <template v-else-if="user">
          <button class="hover:opacity-70 transition-opacity flex items-center" @click="generateQrCode">
            <UIcon name="i-lucide-qr-code" class="size-5" />
          </button>
          <span>{{ user.gitHubLogin }}</span>
        </template>
        <NuxtLink v-else :to="loginUrl" external>
          log in
        </NuxtLink>
      </div>
    </div>

    <!-- Mobile -->
    <div class="flex lg:hidden items-center justify-end h-full px-3">
      <button @click="expanded = !expanded">
        <UIcon
          :name="expanded ? 'i-lucide-x' : 'i-lucide-menu'"
          class="size-5"
        />
      </button>
    </div>

    <!-- Mobile dropdown -->
    <div
      v-if="expanded"
      class="lg:hidden absolute top-12 left-0 w-full bg-white border-b border-black flex flex-col gap-4 py-4 px-3 items-end z-50"
    >
      <button v-for="item in items" :key="item.label" @click="scrollTo(item.target)">
        {{ item.label }}
      </button>
      <span v-if="isLoading">...</span>
      <template v-else-if="user">
        <div class="flex items-center gap-3">
          <button class="hover:opacity-70 transition-opacity flex items-center" @click="generateQrCode">
            <UIcon name="i-lucide-qr-code" class="size-5" />
          </button>
          <span>{{ user.gitHubLogin }}</span>
        </div>
      </template>
      <NuxtLink v-else :to="loginUrl" external>
        log in
      </NuxtLink>
    </div>

    <!-- QR Code Modal -->
    <div
      v-if="showQrModal"
      class="fixed inset-0 bg-black/50 z-[100] flex items-center justify-center p-4"
      @click="closeQrModal"
    >
      <div
        class="bg-white p-4 sm:p-6 md:p-8 rounded-lg shadow-xl max-w-sm w-full"
        @click.stop
      >
        <div class="flex flex-col items-center gap-3 sm:gap-4">
          <h3 class="text-base sm:text-lg font-semibold">
            Your QR Code
          </h3>
          <img
            v-if="qrCodeDataUrl"
            :src="qrCodeDataUrl"
            alt="User QR Code"
            class="w-full max-w-[280px] sm:max-w-[300px] h-auto"
          >
          <p class="text-sm text-gray-600">
            {{ user?.gitHubLogin }}
          </p>
        </div>
      </div>
    </div>
  </nav>
</template>
