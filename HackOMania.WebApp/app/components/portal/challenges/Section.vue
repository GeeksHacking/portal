<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import { computed } from 'vue'

const { data: hackathonsData } = useQuery(hackathonQueries.list)

const hackathonId = computed(() => hackathonsData.value?.hackathons?.[0]?.id ?? null)

const { data: challengesData, isLoading } = useQuery({
  ...challengeQueries.list(hackathonId.value ?? ''),
  enabled: computed(() => !!hackathonId.value),
})
</script>

<template>
  <section class="w-full">
    <header
      class="w-full h-18 flex items-center justify-center"
      style="background: linear-gradient(to right, #FFC1AA 0%, #FF5B84 94%, #FFC1AA 100%)"
    >
      <h2 class="font-['Zalando_Sans_Expanded'] text-black text-center m-0">
        CHALLENGES
      </h2>
    </header>

    <div v-if="isLoading" class="p-4 text-center">
      Loading challenges...
    </div>

    <div v-else class="flex flex-wrap gap-4 p-4">
      <PortalChallengesCard
        v-for="challenge in challengesData?.challenges"
        :key="challenge.id!"
        :challenge-name="challenge.title ?? ''"
        :team-count="0"
        header-color="#FF5B84"
      />
    </div>
  </section>
</template>
