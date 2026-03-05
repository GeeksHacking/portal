<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'

const route = useRoute()
const config = useRuntimeConfig()
const hackathon = useRouteHackathon()
const resolvedHackathonId = useResolvedHackathonId()

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

</script>

<template>
  <div>
    <AppNavBar />
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
    <!-- <div
      id="timeline"
      class="scroll-mt-12 lg:scroll-mt-18"
    >
      <PortalTimelineSection />
    </div> -->
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
    <div class="mx-auto max-w-7xl px-4 py-8">
      <div class="rounded-lg border border-(--ui-border) bg-(--ui-bg-elevated)/60 p-4">
        <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p class="text-sm font-medium text-(--ui-text)">
              Withdraw from hackathon
            </p>
            <p class="text-xs text-(--ui-text-muted)">
              Leave your team first, then withdraw. This can be done anytime before the event ends.
            </p>
          </div>
          <UButton
            size="xs"
            color="error"
            variant="soft"
            icon="i-lucide-user-minus"
            class="sm:shrink-0"
            @click="isWithdrawModalOpen = true"
          >
            Withdraw
          </UButton>
        </div>
      </div>
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
            <template #header>
              <div class="flex items-center justify-between">
                <h3 class="text-base font-semibold">
                  Withdraw from hackathon
                </h3>
                <UButton
                  variant="ghost"
                  icon="i-lucide-x"
                  size="xs"
                  @click="isWithdrawModalOpen = false"
                />
              </div>
            </template>

            <div class="space-y-4">
              <div class="rounded-lg border border-amber-200 bg-amber-50 dark:border-amber-800 dark:bg-amber-950/30 p-3">
                <div class="flex items-start gap-2">
                  <UIcon
                    name="i-lucide-alert-triangle"
                    class="w-4 h-4 text-amber-500 shrink-0 mt-0.5"
                  />
                  <p class="text-xs text-amber-700 dark:text-amber-300">
                    You must leave your team before withdrawing. This will revoke your participant access for this hackathon.
                  </p>
                </div>
              </div>

              <p class="text-sm text-(--ui-text-muted)">
                Are you sure you want to withdraw? You will need to be reviewed again if you want to re-join.
              </p>

              <div class="flex justify-end gap-2">
                <UButton
                  variant="ghost"
                  @click="isWithdrawModalOpen = false"
                >
                  Cancel
                </UButton>
                <UButton
                  color="error"
                  icon="i-lucide-user-minus"
                  :loading="withdrawMutation.isPending.value"
                  @click="withdrawFromHackathon"
                >
                  Withdraw
                </UButton>
              </div>
            </div>
          </UCard>
        </div>
      </template>
    </UModal>
  </div>
</template>
