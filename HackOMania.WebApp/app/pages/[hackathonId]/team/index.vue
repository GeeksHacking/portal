<script setup lang="ts">
import { useQuery, useQueryClient } from '@tanstack/vue-query'

const route = useRoute()
const config = useRuntimeConfig()
const toast = useToast()
const queryClient = useQueryClient()
const hackathon = useRouteHackathon()
const resolvedHackathonId = useResolvedHackathonId()
const withdrawMutation = useWithdrawFromHackathon(resolvedHackathonId)
const isWithdrawModalOpen = ref(false)

useHead({ title: 'Team Portal | GeeksHacking Event Portal' })

const { data: user, isLoading: authLoading } = useQuery(authQueries.whoAmI)
const { data: status, isLoading: statusLoading } = useQuery(
  computed(() => ({
    ...hackathonQueries.status(resolvedHackathonId.value ?? ''),
    enabled: !!resolvedHackathonId.value,
  })),
)

watch(
  [() => user.value, authLoading, () => status.value, statusLoading, hackathon],
  ([userData, authIsLoading, statusData, statusIsLoading, hackathonData]) => {
    if (authIsLoading)
      return
    if (!userData) {
      navigateTo(`${config.public.api}/auth/login?redirect_uri=${encodeURIComponent(route.fullPath)}`, { external: true })
      return
    }
    if (statusIsLoading || !hackathonData)
      return
    if (!statusData?.isParticipant) {
      navigateTo({ path: `/${hackathonData.shortCode}/registration`, query: route.query }, { replace: true })
    }
    else if (statusData.status !== 'Accepted') {
      navigateTo({ path: `/${hackathonData.shortCode}/registration/status`, query: route.query }, { replace: true })
    }
  },
  { immediate: true },
)

async function withdrawFromHackathon() {
  try {
    await withdrawMutation.mutateAsync()
    await queryClient.invalidateQueries({ queryKey: hackathonQueries.status(resolvedHackathonId.value ?? '').queryKey })
    isWithdrawModalOpen.value = false
    toast.add({
      title: 'Withdrawn',
      description: 'You have withdrawn from this hackathon.',
      color: 'success',
    })
    if (hackathon.value?.shortCode) {
      await navigateTo(`/${hackathon.value.shortCode}/registration`)
    }
  }
  catch (error) {
    console.error('[TEAM] Failed to withdraw from hackathon', error)
    toast.add({
      title: 'Could not withdraw',
      description: 'Leave your team first, then try again.',
      color: 'error',
    })
  }
}
</script>

<template>
  <div>
    <AppNavBar />
    <div class="mx-auto max-w-7xl px-4 pt-4 flex justify-end">
      <UButton
        size="sm"
        color="error"
        variant="soft"
        icon="i-lucide-user-minus"
        @click="isWithdrawModalOpen = true"
      >
        Withdraw from hackathon
      </UButton>
    </div>
    <div
      id="home"
      class="scroll-mt-12 lg:scroll-mt-18"
    >
      <AppHeroSection />
    </div>
    <div
      id="challenges"
      class="scroll-mt-12 lg:scroll-mt-18"
    >
      <PortalChallengesSection />
    </div>
    <div
      id="team"
      class="scroll-mt-12 lg:scroll-mt-18"
    >
      <PortalTeamSection />
    </div>
    <div
      id="submission"
      class="scroll-mt-12 lg:scroll-mt-18"
    >
      <PortalSubmissionSection />
    </div>
    <AppFooter />

    <UModal
      v-model:open="isWithdrawModalOpen"
      title="Withdraw from hackathon"
      description="This removes your participant access for this hackathon."
    >
      <template #content>
        <div class="overflow-auto max-h-[80vh]">
          <UCard>
            <p class="text-sm text-(--ui-text-muted)">
              Are you sure you want to withdraw? You must leave your team first if you are currently in one.
            </p>
            <div class="mt-4 flex justify-end gap-2">
              <UButton
                variant="ghost"
                @click="isWithdrawModalOpen = false"
              >
                Cancel
              </UButton>
              <UButton
                color="error"
                :loading="withdrawMutation.isPending.value"
                @click="withdrawFromHackathon"
              >
                Withdraw
              </UButton>
            </div>
          </UCard>
        </div>
      </template>
    </UModal>
  </div>
</template>
