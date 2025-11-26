<script setup lang="ts">
import type { NavigationMenuItem } from '@nuxt/ui'
import { useQuery } from '@tanstack/vue-query'

useHead({
  titleTemplate: title => (title ? `${title} - HackOMania` : 'HackOMania'),
})

const { data: user, isLoading: userIsLoading } = useQuery(authQueries.whoAmI)

const open = ref(false)

const links = [
  [
    {
      label: 'Dashboard',
      icon: 'i-lucide-house',
      exact: true,
      to: '/dash',
      onSelect: () => {
        open.value = false
      },
    },
  ],
] satisfies NavigationMenuItem[][]
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
            class="mt-auto"
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
