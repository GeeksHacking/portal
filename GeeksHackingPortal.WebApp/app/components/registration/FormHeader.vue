<script setup lang="ts">
import { useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint } from '@geekshacking/portal-sdk/hooks'

/**
 * Top bar of the standalone registration form: the GeeksHacking mark, which event this is for,
 * and a way out.
 */
const props = defineProps<{
  hackathonId?: string | null
}>()

const { data: hackathon } = useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint(
  { path: computed(() => ({ hackathonIdOrShortCode: props.hackathonId ?? '' })) },
  { query: { enabled: computed(() => !!props.hackathonId) } },
)
</script>

<template>
  <header class="reg-header">
    <div class="reg-header__inner">
      <span class="reg-header__brand">
        <GLogo
          :wordmark="false"
          size="sm"
          decorative
        />
        <span class="reg-header__text">
          <span class="reg-header__name">GeeksHacking</span>
          <span class="reg-header__event">{{ hackathon?.name ? `Registration · ${hackathon.name}` : 'Registration' }}</span>
        </span>
      </span>
      <GButton
        to="/"
        variant="ghost"
        size="sm"
        icon="material-symbols:close-rounded"
        label="Exit"
        aria-label="Exit registration"
        class="flex-none"
      />
    </div>
  </header>
</template>

<style scoped>
.reg-header {
  position: sticky;
  top: 0;
  z-index: 40;
  border-bottom: var(--gh-border-width) solid var(--gh-line);
  background: var(--gh-bg);
}
.reg-header__inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  max-width: 56rem;
  height: 64px;
  margin: 0 auto;
  padding: 0 16px;
}
.reg-header__brand {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}
.reg-header__text {
  display: flex;
  flex-direction: column;
  min-width: 0;
  line-height: 1.2;
}
.reg-header__name {
  font-family: var(--font-display);
  font-size: 16px;
  font-weight: 700;
}
.reg-header__event {
  overflow: hidden;
  font-size: 12px;
  font-weight: 600;
  color: var(--gh-text-muted);
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
