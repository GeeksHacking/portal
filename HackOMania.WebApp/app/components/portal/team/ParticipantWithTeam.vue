<script setup lang="ts">
import { ref, computed } from 'vue'
import type { HackOManiaApiEndpointsParticipantsHackathonTeamsGetMineResponse } from '~/api-client/models'

const MAX_TEAM_SIZE = 5

const props = defineProps<{
  team: HackOManiaApiEndpointsParticipantsHackathonTeamsGetMineResponse
  hackathonId: string | null
}>()

const hackathonIdRef = computed(() => props.hackathonId)

// State
const isCopied = ref(false)
const showLeaveConfirm = ref(false)

// Computed
const memberCount = computed(() => props.team.members?.length ?? 0)

// Mutations
const leaveTeamMutation = useLeaveTeam(hackathonIdRef)

// Handlers
function handleLeaveTeam() {
  leaveTeamMutation.mutate(undefined, {
    onSuccess() {
      showLeaveConfirm.value = false
    },
    onError(error) {
      console.error('Failed to leave team:', error)
    },
  })
}

function copyInviteUrl() {
  if (props.team.joinCode && props.hackathonId) {
    const url = new URL(`/${props.hackathonId}/team/`, window.location.origin)
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
  <div class="w-full lg:max-w-3xl text-black">
    <!-- Team Name -->
    <h3 class="font-['Zalando_Sans_Expanded'] text-xl lg:text-3xl font-bold mb-2 uppercase">
      {{ team.name }}
    </h3>

    <!-- Team Description -->
    <p class="font-['Raleway'] text-base lg:text-xl text-black/80 mb-6 lg:mb-8">
      {{ team.description || 'No description yet' }}
    </p>

    <!-- Team Members Container -->
    <div class="rounded-xl shadow-md overflow-hidden border border-black">
      <!-- Header -->
      <div class="flex items-center justify-between px-5 py-2 lg:py-3 rounded-b-xl bg-linear-to-r from-[#4BBC7D]/50 via-[#7DFFA9]/50 to-[#AAFFEE]/50">
        <h4 class="font-['Zalando_Sans_Expanded'] text-2xl uppercase font-bold">
          Your Team
        </h4>
        <span class="font-['Raleway'] text-base lg:text-lg">
          {{ memberCount }}/{{ MAX_TEAM_SIZE }} Members
        </span>
      </div>
      <!-- Members List -->
      <div class="px-4 py-2 lg:px-6 divide-y divide-black/10">
        <div
          v-for="(_, index) in MAX_TEAM_SIZE"
          :key="team.members?.[index]?.userId ?? `empty-${index}`"
          class="py-2 lg:py-3"
        >
          <template v-if="team.members?.[index]">
            <p class="font-['Raleway'] text-base lg:text-2xl truncate">
              {{ team.members[index].name }}
              <span
                v-if="team.members[index].isCurrentUser"
                class="text-black/60"
              >(you)</span>
            </p>
          </template>
          <template v-else>
            <p class="font-['Raleway'] text-base lg:text-2xl text-black/30">
              Empty slot
            </p>
          </template>
        </div>
      </div>
    </div>

    <!-- Actions Row -->
    <div class="flex justify-between items-center mt-4 lg:mt-5">
      <!-- Invite Members Link -->
      <button
        class="flex items-center gap-1.5 font-['Raleway'] text-sm lg:text-base text-black hover:text-black/70 underline"
        @click="copyInviteUrl"
      >
        <UIcon :name="isCopied ? 'i-heroicons-check' : 'i-heroicons-clipboard'" class="size-4" />
        {{ isCopied ? 'Link copied!' : 'Invite members' }}
      </button>

      <!-- Leave Team Link -->
      <button
        class="font-['Raleway'] text-sm lg:text-base text-red-600 hover:text-red-700 underline"
        @click="showLeaveConfirm = true"
      >
        Leave Team
      </button>
    </div>

    <!-- Leave Team Confirmation Modal -->
    <UModal v-model:open="showLeaveConfirm">
      <template #content>
        <div class="p-6">
          <h3 class="font-['Zalando_Sans_Expanded'] text-lg font-bold mb-2">
            Leave Team
          </h3>
          <p class="font-['Raleway'] text-base mb-6">
            Are you sure you want to leave this team?
          </p>
          <div class="flex gap-3 justify-end font-['Raleway']">
            <UButton
              color="neutral"
              variant="outline"
              @click="showLeaveConfirm = false"
            >
              Cancel
            </UButton>
            <UButton
              color="error"
              :loading="leaveTeamMutation.isPending.value"
              @click="handleLeaveTeam"
            >
              Leave team
            </UButton>
          </div>
        </div>
      </template>
    </UModal>
  </div>
</template>
