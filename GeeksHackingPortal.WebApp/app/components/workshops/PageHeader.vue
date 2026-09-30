<script setup lang="ts">
defineProps<{
  loginUrl?: string
  showSignIn?: boolean
  signedInAs?: string | null
  badgeLabel?: string | null
  badgeColor?: 'success' | 'warning' | 'info' | 'primary' | 'neutral'
}>()
</script>

<template>
  <header class="sticky top-0 z-40 border-b border-default bg-default/90 backdrop-blur-md">
    <div class="mx-auto flex h-14 w-full max-w-7xl items-center justify-between gap-4 px-4 sm:px-6 lg:px-8">
      <div class="flex min-w-0 items-center gap-3 sm:gap-4">
        <NuxtLink
          to="/dash"
          class="flex min-w-0 items-center gap-2.5 transition-opacity hover:opacity-80"
        >
          <img
            src="/logos/geekshacking.svg"
            alt="GeeksHacking"
            class="h-7 w-auto max-w-[9.5rem] sm:h-8 sm:max-w-[11rem]"
          >
          <span class="hidden text-xs font-medium tracking-[0.16em] text-muted uppercase sm:inline">
            Portal
          </span>
        </NuxtLink>

        <div class="hidden h-5 w-px bg-default sm:block" />

        <NuxtLink
          to="/dash"
          class="hidden items-center gap-1.5 text-sm text-muted transition-colors hover:text-default sm:inline-flex"
        >
          <UIcon
            name="i-lucide-layout-grid"
            class="size-3.5"
          />
          Explore events
        </NuxtLink>
      </div>

      <div class="flex shrink-0 items-center gap-2 sm:gap-3">
        <UBadge
          v-if="badgeLabel"
          :color="badgeColor ?? 'success'"
          variant="subtle"
          size="sm"
        >
          {{ badgeLabel }}
        </UBadge>

        <UBadge
          v-if="signedInAs"
          color="neutral"
          variant="soft"
          size="sm"
          class="max-w-[10rem] truncate"
        >
          @{{ signedInAs }}
        </UBadge>

        <UButton
          v-else-if="showSignIn && loginUrl"
          :to="loginUrl"
          external
          size="sm"
          variant="outline"
          color="neutral"
          icon="i-lucide-github"
        >
          Sign in
        </UButton>
      </div>
    </div>
  </header>
</template>
