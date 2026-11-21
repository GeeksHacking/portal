<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsParticipantsHackathonTeamsCreateEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsTeamsJoinByCodeEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { computed, nextTick, onMounted, ref } from 'vue'

/**
 * No team yet: start one or join one with an invite code. An invite link's code arrives
 * prefilled, and then the join form comes first and gets focus.
 */
const props = defineProps<{
  hackathonId: string | null
  initialJoinCode?: string | null
}>()

const { refresh } = useMyTeamCache(() => props.hackathonId)

const newTeamName = ref('')
const newTeamDescription = ref('')
const joinCode = ref(props.initialJoinCode ?? '')
const createError = ref('')
const joinError = ref('')

const createTeamMutation = useGeeksHackingPortalApiEndpointsParticipantsHackathonTeamsCreateEndpoint()
const joinTeamMutation = useGeeksHackingPortalApiEndpointsParticipantsTeamsJoinByCodeEndpoint()

const fromInvite = computed(() => !!props.initialJoinCode)

async function handleCreateTeam() {
  createError.value = ''
  if (!newTeamName.value.trim()) {
    createError.value = 'Give your team a name.'
    return
  }
  if (!props.hackathonId)
    return
  try {
    await createTeamMutation.mutateAsync({
      path: { hackathonId: props.hackathonId },
      body: {
        name: newTeamName.value.trim(),
        description: newTeamDescription.value.trim() || undefined,
      },
    })
    await refresh()
  }
  catch (error) {
    createError.value = getApiErrorMessage(error, 'We couldn\'t create the team. Try again.')
  }
}

async function handleJoinTeam() {
  joinError.value = ''
  if (!joinCode.value.trim()) {
    joinError.value = 'Enter the invite code your team shared with you.'
    return
  }
  try {
    await joinTeamMutation.mutateAsync({ body: { joinCode: joinCode.value.trim() } })
    await refresh()
  }
  catch (error) {
    joinError.value = getApiErrorMessage(error, 'That invite code didn\'t work. Check it and try again.')
  }
}

const joinInput = ref<HTMLElement | null>(null)
onMounted(async () => {
  if (!fromInvite.value)
    return
  await nextTick()
  joinInput.value?.querySelector('input')?.focus()
})
</script>

<template>
  <div class="no-team">
    <div class="no-team__intro">
      <h2 class="no-team__title">
        You don't have a team yet
      </h2>
      <p class="no-team__lead">
        Start a team and invite up to four others, or join a team with the invite code they shared.
      </p>
    </div>

    <div
      class="no-team__grid"
      :class="{ 'no-team__grid--invite': fromInvite }"
    >
      <!-- start a team -->
      <form
        class="no-team__card"
        novalidate
        aria-labelledby="create-team-title"
        @submit.prevent="handleCreateTeam"
      >
        <h3
          id="create-team-title"
          class="no-team__card-title"
        >
          Start a team
        </h3>
        <GFormField
          label="Team name"
          required
          :error="createError"
        >
          <GInput
            v-model="newTeamName"
            autocomplete="off"
          />
        </GFormField>
        <GFormField
          label="Description"
          hint="Optional. What you're hoping to build, or who you're looking for."
        >
          <GTextarea
            v-model="newTeamDescription"
            :rows="3"
          />
        </GFormField>
        <GButton
          type="submit"
          :variant="fromInvite ? 'secondary' : 'primary'"
          icon="material-symbols:group-add-rounded"
          label="Create team"
          :loading="createTeamMutation.isPending.value"
          loading-label="Creating team"
          class="self-start"
        />
      </form>

      <!-- join a team -->
      <form
        class="no-team__card"
        novalidate
        aria-labelledby="join-team-title"
        @submit.prevent="handleJoinTeam"
      >
        <h3
          id="join-team-title"
          class="no-team__card-title"
        >
          Join a team
        </h3>
        <p
          v-if="fromInvite"
          class="no-team__note"
        >
          You opened an invite link, so its code is filled in for you.
        </p>
        <div ref="joinInput">
          <GFormField
            label="Invite code"
            required
            :error="joinError"
            hint="Ask a teammate for their team's invite link or code."
          >
            <GInput
              v-model="joinCode"
              autocomplete="off"
              spellcheck="false"
              class="no-team__code"
            />
          </GFormField>
        </div>
        <GButton
          type="submit"
          :variant="fromInvite ? 'primary' : 'secondary'"
          icon="material-symbols:login-rounded"
          label="Join team"
          :loading="joinTeamMutation.isPending.value"
          loading-label="Joining team"
          class="self-start"
        />
      </form>
    </div>
  </div>
</template>

<style scoped>
.no-team {
  display: flex;
  flex-direction: column;
  gap: 20px;
}
.no-team__intro {
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.no-team__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: clamp(24px, 3vw, 30px);
  font-weight: 700;
  line-height: 1.15;
}
.no-team__lead {
  max-width: 60ch;
  margin: 0;
  color: var(--gh-text-muted);
}
.no-team__grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 16px;
}
@media (min-width: 768px) {
  .no-team__grid { grid-template-columns: 1fr 1fr; }
}
/* arriving from an invite: the join form comes first */
.no-team__grid--invite > :last-child {
  order: -1;
}
.no-team__card {
  display: flex;
  flex-direction: column;
  gap: 16px;
  min-width: 0;
  padding: 20px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.no-team__card-title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 20px;
  font-weight: 700;
}
.no-team__note {
  margin: -8px 0 0;
  font-size: 14px;
  color: var(--gh-text-muted);
}
.no-team__code :deep(input) {
  font-family: var(--font-mono);
  letter-spacing: 0.04em;
}
</style>
