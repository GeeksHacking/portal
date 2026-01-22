<script setup lang="ts">
import { useQuery, useQueries } from '@tanstack/vue-query'
import { computed, ref } from 'vue'

const hackathonId = useRouteHackathonId()

// Fetch challenges list for the hackathon
const { data: challengesData, isLoading } = useQuery(
  computed(() => ({
    ...challengeQueries.list(hackathonId.value ?? ''),
    enabled: !!hackathonId.value,
  })),
)
const challenges = computed(() => [...(challengesData.value?.challenges ?? [])].reverse())

// Prefetch all challenge details on load for instant display when selected
const detailsQueries = useQueries({
  queries: computed(() =>
    challenges.value.map(challenge => ({
      ...challengeQueries.detail(hackathonId.value ?? '', challenge.id ?? ''),
      enabled: !!hackathonId.value && !!challenge.id,
    })),
  ),
})

// Track selected challenge and get its description from prefetched data
const selectedIndex = ref<number | null>(null)
const selectedDescription = computed(() =>
  selectedIndex.value !== null ? detailsQueries.value[selectedIndex.value]?.data?.description : null,
)
</script>

<template>
  <section>
    <!-- Section header -->
    <header class="h-18 flex items-center justify-center bg-linear-to-r from-[#ffc1aa] via-[#ff5b84] to-[#ffc1aa]">
      <h2 class="font-['Zalando_Sans_Expanded'] text-black text-center m-0 text-2xl">
        CHALLENGES
      </h2>
    </header>

    <div v-if="isLoading">
      Loading challenges...
    </div>

    <div
      v-else
      class="flex flex-col items-center p-8 lg:py-16 lg:px-28 mx-auto lg:max-w-300"
    >
      <!-- Challenge cards -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 w-full">
        <PortalChallengesCard
          v-for="(challenge, index) in challenges"
          :key="challenge.id!"
          :title="challenge.title ?? ''"
          :team-count="0"
          :selected="selectedIndex === index"
          @select="selectedIndex = index"
        />
      </div>

      <!-- Challenge details -->
      <div class="mt-8 lg:mt-16 w-full text-left">
        <h3 class="font-['Zalando_Sans_Expanded'] text-xl lg:text-[32px] font-bold mb-2">
          {{ selectedIndex !== null ? 'Challenge Statement' : 'Explore' }}
        </h3>
        <p
          v-if="selectedIndex !== null"
          class="font-['Raleway'] text-base lg:text-xl mb-4"
        >
          Sponsor Name
        </p>
        <p class="font-['Raleway'] text-base lg:text-xl whitespace-pre-line">
          {{ selectedDescription ?? `Each sponsor has designed a unique challenge for this hackathon, reflecting their goals and areas of focus. Browse through the challenges to find the one that excites you the most or best matches your skills!

The dashboard updates live. Feel free to use this dashboard to help you choose your challenge statement!` }}
        </p>
      </div>
    </div>
  </section>
</template>
