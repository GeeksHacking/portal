<script setup lang="ts">
import type { NavigationMenuItem } from '@nuxt/ui'
import { useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint } from '@geekshacking/portal-sdk/hooks'

useHead({
  titleTemplate: title => (title ? `${title} - GeeksHacking` : 'GeeksHacking'),
})

const { data: user, isLoading: userIsLoading } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()

const open = ref(false)
const route = useRoute()
const hackathonId = computed(() => route.params.hackathonId as string | undefined)
const standaloneWorkshopId = computed(() => route.params.standaloneWorkshopId as string | undefined)

const links = computed<NavigationMenuItem[][]>(() => {
  const defaultLinks: NavigationMenuItem[] = [
    {
      label: 'Dashboard',
      icon: 'i-lucide-house',
      exact: true,
      to: '/dash',
      onSelect: () => {
        open.value = false
      },
    },
  ]

  const isParticipantView = (route.path.includes('/participant/') || route.path.endsWith('/participant')) || route.path.includes('/registration')

  if (standaloneWorkshopId.value) {
    const standaloneLinks: NavigationMenuItem[] = [
      { label: 'Check Ins', icon: 'i-lucide-qr-code', to: `/dash/standalone/${standaloneWorkshopId.value}/checkin` },
      { label: 'Resources', icon: 'i-lucide-gift', to: `/dash/standalone/${standaloneWorkshopId.value}/resources` },
      { label: 'Stats', icon: 'i-lucide-chart-pie', to: `/dash/standalone/${standaloneWorkshopId.value}/stats` },
      { label: 'Participants', icon: 'i-lucide-users', to: `/dash/standalone/${standaloneWorkshopId.value}/participants` },
      { label: 'Data Export', icon: 'i-lucide-file-down', to: `/dash/standalone/${standaloneWorkshopId.value}/infopack` },
      { label: 'Questions', icon: 'i-lucide-circle-help', to: `/dash/standalone/${standaloneWorkshopId.value}/questions` },
      { label: 'Settings', icon: 'i-lucide-settings-2', to: `/dash/standalone/${standaloneWorkshopId.value}/settings` },
    ]

    return [
      defaultLinks,
      standaloneLinks.map(link => ({
        ...link,
        onSelect: () => {
          open.value = false
        },
      })),
    ]
  }

  if (!hackathonId.value || isParticipantView) {
    return [defaultLinks, []]
  }

  const organizerLinks: NavigationMenuItem[] = [
    { label: 'Check Ins', icon: 'i-lucide-qr-code', to: `/dash/${hackathonId.value}/checkin` },
    { label: 'Resources', icon: 'i-lucide-gift', to: `/dash/${hackathonId.value}/resources` },
    { label: 'Stats', icon: 'i-lucide-chart-pie', to: `/dash/${hackathonId.value}/stats` },
    { label: 'Participants', icon: 'i-lucide-users', to: `/dash/${hackathonId.value}/participants` },
    { label: 'Teams', icon: 'i-lucide-user-round-plus', to: `/dash/${hackathonId.value}/teams` },
    {
      label: 'Challenges',
      icon: 'i-lucide-trophy',
      children: [
        { label: 'Challenges', to: `/dash/${hackathonId.value}/challenges` },
        { label: 'Analytics', to: `/dash/${hackathonId.value}/challenge-dashboard` },
      ],
    },
    { label: 'Submissions', icon: 'i-lucide-file-text', to: `/dash/${hackathonId.value}/submissions` },
    { label: 'Judges', icon: 'i-lucide-scale', to: `/dash/${hackathonId.value}/judges` },
    { label: 'Questions', icon: 'i-lucide-circle-help', to: `/dash/${hackathonId.value}/questions` },
    { label: 'Settings', icon: 'i-lucide-settings-2', to: `/dash/${hackathonId.value}/settings` },
    { label: 'Data Export', icon: 'i-lucide-file-down', to: `/dash/${hackathonId.value}/infopack` },
  ]

  return [
    defaultLinks,
    organizerLinks.map(link => ({
      ...link,
      onSelect: () => {
        open.value = false
      },
    })),
  ]
})
</script>

<template>
  <UMain>
    <UDashboardGroup unit="rem">
      <UDashboardSidebar
        id="default"
        v-model:open="open"
        collapsible
        resizable
        class="bg-elevated/25"
        :ui="{ footer: 'lg:border-t lg:border-default' }"
      >
        <template #header>
          HackOMania
        </template>
        <template #default="{ collapsed }">
          <UNavigationMenu
            :collapsed="collapsed"
            :items="links[0]"
            orientation="vertical"
            tooltip
            popover
          />

          <UNavigationMenu
            :collapsed="collapsed"
            :items="links[1]"
            orientation="vertical"
            tooltip
            class="mt-3"
          />
        </template>

        <template #footer>
          <div class="p-4 text-sm text-center">
            Logged in as
            <span v-if="userIsLoading">Loading...</span>
            <span v-else>{{ user?.gitHubLogin }}</span>
          </div>
        </template>
      </UDashboardSidebar>

      <NuxtPage />
    </UDashboardGroup>
  </UMain>
</template>
