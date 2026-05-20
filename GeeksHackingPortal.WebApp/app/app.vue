<script setup lang="ts">
const { $pwa } = useNuxtApp()

const toast = useToast()

onMounted(() => {
  if ($pwa.offlineReady)
    toast.success('App ready to work offline')
})
</script>

<template>
  <NuxtPwaManifest />
  <UApp>
    <NuxtLayout>
      <NuxtRouteAnnouncer />
      <NuxtPage />
      <div
        v-show="$pwa.needRefresh"
        class="fixed right-4 bottom-4 z-50 w-[calc(100vw-2rem)] max-w-md"
      >
        <UAlert
          color="primary"
          variant="subtle"
          orientation="vertical"
          icon="i-lucide-refresh-cw"
          title="Update available"
          description="New content is ready. Reload to update the app."
        >
          <template #actions>
            <UButton
              color="primary"
              icon="i-lucide-rotate-cw"
              label="Reload"
              @click="$pwa.updateServiceWorker()"
            />
          </template>
        </UAlert>
      </div>
    </NuxtLayout>
  </UApp>
</template>
