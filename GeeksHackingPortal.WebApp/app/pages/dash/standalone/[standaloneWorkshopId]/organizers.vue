<script setup lang="ts">
import {
  geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersListEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersDeleteEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersInviteEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersListEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import { computed, onMounted, ref, watch } from 'vue'

interface OrganizerInviteItem {
  id: string
  code: string
  type: 'Admin' | 'Volunteer'
  createdAt: string
  expiresAt?: string | null
  maxUses?: number | null
  useCount: number
  isExpired: boolean
  isExhausted: boolean
}

const route = useRoute()
const requestUrl = useRequestURL()
const config = useRuntimeConfig()
const toast = useToast()
const queryClient = useQueryClient()
const standaloneWorkshopId = computed(() => (route.params.standaloneWorkshopId as string | undefined) ?? '')

const { data: organizersData, isLoading } = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersListEndpoint(
  standaloneWorkshopId,
  { query: { enabled: computed(() => !!standaloneWorkshopId.value) } },
)

const organizers = computed(() => organizersData.value?.organizers ?? [])

const deleteMutation = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersDeleteEndpoint()
const inviteMutation = useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersInviteEndpoint()

const isInviteModalOpen = ref(false)
const inviteForm = ref({
  type: 'Volunteer' as 'Admin' | 'Volunteer',
  maxUses: null as number | null,
})
const generatedInviteCode = ref<string | null>(null)
const generatedInviteExpiresAt = ref<string | null>(null)
const generatedInviteMaxUses = ref<number | null>(null)
const invites = ref<OrganizerInviteItem[]>([])
const isLoadingInvites = ref(false)
const revokingInviteId = ref<string | null>(null)
const generatedInviteLink = computed(() => {
  if (!generatedInviteCode.value)
    return null

  return new URL(`/organizers/invites/${generatedInviteCode.value}`, requestUrl.origin).toString()
})
const activeInvites = computed(() => invites.value.filter(invite => !invite.isExpired && !invite.isExhausted))
const inactiveInvites = computed(() => invites.value.filter(invite => invite.isExpired || invite.isExhausted))

function resetInviteForm() {
  inviteForm.value = { type: 'Volunteer', maxUses: null }
  generatedInviteCode.value = null
  generatedInviteExpiresAt.value = null
  generatedInviteMaxUses.value = null
}

function openInviteModal() {
  resetInviteForm()
  isInviteModalOpen.value = true
}

async function handleGenerateInvite() {
  try {
    const result = await inviteMutation.mutateAsync({
      standaloneWorkshopId: standaloneWorkshopId.value,
      data: {
        type: inviteForm.value.type,
        maxUses: inviteForm.value.maxUses ?? undefined,
      },
    })
    generatedInviteCode.value = result?.code ?? null
    generatedInviteExpiresAt.value = result?.expiresAt ?? null
    generatedInviteMaxUses.value = result?.maxUses ?? null
    await fetchInvites()
  }
  catch {
    toast.add({ title: 'Failed to generate invite code', color: 'error' })
  }
}

async function copyInviteLink(inviteLink = generatedInviteLink.value) {
  if (inviteLink) {
    await navigator.clipboard.writeText(inviteLink)
    toast.add({ title: 'Invite link copied!', color: 'success' })
  }
}

function getInviteLink(code: string) {
  return new URL(`/organizers/invites/${code}`, requestUrl.origin).toString()
}

async function fetchInvites() {
  if (!standaloneWorkshopId.value)
    return

  isLoadingInvites.value = true
  try {
    const result = await $fetch<{ invites?: OrganizerInviteItem[] }>(`/organizers/standalone-workshops/${standaloneWorkshopId.value}/organizers/invites`, {
      baseURL: config.public.api,
      credentials: 'include',
    })
    invites.value = result.invites ?? []
  }
  catch (error) {
    toast.add({ title: 'Failed to load invite links', description: getApiErrorMessage(error, 'Please refresh and try again.'), color: 'error' })
  }
  finally {
    isLoadingInvites.value = false
  }
}

async function revokeInvite(inviteId: string) {
  revokingInviteId.value = inviteId
  try {
    await $fetch(`/organizers/standalone-workshops/${standaloneWorkshopId.value}/organizers/invites/${inviteId}`, {
      method: 'DELETE',
      baseURL: config.public.api,
      credentials: 'include',
    })
    toast.add({ title: 'Invite link revoked', color: 'success' })
    await fetchInvites()
  }
  catch (error) {
    toast.add({ title: 'Failed to revoke invite link', description: getApiErrorMessage(error, 'Please try again.'), color: 'error' })
  }
  finally {
    revokingInviteId.value = null
  }
}

async function handleDelete(userId: string) {
  try {
    await deleteMutation.mutateAsync({ standaloneWorkshopId: standaloneWorkshopId.value, userId })
    await queryClient.invalidateQueries({
      queryKey: geeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsOrganizersListEndpointQueryKey(standaloneWorkshopId.value),
    })
    toast.add({ title: 'Organizer removed', color: 'success' })
  }
  catch {
    toast.add({ title: 'Failed to remove organizer', color: 'error' })
  }
}

const isGeneratingInvite = computed(() => inviteMutation.isPending.value)

onMounted(fetchInvites)
watch(standaloneWorkshopId, fetchInvites)
</script>

<template>
  <UDashboardPanel id="standalone-organizers">
    <template #header>
      <UDashboardNavbar title="Organizers">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="space-y-3">
        <div class="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
          <UBadge
            variant="subtle"
            size="sm"
            class="w-fit"
          >
            {{ organizers.length }} total
          </UBadge>
          <UButton
            size="xs"
            icon="i-lucide-link"
            variant="outline"
            class="w-full sm:w-auto"
            @click="openInviteModal"
          >
            Create Invite Link
          </UButton>
        </div>

        <div class="rounded-xl border border-(--ui-border) bg-(--ui-bg)">
          <div
            v-if="isLoading"
            class="px-4 py-4 text-sm text-(--ui-text-muted)"
          >
            Loading organizers...
          </div>

          <div
            v-else-if="!organizers.length"
            class="px-4 py-4 text-sm text-(--ui-text-muted)"
          >
            No organizers yet.
          </div>

          <div
            v-else
            class="divide-y divide-(--ui-border) px-4"
          >
            <div
              v-for="organizer in organizers"
              :key="organizer.userId"
              class="py-3 flex items-center justify-between gap-3"
            >
              <div class="flex-1 min-w-0">
                <p class="text-sm font-medium truncate">
                  {{ organizer.name }}
                </p>
                <p class="text-xs text-(--ui-text-muted) truncate">
                  {{ organizer.email }}
                </p>
              </div>
              <div class="flex items-center gap-2 shrink-0">
                <UBadge
                  :color="organizer.type === 'Admin' ? 'primary' : 'neutral'"
                  variant="subtle"
                  size="xs"
                >
                  {{ organizer.type }}
                </UBadge>
                <UButton
                  v-if="organizer.userId"
                  size="xs"
                  variant="ghost"
                  color="error"
                  icon="i-lucide-trash-2"
                  :loading="deleteMutation.isPending.value"
                  @click="handleDelete(organizer.userId)"
                />
              </div>
            </div>
          </div>
        </div>

        <div class="rounded-xl border border-(--ui-border) bg-(--ui-bg)">
          <div class="flex items-center justify-between gap-3 border-b border-(--ui-border) px-4 py-3">
            <div>
              <h2 class="text-sm font-semibold">
                Invite Links
              </h2>
              <p class="text-xs text-(--ui-text-muted)">
                {{ activeInvites.length }} active, {{ inactiveInvites.length }} inactive
              </p>
            </div>
            <UButton
              size="xs"
              variant="ghost"
              icon="i-lucide-refresh-cw"
              :loading="isLoadingInvites"
              @click="fetchInvites"
            />
          </div>

          <div
            v-if="isLoadingInvites"
            class="px-4 py-4 text-sm text-(--ui-text-muted)"
          >
            Loading invite links...
          </div>

          <div
            v-else-if="!invites.length"
            class="px-4 py-4 text-sm text-(--ui-text-muted)"
          >
            No invite links created yet.
          </div>

          <div
            v-else
            class="divide-y divide-(--ui-border) px-4"
          >
            <div
              v-for="invite in invites"
              :key="invite.id"
              class="flex flex-col gap-3 py-3 sm:flex-row sm:items-center sm:justify-between"
            >
              <div class="min-w-0 flex-1 space-y-1">
                <div class="flex flex-wrap items-center gap-2">
                  <code class="text-sm font-semibold tracking-widest">{{ invite.code }}</code>
                  <UBadge
                    :color="invite.type === 'Admin' ? 'primary' : 'neutral'"
                    variant="subtle"
                    size="xs"
                  >
                    {{ invite.type }}
                  </UBadge>
                  <UBadge
                    :color="invite.isExpired || invite.isExhausted ? 'error' : 'success'"
                    variant="subtle"
                    size="xs"
                  >
                    {{ invite.isExhausted ? 'Exhausted' : invite.isExpired ? 'Expired' : 'Active' }}
                  </UBadge>
                </div>
                <p class="text-xs text-(--ui-text-muted)">
                  Uses: {{ invite.useCount }} / {{ invite.maxUses ?? 'Unlimited' }} · Expires: {{ invite.expiresAt ? new Date(invite.expiresAt).toLocaleString() : 'Never' }}
                </p>
              </div>
              <div class="flex shrink-0 gap-2">
                <UButton
                  size="xs"
                  variant="ghost"
                  icon="i-lucide-copy"
                  @click="copyInviteLink(getInviteLink(invite.code))"
                />
                <UButton
                  size="xs"
                  variant="ghost"
                  color="error"
                  icon="i-lucide-ban"
                  :disabled="invite.isExpired"
                  :loading="revokingInviteId === invite.id"
                  @click="revokeInvite(invite.id)"
                >
                  Revoke
                </UButton>
              </div>
            </div>
          </div>
        </div>

        <!-- Create Invite Link Modal -->
        <UModal v-model:open="isInviteModalOpen">
          <template #content>
            <UCard>
              <template #header>
                <div class="flex items-center justify-between">
                  <h3 class="text-base font-semibold">
                    Create Invite Link
                  </h3>
                  <UButton
                    variant="ghost"
                    icon="i-lucide-x"
                    size="xs"
                    @click="isInviteModalOpen = false"
                  />
                </div>
              </template>

              <div class="space-y-4">
                <div
                  v-if="!generatedInviteCode"
                  class="space-y-4"
                >
                  <UFormField label="Role">
                    <USelect
                      v-model="inviteForm.type"
                      :items="[{ label: 'Volunteer', value: 'Volunteer' }, { label: 'Admin', value: 'Admin' }]"
                    />
                  </UFormField>

                  <UFormField
                    label="Max Uses"
                    description="Leave empty for unlimited uses."
                  >
                    <UInput
                      v-model.number="inviteForm.maxUses"
                      type="number"
                      placeholder="Unlimited"
                      :min="1"
                    />
                  </UFormField>

                  <div class="flex justify-end gap-2">
                    <UButton
                      variant="ghost"
                      @click="isInviteModalOpen = false"
                    >
                      Cancel
                    </UButton>
                    <UButton
                      :loading="isGeneratingInvite"
                      icon="i-lucide-link"
                      @click="handleGenerateInvite"
                    >
                      Generate
                    </UButton>
                  </div>
                </div>

                <div
                  v-else
                  class="space-y-4"
                >
                  <div class="rounded-lg border border-(--ui-border) bg-(--ui-bg-elevated) p-4 space-y-2">
                    <p class="text-xs text-(--ui-text-muted)">
                      Invite link
                    </p>
                    <div class="flex items-center gap-2">
                      <code class="flex-1 min-w-0 break-all text-sm font-mono font-semibold">{{ generatedInviteLink }}</code>
                      <UButton
                        size="xs"
                        variant="ghost"
                        icon="i-lucide-copy"
                        @click="copyInviteLink"
                      />
                    </div>
                    <p class="text-xs text-(--ui-text-muted)">
                      Code: <span class="font-mono font-semibold tracking-widest">{{ generatedInviteCode }}</span>
                    </p>
                    <p class="text-xs text-(--ui-text-muted)">
                      Expires: {{ generatedInviteExpiresAt ? new Date(generatedInviteExpiresAt).toLocaleString() : 'N/A' }}
                    </p>
                    <p class="text-xs text-(--ui-text-muted)">
                      Max uses: {{ generatedInviteMaxUses ?? 'Unlimited' }}
                    </p>
                  </div>

                  <p class="text-sm text-(--ui-text-muted)">
                    Share this link with anyone you want to add as an organizer. They will join by opening it.
                  </p>

                  <div class="flex justify-end gap-2">
                    <UButton
                      variant="ghost"
                      @click="resetInviteForm"
                    >
                      Generate Another
                    </UButton>
                    <UButton @click="isInviteModalOpen = false">
                      Done
                    </UButton>
                  </div>
                </div>
              </div>
            </UCard>
          </template>
        </UModal>
      </div>
    </template>
  </UDashboardPanel>
</template>
