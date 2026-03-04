<script setup lang="ts">
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useVirtualList } from '@vueuse/core'
import { computed, ref } from 'vue'
import { judgeQueries, useCreateJudgeMutation, useUpdateJudgeMutation } from '~/composables/judges'

const props = withDefaults(defineProps<{
  hackathonId?: string
}>(), {
  hackathonId: '',
})
const route = useRoute()
const hackathonId = computed(() => props.hackathonId || (route.params.hackathonId as string | undefined) || '')

const queryClient = useQueryClient()

const { data: judgesData, isLoading: isLoadingJudges } = useQuery(
  computed(() => ({
    ...judgeQueries.list(hackathonId.value),
    enabled: !!hackathonId.value,
  })),
)

const judges = computed(() => judgesData.value?.judges ?? [])

const {
  list: virtualJudges,
  containerProps: judgesContainerProps,
  wrapperProps: judgesWrapperProps,
} = useVirtualList(judges, {
  itemHeight: 56,
  overscan: 8,
})

// Mutations
const createMutation = useCreateJudgeMutation(hackathonId)
const updateMutation = useUpdateJudgeMutation(hackathonId)

// Modal state
const isModalOpen = ref(false)
const isEditing = ref(false)
const editingJudgeId = ref<string | null>(null)

// Form state
const form = ref({
  name: '',
  active: true,
  regenerateSecret: false,
})

function resetForm() {
  form.value = {
    name: '',
    active: true,
    regenerateSecret: false,
  }
  isEditing.value = false
  editingJudgeId.value = null
}

function openCreateModal() {
  resetForm()
  isModalOpen.value = true
}

function openEditModal(judge: typeof judges.value[number]) {
  form.value = {
    name: judge.name ?? '',
    active: judge.active ?? true,
    regenerateSecret: false,
  }
  isEditing.value = true
  editingJudgeId.value = judge.id ?? null
  isModalOpen.value = true
}

async function handleSubmit() {
  if (isEditing.value && editingJudgeId.value) {
    await updateMutation.mutateAsync({
      judgeId: editingJudgeId.value,
      data: {
        name: form.value.name,
        active: form.value.active,
        regenerateSecret: form.value.regenerateSecret,
      },
    })
  }
  else {
    await createMutation.mutateAsync({ name: form.value.name })
  }
  await queryClient.invalidateQueries({ queryKey: ['hackathons', hackathonId.value, 'judges'] })
  isModalOpen.value = false
  resetForm()
}

const isSubmitting = computed(() => createMutation.isPending.value || updateMutation.isPending.value)
</script>

<template>
  <UDashboardPanel id="judges">
    <template #header>
      <UDashboardNavbar title="Judges">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="p-4 space-y-4 overflow-y-auto">
        <UCard>
          <template #header>
            <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <h3 class="text-sm font-semibold">
                Judges
              </h3>
              <div class="flex items-center gap-2">
                <UBadge
                  variant="subtle"
                  size="sm"
                >
                  {{ judges.length }} total
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
            v-if="isLoadingJudges"
            class="text-(--ui-text-muted) text-sm"
          >
            Loading judges...
          </div>

          <div
            v-else-if="!judges.length"
            class="text-(--ui-text-muted) text-sm"
          >
            No judges yet.
          </div>

          <div
            v-else
            v-bind="judgesContainerProps"
            class="max-h-[36rem] overflow-y-auto"
          >
            <div
              v-bind="judgesWrapperProps"
              class="divide-y divide-(--ui-border)"
            >
              <div
                v-for="{ data: judge, index } in virtualJudges"
                :key="judge.id ?? index"
                class="py-2 flex items-center justify-between"
              >
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium">
                    {{ judge.name }}
                  </p>
                  <p class="text-xs text-(--ui-text-muted) truncate">
                    Secret: {{ judge.secret }}
                  </p>
                </div>
                <div class="flex flex-wrap items-center justify-end gap-2 ml-2">
                  <UBadge
                    :color="judge.active ? 'success' : 'warning'"
                    variant="subtle"
                    size="xs"
                  >
                    {{ judge.active ? 'Active' : 'Inactive' }}
                  </UBadge>
                  <UButton
                    size="xs"
                    variant="ghost"
                    icon="i-lucide-pencil"
                    @click="openEditModal(judge)"
                  />
                </div>
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
                    {{ isEditing ? 'Edit Judge' : 'Create Judge' }}
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
                <UFormField label="Name">
                  <UInput
                    v-model="form.name"
                    placeholder="Judge name"
                  />
                </UFormField>

                <UCheckbox
                  v-if="isEditing"
                  v-model="form.active"
                  label="Active"
                />

                <UCheckbox
                  v-if="isEditing"
                  v-model="form.regenerateSecret"
                  label="Regenerate Secret"
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
