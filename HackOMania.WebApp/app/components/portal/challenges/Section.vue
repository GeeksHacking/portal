<script setup lang="ts">
import { useQuery, useQueries } from '@tanstack/vue-query'
import { computed, ref } from 'vue'
import sponsorsData from '~/data/sponsors.json'

const hackathonId = useResolvedHackathonId()
const hackathon = useRouteHackathon()

// Check if hackathon has started
const eventStartDate = computed(() => hackathon.value?.eventStartDate ?? null)
const hasHackathonStarted = computed(() => {
  if (!eventStartDate.value) return true // If no start date, show content
  return new Date() >= eventStartDate.value
})

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

type SponsorData = {
  'sponsor': string
  'sponsor-logo': string
  'colours': string[]
}

const typedSponsorsData = sponsorsData as Record<string, SponsorData>

// Get sponsor colours for a challenge
const getSponsorColours = (challengeId: string | undefined) => {
  if (!challengeId) return null
  return typedSponsorsData[challengeId]?.colours ?? null
}

// Get sponsor data for selected challenge
const selectedSponsor = computed(() => {
  if (selectedIndex.value === null) return null
  const challengeId = challenges.value[selectedIndex.value]?.id
  if (!challengeId) return null
  return typedSponsorsData[challengeId] ?? null
})

const selectedSponsorName = computed(() => selectedSponsor.value?.sponsor ?? null)
const selectedSponsorLogo = computed(() => selectedSponsor.value?.['sponsor-logo'] ?? null)

// Track card title heights for consistent sizing
const titleHeights = ref<Map<number, number>>(new Map())
const maxTitleHeight = computed(() => {
  const heights = Array.from(titleHeights.value.values())
  return heights.length > 0 ? Math.max(...heights) : 0
})

const onTitleMounted = (index: number, height: number) => {
  titleHeights.value.set(index, height)
  // Trigger reactivity
  titleHeights.value = new Map(titleHeights.value)
}
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

    <!-- Hackathon hasn't started yet -->
    <div
      v-else-if="!hasHackathonStarted"
      class="flex flex-col items-center p-8 lg:py-16 lg:px-28 mx-auto lg:max-w-300"
    >
      <p class="font-['Raleway'] text-base lg:text-xl text-center">
        Challenges will be revealed when the hackathon begins.
      </p>
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
          :title-height="maxTitleHeight > 0 ? maxTitleHeight : undefined"
          :colours="getSponsorColours(challenge.id)"
          @select="selectedIndex = index"
          @title-mounted="(height) => onTitleMounted(index, height)"
        />
      </div>

      <!-- Challenge details -->
      <div class="mt-8 lg:mt-16 w-full text-left flex flex-col lg:flex-row gap-8">
        <div class="flex-1">
          <h3 class="font-['Zalando_Sans_Expanded'] text-xl lg:text-[32px] font-bold mb-2">
            {{ selectedIndex !== null ? 'Challenge Statement' : 'Explore' }}
          </h3>
          <p
            v-if="selectedIndex !== null && selectedSponsorName"
            class="font-['Raleway'] text-base lg:text-xl mb-4"
          >
            {{ selectedSponsorName }}
          </p>
          <p class="font-['Raleway'] text-base lg:text-xl whitespace-pre-line">
            {{ selectedDescription ?? `Each sponsor has designed a unique challenge for this hackathon, reflecting their goals and areas of focus. Browse through the challenges to find the one that excites you the most or best matches your skills!

The dashboard updates live. Feel free to use this dashboard to help you choose your challenge statement!` }}
          </p>
        </div>
        <!-- Sponsor Logo -->
        <div
          v-if="selectedIndex !== null && selectedSponsorLogo"
          class="w-full lg:w-[386px] h-[209px] shrink-0 rounded-lg overflow-hidden"
        >
          <img
            :src="selectedSponsorLogo"
            :alt="selectedSponsorName ?? 'Sponsor logo'"
            class="w-full h-full object-cover"
          >
        </div>
      </div>
    </div>
  </section>
</template>
