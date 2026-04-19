<script setup lang="ts">
import { useQueries, useQuery } from '@tanstack/vue-query'
import { useLocalStorage } from '@vueuse/core'
import { computed, ref } from 'vue'

const hackathonId = useResolvedHackathonId()

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
const selectedIndex = useLocalStorage<number>('challenge-selected-index', 0, { serializer: { read: v => Number(v), write: v => String(v) } })
const selectedDescription = computed(() =>
  detailsQueries.value[selectedIndex.value]?.data?.description ?? null,
)
const selectedChallengeTitle = computed(() =>
  challenges.value[selectedIndex.value]?.title ?? null,
)

// Sponsor colour gradients: [start, middle, end]
const sponsorColours: Record<string, string[]> = {
  'GovTech': ['#FFC1AA', '#FF5B84', '#FFC1AA'],
  'Ahrefs': ['#B4E2FB', '#81CAF6', '#B4E2FB'],
  'Interledger': ['#4BBC7D', '#7DFFA9', '#AAFFEE'],
  'SP Group': ['#FFF3A0', '#FFD74B', '#FFF3A0'],
}

// Fallback colours by index
const fallbackColours = [
  ['#FFC1AA', '#FF5B84', '#FFC1AA'],
  ['#B4E2FB', '#81CAF6', '#B4E2FB'],
  ['#4BBC7D', '#7DFFA9', '#AAFFEE'],
  ['#FFF3A0', '#FFD74B', '#FFF3A0'],
]

function getSponsorColours(challenge: { sponsor?: string | null, id?: string | null }, index: number) {
  if (challenge.sponsor && sponsorColours[challenge.sponsor]) {
    return sponsorColours[challenge.sponsor]
  }
  return fallbackColours[index % fallbackColours.length]
}

const sponsorLogos: Record<string, { src: string, maxHeight?: string }> = {
  'GovTech': { src: '/logos/govtech-colored-logo.png' },
  'Ahrefs': { src: '/logos/ahrefs-colored-logo.svg', maxHeight: '120px' },
  'Interledger': { src: '/logos/Interledger-Foundation.png' },
  'SP Group': { src: '/logos/SP_Group_Full_Colour_Logo_Horizontal_RGB.png' },
}

const selectedSponsorName = computed(() =>
  challenges.value[selectedIndex.value]?.sponsor || null,
)
const selectedSponsorLogo = computed(() => {
  if (!selectedSponsorName.value)
    return null
  return sponsorLogos[selectedSponsorName.value]?.src ?? null
})
const selectedSponsorLogoMaxHeight = computed(() => {
  if (!selectedSponsorName.value)
    return '200px'
  return sponsorLogos[selectedSponsorName.value]?.maxHeight ?? '200px'
})

// Track card title heights for consistent sizing
const titleHeights = ref<Map<number, number>>(new Map())
const maxTitleHeight = computed(() => {
  const heights = Array.from(titleHeights.value.values())
  return heights.length > 0 ? Math.max(...heights) : 0
})

function onTitleMounted(index: number, height: number) {
  titleHeights.value.set(index, height)
  // Trigger reactivity
  titleHeights.value = new Map(titleHeights.value)
}

// Mobile navigation with slide direction tracking
const mobileIndex = ref(selectedIndex.value)

// Auto-select challenge when loaded (restore from localStorage or default to first)
watch(challenges, (val) => {
  if (val.length === 0)
    return
  if (selectedIndex.value >= val.length) {
    selectedIndex.value = 0
  }
  mobileIndex.value = selectedIndex.value
}, { immediate: true })
const slideDirection = ref<'left' | 'right'>('right')

function mobilePrev() {
  if (challenges.value.length === 0)
    return
  slideDirection.value = 'left'
  mobileIndex.value = (mobileIndex.value - 1 + challenges.value.length) % challenges.value.length
  selectedIndex.value = mobileIndex.value
}
function mobileNext() {
  if (challenges.value.length === 0)
    return
  slideDirection.value = 'right'
  mobileIndex.value = (mobileIndex.value + 1) % challenges.value.length
  selectedIndex.value = mobileIndex.value
}

// Sync mobileIndex when selectedIndex changes (e.g. from desktop grid click)
watch(selectedIndex, (val) => {
  mobileIndex.value = val
})
</script>

<template>
  <section>
    <!-- Section header -->
    <header class="h-18 flex items-center justify-center bg-linear-to-r from-[#ffc1aa] via-[#ff5b84] to-[#ffc1aa] border-y border-y-black" style="border-image: linear-gradient(to right, black, transparent) 1">
      <h2 class="font-['Zalando_Sans_Expanded'] text-black text-center m-0 text-2xl">
        CHALLENGES
      </h2>
    </header>

    <div v-if="isLoading">
      Loading challenges...
    </div>

    <div
      v-else-if="challenges.length === 0"
      class="flex flex-col items-center p-8 lg:py-16 lg:px-28 mx-auto lg:max-w-300"
    >
      <p class="font-['Raleway'] text-base lg:text-xl text-center">
        Challenge statements will be released soon!
      </p>
    </div>

    <div
      v-else
      class="flex flex-col items-center p-8 lg:py-16 lg:px-28 mx-auto lg:max-w-300"
    >
      <!-- Challenge cards: single card with arrows on mobile -->
      <div class="relative w-full max-w-sm mx-auto md:hidden">
        <button
          class="absolute left-2 top-1/2 -translate-y-1/2 z-10 size-9 flex items-center justify-center rounded-full bg-white/90 shadow-md backdrop-blur-sm active:scale-95 transition-transform"
          @click="mobilePrev"
        >
          <UIcon name="i-lucide-chevron-left" class="size-5" />
        </button>
        <div class="relative overflow-hidden p-1">
          <Transition :name="`slide-${slideDirection}`" mode="out-in">
            <PortalChallengesCard
              v-if="challenges[mobileIndex]"
              :key="mobileIndex"
              :title="challenges[mobileIndex].sponsor || (challenges[mobileIndex].title ?? '')"
              :team-count="challenges[mobileIndex].teamCount ?? 0"
              :selected="true"
              :colours="getSponsorColours(challenges[mobileIndex], mobileIndex)"
              @select="selectedIndex = mobileIndex"
            />
          </Transition>
        </div>
        <button
          class="absolute right-2 top-1/2 -translate-y-1/2 z-10 size-9 flex items-center justify-center rounded-full bg-white/90 shadow-md backdrop-blur-sm active:scale-95 transition-transform"
          @click="mobileNext"
        >
          <UIcon name="i-lucide-chevron-right" class="size-5" />
        </button>
      </div>
      <div class="hidden md:grid md:grid-cols-2 lg:grid-cols-4 gap-4 w-full">
        <PortalChallengesCard
          v-for="(challenge, index) in challenges"
          :key="challenge.id!"
          :title="challenge.sponsor || (challenge.title ?? '')"
          :team-count="challenge.teamCount ?? 0"
          :selected="selectedIndex === index"
          :title-height="maxTitleHeight > 0 ? maxTitleHeight : undefined"
          :colours="getSponsorColours(challenge, index)"
          @select="selectedIndex = index"
          @title-mounted="(height) => onTitleMounted(index, height)"
        />
      </div>

      <!-- Challenge details -->
      <div class="mt-8 lg:mt-16 w-full text-left flex flex-col-reverse md:flex-row gap-8">
        <div class="flex-1 md:flex-[2] min-h-[320px] md:min-h-0 overflow-y-auto">
          <h3
            v-if="selectedChallengeTitle"
            class="font-['Zalando_Sans_Expanded'] text-xl lg:text-[32px] font-bold mb-4"
          >
            {{ selectedChallengeTitle }}
          </h3>
          <p class="font-['Zalando_Sans_Expanded'] text-lg lg:text-2xl mb-2">
            Challenge Statement
          </p>
          <p class="font-['Raleway'] text-base lg:text-xl whitespace-pre-line">
            {{ selectedDescription ?? `Each sponsor has designed a unique challenge for this hackathon, reflecting their goals and areas of focus. Browse through the challenges to find the one that excites you the most or best matches your skills!

The dashboard updates live. Feel free to use this dashboard to help you choose your challenge statement!` }}
          </p>
        </div>
        <!-- Sponsor Logo -->
        <div
          v-if="selectedSponsorLogo"
          class="flex items-center justify-center h-[120px] md:h-auto md:flex-1"
        >
          <img
            :src="selectedSponsorLogo"
            :alt="selectedSponsorName ?? 'Sponsor logo'"
            class="max-w-full h-full md:h-auto object-contain"
            :style="{ maxHeight: selectedSponsorLogoMaxHeight }"
          >
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.slide-right-enter-active,
.slide-right-leave-active,
.slide-left-enter-active,
.slide-left-leave-active {
  transition: transform 0.25s ease, opacity 0.25s ease;
}

.slide-right-enter-from {
  transform: translateX(100%);
  opacity: 0;
}
.slide-right-leave-to {
  transform: translateX(-100%);
  opacity: 0;
}

.slide-left-enter-from {
  transform: translateX(-100%);
  opacity: 0;
}
.slide-left-leave-to {
  transform: translateX(100%);
  opacity: 0;
}
</style>
