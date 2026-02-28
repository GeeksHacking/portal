<script setup lang="ts">
import type { NavigationMenuItem } from '@nuxt/ui'
import { useQuery } from '@tanstack/vue-query'

useHead({
  titleTemplate: title => (title ? `${title} - HackOMania` : 'HackOMania'),
})

const { data: user, isLoading: userIsLoading } = useQuery(authQueries.whoAmI)

const open = ref(false)
const route = useRoute()
const hackathonId = computed(() => route.params.hackathonId as string | undefined)

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

  if (!hackathonId.value || isParticipantView) {
    return [defaultLinks, []]
  }

  const organizerLinks: NavigationMenuItem[] = [
    { label: 'Check Ins', icon: 'i-lucide-qr-code', to: `/dash/${hackathonId.value}/checkin` },
    { label: 'Analytics', icon: 'i-lucide-chart-line', to: `/dash/${hackathonId.value}/challenge-dashboard` },
    { label: 'Participants', icon: 'i-lucide-users', to: `/dash/${hackathonId.value}/participants` },
    { label: 'Teams', icon: 'i-lucide-user-round-plus', to: `/dash/${hackathonId.value}/teams` },
    { label: 'Challenges', icon: 'i-lucide-trophy', to: `/dash/${hackathonId.value}/challenges` },
    { label: 'Judges', icon: 'i-lucide-scale', to: `/dash/${hackathonId.value}/judges` },
    { label: 'Questions', icon: 'i-lucide-circle-help', to: `/dash/${hackathonId.value}/questions` },
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
