<script setup lang="ts">
import { useQuery } from '@tanstack/vue-query'
import { computed } from 'vue'

// Fetch hackathons and extract the first hackathon's ID
const { data: hackathonsData } = useQuery(hackathonQueries.list)
const hackathonId = computed(() => hackathonsData.value?.hackathons?.[0]?.id ?? null)

// Fetch current user's team
const { data: teamData, isLoading } = useQuery(
  computed(() => ({
    ...teamQueries.me(hackathonId.value ?? ''),
    enabled: !!hackathonId.value,
  })),
)
</script>

<template>
  <section>
    <!-- Section header -->
    <header class="h-18 flex items-center justify-center bg-linear-to-r from-[#ffc1aa] via-[#ff5b84] to-[#ffc1aa]">
      <h2 class="font-['Zalando_Sans_Expanded'] text-black text-center m-0 text-2xl">
        TEAM
      </h2>
    </header>

    <div
      v-if="isLoading"
      class="py-16 px-28"
    >
      Loading team info...
    </div>

    <div
      v-else
      class="py-16 px-28"
    >
      <PortalTeamView
        :team="teamData"
        :hackathon-id="hackathonId"
      />
    </div>
  </section>
</template>
