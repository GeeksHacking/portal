<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonTeamsGetMineEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import QRCode from 'qrcode'

/**
 * "My QR code": the participant's check-in code (their user id), shown in a modal with their
 * team name. Lives in the event pages' header.
 */
const props = defineProps<{
  hackathonId?: string
}>()

const { data: user } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const { data: teamData } = useGeeksHackingPortalApiEndpointsParticipantsHackathonTeamsGetMineEndpoint({ path: computed(() => ({ hackathonId: props.hackathonId ?? '' })) }, { query: { enabled: computed(() => !!props.hackathonId) } })
const teamName = computed(() => teamData.value?.name ?? null)

const open = ref(false)
const qrCodeDataUrl = ref('')

async function show() {
  if (!user.value?.id)
    return
  try {
    qrCodeDataUrl.value = await QRCode.toDataURL(JSON.stringify({ userId: user.value.id }), {
      width: 300,
      margin: 2,
      color: { dark: '#000000', light: '#FFFFFF' },
    })
    open.value = true
  }
  catch (error) {
    console.error('Error generating QR code:', error)
  }
}
</script>

<template>
  <GButton
    variant="secondary"
    size="sm"
    icon="material-symbols:qr-code-2-rounded"
    label="My QR code"
    :disabled="!user?.id"
    @click="show"
  />
  <GModal
    v-model:open="open"
    title="Your QR Code"
    description="Show this at check-in and when collecting resources."
    size="sm"
  >
    <div class="flex flex-col items-center gap-4">
      <img
        v-if="qrCodeDataUrl"
        :src="qrCodeDataUrl"
        alt="Your check-in QR code"
        class="h-auto w-full max-w-[280px] rounded-gh border-[1.5px] border-gh-line bg-white sm:max-w-[300px]"
      >
      <p
        v-if="teamName"
        class="gh-small text-gh-muted"
      >
        {{ teamName }}
      </p>
      <GBadge
        v-else
        tone="err"
        label="No Team"
      />
    </div>
  </GModal>
</template>
