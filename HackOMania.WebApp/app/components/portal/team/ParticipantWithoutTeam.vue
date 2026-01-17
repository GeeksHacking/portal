<script setup lang="ts">
import { ref, computed } from 'vue'

const props = defineProps<{
  hackathonId: string | null
  initialJoinCode?: string | null
}>()

const hackathonIdRef = computed(() => props.hackathonId)
const toast = useToast()

// Form inputs
const newTeamName = ref('')
const newTeamDescription = ref('')
const joinCode = ref(props.initialJoinCode ?? '')

// Mutations
const createTeamMutation = useCreateTeam(hackathonIdRef)
const joinTeamMutation = useJoinTeamByCode()

// Handlers
function handleCreateTeam() {
  if (!newTeamName.value.trim()) return
  createTeamMutation.mutate({
    name: newTeamName.value.trim(),
    description: newTeamDescription.value.trim() || undefined,
  }, {
    onSuccess() {
      newTeamName.value = ''
      newTeamDescription.value = ''
    },
    onError() {
      toast.add({
        title: 'Failed to create team',
        description: 'Please try again.',
        color: 'error',
      })
    },
  })
}

function handleJoinTeam() {
  if (!joinCode.value.trim()) return
  joinTeamMutation.mutate(joinCode.value.trim(), {
    onSuccess() {
      joinCode.value = ''
    },
    onError() {
      toast.add({
        title: 'Invalid invite code',
        description: 'Please check and try again.',
        color: 'error',
      })
    },
  })
}
</script>

<template>
  <div class="w-full grid grid-cols-1 md:grid-cols-2 gap-4 md:gap-8">
    <!-- Create Team Form -->
    <div class="flex flex-col rounded-xl shadow-md overflow-hidden">
      <div class="px-3 py-2 md:px-4 md:py-3 bg-[#7DFFA9] text-center rounded-b-xl">
        <h3 class="font-['Zalando_Sans_Expanded'] text-base md:text-lg lg:text-xl text-black uppercase">
          Create Team
        </h3>
      </div>
      <div class="space-y-4 md:space-y-6 flex-1 p-4 md:p-6 bg-white/5">
        <div>
          <label class="block font-['Raleway'] text-base md:text-lg font-semibold mb-1 md:mb-2">Team Name *</label>
          <input
            v-model="newTeamName"
            type="text"
            class="w-full bg-transparent border border-black/20 rounded px-3 py-2 md:px-4 md:py-3 font-['Raleway'] text-sm md:text-base focus:outline-none focus:border-black/40"
            placeholder="Enter your team name"
            @keyup.enter="handleCreateTeam"
          >
        </div>
        <div>
          <label class="block font-['Raleway'] text-base md:text-lg font-semibold mb-1 md:mb-2">Description (optional)</label>
          <textarea
            v-model="newTeamDescription"
            rows="3"
            class="w-full bg-transparent border border-black/20 rounded px-3 py-2 md:px-4 md:py-3 font-['Raleway'] text-sm md:text-base focus:outline-none focus:border-black/40"
            placeholder="Tell us about your team..."
          />
        </div>
        <button
          class="w-full py-2 md:py-3 bg-linear-to-r from-[#4BBC7D] via-[#7DFFA9] to-[#AAFFEE] text-black rounded font-['Zalando_Sans_Expanded'] text-base md:text-lg hover:opacity-90 disabled:opacity-50 disabled:cursor-not-allowed"
          :disabled="!newTeamName.trim() || createTeamMutation.isPending.value"
          @click="handleCreateTeam"
        >
          {{ createTeamMutation.isPending.value ? 'Creating...' : 'Create Team' }}
        </button>
      </div>
    </div>

    <!-- Join Team Form -->
    <div class="flex flex-col rounded-xl shadow-md overflow-hidden">
      <div class="px-3 py-2 md:px-4 md:py-3 bg-[#7DFFA9] text-center rounded-b-xl">
        <h3 class="font-['Zalando_Sans_Expanded'] text-base md:text-lg lg:text-xl text-black uppercase">
          Join Team
        </h3>
      </div>
      <div class="flex flex-col flex-1 p-4 md:p-6 bg-white/5">
        <div class="flex-1">
          <label class="block font-['Raleway'] text-base md:text-lg font-semibold mb-1 md:mb-2">Invite Code</label>
          <input
            v-model="joinCode"
            type="text"
            class="w-full bg-transparent border border-black/20 rounded px-3 py-2 md:px-4 md:py-3 text-xs md:text-sm focus:outline-none focus:border-black/40 font-mono"
            placeholder="Enter the team's invite code"
            @keyup.enter="handleJoinTeam"
          >
        </div>
        <button
          class="w-full py-2 md:py-3 bg-linear-to-r from-[#4BBC7D] via-[#7DFFA9] to-[#AAFFEE] text-black rounded font-['Zalando_Sans_Expanded'] text-base md:text-lg hover:opacity-90 disabled:opacity-50 disabled:cursor-not-allowed mt-4 md:mt-6"
          :disabled="!joinCode.trim() || joinTeamMutation.isPending.value"
          @click="handleJoinTeam"
        >
          {{ joinTeamMutation.isPending.value ? 'Joining...' : 'Join Team' }}
        </button>
      </div>
    </div>
  </div>
</template>
