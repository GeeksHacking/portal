<script setup lang="ts">
import QRCode from 'qrcode'

/**
 * A registered participant's workshop check-in code: a button that opens the QR code in a
 * dialog. The code carries the workshop, user and registration ids, which the organisers'
 * scanner reads. Button styling comes from the props (header button vs. full-width primary).
 */
const props = withDefaults(defineProps<{
  workshopId: string
  userId: string
  registrationId?: string | null
  workshopTitle?: string | null
  label?: string
  variant?: 'primary' | 'secondary'
  size?: 'sm' | 'md' | 'lg'
  block?: boolean
}>(), {
  label: 'My QR code',
  variant: 'secondary',
  size: 'sm',
})

const toast = useGToast()
const open = ref(false)
const dataUrl = ref('')

async function show() {
  if (!props.userId || !import.meta.client)
    return
  try {
    dataUrl.value = await QRCode.toDataURL(JSON.stringify({
      kind: 'standalone-workshop-check-in',
      workshopId: props.workshopId,
      userId: props.userId,
      registrationId: props.registrationId ?? '',
    }), {
      width: 320,
      margin: 2,
      color: { dark: '#000000', light: '#FFFFFF' },
    })
    open.value = true
  }
  catch (error) {
    console.error('Failed to generate participant QR code', error)
    toast.add({ title: 'Couldn\'t show your QR code', description: 'Try again in a moment.', tone: 'err' })
  }
}
</script>

<template>
  <GButton
    :variant="variant"
    :size="size"
    :block="block"
    icon="material-symbols:qr-code-2-rounded"
    :label="label"
    :disabled="!userId"
    @click="show"
  />
  <GModal
    v-model:open="open"
    title="Your QR code"
    description="Show this to an organiser when they check you in."
    size="sm"
  >
    <div class="flex flex-col items-center gap-3">
      <img
        v-if="dataUrl"
        :src="dataUrl"
        alt="Your workshop check-in QR code"
        class="h-auto w-full max-w-[280px] rounded-gh border-[1.5px] border-gh-line bg-white"
      >
      <p
        v-if="workshopTitle"
        class="gh-small text-gh-muted"
      >
        {{ workshopTitle }}
      </p>
    </div>
  </GModal>
</template>
