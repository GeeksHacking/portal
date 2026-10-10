<script setup lang="ts">
import type { NavigationMenuItem } from '@nuxt/ui'
import {
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint,
} from '@geekshacking/portal-sdk/hooks'

useHead({
  titleTemplate: title => (title ? `${title} - GeeksHacking` : 'GeeksHacking'),
})

const { data: user, isLoading: userIsLoading } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()

const { data: organizerHackathons } = useGeeksHackingPortalApiEndpointsOrganizersHackathonListEndpoint({
  query: { enabled: computed(() => !!user.value?.id) },
})
const { data: organizerWorkshops } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint({
  query: { enabled: computed(() => !!user.value?.id) },
})
const isOrganizer = computed(() =>
  (organizerHackathons.value?.hackathons?.length ?? 0) > 0
  || (organizerWorkshops.value?.standaloneWorkshops?.length ?? 0) > 0,
)

const open = ref(false)
const route = useRoute()
const hackathonId = computed(() => route.params.hackathonId as string | undefined)
const standaloneWorkshopId = computed(() => route.params.standaloneWorkshopId as string | undefined)

const links = computed<NavigationMenuItem[][]>(() => {
  const close = () => {
    open.value = false
  }
  const withClose = (items: NavigationMenuItem[]) => items.map(link => ({ ...link, onSelect: close }))

  const groups: NavigationMenuItem[][] = [
    withClose([
      { label: 'Explore', icon: 'i-lucide-compass', exact: true, to: '/dash' },
    ]),
  ]

  const isParticipantView = (route.path.includes('/participant/') || route.path.endsWith('/participant')) || route.path.includes('/registration')

  if (user.value?.id && isOrganizer.value) {
    const organizerGroup: NavigationMenuItem[] = [
      { label: 'Organizer', type: 'label' },
      ...withClose([{ label: 'Workspace', icon: 'i-lucide-layout-dashboard', exact: true, to: '/dash/manage' }]),
    ]

    if (standaloneWorkshopId.value) {
      const base = `/dash/standalone/${standaloneWorkshopId.value}`
      organizerGroup.push(...withClose([
        { label: 'Check Ins', icon: 'i-lucide-qr-code', to: `${base}/checkin` },
        { label: 'Resources', icon: 'i-lucide-gift', to: `${base}/resources` },
        { label: 'Stats', icon: 'i-lucide-chart-pie', to: `${base}/stats` },
        { label: 'Participants', icon: 'i-lucide-users', to: `${base}/participants` },
        { label: 'Organizers', icon: 'i-lucide-user-cog', to: `${base}/organizers` },
        { label: 'Data Export', icon: 'i-lucide-file-down', to: `${base}/infopack` },
        { label: 'Questions', icon: 'i-lucide-circle-help', to: `${base}/questions` },
        { label: 'Settings', icon: 'i-lucide-settings-2', to: `${base}/settings` },
      ]))
    }
    else if (hackathonId.value && !isParticipantView) {
      const base = `/dash/${hackathonId.value}`
      organizerGroup.push(
        ...withClose([
          { label: 'Check Ins', icon: 'i-lucide-qr-code', to: `${base}/checkin` },
          { label: 'Resources', icon: 'i-lucide-gift', to: `${base}/resources` },
          { label: 'Stats', icon: 'i-lucide-chart-pie', to: `${base}/stats` },
          { label: 'Participants', icon: 'i-lucide-users', to: `${base}/participants` },
          { label: 'Teams', icon: 'i-lucide-user-round-plus', to: `${base}/teams` },
        ]),
        {
          label: 'Challenges',
          icon: 'i-lucide-trophy',
          children: withClose([
            { label: 'Challenges', to: `${base}/challenges` },
            { label: 'Analytics', to: `${base}/challenge-dashboard` },
          ]),
        },
        ...withClose([
          { label: 'Submissions', icon: 'i-lucide-file-text', to: `${base}/submissions` },
          { label: 'Judges', icon: 'i-lucide-scale', to: `${base}/judges` },
          { label: 'Organizers', icon: 'i-lucide-user-cog', to: `${base}/organizers` },
          { label: 'Questions', icon: 'i-lucide-circle-help', to: `${base}/questions` },
          { label: 'Settings', icon: 'i-lucide-settings-2', to: `${base}/settings` },
          { label: 'Data Export', icon: 'i-lucide-file-down', to: `${base}/infopack` },
        ]),
      )
    }

    groups.push(organizerGroup)
  }

  if (user.value?.isRoot) {
    groups.push([
      { label: 'Admin', type: 'label' },
      ...withClose([
        { label: 'My OAuth Apps', icon: 'i-lucide-key-round', to: '/dash/oauth-applications' },
        { label: 'All OAuth Apps', icon: 'i-lucide-app-window', to: '/dash/admin/oauth-applications' },
      ]),
    ])
  }

  return groups
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
        :ui="{ footer: 'border-t border-default' }"
      >
        <template #header>
          GeeksHacking Portal
        </template>
        <template #default="{ collapsed }">
          <UNavigationMenu
            v-for="(group, index) in links"
            :key="index"
            :collapsed="collapsed"
            :items="group"
            orientation="vertical"
            tooltip
            popover
            :class="index > 0 ? 'mt-4' : ''"
          />
        </template>

        <template #footer="{ collapsed }">
          <USkeleton
            v-if="userIsLoading"
            class="h-10 w-full"
          />
          <UButton
            v-else
            to="/dash/profile"
            color="neutral"
            variant="ghost"
            block
            :square="collapsed"
            class="justify-start"
            :avatar="{ src: user?.gitHubLogin ? `https://github.com/${user.gitHubLogin}.png` : undefined, alt: user?.gitHubLogin ?? 'Profile' }"
            :label="collapsed ? undefined : (user?.gitHubLogin ?? 'Profile')"
            @click="open = false"
          />
        </template>
      </UDashboardSidebar>

      <NuxtPage />
    </UDashboardGroup>
  </UMain>
</template>
