<script setup lang="ts">
import { ref, computed } from 'vue'
import type { HackOManiaApiEndpointsParticipantsHackathonTeamsGetMineResponse } from '~/api-client/models'

const props = defineProps<{
  team: HackOManiaApiEndpointsParticipantsHackathonTeamsGetMineResponse
  hackathonId: string | null
}>()

const hackathonIdRef = computed(() => props.hackathonId)

// State
const isLeavingTeam = ref(false)
const isCopied = ref(false)

// Mutations
const leaveTeamMutation = useLeaveTeam(hackathonIdRef)

// Computed
const memberCount = computed(() => props.team.members?.length ?? 0)

// Handlers
function handleLeaveTeam() {
  leaveTeamMutation.mutate(undefined, {
    onSuccess() {
      isLeavingTeam.value = false
    },
    onError(error) {
      console.error('Failed to leave team:', error)
    },
  })
}

function copyInviteUrl() {
  if (props.team.joinCode) {
    const url = new URL(window.location.origin)
    url.searchParams.set('joinCode', props.team.joinCode)
    navigator.clipboard.writeText(url.toString())
    isCopied.value = true
    setTimeout(() => {
      isCopied.value = false
    }, 2000)
  }
}
</script>

<template>
  <div>
    <!-- Team Name -->
    <div class="mb-6 md:mb-8">
      <h3 class="font-['Zalando_Sans_Expanded'] text-2xl md:text-[32px] font-bold">
        {{ team.name }}
      </h3>
    </div>

    <!-- Team Description -->
    <div class="mb-6 md:mb-8">
      <h4 class="font-['Raleway'] text-base md:text-lg font-semibold mb-1 md:mb-2">
        Description
      </h4>
      <p class="font-['Raleway'] text-base md:text-lg text-white/80">
        {{ team.description || 'No description yet' }}
      </p>
    </div>

    <!-- Invite URL -->
    <div class="mb-6 md:mb-8">
      <button
        class="flex items-center gap-2 px-3 py-1.5 md:px-4 md:py-2 bg-white text-black font-['Raleway'] text-sm md:text-base font-semibold rounded hover:bg-gray-200"
        @click="copyInviteUrl"
      >
        <template v-if="isCopied">
          <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 md:w-5 md:h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="20 6 9 17 4 12" />
          </svg>
          Copied!
        </template>
        <template v-else>
          <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4 md:w-5 md:h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <rect x="9" y="9" width="13" height="13" rx="2" ry="2" />
            <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
          </svg>
          Get Invite URL
        </template>
      </button>
      <p class="font-['Raleway'] text-xs md:text-sm text-white/60 mt-1 md:mt-2">
        Copy and share with teammates to let them join your team
      </p>
    </div>

    <!-- Team Members -->
    <div class="mb-6 md:mb-8">
      <h4 class="font-['Raleway'] text-base md:text-lg font-semibold mb-2 md:mb-3">
        Members ({{ memberCount }}/4)
      </h4>
      <div class="space-y-2">
        <div
          v-for="member in team.members"
          :key="member.userId!"
          class="flex items-center gap-2 md:gap-3 bg-white/5 rounded px-3 py-2 md:px-4 md:py-3"
        >
          <div class="w-8 h-8 md:w-10 md:h-10 rounded-full bg-white/20 flex items-center justify-center font-bold text-sm md:text-base">
            {{ member.name?.charAt(0)?.toUpperCase() || '?' }}
          </div>
          <div class="flex-1 min-w-0">
            <p class="font-['Raleway'] font-semibold text-sm md:text-base truncate">
              {{ member.name }}
              <span
                v-if="member.isCurrentUser"
                class="text-xs md:text-sm text-white/60"
              >(you)</span>
            </p>
            <p class="font-['Raleway'] text-xs md:text-sm text-white/60 truncate">
              {{ member.email }}
            </p>
          </div>
        </div>
      </div>
    </div>

    <!-- Leave Team -->
    <div class="pt-6 md:pt-8 border-t border-white/20">
      <template v-if="isLeavingTeam">
        <p class="font-['Raleway'] text-base md:text-lg mb-2 md:mb-3">
          Are you sure you want to leave this team?
        </p>
        <div class="flex gap-2 md:gap-3">
          <button
            class="px-3 py-1.5 md:px-4 md:py-2 text-sm md:text-base bg-red-600 text-white rounded hover:bg-red-700"
            :disabled="leaveTeamMutation.isPending.value"
            @click="handleLeaveTeam"
          >
            {{ leaveTeamMutation.isPending.value ? 'Leaving...' : 'Yes, leave team' }}
          </button>
          <button
            class="px-3 py-1.5 md:px-4 md:py-2 text-sm md:text-base border border-white rounded hover:bg-white/10"
            @click="isLeavingTeam = false"
          >
            Cancel
          </button>
        </div>
      </template>
      <template v-else>
        <button
          class="text-red-400 hover:text-red-300 font-['Raleway'] text-sm md:text-base"
          @click="isLeavingTeam = true"
        >
          Leave team
        </button>
      </template>
    </div>
  </div>
</template>
