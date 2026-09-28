<script setup lang="ts">
import { useGeeksHackingPortalApiEndpointsOrganizersAcceptInviteEndpoint } from '@geekshacking/portal-sdk/hooks'
import { computed, onMounted, ref } from 'vue'

const route = useRoute()
const toast = useToast()
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

  status.value = 'idle'
  errorMessage.value = ''

  try {
    const result = await acceptInviteMutation.mutateAsync({
      body: { code: code.value },
    })

    acceptedRole.value = result?.type ?? null
    status.value = 'success'
    toast.add({ title: 'Organizer access added', color: 'success' })
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
  <main class="min-h-screen bg-(--ui-bg-muted) px-4 py-10">
    <div class="mx-auto flex min-h-[70vh] w-full max-w-md items-center">
      <UCard class="w-full">
        <template #header>
          <div class="flex items-center gap-3">
            <UIcon
              name="i-lucide-user-plus"
              class="size-5 text-(--ui-primary)"
            />
            <h1 class="text-base font-semibold">
              Organizer Invite
            </h1>
          </div>
        </template>

        <div class="space-y-4">
          <div
            v-if="isAccepting || status === 'idle'"
            class="flex items-center gap-3 text-sm text-(--ui-text-muted)"
          >
            <UIcon
              name="i-lucide-loader-circle"
              class="size-5 animate-spin"
            />
            Joining as organizer...
          </div>

          <div
            v-else-if="status === 'success'"
            class="space-y-4"
          >
            <UAlert
              color="success"
              variant="subtle"
              icon="i-lucide-circle-check"
              title="You are now an organizer"
              :description="acceptedRole ? `Role: ${acceptedRole}` : undefined"
            />

            <UButton
              to="/dash"
              icon="i-lucide-layout-dashboard"
              block
            >
              Open Dashboard
            </UButton>
          </div>

          <div
            v-else
            class="space-y-4"
          >
            <UAlert
              color="error"
              variant="subtle"
              icon="i-lucide-circle-alert"
              title="Invite could not be accepted"
              :description="errorMessage"
            />

            <div class="flex flex-col gap-2 sm:flex-row sm:justify-end">
              <UButton
                variant="ghost"
                to="/dash"
              >
                Back to Dashboard
              </UButton>
              <UButton
                icon="i-lucide-refresh-cw"
                :loading="isAccepting"
                @click="acceptInvite"
              >
                Try Again
              </UButton>
            </div>
          </div>
        </div>
      </UCard>
    </div>
  </main>
</template>
