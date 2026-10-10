<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { githubLoginUrl } from '~/utils/login-url'

const route = useRoute()
const config = useRuntimeConfig()
const routeHackathonId = computed(() => (route.params.hackathonId as string) ?? '')
const { data: hackathon } = useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint({ path: computed(() => ({ hackathonIdOrShortCode: routeHackathonId.value })) })
const resolvedHackathonId = computed(() => hackathon.value?.id ?? '')

useHead({ title: 'Team Portal | GeeksHacking Event Portal' })

const { data: user, isLoading: authLoading } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const { data: status, isLoading: statusLoading } = useGeeksHackingPortalApiEndpointsParticipantsHackathonStatusEndpoint({ path: computed(() => ({ hackathonId: resolvedHackathonId.value })) }, { query: { enabled: computed(() => !!resolvedHackathonId.value) } })

watch(
  [() => user.value, authLoading, () => status.value, statusLoading, hackathon],
  ([userData, authIsLoading, statusData, statusIsLoading, hackathonData]) => {
    if (authIsLoading)
      return
    if (!userData) {
      navigateTo(githubLoginUrl(config.public.api, route.fullPath), { external: true })
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
    <div
      id="timeline"
      class="scroll-mt-12 lg:scroll-mt-18"
    >
      <PortalTimelineSection />
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
