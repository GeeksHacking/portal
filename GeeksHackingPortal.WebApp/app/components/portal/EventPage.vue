<script setup lang="ts">
/**
 * Frame for the participant event pages (/[shortCode]/team, challenges, submission, schedule):
 * checks the visitor is an accepted participant (see useEventParticipantGuard), then shows the
 * page in the portal panel with the event's name and the check-in QR button.
 */
const props = defineProps<{
  title: string
  panelId: string
}>()

const { hackathon, ready } = useEventParticipantGuard()

useHead({
  title: computed(() => (hackathon.value?.name ? `${props.title} · ${hackathon.value.name}` : props.title)),
})
</script>

<template>
  <GPanel :id="panelId">
    <template #header>
      <GPanelHeader :title="title">
        <template #right>
          <PortalCheckinQr
            v-if="ready"
            :hackathon-id="hackathon?.id"
          />
        </template>
      </GPanelHeader>
    </template>

    <template #body>
      <!-- one centred column for every event page: the event strip lines up with the content -->
      <div class="event-page">
        <GLabelStrip
          v-if="hackathon?.name"
          :label="hackathon.name"
          class="self-start"
        />
        <slot v-if="ready" />
        <GLoading
          v-else
          label="Checking your session…"
        />
      </div>
    </template>
  </GPanel>
</template>

<style scoped>
.event-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
  width: 100%;
  max-width: 72rem;
  margin: 0 auto;
}
@media (min-width: 640px) {
  .event-page { gap: 24px; }
}
</style>
