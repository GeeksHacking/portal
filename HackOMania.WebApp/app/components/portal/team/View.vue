<script setup lang="ts">
import { ref, computed } from 'vue'
import type { HackOManiaApiEndpointsParticipantsHackathonTeamsGetMineResponse } from '~/api-client/models'

const props = defineProps<{
  team: HackOManiaApiEndpointsParticipantsHackathonTeamsGetMineResponse | null | undefined
  hackathonId: string | null
}>()

const hackathonIdRef = computed(() => props.hackathonId)
const teamIdRef = computed(() => props.team?.id ?? null)

// State
const isLeavingTeam = ref(false)
const isCreatingTeam = ref(true) // Default to create team view
const isEditingName = ref(false)
const isEditingDescription = ref(false)

// Form inputs
const newTeamName = ref('')
const newTeamDescription = ref('')
const joinCode = ref('')
const editedTeamName = ref('')
const editedTeamDescription = ref('')

// Mutations
const createTeamMutation = useCreateTeam(hackathonIdRef)
const updateTeamMutation = useUpdateTeam(hackathonIdRef, teamIdRef)
const leaveTeamMutation = useLeaveTeam(hackathonIdRef)
const joinTeamMutation = useJoinTeamByCode()

// Computed
const hasTeam = computed(() => !!props.team?.id)
const memberCount = computed(() => props.team?.members?.length ?? 0)

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
  })
}

function handleJoinTeam() {
  if (!joinCode.value.trim()) return
  joinTeamMutation.mutate(joinCode.value.trim(), {
    onSuccess() {
      joinCode.value = ''
    },
  })
}

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

function startEditingName() {
  editedTeamName.value = props.team?.name ?? ''
  isEditingName.value = true
}

function saveTeamName() {
  if (!editedTeamName.value.trim()) return
  updateTeamMutation.mutate({ name: editedTeamName.value.trim() }, {
    onSuccess() {
      isEditingName.value = false
    },
  })
}

function startEditingDescription() {
  editedTeamDescription.value = props.team?.description ?? ''
  isEditingDescription.value = true
}

function saveTeamDescription() {
  updateTeamMutation.mutate({ description: editedTeamDescription.value.trim() }, {
    onSuccess() {
      isEditingDescription.value = false
    },
  })
}

function copyJoinCode() {
  if (props.team?.joinCode) {
    navigator.clipboard.writeText(props.team.joinCode)
  }
}
</script>

<template>
  <!-- IN A TEAM -->
  <div v-if="hasTeam">
    <div class="bg-white rounded-lg p-8 shadow-sm">
      <!-- Team Header -->
      <div class="flex items-center justify-between mb-6">
        <div class="flex-1">
          <!-- Editable Team Name -->
          <div v-if="isEditingName" class="flex items-center gap-2">
            <input
              v-model="editedTeamName"
              type="text"
              class="font-['Zalando_Sans_Expanded'] text-3xl font-bold border-b-2 border-[#FF5B84] outline-none bg-transparent"
              @keyup.enter="saveTeamName"
              @keyup.escape="isEditingName = false"
            >
            <button
              class="text-[#FF5B84] hover:text-[#FF5B84]/80"
              :disabled="updateTeamMutation.isPending.value"
              @click="saveTeamName"
            >
              {{ updateTeamMutation.isPending.value ? 'Saving...' : 'Save' }}
            </button>
            <button
              class="text-gray-500 hover:text-gray-700"
              @click="isEditingName = false"
            >
              Cancel
            </button>
          </div>
          <h3
            v-else
            class="font-['Zalando_Sans_Expanded'] text-3xl font-bold cursor-pointer hover:text-[#FF5B84]"
            @click="startEditingName"
          >
            {{ team?.name }}
            <span class="text-sm text-gray-400 ml-2">(click to edit)</span>
          </h3>
        </div>
        <div class="text-right">
          <span class="font-['Raleway'] text-lg text-gray-600">{{ memberCount }} member{{ memberCount !== 1 ? 's' : '' }}</span>
        </div>
      </div>

      <!-- Team Description -->
      <div class="mb-6">
        <h4 class="font-['Zalando_Sans_Expanded'] text-lg mb-2">Description</h4>
        <div v-if="isEditingDescription" class="flex flex-col gap-2">
          <textarea
            v-model="editedTeamDescription"
            class="font-['Raleway'] border border-gray-300 rounded-lg p-3 outline-none focus:border-[#FF5B84]"
            rows="3"
            @keyup.escape="isEditingDescription = false"
          />
          <div class="flex gap-2">
            <button
              class="bg-[#FF5B84] text-white px-4 py-2 rounded-lg hover:bg-[#FF5B84]/80"
              :disabled="updateTeamMutation.isPending.value"
              @click="saveTeamDescription"
            >
              {{ updateTeamMutation.isPending.value ? 'Saving...' : 'Save' }}
            </button>
            <button
              class="text-gray-500 hover:text-gray-700 px-4 py-2"
              @click="isEditingDescription = false"
            >
              Cancel
            </button>
          </div>
        </div>
        <p
          v-else
          class="font-['Raleway'] text-gray-600 cursor-pointer hover:text-[#FF5B84]"
          @click="startEditingDescription"
        >
          {{ team?.description || 'No description yet. Click to add one.' }}
        </p>
      </div>

      <!-- Join Code -->
      <div class="mb-8 p-4 bg-gray-50 rounded-lg">
        <h4 class="font-['Zalando_Sans_Expanded'] text-lg mb-2">Join Code</h4>
        <div class="flex items-center gap-4">
          <code class="font-mono text-2xl font-bold text-[#FF5B84] bg-white px-4 py-2 rounded border">
            {{ team?.joinCode }}
          </code>
          <button
            class="bg-gray-200 hover:bg-gray-300 px-4 py-2 rounded-lg font-['Raleway']"
            @click="copyJoinCode"
          >
            Copy
          </button>
        </div>
        <p class="font-['Raleway'] text-sm text-gray-500 mt-2">
          Share this code with others to let them join your team.
        </p>
      </div>

      <!-- Team Members -->
      <div class="mb-8">
        <h4 class="font-['Zalando_Sans_Expanded'] text-lg mb-4">Team Members</h4>
        <ul class="space-y-3">
          <li
            v-for="member in team?.members"
            :key="member.userId!"
            class="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
          >
            <div>
              <span class="font-['Raleway'] font-semibold">{{ member.name }}</span>
              <span v-if="member.isCurrentUser" class="ml-2 text-sm text-[#FF5B84]">(You)</span>
            </div>
            <span class="font-['Raleway'] text-sm text-gray-500">{{ member.email }}</span>
          </li>
        </ul>
      </div>

      <!-- Leave Team -->
      <div class="border-t pt-6">
        <div v-if="isLeavingTeam" class="bg-red-50 p-4 rounded-lg">
          <p class="font-['Raleway'] text-red-800 mb-4">
            Are you sure you want to leave this team? If you are the last member, the team will be deleted.
          </p>
          <div class="flex gap-3">
            <button
              class="bg-red-600 text-white px-4 py-2 rounded-lg hover:bg-red-700 font-['Raleway']"
              :disabled="leaveTeamMutation.isPending.value"
              @click="handleLeaveTeam"
            >
              {{ leaveTeamMutation.isPending.value ? 'Leaving...' : 'Yes, Leave Team' }}
            </button>
            <button
              class="bg-gray-200 hover:bg-gray-300 px-4 py-2 rounded-lg font-['Raleway']"
              @click="isLeavingTeam = false"
            >
              Cancel
            </button>
          </div>
          <p v-if="leaveTeamMutation.isError.value" class="text-red-600 font-['Raleway'] mt-2">
            Failed to leave team. Please try again.
          </p>
        </div>
        <button
          v-else
          class="text-red-600 hover:text-red-800 font-['Raleway']"
          @click="isLeavingTeam = true"
        >
          Leave Team
        </button>
      </div>
    </div>
  </div>

  <!-- NOT IN A TEAM -->
  <div v-else>
    <div class="bg-white rounded-lg p-8 shadow-sm">
      <h3 class="font-['Zalando_Sans_Expanded'] text-2xl font-bold mb-6">
        Join or Create a Team
      </h3>

      <!-- Toggle buttons -->
      <div class="flex gap-4 mb-8">
        <button
          class="flex-1 py-3 rounded-lg font-['Raleway'] font-semibold transition-colors"
          :class="isCreatingTeam ? 'bg-[#FF5B84] text-white' : 'bg-gray-100 text-gray-700 hover:bg-gray-200'"
          @click="isCreatingTeam = true"
        >
          Create Team
        </button>
        <button
          class="flex-1 py-3 rounded-lg font-['Raleway'] font-semibold transition-colors"
          :class="!isCreatingTeam ? 'bg-[#FF5B84] text-white' : 'bg-gray-100 text-gray-700 hover:bg-gray-200'"
          @click="isCreatingTeam = false"
        >
          Join Team
        </button>
      </div>

      <!-- Create Team Form -->
      <div v-if="isCreatingTeam">
        <div class="space-y-4">
          <div>
            <label class="block font-['Raleway'] font-semibold mb-2">Team Name *</label>
            <input
              v-model="newTeamName"
              type="text"
              placeholder="Enter your team name"
              class="w-full border border-gray-300 rounded-lg px-4 py-3 outline-none focus:border-[#FF5B84] font-['Raleway']"
            >
          </div>
          <div>
            <label class="block font-['Raleway'] font-semibold mb-2">Description (optional)</label>
            <textarea
              v-model="newTeamDescription"
              placeholder="Tell us about your team"
              rows="3"
              class="w-full border border-gray-300 rounded-lg px-4 py-3 outline-none focus:border-[#FF5B84] font-['Raleway']"
            />
          </div>
          <button
            class="w-full bg-[#FF5B84] text-white py-3 rounded-lg font-['Raleway'] font-semibold hover:bg-[#FF5B84]/80 disabled:opacity-50"
            :disabled="!newTeamName.trim() || createTeamMutation.isPending.value"
            @click="handleCreateTeam"
          >
            {{ createTeamMutation.isPending.value ? 'Creating Team...' : 'Create Team' }}
          </button>
          <p v-if="createTeamMutation.isError.value" class="text-red-600 font-['Raleway']">
            Failed to create team. Please try again.
          </p>
        </div>
      </div>

      <!-- Join Team Form -->
      <div v-else>
        <div class="space-y-4">
          <div>
            <label class="block font-['Raleway'] font-semibold mb-2">Join Code *</label>
            <input
              v-model="joinCode"
              type="text"
              placeholder="Enter the team join code"
              class="w-full border border-gray-300 rounded-lg px-4 py-3 outline-none focus:border-[#FF5B84] font-['Raleway'] font-mono"
            >
          </div>
          <p class="font-['Raleway'] text-gray-500 text-sm">
            Ask your team leader for the join code to join an existing team.
          </p>
          <button
            class="w-full bg-[#FF5B84] text-white py-3 rounded-lg font-['Raleway'] font-semibold hover:bg-[#FF5B84]/80 disabled:opacity-50"
            :disabled="!joinCode.trim() || joinTeamMutation.isPending.value"
            @click="handleJoinTeam"
          >
            {{ joinTeamMutation.isPending.value ? 'Joining Team...' : 'Join Team' }}
          </button>
          <p v-if="joinTeamMutation.isError.value" class="text-red-600 font-['Raleway']">
            Failed to join team. Please check the code and try again.
          </p>
        </div>
      </div>
    </div>
  </div>
</template>
