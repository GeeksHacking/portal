<script setup lang="ts">
import { computed, ref } from 'vue'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { challengeQueries, challengeOrganizerQueries, useCreateChallengeMutation, useDeleteChallengeMutation, useUpdateChallengeMutation } from '~/composables/challenges'

const route = useRoute()
const props = withDefaults(defineProps<{
  hackathonId?: string
}>(), {
  hackathonId: '',
})
const hackathonId = computed(() => props.hackathonId || (route.params.hackathonId as string | undefined) || '')

const queryClient = useQueryClient()

const { data: challengesData, isLoading: isLoadingChallenges } = useQuery(
  computed(() => ({
    ...challengeOrganizerQueries.list(hackathonId.value),
    enabled: !!hackathonId.value,
  })),
)

const challenges = computed(() => challengesData.value?.challenges ?? [])

// Fetch participant challenges list for team counts
const { data: participantChallengesData } = useQuery(
  computed(() => ({
    ...challengeQueries.list(hackathonId.value),
    enabled: !!hackathonId.value,
  })),
)

const teamCountByChallengeId = computed(() => {
  const map = new Map<string, number>()
  for (const c of participantChallengesData.value?.challenges ?? []) {
    if (c.id != null) {
      map.set(c.id, c.teamCount ?? 0)
    }
  }
  return map
})

// Mutations
const createMutation = useCreateChallengeMutation(hackathonId)
const updateMutation = useUpdateChallengeMutation(hackathonId)
const deleteMutation = useDeleteChallengeMutation(hackathonId)

// Modal state
const isModalOpen = ref(false)
const isEditing = ref(false)
const editingChallengeId = ref<string | null>(null)

// Form state
const form = ref({
  title: '',
  description: '',
  criteria: '',
  isPublished: false,
})

function resetForm() {
  form.value = {
    title: '',
    description: '',
    criteria: '',
    isPublished: false,
  }
  isEditing.value = false
  editingChallengeId.value = null
}

function openCreateModal() {
  resetForm()
  isModalOpen.value = true
}

function openEditModal(challenge: typeof challenges.value[number]) {
  form.value = {
    title: challenge.title ?? '',
    description: challenge.description ?? '',
    criteria: challenge.criteria ?? '',
    isPublished: challenge.isPublished ?? false,
  }
  isEditing.value = true
  editingChallengeId.value = challenge.id ?? null
  isModalOpen.value = true
}

async function handleSubmit() {
  if (isEditing.value && editingChallengeId.value) {
    await updateMutation.mutateAsync({
      challengeId: editingChallengeId.value,
      data: form.value,
    })
  }
  else {
    await createMutation.mutateAsync(form.value)
  }
  await queryClient.invalidateQueries({ queryKey: ['hackathons', hackathonId.value, 'challenges', 'organizer'] })
  isModalOpen.value = false
  resetForm()
}

async function handleDelete(challengeId: string) {
  await deleteMutation.mutateAsync(challengeId)
  await queryClient.invalidateQueries({ queryKey: ['hackathons', hackathonId.value, 'challenges', 'organizer'] })
}

const isSubmitting = computed(() => createMutation.isPending.value || updateMutation.isPending.value)
</script>

<template>
  <UDashboardPanel id="challenges">
    <template #header>
      <UDashboardNavbar title="Challenges">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="p-4 space-y-4 overflow-y-auto">
        <ParticipantAnalytics
          :hackathon-id="hackathonId"
          :is-organizer="true"
        />

        <UCard>
          <template #header>
            <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <h3 class="text-sm font-semibold">
                Challenges
              </h3>
              <div class="flex items-center gap-2">
                <UBadge
                  variant="subtle"
                  size="sm"
                >
                  {{ challenges.length }} total
                </UBadge>
                <UButton
                  size="xs"
                  icon="i-lucide-plus"
                  @click="openCreateModal"
                >
                  Add
                </UButton>
              </div>
            </div>
          </template>

          <div
            v-if="isLoadingChallenges"
            class="text-(--ui-text-muted) text-sm"
          >
            Loading challenges...
          </div>

          <div
            v-else-if="!challenges.length"
            class="text-(--ui-text-muted) text-sm"
          >
            No challenges yet.
          </div>

          <div
            v-else
            class="divide-y divide-(--ui-border)"
          >
            <div
              v-for="challenge in challenges"
              :key="challenge.id ?? ''"
              class="py-2 flex items-center justify-between"
            >
              <div class="flex-1 min-w-0">
                <p class="text-sm font-medium">
                  {{ challenge.title }}
                </p>
                <p class="text-xs text-(--ui-text-muted) truncate">
                  {{ challenge.description ?? 'No description' }}
                </p>
              </div>
              <div class="flex flex-wrap items-center justify-end gap-2 ml-2">
                <UBadge
                  variant="subtle"
                  size="xs"
                  color="neutral"
                >
                  {{ teamCountByChallengeId.get(challenge.id ?? '') ?? 0 }} {{ teamCountByChallengeId.get(challenge.id ?? '') === 1 ? 'team' : 'teams' }}
                </UBadge>
                <UBadge
                  :color="challenge.isPublished ? 'success' : 'warning'"
                  variant="subtle"
                  size="xs"
                >
                  {{ challenge.isPublished ? 'Published' : 'Draft' }}
                </UBadge>
                <UButton
                  size="xs"
                  variant="ghost"
                  icon="i-lucide-pencil"
                  @click="openEditModal(challenge)"
                />
                <UButton
                  size="xs"
                  variant="ghost"
                  color="error"
                  icon="i-lucide-trash-2"
                  :loading="deleteMutation.isPending.value"
                  @click="handleDelete(challenge.id ?? '')"
                />
              </div>
            </div>
          </div>
        </UCard>

        <UModal v-model:open="isModalOpen">
          <template #content>
            <UCard>
              <template #header>
                <div class="flex items-center justify-between">
                  <h3 class="text-base font-semibold">
                    {{ isEditing ? 'Edit Challenge' : 'Create Challenge' }}
                  </h3>
                  <UButton
                    variant="ghost"
                    icon="i-lucide-x"
                    size="xs"
                    @click="isModalOpen = false"
                  />
                </div>
              </template>

              <form
                class="space-y-4"
                @submit.prevent="handleSubmit"
              >
                <UFormField label="Title">
                  <UInput
                    v-model="form.title"
                    placeholder="Challenge title"
                  />
                </UFormField>

                <UFormField label="Description">
                  <UTextarea
                    v-model="form.description"
                    placeholder="Challenge description"
                    :rows="3"
                  />
                </UFormField>

                <UFormField label="Criteria">
                  <UTextarea
                    v-model="form.criteria"
                    placeholder="Judging criteria"
                    :rows="3"
                  />
                </UFormField>

                <UCheckbox
                  v-model="form.isPublished"
                  label="Published"
                />

                <div class="flex justify-end gap-2">
                  <UButton
                    variant="ghost"
                    @click="isModalOpen = false"
                  >
                    Cancel
                  </UButton>
                  <UButton
                    type="submit"
                    :loading="isSubmitting"
                  >
                    {{ isEditing ? 'Update' : 'Create' }}
                  </UButton>
                </div>
              </form>
            </UCard>
          </template>
        </UModal>
      </div>
    </template>
  </UDashboardPanel>
</template>
