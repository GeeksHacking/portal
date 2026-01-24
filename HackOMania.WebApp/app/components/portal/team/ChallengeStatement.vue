<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import { ref, computed } from 'vue'

defineProps<{
  teamName: string
}>()

const hackathonId = useCurrentHackathonId()

// Fetch challenges list for the hackathon
const { data: challengesData } = useQuery(
  computed(() => ({
    ...challengeQueries.list(hackathonId.value ?? ''),
    enabled: !!hackathonId.value,
  })),
)

const challenges = computed(() => [...(challengesData.value?.challenges ?? [])].reverse())

const challengeItems = computed(() =>
  challenges.value.map(challenge => ({
    label: challenge.title ?? '',
    value: challenge.id ?? '',
  })),
)

const selectedChallenge = ref<string | undefined>(undefined)
</script>

<template>
  <div class="w-full lg:max-w-3xl text-black">
    <h3 class="font-['Zalando_Sans_Expanded'] text-xl lg:text-3xl font-bold mb-2 uppercase">
      Challenge Statement
    </h3>

    <p class="font-['Raleway'] text-base lg:text-xl text-black/80">
      Select a challenge statement your team will be working on.
    </p>

    <p class="font-['Raleway'] text-base lg:text-xl text-black/80 mt-[34px]">
      Selected Challenge Statement for:
    </p>
    <p class="font-['Zalando_Sans_Expanded'] text-2xl font-bold uppercase">
      {{ teamName }}
    </p>

    <USelect
      v-model="selectedChallenge"
      :items="challengeItems"
      value-key="value"
      label-key="label"
      placeholder="Choose a Challenge Statement"
      class="w-full mt-6"
      :ui="{
        base: 'bg-white rounded-lg border border-black font-[\'Raleway\'] text-base lg:text-2xl h-11 lg:h-14',
        content: 'font-[\'Raleway\'] text-base lg:text-2xl',
        item: 'font-[\'Raleway\'] text-base lg:text-2xl',
      }"
    />
  </div>
</template>
