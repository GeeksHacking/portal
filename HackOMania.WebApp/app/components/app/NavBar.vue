<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'

const items = [
  { label: 'home', target: 'home' },
  { label: 'challenges', target: 'challenges' },
  { label: 'team', target: 'team' },
  { label: 'contacts', target: 'contacts' },
]

const expanded = ref(false)

function scrollTo(targetId: string) {
  document.getElementById(targetId)?.scrollIntoView({ behavior: 'smooth' })
  expanded.value = false
}
const config = useRuntimeConfig()
const loginUrl = `${config.public.api}/auth/login`

const { data: user, isLoading } = useQuery(authQueries.whoAmI)
</script>

<template>
  <nav class="h-12 lg:h-18 border-b border-black sticky top-0 bg-white z-50 font-[Raleway] font-medium text-black">
    <!-- Desktop -->
    <div class="hidden lg:flex items-center justify-between h-full px-32">
      <div class="flex items-center gap-20">
        <button v-for="item in items" :key="item.label" @click="scrollTo(item.target)">{{ item.label }}</button>
      </div>
      <div>
        <span v-if="isLoading">...</span>
        <span v-else-if="user">{{ user.gitHubLogin }}</span>
        <NuxtLink v-else :to="loginUrl" external>log in</NuxtLink>
      </div>
    </div>

    <!-- Mobile -->
    <div class="flex lg:hidden items-center justify-end h-full px-3">
      <button @click="expanded = !expanded">
        <UIcon
          :name="expanded ? 'i-lucide-x' : 'i-lucide-menu'"
          class="size-5"
        />
      </button>
    </div>

    <!-- Mobile dropdown -->
    <div
      v-if="expanded"
      class="lg:hidden absolute top-12 left-0 w-full bg-white border-b border-black flex flex-col gap-4 py-4 px-3 items-end z-50"
    >
      <button v-for="item in items" :key="item.label" @click="scrollTo(item.target)">{{ item.label }}</button>
      <span v-if="isLoading">...</span>
      <span v-else-if="user">{{ user.gitHubLogin }}</span>
      <NuxtLink v-else :to="loginUrl" external>log in</NuxtLink>
    </div>
  </nav>
</template>
