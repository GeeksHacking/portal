<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'

const route = useRoute()
const config = useRuntimeConfig()
const hackathon = useRouteHackathon()
const resolvedHackathonId = useResolvedHackathonId()

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
    if (authIsLoading) return
    if (!userData) {
      navigateTo(`${config.public.api}/auth/login?redirect_uri=${encodeURIComponent(route.fullPath)}`, { external: true })
      return
    }
    if (statusIsLoading || !hackathonData) return
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
  </div>
</template>
