<script setup lang="ts">
import type { GeeksHackingPortalApiEndpointsParticipantsHackathonTeamsGetMineEndpointStatus200 } from '@geekshacking/portal-sdk'
import {
  useGeeksHackingPortalApiEndpointsParticipantsHackathonTeamsLeaveEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonTeamsRemoveMemberEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonTeamsUpdateEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { computed, ref } from 'vue'

/**
 * Your team: name and description (editable), the member list with open spots, the invite
 * link to share, and leaving. Edits, removals and leaving go through confirm dialogs that keep
 * the API's error message in view when something fails.
 */
const props = defineProps<{
  team: GeeksHackingPortalApiEndpointsParticipantsHackathonTeamsGetMineEndpointStatus200
  hackathonId: string | null
  shortCode: string
}>()

const MAX_TEAM_SIZE = 5

const toast = useGToast()
const { refresh, reset } = useMyTeamCache(() => props.hackathonId)

const members = computed(() => props.team.members ?? [])
const openSpots = computed(() => Math.max(0, MAX_TEAM_SIZE - members.value.length))
const isFull = computed(() => openSpots.value === 0)

const leaveTeamMutation = useGeeksHackingPortalApiEndpointsParticipantsHackathonTeamsLeaveEndpoint()
const removeTeamMemberMutation = useGeeksHackingPortalApiEndpointsParticipantsHackathonTeamsRemoveMemberEndpoint()
const updateTeamMutation = useGeeksHackingPortalApiEndpointsParticipantsHackathonTeamsUpdateEndpoint()

// ---- edit details
const showEditTeam = ref(false)
const editTeamName = ref('')
const editTeamDescription = ref('')
const editError = ref('')

function openEditTeam() {
  editTeamName.value = props.team.name ?? ''
  editTeamDescription.value = props.team.description ?? ''
  editError.value = ''
  showEditTeam.value = true
}

async function handleUpdateTeam() {
  if (!props.hackathonId || !props.team.id)
    return
  if (!editTeamName.value.trim()) {
    editError.value = 'Your team needs a name.'
    return
  }
  editError.value = ''
  try {
    await updateTeamMutation.mutateAsync({ path: { hackathonId: props.hackathonId, teamId: props.team.id }, body: { name: editTeamName.value.trim(), description: editTeamDescription.value.trim() } })
    await refresh()
    showEditTeam.value = false
    toast.add({ title: 'Team details saved', tone: 'ok' })
  }
  catch (error) {
    editError.value = getApiErrorMessage(error, 'We couldn\'t save your changes. Try again.')
  }
}

// ---- remove a member
const removeTarget = ref<{ userId: string, name: string } | null>(null)
const removeError = ref('')

function askRemove(member: { userId?: string | null, name?: string | null }) {
  if (!member.userId)
    return
  removeError.value = ''
  removeTarget.value = { userId: member.userId, name: member.name || 'this member' }
}

async function handleRemoveMember() {
  if (!removeTarget.value || !props.hackathonId || !props.team.id)
    return
  removeError.value = ''
  try {
    const name = removeTarget.value.name
    await removeTeamMemberMutation.mutateAsync({ path: { hackathonId: props.hackathonId, teamId: props.team.id, userId: removeTarget.value.userId } })
    await refresh()
    removeTarget.value = null
    toast.add({ title: `${name} was removed from the team`, tone: 'ok' })
  }
  catch (error) {
    removeError.value = getApiErrorMessage(error, 'We couldn\'t remove this member. Try again.')
  }
}

// ---- leave
const showLeaveConfirm = ref(false)
const leaveError = ref('')

function askLeave() {
  leaveError.value = ''
  showLeaveConfirm.value = true
}

async function handleLeaveTeam() {
  if (!props.hackathonId)
    return
  leaveError.value = ''
  try {
    await leaveTeamMutation.mutateAsync({ path: { hackathonId: props.hackathonId } })
    showLeaveConfirm.value = false
    toast.add({ title: `You left ${props.team.name ?? 'the team'}`, tone: 'ok' })
    await reset()
  }
  catch (error) {
    leaveError.value = getApiErrorMessage(error, 'We couldn\'t take you out of the team. Try again.')
  }
}

// ---- invite link
const inviteUrl = computed(() => {
  if (!props.team.joinCode || !import.meta.client)
    return ''
  const url = new URL(`/${props.shortCode}/team`, window.location.origin)
  url.searchParams.set('joinCode', props.team.joinCode)
  return url.toString()
})
const isCopied = ref(false)
async function copyInviteUrl() {
  if (!inviteUrl.value)
    return
  try {
    await navigator.clipboard.writeText(inviteUrl.value)
    isCopied.value = true
    setTimeout(() => {
      isCopied.value = false
    }, 2000)
  }
  catch {
    toast.add({ title: 'Couldn\'t copy the link', description: 'Select the link and copy it yourself.', tone: 'err' })
  }
}

function initialOf(name?: string | null) {
  return (name?.trim()?.[0] ?? '?').toUpperCase()
}
</script>

<template>
  <div class="my-team">
    <!-- name + description -->
    <div class="my-team__head">
      <div class="my-team__head-text">
        <GLabelStrip
          label="Your team"
          size="sm"
        />
        <h2 class="my-team__name">
          {{ team.name }}
        </h2>
        <p
          class="my-team__desc"
          :class="{ 'my-team__desc--empty': !team.description }"
        >
          {{ team.description || 'No description yet.' }}
        </p>
      </div>
      <GButton
        variant="ghost"
        size="sm"
        icon="material-symbols:edit-rounded"
        label="Edit details"
        class="flex-none"
        @click="openEditTeam"
      />
    </div>

    <!-- members -->
    <section
      class="my-team__card"
      aria-labelledby="team-members-title"
    >
      <div class="my-team__card-head">
        <h3
          id="team-members-title"
          class="my-team__card-title"
        >
          Members
        </h3>
        <GBadge
          :tone="isFull ? 'ok' : 'neutral'"
          :label="`${members.length} of ${MAX_TEAM_SIZE}`"
        />
      </div>
      <ul class="my-team__members">
        <li
          v-for="member in members"
          :key="member.userId!"
          class="my-team__member"
        >
          <span
            class="my-team__avatar"
            aria-hidden="true"
          >{{ initialOf(member.name) }}</span>
          <span class="my-team__member-name">
            {{ member.name }}
            <span
              v-if="member.isCurrentUser"
              class="my-team__you"
            >You</span>
          </span>
          <GButton
            v-if="!member.isCurrentUser"
            variant="ghost"
            size="sm"
            label="Remove"
            :aria-label="`Remove ${member.name}`"
            class="my-team__remove"
            @click="askRemove(member)"
          />
        </li>
        <li
          v-for="n in openSpots"
          :key="`open-${n}`"
          class="my-team__member my-team__member--open"
        >
          <span
            class="my-team__avatar my-team__avatar--open"
            aria-hidden="true"
          >
            <Icon name="material-symbols:add-rounded" />
          </span>
          <span class="my-team__member-name">Open spot</span>
        </li>
      </ul>
    </section>

    <!-- invite -->
    <section
      v-if="!isFull"
      class="my-team__card"
      aria-labelledby="team-invite-title"
    >
      <div class="flex flex-col gap-1">
        <h3
          id="team-invite-title"
          class="my-team__card-title"
        >
          Invite teammates
        </h3>
        <p class="my-team__hint">
          Share this link. Anyone accepted into the event can use it to join, until the team is full.
        </p>
      </div>
      <div class="my-team__invite">
        <GFormField
          label="Invite link"
          hide-label
          class="min-w-0 flex-1"
        >
          <GInput
            :model-value="inviteUrl"
            readonly
            class="my-team__link"
            @focus="(e: FocusEvent) => (e.target as HTMLInputElement).select()"
          />
        </GFormField>
        <GButton
          variant="primary"
          :icon="isCopied ? 'material-symbols:check-rounded' : 'material-symbols:content-copy-rounded'"
          :label="isCopied ? 'Copied' : 'Copy link'"
          class="flex-none"
          @click="copyInviteUrl"
        />
      </div>
      <p class="my-team__hint">
        Invite code: <code class="my-team__code">{{ team.joinCode }}</code>
      </p>
      <span
        class="sr-only"
        aria-live="polite"
      >{{ isCopied ? 'Invite link copied' : '' }}</span>
    </section>

    <div>
      <GButton
        variant="ghost"
        icon="material-symbols:logout-rounded"
        label="Leave team"
        class="my-team__leave"
        @click="askLeave"
      />
    </div>

    <!-- edit details -->
    <GModal
      v-model:open="showEditTeam"
      title="Edit team details"
    >
      <form
        id="edit-team-form"
        class="flex flex-col gap-4"
        novalidate
        @submit.prevent="handleUpdateTeam"
      >
        <GFormField
          label="Team name"
          required
          :error="editError"
        >
          <GInput v-model="editTeamName" />
        </GFormField>
        <GFormField label="Description">
          <GTextarea
            v-model="editTeamDescription"
            :rows="3"
          />
        </GFormField>
      </form>
      <template #footer>
        <GButton
          variant="ghost"
          label="Cancel"
          @click="showEditTeam = false"
        />
        <GButton
          type="submit"
          form="edit-team-form"
          variant="primary"
          label="Save"
          :loading="updateTeamMutation.isPending.value"
          loading-label="Saving"
        />
      </template>
    </GModal>

    <!-- remove member -->
    <GModal
      :open="!!removeTarget"
      :title="`Remove ${removeTarget?.name ?? 'member'}?`"
      description="They'll lose access to the team and its submission. They can rejoin with the invite link."
      size="sm"
      @update:open="(open: boolean) => { if (!open) removeTarget = null }"
    >
      <GAlert
        v-if="removeError"
        tone="err"
        title="Not removed"
        :description="removeError"
      />
      <template #footer>
        <GButton
          variant="ghost"
          label="Cancel"
          @click="removeTarget = null"
        />
        <GButton
          variant="danger"
          label="Remove"
          :loading="removeTeamMemberMutation.isPending.value"
          loading-label="Removing"
          @click="handleRemoveMember"
        />
      </template>
    </GModal>

    <!-- leave -->
    <GModal
      v-model:open="showLeaveConfirm"
      :title="`Leave ${team.name}?`"
      :description="members.length === 1 ? 'You\'re the only member, so the team will be left empty.' : 'Your teammates keep the team. You can rejoin with their invite link.'"
      size="sm"
    >
      <GAlert
        v-if="leaveError"
        tone="err"
        title="You're still in the team"
        :description="leaveError"
      />
      <template #footer>
        <GButton
          variant="ghost"
          label="Stay"
          @click="showLeaveConfirm = false"
        />
        <GButton
          variant="danger"
          label="Leave team"
          :loading="leaveTeamMutation.isPending.value"
          loading-label="Leaving"
          @click="handleLeaveTeam"
        />
      </template>
    </GModal>
  </div>
</template>

<style scoped>
.my-team {
  display: flex;
  flex-direction: column;
  gap: 20px;
}
.my-team__head {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: space-between;
  gap: 8px 16px;
}
.my-team__head-text {
  display: flex;
  flex: 1 1 18rem;
  flex-direction: column;
  align-items: flex-start;
  gap: 8px;
  min-width: 0;
}
.my-team__name {
  margin: 0;
  font-family: var(--font-display);
  font-size: clamp(26px, 4vw, 36px);
  font-weight: 700;
  line-height: 1.1;
  overflow-wrap: anywhere;
}
.my-team__desc {
  max-width: 60ch;
  margin: 0;
}
.my-team__desc--empty {
  color: var(--gh-text-muted);
}
.my-team__card {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 20px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.my-team__card-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}
.my-team__card-title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 18px;
  font-weight: 700;
}
.my-team__hint {
  margin: 0;
  font-size: 14px;
  color: var(--gh-text-muted);
}
.my-team__members {
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0;
  list-style: none;
}
.my-team__member {
  display: flex;
  align-items: center;
  gap: 12px;
  min-height: 52px;
  padding: 6px 0;
}
.my-team__member + .my-team__member {
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.my-team__avatar {
  display: grid;
  flex: none;
  place-items: center;
  width: 36px;
  height: 36px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-pill);
  background: var(--gh-surface-sunken);
  font-family: var(--font-display);
  font-weight: 700;
}
.my-team__avatar--open {
  border-style: dashed;
  border-color: var(--gh-line-soft);
  background: none;
  color: var(--gh-text-muted);
}
.my-team__member-name {
  display: flex;
  flex: 1;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  min-width: 0;
  font-weight: 600;
  overflow-wrap: anywhere;
}
.my-team__member--open .my-team__member-name {
  font-weight: 500;
  color: var(--gh-text-muted);
}
.my-team__you {
  padding: 1px 8px;
  border-radius: var(--radius-gh-pill);
  background: var(--gh-accent);
  color: var(--gh-on-accent);
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}
.my-team .my-team__remove,
.my-team .my-team__leave {
  color: var(--gh-err);
}
.my-team__invite {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  gap: 10px;
}
.my-team__link :deep(input) {
  font-family: var(--font-mono);
  font-size: 13px;
  letter-spacing: 0;
}
.my-team__code {
  padding: 1px 6px;
  border-radius: var(--radius-gh-sm);
  background: var(--gh-surface-sunken);
  color: var(--gh-text);
  font-family: var(--font-mono);
  font-size: 13px;
}
</style>
