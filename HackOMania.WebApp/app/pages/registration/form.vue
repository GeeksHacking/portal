<script setup lang="ts">
import { computed } from 'vue'

const route = useRoute()
const hackathonId = computed(() => (route.query.hackathonId as string | undefined) ?? null)
const index = computed(() => (route.query.index ? String(route.query.index) : null))

if (hackathonId.value) {
  await navigateTo({
    path: `/dash/${hackathonId.value}/registration`,
    query: index.value ? { index: index.value } : undefined,
    replace: true,
  })
}
</script>

<template>
  <RegistrationFormPage :hackathon-id="hackathonId" />
</template>
