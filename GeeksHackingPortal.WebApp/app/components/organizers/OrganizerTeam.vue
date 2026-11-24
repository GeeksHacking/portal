<script setup lang="ts">
import {
  geeksHackingPortalApiEndpointsOrganizersHackathonOrganizersInvitesListEndpointQueryKey,
  geeksHackingPortalApiEndpointsOrganizersHackathonOrganizersListEndpointQueryKey,
  geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersInvitesListEndpointQueryKey,
  geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersListEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonOrganizersDeleteEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonOrganizersInviteEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonOrganizersInvitesListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonOrganizersInvitesRevokeEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonOrganizersListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersDeleteEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersInviteEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersInvitesListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersInvitesRevokeEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersListEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import { useClipboard } from '@vueuse/core'

/**
 * The organiser team of one event (hackathon or standalone workshop): who's on it, and the
 * invite links that add people. Shared by both event types: only the endpoints differ.
 */
const props = defineProps<{
  kind: 'hackathon' | 'workshop'
  eventId: string
}>()

type Role = 'Admin' | 'Volunteer'
interface Organizer { userId?: string, name?: string, email?: string, type?: string }
interface Invite {
  id?: string
  code?: string
  type?: string
  createdAt?: string
  expiresAt?: string | null
  maxUses?: number | null
  useCount?: number
  isExpired?: boolean
  isExhausted?: boolean
}

const queryClient = useQueryClient()
const toast = useGToast()
const requestUrl = useRequestURL()
const { copy, isSupported: canCopy } = useClipboard({ legacy: true })
const enabled = { query: { enabled: computed(() => !!props.eventId) } }

// ---- endpoints for this kind of event (the kind never changes for a mounted page) ----
const isHackathon = props.kind === 'hackathon'
const hackathonPath = computed(() => ({ hackathonId: props.eventId }))
const workshopPath = computed(() => ({ standaloneWorkshopId: props.eventId }))

const organizersQuery = isHackathon
  ? useGeeksHackingPortalApiEndpointsOrganizersHackathonOrganizersListEndpoint({ path: hackathonPath }, enabled)
  : useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersListEndpoint({ path: workshopPath }, enabled)
const invitesQuery = isHackathon
  ? useGeeksHackingPortalApiEndpointsOrganizersHackathonOrganizersInvitesListEndpoint({ path: hackathonPath }, enabled)
  : useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersInvitesListEndpoint({ path: workshopPath }, enabled)
const deleteMutation = isHackathon
  ? useGeeksHackingPortalApiEndpointsOrganizersHackathonOrganizersDeleteEndpoint()
  : useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersDeleteEndpoint()
const inviteMutation = isHackathon
  ? useGeeksHackingPortalApiEndpointsOrganizersHackathonOrganizersInviteEndpoint()
  : useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersInviteEndpoint()
const revokeMutation = isHackathon
  ? useGeeksHackingPortalApiEndpointsOrganizersHackathonOrganizersInvitesRevokeEndpoint()
  : useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersInvitesRevokeEndpoint()

const eventPath = () => (isHackathon ? { hackathonId: props.eventId } : { standaloneWorkshopId: props.eventId })

function refreshOrganizers() {
  return queryClient.invalidateQueries({
    queryKey: isHackathon
      ? geeksHackingPortalApiEndpointsOrganizersHackathonOrganizersListEndpointQueryKey({ path: { hackathonId: props.eventId } })
      : geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersListEndpointQueryKey({ path: { standaloneWorkshopId: props.eventId } }),
  })
}
function refreshInvites() {
  return queryClient.invalidateQueries({
    queryKey: isHackathon
      ? geeksHackingPortalApiEndpointsOrganizersHackathonOrganizersInvitesListEndpointQueryKey({ path: { hackathonId: props.eventId } })
      : geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersInvitesListEndpointQueryKey({ path: { standaloneWorkshopId: props.eventId } }),
  })
}

// ---- the team ----
const { data: me } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
const organizers = computed<Organizer[]>(() => [...((organizersQuery.data.value as { organizers?: Organizer[] } | undefined)?.organizers ?? [])]
  .sort((a, b) => Number(b.type === 'Admin') - Number(a.type === 'Admin') || (a.name ?? '').localeCompare(b.name ?? '')))
const adminCount = computed(() => organizers.value.filter(o => o.type === 'Admin').length)
const amAdmin = computed(() => organizers.value.some(o => o.userId === me.value?.id && o.type === 'Admin'))
const isMe = (organizer: Organizer) => !!me.value?.id && organizer.userId === me.value.id

const confirmingRemoval = ref<string | null>(null)
const removalError = ref<{ userId: string, message: string } | null>(null)

function askRemove(organizer: Organizer) {
  removalError.value = null
  confirmingRevoke.value = null
  confirmingRemoval.value = organizer.userId ?? null
}

async function remove(organizer: Organizer) {
  if (!organizer.userId || deleteMutation.isPending.value)
    return
  removalError.value = null
  try {
    await deleteMutation.mutateAsync({ path: { ...eventPath(), userId: organizer.userId } } as never)
    confirmingRemoval.value = null
    await refreshOrganizers()
    toast.add({ tone: 'ok', title: `Removed ${organizer.name || 'the organizer'}` })
  }
  catch (error) {
    const message = getErrorStatusCode(error) === 403
      ? 'The server refused this removal (permission denied). They\'re still on the team.'
      : getApiErrorMessage(error, 'They weren\'t removed. Try again.')
    removalError.value = { userId: organizer.userId, message }
  }
}

// ---- invite links ----
const invites = computed<Invite[]>(() => [...((invitesQuery.data.value as { invites?: Invite[] } | undefined)?.invites ?? [])]
  .sort((a, b) => (b.createdAt ?? '').localeCompare(a.createdAt ?? '')))
const isInviteLive = (invite: Invite) => !invite.isExpired && !invite.isExhausted
const liveInvites = computed(() => invites.value.filter(isInviteLive))
const endedInvites = computed(() => invites.value.filter(invite => !isInviteLive(invite)))

const inviteLink = (code: string | undefined) => new URL(`/organizers/invites/${code ?? ''}`, requestUrl.origin).toString()

function usesText(invite: Invite) {
  const used = invite.useCount ?? 0
  if (invite.maxUses)
    return `Used ${used} of ${invite.maxUses}`
  return used === 1 ? 'Used once' : `Used ${used} times`
}

function expiryText(invite: Invite) {
  if (invite.isExhausted)
    return 'All uses taken'
  if (!invite.expiresAt)
    return 'Never expires'
  const when = new Date(invite.expiresAt)
  if (invite.isExpired || when.getTime() <= Date.now())
    return `Ended ${formatOrganizerDateTime(when)}`
  const days = Math.ceil((when.getTime() - Date.now()) / 86_400_000)
  return `Expires ${days <= 1 ? 'within a day' : `in ${days} days`} · ${formatOrganizerDateTime(when)}`
}

async function copyLink(code: string | undefined) {
  if (!code)
    return
  try {
    await copy(inviteLink(code))
    toast.add({ tone: 'ok', title: 'Invite link copied' })
  }
  catch {
    toast.add({ tone: 'err', title: 'Couldn\'t copy', description: 'Select the link and copy it by hand.' })
  }
}

const confirmingRevoke = ref<string | null>(null)
const revokeError = ref<{ inviteId: string, message: string } | null>(null)

function askRevoke(invite: Invite) {
  revokeError.value = null
  confirmingRemoval.value = null
  confirmingRevoke.value = invite.id ?? null
}

async function revoke(invite: Invite) {
  if (!invite.id || revokeMutation.isPending.value)
    return
  revokeError.value = null
  try {
    await revokeMutation.mutateAsync({ path: { ...eventPath(), inviteId: invite.id } } as never)
    confirmingRevoke.value = null
    await refreshInvites()
    toast.add({ tone: 'ok', title: 'Invite link turned off' })
  }
  catch (error) {
    revokeError.value = { inviteId: invite.id, message: getApiErrorMessage(error, 'The link still works. Try again.') }
  }
}

// ---- new invite dialog ----
const isInviteOpen = ref(false)
const inviteForm = reactive({ role: 'Volunteer' as Role, limitUses: false, maxUses: 1 as number | null })
const maxUsesError = ref('')
const inviteError = ref('')
const created = ref<{ code: string, role: string, expiresAt?: string, maxUses?: number | null } | null>(null)

function openInvite() {
  Object.assign(inviteForm, { role: 'Volunteer', limitUses: false, maxUses: 1 })
  maxUsesError.value = ''
  inviteError.value = ''
  created.value = null
  isInviteOpen.value = true
}

async function createInvite() {
  if (inviteMutation.isPending.value)
    return
  maxUsesError.value = ''
  inviteError.value = ''
  const maxUses = inviteForm.limitUses ? Number(inviteForm.maxUses) : null
  if (inviteForm.limitUses && (!Number.isInteger(maxUses) || (maxUses ?? 0) < 1)) {
    maxUsesError.value = 'Enter a whole number, 1 or more.'
    nextTick(() => document.querySelector<HTMLElement>('#ot-invite-form [aria-invalid="true"]')?.focus())
    return
  }
  try {
    const result = await inviteMutation.mutateAsync({
      path: eventPath(),
      body: { type: inviteForm.role, maxUses: maxUses ?? undefined },
    } as never) as { code?: string, type?: string, expiresAt?: string, maxUses?: number | null } | undefined
    if (!result?.code)
      throw new Error('No invite code came back.')
    created.value = { code: result.code, role: result.type ?? inviteForm.role, expiresAt: result.expiresAt, maxUses: result.maxUses }
    await refreshInvites()
  }
  catch (error) {
    maxUsesError.value = getApiFieldError(getApiValidationErrors(error), 'maxUses') ?? ''
    if (!maxUsesError.value)
      inviteError.value = getApiErrorMessage(error, 'No link was made. Try again.')
  }
}

const roleChoices: { value: Role, label: string, description: string, icon: string }[] = [
  { value: 'Volunteer', label: 'Volunteer', description: 'Helps run the event.', icon: 'material-symbols:volunteer-activism-rounded' },
  { value: 'Admin', label: 'Admin', description: 'Can also remove organizers.', icon: 'material-symbols:shield-person-rounded' },
]
const eventNoun = props.kind === 'hackathon' ? 'hackathon' : 'workshop'
</script>

<template>
  <GPanel :id="kind === 'hackathon' ? 'organizers' : 'standalone-organizers'">
    <template #header>
      <GPanelHeader title="Organizers">
        <template #right>
          <GButton
            variant="primary"
            icon="material-symbols:person-add-rounded"
            label="Invite organizers"
            @click="openInvite"
          />
        </template>
      </GPanelHeader>
    </template>

    <template #body>
      <div class="ot">
        <!-- the team -->
        <section
          class="ot-section"
          aria-labelledby="ot-team-title"
        >
          <div class="ot-section__head">
            <h2
              id="ot-team-title"
              class="ot-section__title"
            >
              Team
            </h2>
            <p
              v-if="organizers.length"
              class="ot-muted"
            >
              {{ organizers.length }} {{ organizers.length === 1 ? 'organizer' : 'organizers' }} · {{ adminCount }} {{ adminCount === 1 ? 'admin' : 'admins' }}.
              Everyone here can manage this {{ eventNoun }}; only admins can remove organizers.
            </p>
          </div>

          <GAlert
            v-if="organizersQuery.error.value && !organizers.length"
            tone="err"
            title="Couldn't load the team"
            :description="getApiErrorMessage(organizersQuery.error.value, 'Refresh the page to try again.')"
          />

          <div
            v-if="organizersQuery.isLoading.value"
            class="ot-list ot-list--loading"
            aria-busy="true"
          >
            <GSkeleton
              v-for="i in 3"
              :key="i"
              class="ot-skeleton"
            />
          </div>

          <ul
            v-else-if="organizers.length"
            class="ot-list"
          >
            <li
              v-for="organizer in organizers"
              :key="organizer.userId"
              class="ot-row"
            >
              <div class="ot-row__main">
                <div class="ot-row__who">
                  <p class="ot-row__name">
                    {{ organizer.name || 'Unnamed organizer' }}
                    <span
                      v-if="isMe(organizer)"
                      class="ot-you"
                    >(you)</span>
                  </p>
                  <p
                    v-if="organizer.email"
                    class="ot-row__meta"
                  >
                    {{ organizer.email }}
                  </p>
                </div>
                <GBadge
                  size="sm"
                  variant="outline"
                  :icon="organizer.type === 'Admin' ? 'material-symbols:shield-person-rounded' : undefined"
                  :label="organizer.type || 'Organizer'"
                />
                <GButton
                  v-if="amAdmin && !isMe(organizer) && confirmingRemoval !== organizer.userId"
                  variant="ghost"
                  size="sm"
                  icon="material-symbols:person-remove-rounded"
                  label="Remove"
                  :aria-label="`Remove ${organizer.name || 'organizer'}`"
                  @click="askRemove(organizer)"
                />
              </div>

              <div
                v-if="confirmingRemoval === organizer.userId"
                class="ot-confirm"
                role="group"
                :aria-label="`Remove ${organizer.name}?`"
              >
                <p class="ot-confirm__text">
                  Remove <strong>{{ organizer.name || 'this organizer' }}</strong>? They lose access to this {{ eventNoun }}'s dashboard.
                </p>
                <div class="ot-confirm__actions">
                  <GButton
                    variant="ghost"
                    size="sm"
                    label="Keep"
                    :disabled="deleteMutation.isPending.value"
                    @click="confirmingRemoval = null"
                  />
                  <GButton
                    variant="danger"
                    size="sm"
                    label="Remove"
                    :loading="deleteMutation.isPending.value"
                    loading-label="Removing"
                    @click="remove(organizer)"
                  />
                </div>
              </div>
              <p
                v-if="removalError?.userId === organizer.userId"
                class="ot-error"
                role="alert"
              >
                {{ removalError.message }}
              </p>
            </li>
          </ul>
        </section>

        <!-- invite links -->
        <section
          class="ot-section"
          aria-labelledby="ot-links-title"
        >
          <div class="ot-section__head">
            <h2
              id="ot-links-title"
              class="ot-section__title"
            >
              Invite links
            </h2>
            <p class="ot-muted">
              Anyone who opens a working link while signed in joins the team with the link's role.
            </p>
          </div>

          <GAlert
            v-if="invitesQuery.error.value && !invites.length"
            tone="err"
            title="Couldn't load invite links"
            :description="getApiErrorMessage(invitesQuery.error.value, 'Refresh the page to try again.')"
          />

          <div
            v-if="invitesQuery.isLoading.value"
            class="ot-list ot-list--loading"
            aria-busy="true"
          >
            <GSkeleton
              v-for="i in 2"
              :key="i"
              class="ot-skeleton"
            />
          </div>

          <GEmptyState
            v-else-if="!liveInvites.length && !invitesQuery.error.value"
            size="sm"
            bordered
            icon="material-symbols:link-rounded"
            :title="endedInvites.length ? 'No working links' : 'No invite links yet'"
            :description="`Make a link and send it to the people helping you run this ${eventNoun}.`"
          >
            <template #actions>
              <GButton
                variant="secondary"
                icon="material-symbols:add-link-rounded"
                label="Make an invite link"
                @click="openInvite"
              />
            </template>
          </GEmptyState>

          <ul
            v-else-if="liveInvites.length"
            class="ot-list"
            aria-label="Working links"
          >
            <li
              v-for="invite in liveInvites"
              :key="invite.id"
              class="ot-row"
            >
              <div class="ot-row__main">
                <div class="ot-row__who">
                  <p class="ot-row__name">
                    <code class="ot-code">{{ invite.code }}</code>
                    <GBadge
                      size="sm"
                      variant="outline"
                      :label="invite.type || 'Volunteer'"
                    />
                  </p>
                  <p class="ot-row__meta">
                    {{ usesText(invite) }} · {{ expiryText(invite) }}
                  </p>
                </div>
                <GButton
                  v-if="canCopy"
                  variant="secondary"
                  size="sm"
                  icon="material-symbols:content-copy-rounded"
                  label="Copy link"
                  :aria-label="`Copy invite link ${invite.code}`"
                  @click="copyLink(invite.code)"
                />
                <GButton
                  v-if="confirmingRevoke !== invite.id"
                  variant="ghost"
                  size="sm"
                  icon="material-symbols:link-off-rounded"
                  label="Turn off"
                  :aria-label="`Turn off invite link ${invite.code}`"
                  @click="askRevoke(invite)"
                />
              </div>
              <div
                v-if="!canCopy"
                class="ot-row__link"
              >
                {{ inviteLink(invite.code) }}
              </div>
              <div
                v-if="confirmingRevoke === invite.id"
                class="ot-confirm"
                role="group"
                :aria-label="`Turn off link ${invite.code}?`"
              >
                <p class="ot-confirm__text">
                  Turn off <strong>{{ invite.code }}</strong>? It stops working straight away. People who already joined stay on the team.
                </p>
                <div class="ot-confirm__actions">
                  <GButton
                    variant="ghost"
                    size="sm"
                    label="Keep it"
                    :disabled="revokeMutation.isPending.value"
                    @click="confirmingRevoke = null"
                  />
                  <GButton
                    variant="danger"
                    size="sm"
                    label="Turn off"
                    :loading="revokeMutation.isPending.value"
                    loading-label="Turning off"
                    @click="revoke(invite)"
                  />
                </div>
              </div>
              <p
                v-if="revokeError?.inviteId === invite.id"
                class="ot-error"
                role="alert"
              >
                {{ revokeError.message }}
              </p>
            </li>
          </ul>

          <details
            v-if="endedInvites.length"
            class="ot-ended"
          >
            <summary class="ot-ended__summary">
              {{ endedInvites.length }} {{ endedInvites.length === 1 ? 'link has' : 'links have' }} stopped working
            </summary>
            <ul class="ot-list ot-list--quiet">
              <li
                v-for="invite in endedInvites"
                :key="invite.id"
                class="ot-row"
              >
                <div class="ot-row__main">
                  <div class="ot-row__who">
                    <p class="ot-row__name">
                      <code class="ot-code ot-code--ended">{{ invite.code }}</code>
                      <span class="ot-row__meta">{{ invite.type }}</span>
                    </p>
                    <p class="ot-row__meta">
                      {{ usesText(invite) }} · {{ expiryText(invite) }}
                    </p>
                  </div>
                </div>
              </li>
            </ul>
          </details>
        </section>
      </div>

      <!-- new invite link -->
      <GModal
        v-model:open="isInviteOpen"
        :title="created ? 'Your invite link' : 'Invite organizers'"
        :description="created ? 'Send it to the people you want on the team. Anyone with the link can join until it stops working.' : 'You\'ll get a link to send. It works for 7 days.'"
      >
        <div
          v-if="created"
          class="ot-created"
        >
          <code class="ot-created__link">{{ inviteLink(created.code) }}</code>
          <GButton
            v-if="canCopy"
            variant="secondary"
            icon="material-symbols:content-copy-rounded"
            label="Copy link"
            @click="copyLink(created.code)"
          />
          <dl class="ot-facts">
            <div>
              <dt>Joins as</dt>
              <dd>{{ created.role }}</dd>
            </div>
            <div>
              <dt>Uses</dt>
              <dd>{{ created.maxUses ? `Up to ${created.maxUses}` : 'No limit' }}</dd>
            </div>
            <div>
              <dt>Stops working</dt>
              <dd>{{ created.expiresAt ? formatOrganizerDateTime(created.expiresAt) : 'Never' }}</dd>
            </div>
          </dl>
        </div>

        <form
          v-else
          id="ot-invite-form"
          class="ot-form"
          novalidate
          @submit.prevent="createInvite"
        >
          <fieldset class="ot-choice">
            <legend class="ot-choice__legend">
              They join as
            </legend>
            <label
              v-for="choice in roleChoices"
              :key="choice.value"
              class="ot-choice__option"
              :class="{ 'is-on': inviteForm.role === choice.value }"
            >
              <input
                v-model="inviteForm.role"
                type="radio"
                name="ot-role"
                :value="choice.value"
              >
              <Icon
                :name="choice.icon"
                aria-hidden="true"
              />
              <span>
                <span class="ot-choice__label">{{ choice.label }}</span>
                <span class="ot-choice__desc">{{ choice.description }}</span>
              </span>
            </label>
          </fieldset>

          <GCheckbox
            v-model="inviteForm.limitUses"
            label="Limit how many people can use it"
            description="Otherwise anyone with the link can join until it expires."
          />
          <GFormField
            v-if="inviteForm.limitUses"
            label="Number of people"
            required
            :error="maxUsesError"
          >
            <GInput
              v-model.number="inviteForm.maxUses"
              type="number"
              min="1"
              step="1"
              inputmode="numeric"
              class="ot-number"
            />
          </GFormField>

          <GAlert
            v-if="inviteError"
            tone="err"
            title="No link made"
            :description="inviteError"
          />
        </form>

        <template #footer>
          <template v-if="created">
            <GButton
              variant="ghost"
              label="Make another"
              @click="openInvite"
            />
            <GButton
              variant="primary"
              label="Done"
              @click="isInviteOpen = false"
            />
          </template>
          <template v-else>
            <GButton
              variant="ghost"
              label="Cancel"
              :disabled="inviteMutation.isPending.value"
              @click="isInviteOpen = false"
            />
            <GButton
              variant="primary"
              type="submit"
              form="ot-invite-form"
              icon="material-symbols:add-link-rounded"
              label="Make link"
              :loading="inviteMutation.isPending.value"
              loading-label="Making link"
            />
          </template>
        </template>
      </GModal>
    </template>
  </GPanel>
</template>

<style scoped>
.ot {
  display: flex;
  flex-direction: column;
  gap: 32px;
  width: 100%;
  max-width: 64rem;
  margin: 0 auto;
}
.ot-section {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.ot-section__head {
  display: flex;
  flex-direction: column;
  gap: 4px;
}
.ot-section__title {
  margin: 0;
  font-size: 18px;
  font-weight: 700;
}
.ot-muted {
  margin: 0;
  color: var(--gh-text-muted);
}
.ot-list {
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0;
  list-style: none;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.ot-list--loading {
  gap: 8px;
  padding: 12px;
}
.ot-list--quiet {
  margin-top: 8px;
  border-color: var(--gh-line-soft);
}
.ot-skeleton {
  display: block;
  height: 48px;
}
.ot-row {
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 12px 16px;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.ot-row:first-child { border-top: 0; }
.ot-row__main {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px 12px;
}
.ot-row__who {
  flex: 1 1 14rem;
  min-width: 0;
}
.ot-row__name {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px 10px;
  margin: 0;
  font-weight: 700;
  overflow-wrap: anywhere;
}
.ot-row__meta {
  margin: 2px 0 0;
  font-size: 14px;
  font-weight: 400;
  color: var(--gh-text-muted);
  overflow-wrap: anywhere;
}
.ot-row__link {
  font-family: var(--font-mono);
  font-size: 13px;
  overflow-wrap: anywhere;
  user-select: all;
}
.ot-you {
  font-weight: 400;
  color: var(--gh-text-muted);
}
.ot-code {
  font-family: var(--font-mono);
  font-size: 15px;
  letter-spacing: 0.12em;
}
.ot-code--ended {
  color: var(--gh-text-muted);
  text-decoration: line-through;
}
.ot-confirm {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 8px 16px;
  padding: 10px 12px;
  border: var(--gh-border-width) solid var(--gh-err);
  border-radius: var(--radius-gh);
  background: var(--gh-err-soft);
}
.ot-confirm__text {
  flex: 1 1 16rem;
  margin: 0;
  font-size: 14px;
}
.ot-confirm__actions {
  display: flex;
  gap: 8px;
}
.ot-error {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
  color: var(--gh-err);
}
.ot-ended__summary {
  width: fit-content;
  font-weight: 600;
  cursor: pointer;
}
.ot-ended__summary:focus-visible {
  outline: 2px solid var(--gh-focus);
  outline-offset: 2px;
}
.ot-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.ot-number { max-width: 10rem; }
.ot-choice {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr));
  gap: 8px;
  margin: 0;
  padding: 0;
  border: 0;
}
.ot-choice__legend {
  margin-bottom: 8px;
  padding: 0;
  font-size: 14px;
  font-weight: 600;
}
.ot-choice__option {
  position: relative;
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 12px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh);
  background: var(--gh-surface);
  cursor: pointer;
}
.ot-choice__option:hover { background: var(--gh-ghost-hover); }
.ot-choice__option input {
  position: absolute;
  opacity: 0;
  pointer-events: none;
}
.ot-choice__option:has(input:focus-visible) {
  outline: 2px solid var(--gh-focus);
  outline-offset: 2px;
}
.ot-choice__option .iconify,
.ot-choice__option :deep(svg) {
  flex: none;
  width: 22px;
  height: 22px;
  margin-top: 1px;
}
.ot-choice__option.is-on {
  background: var(--gh-accent);
  color: var(--gh-on-accent);
  box-shadow: 3px 3px 0 var(--gh-line);
}
.ot-choice__label {
  display: block;
  font-weight: 700;
}
.ot-choice__desc {
  display: block;
  font-size: 14px;
}
.ot-created {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 12px;
}
.ot-created__link {
  display: block;
  width: 100%;
  padding: 12px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh);
  background: var(--gh-surface-sunken);
  font-family: var(--font-mono);
  font-size: 14px;
  overflow-wrap: anywhere;
  user-select: all;
}
.ot-facts {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(9rem, 1fr));
  gap: 8px 16px;
  width: 100%;
  margin: 4px 0 0;
}
.ot-facts dt {
  font-size: 13px;
  color: var(--gh-text-muted);
}
.ot-facts dd {
  margin: 0;
  font-weight: 600;
}
</style>
