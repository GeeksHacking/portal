<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import { computed, ref, watch, nextTick } from 'vue'

const route = useRoute()
const hackathonId = useCurrentHackathonId()
const hackathon = useCurrentHackathon()

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
  <section ref="sectionRef">
    <!-- Section header -->
    <header class="h-18 flex items-center justify-center bg-linear-to-r from-[#4BBC7D] via-[#7DFFA9] to-[#AAFFEE]">
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
        <PortalTeamParticipantWithoutTeam :hackathon-id="hackathonId" :initial-join-code="joinCodeParam" />
      </template>

      <!-- Participant with a team -->
      <PortalTeamParticipantWithTeam
        v-else
        :team="teamData!"
        :hackathon-id="hackathonId"
      />
    </div>
  </section>
</template>
