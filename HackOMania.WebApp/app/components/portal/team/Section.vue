<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import { computed, nextTick, ref, watch } from 'vue'

const route = useRoute()
const hackathon = useRouteHackathon()
const hackathonId = useResolvedHackathonId()

// Get joinCode from URL query param
const joinCodeParam = computed(() => {
  const code = route.query.joinCode
  return typeof code === 'string' ? code : null
})

// Template ref for scrolling
const sectionRef = ref<HTMLElement | null>(null)

// Fetch participation status
const { data: statusData, isLoading: isLoadingStatus } = useQuery(
  computed(() => ({
    ...hackathonQueries.status(hackathonId.value ?? ''),
    enabled: !!hackathonId.value,
  })),
)

// Fetch current user's team
const { data: teamData, isLoading: isLoadingTeam } = useQuery(
  computed(() => ({
    ...teamQueries.me(hackathonId.value ?? ''),
    enabled: !!hackathonId.value && !!statusData.value?.isParticipant,
  })),
)

const isLoading = computed(() => isLoadingStatus.value || isLoadingTeam.value)
const isParticipant = computed(() => !!statusData.value?.isParticipant)
const hasTeam = computed(() => !!teamData.value?.id)

// Check if hackathon has started
const eventStartDate = computed(() => hackathon.value?.eventStartDate ?? null)
const hasHackathonStarted = computed(() => {
  if (!eventStartDate.value)
    return true // If no start date, show content
  return new Date() >= eventStartDate.value
})

// Auto-scroll to team section when user has joinCode param and no team
watch(
  [isLoading, isParticipant, hasTeam, joinCodeParam],
  ([loading, participant, team, code]) => {
    if (!loading && participant && !team && code) {
      nextTick(() => {
        sectionRef.value?.scrollIntoView({ behavior: 'smooth' })
      })
    }
  },
  { immediate: true },
)
</script>

<template>
  <section ref="sectionRef" class="relative z-10">
    <!-- Section header -->
    <header class="h-18 flex items-center justify-center bg-[linear-gradient(to_right,#B4E2FB_0%,#81CAF6_91%,#B4E2FB_100%)] border-y border-y-black" style="border-image: linear-gradient(to right, black, transparent) 1">
      <h2 class="font-['Zalando_Sans_Expanded'] text-black text-center m-0 text-2xl">
        TEAM
      </h2>
    </header>

    <div
      v-if="isLoading"
      class="p-8 lg:py-16 lg:px-28"
    >
      Loading team info...
    </div>

    <div
      v-else
      class="flex flex-col items-center p-8 lg:py-16 lg:px-28 mx-auto lg:max-w-300"
    >
      <!-- Not a participant -->
      <PortalTeamNotParticipant
        v-if="!isParticipant"
        :hackathon-name="hackathon?.name"
      />

      <!-- Participant without a team -->
      <template v-else-if="!hasTeam">
        <p class="text-center font-['Raleway'] text-base md:text-xl mb-8 md:mb-8 lg:mb-16">
          You do not have a team yet. Create or join a team below!
        </p>
        <PortalTeamParticipantWithoutTeam
          :hackathon-id="hackathonId"
          :initial-join-code="joinCodeParam"
        />
      </template>

      <!-- Participant with a team -->
      <PortalTeamParticipantWithTeam
        v-else
        :team="teamData!"
        :hackathon-id="hackathonId"
      />

      <!-- Challenge Statement Section -->
      <PortalTeamChallengeStatement
        v-if="hasTeam && hasHackathonStarted"
        :team-name="teamData!.name!"
        :team-id="teamData!.id!"
        :hackathon-id="hackathonId!"
        :selected-challenge-id="teamData!.challengeId ?? null"
        class="mt-12"
      />
    </div>
  </section>
</template>
