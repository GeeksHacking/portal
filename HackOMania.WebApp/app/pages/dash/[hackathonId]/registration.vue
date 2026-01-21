<script setup lang="ts">
import { computed } from 'vue'

const route = useRoute()
const hackathonId = computed(() => (route.params.hackathonId as string | undefined) ?? null)
</script>

<template>
  <RegistrationFormPage :hackathon-id="hackathonId" />
</template>
