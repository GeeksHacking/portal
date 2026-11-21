<script setup lang="ts">
import { useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint } from '@geekshacking/portal-sdk/hooks'
import { computed } from 'vue'

const route = useRoute()
const hackathonIdOrShortCode = computed(() => route.params.hackathonId as string | undefined)

const { data: hackathon } = useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint({ path: computed(() => ({ hackathonIdOrShortCode: hackathonIdOrShortCode.value ?? '' })) }, { query: { enabled: computed(() => !!hackathonIdOrShortCode.value) } })

const resolvedHackathonId = computed(() => hackathon.value?.id ?? null)
</script>

<template>
  <GPanel id="registration">
    <template #header>
      <GPanelHeader title="Registration" />
    </template>

    <template #body>
      <RegistrationFormPage
        :hackathon-id="resolvedHackathonId"
        embedded
      />
    </template>
  </GPanel>
</template>
