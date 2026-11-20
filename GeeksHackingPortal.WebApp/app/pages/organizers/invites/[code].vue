<script setup lang="ts">
import { useGeeksHackingPortalApiEndpointsOrganizersAcceptInviteEndpoint } from '@geekshacking/portal-sdk/hooks'
import { computed, onMounted, ref } from 'vue'

const route = useRoute()
const acceptInviteMutation = useGeeksHackingPortalApiEndpointsOrganizersAcceptInviteEndpoint()

const code = computed(() => (route.params.code as string | undefined)?.trim() ?? '')
const status = ref<'idle' | 'success' | 'error'>('idle')
const errorMessage = ref('')
const acceptedRole = ref<string | null>(null)

const isAccepting = computed(() => acceptInviteMutation.isPending.value)

useHead({
  title: 'Organizer Invite',
})

async function acceptInvite() {
  if (!code.value) {
    status.value = 'error'
    errorMessage.value = 'Invite code is missing.'
    return
  }

  // a retry keeps the error on screen (the button shows it's working) so focus isn't lost
  try {
    const result = await acceptInviteMutation.mutateAsync({
      body: { code: code.value },
    })

    acceptedRole.value = result?.type ?? null
    status.value = 'success'
  }
  catch (error) {
    status.value = 'error'
    errorMessage.value = getApiErrorMessage(error, 'Unable to accept this invite.')
  }
}

onMounted(() => {
  acceptInvite()
})
</script>

<template>
  <AuthFrame>
    <GLabelStrip
      as="p"
      label="Organizer invite"
      class="self-start"
    />

    <template v-if="status === 'idle'">
      <h1 class="sr-only">
        Organizer invite
      </h1>
      <GLoading label="Adding you as an organizer…" />
    </template>

    <template v-else-if="status === 'success'">
      <div class="flex flex-col gap-3">
        <h1 class="font-display text-[32px] leading-tight font-bold">
          You're now an organizer
        </h1>
        <p class="text-gh-muted">
          Your organizer tools are in the Organizer section of the portal sidebar.
        </p>
      </div>
      <GAlert
        tone="ok"
        title="Invite accepted"
        :description="acceptedRole ? `Your role: ${acceptedRole}` : undefined"
      />
      <GButton
        to="/dash/manage"
        variant="primary"
        size="lg"
        block
        icon="material-symbols:dashboard-rounded"
        label="Open the organizer workspace"
      />
    </template>

    <template v-else>
      <div class="flex flex-col gap-3">
        <h1 class="font-display text-[32px] leading-tight font-bold">
          We couldn't accept this invite
        </h1>
        <p class="text-gh-muted">
          The link may have expired or already been used. Try again, or ask the organizer who invited you for a new link.
        </p>
      </div>
      <GAlert
        tone="err"
        title="Invite not accepted"
        :description="errorMessage"
      />
      <div class="flex flex-col gap-3 sm:flex-row-reverse">
        <GButton
          variant="primary"
          icon="material-symbols:refresh-rounded"
          label="Try again"
          :loading="isAccepting"
          loading-label="Trying again"
          @click="acceptInvite"
        />
        <GButton
          to="/dash"
          variant="ghost"
          label="Back to the portal"
        />
      </div>
    </template>
  </AuthFrame>
</template>
