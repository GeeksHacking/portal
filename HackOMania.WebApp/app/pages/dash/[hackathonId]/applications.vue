<script setup lang="ts">
const route = useRoute()
const hackathonId = computed(() => route.params.hackathonId as string)
</script>

<template>
  <UCard>
    <template #header>
      <div class="flex flex-col gap-1">
        <h3 class="text-sm font-semibold">
          Applications
        </h3>
        <p class="text-sm text-(--ui-text-muted)">
          Application reviews are now managed from the participants workspace.
        </p>
      </div>
    </template>

    <div class="flex flex-col gap-3 text-sm text-(--ui-text-muted)">
      <p>
        Use the pending review queue to approve or reject applications and track overdue reviews.
      </p>
      <div>
        <UButton
          :to="`/dash/${hackathonId}/participants`"
          icon="i-lucide-users"
          size="sm"
        >
          Go to participants
        </UButton>
      </div>
    </div>
  </UCard>
</template>
