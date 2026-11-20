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
      <GLabelStrip
        v-if="hackathon?.name"
        :label="hackathon.name"
        class="self-start"
      />
      <slot v-if="ready" />
      <div
        v-else
        class="flex flex-col gap-3"
        role="status"
      >
        <span class="sr-only">Checking your session…</span>
        <GSkeleton class="h-6 w-1/3" />
        <GSkeleton class="h-40 w-full" />
      </div>
    </template>
  </GPanel>
</template>
