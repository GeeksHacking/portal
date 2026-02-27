<script setup lang="ts">
import { computed } from 'vue'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { registrationQuestionOrganizerQueries, useUpdateQuestionMutation, useInitQuestionMutation, useCreateQuestionMutation, useDeleteQuestionMutation } from '~/composables/question'
import type {
  HackOManiaApiEndpointsOrganizersHackathonRegistrationQuestionsListQuestionDto,
  HackOManiaApiEndpointsOrganizersHackathonRegistrationQuestionsUpdateUpdateOptionDto,
} from '~/api-client/models'

type Question = HackOManiaApiEndpointsOrganizersHackathonRegistrationQuestionsListQuestionDto

const route = useRoute()
const props = withDefaults(defineProps<{
  hackathonId?: string
}>(), {
  hackathonId: '',
})
const hackathonId = computed(() => props.hackathonId || (route.params.hackathonId as string | undefined) || '')

const queryClient = useQueryClient()

const { data: questionsData, isLoading } = useQuery(
  computed(() => ({
    ...registrationQuestionOrganizerQueries.list(hackathonId.value),
    enabled: !!hackathonId.value,
  })),
)

const questions = computed(() => questionsData.value?.questions ?? [])

const editingId = ref<string | null>(null)
const isCreating = ref(false)
const editForm = ref({
  questionText: '',
  questionKey: '',
  helpText: '',
  isRequired: false,
  type: 0 as number,
  displayOrder: 0,
  category: '',
  conditionalLogic: '',
  validationRules: '',
  optionsJson: '',
})

const typesWithOptions = [3, 4, 10] // SingleChoice, MultipleChoice, Dropdown
const showOptions = computed(() => typesWithOptions.includes(editForm.value.type))
const optionsJsonError = ref('')

const questionTypes = [
  { value: 0, label: 'Text' },
  { value: 1, label: 'Long Text' },
  { value: 2, label: 'Number' },
  { value: 3, label: 'Single Choice' },
  { value: 4, label: 'Multiple Choice' },
  { value: 5, label: 'Boolean' },
  { value: 6, label: 'Email' },
  { value: 7, label: 'URL' },
  { value: 8, label: 'Phone' },
  { value: 9, label: 'Date' },
  { value: 10, label: 'Dropdown' },
]

const updateMutation = useUpdateQuestionMutation(hackathonId)
const initMutation = useInitQuestionMutation()
const createMutation = useCreateQuestionMutation(hackathonId)
const deleteMutation = useDeleteQuestionMutation(hackathonId)

async function invalidateQuestions() {
  await queryClient.invalidateQueries({
    queryKey: ['hackathons', hackathonId.value, 'registration', 'questions', 'organizer'],
  })
}

async function initializeQuestions() {
  if (!hackathonId.value) return
  await initMutation.mutateAsync(hackathonId.value)
  await invalidateQuestions()
}

function startEditing(question: Question) {
  editingId.value = question.id ?? null
  optionsJsonError.value = ''
  const options = (question.options ?? []).map((o, i) => ({
    optionText: o.optionText ?? '',
    optionValue: o.optionValue ?? '',
    displayOrder: o.displayOrder ?? i,
    hasFollowUpText: o.hasFollowUpText ?? false,
    followUpPlaceholder: o.followUpPlaceholder ?? null,
  }))
  editForm.value = {
    questionText: question.questionText ?? '',
    questionKey: question.questionKey ?? '',
    helpText: question.helpText ?? '',
    isRequired: question.isRequired ?? false,
    type: question.type ?? 0,
    displayOrder: question.displayOrder ?? 0,
    category: question.category ?? '',
    conditionalLogic: question.conditionalLogic ?? '',
    validationRules: question.validationRules ?? '',
    optionsJson: options.length ? JSON.stringify(options, null, 2) : '[]',
  }
}

function startCreating() {
  isCreating.value = true
  editingId.value = null
  optionsJsonError.value = ''
  const nextDisplayOrder = questions.value.length > 0
    ? Math.max(...questions.value.map(q => q.displayOrder ?? 0)) + 1
    : 0
  editForm.value = {
    questionText: '',
    questionKey: '',
    helpText: '',
    isRequired: false,
    type: 0,
    displayOrder: nextDisplayOrder,
    category: '',
    conditionalLogic: '',
    validationRules: '',
    optionsJson: '[]',
  }
}

function cancelEditing() {
  editingId.value = null
  isCreating.value = false
}

function parseOptionsJson(): Record<string, unknown>[] | null {
  const hasOptions = typesWithOptions.includes(editForm.value.type)
  if (!hasOptions) return null
  const raw = editForm.value.optionsJson.trim()
  if (!raw || raw === '[]') return []
  const parsed = JSON.parse(raw)
  if (!Array.isArray(parsed)) throw new Error('Options must be an array')
  return parsed
}

async function saveQuestion() {
  optionsJsonError.value = ''

  let options: Record<string, unknown>[] | null
  try {
    options = parseOptionsJson()
  }
  catch (e) {
    optionsJsonError.value = e instanceof Error ? e.message : 'Invalid JSON'
    return
  }

  if (isCreating.value) {
    const createData = {
      questionText: editForm.value.questionText,
      questionKey: editForm.value.questionKey || null,
      helpText: editForm.value.helpText || null,
      isRequired: editForm.value.isRequired,
      type: editForm.value.type,
      displayOrder: editForm.value.displayOrder,
      category: editForm.value.category || null,
      conditionalLogic: editForm.value.conditionalLogic || null,
      validationRules: editForm.value.validationRules || null,
      options: options as typeof options & HackOManiaApiEndpointsOrganizersHackathonRegistrationQuestionsUpdateUpdateOptionDto[],
    }
    await createMutation.mutateAsync(createData)
    isCreating.value = false
  }
  else if (editingId.value) {
    const updateData = {
      questionText: editForm.value.questionText,
      helpText: editForm.value.helpText || null,
      isRequired: editForm.value.isRequired,
      type: editForm.value.type,
      displayOrder: editForm.value.displayOrder,
      category: editForm.value.category || null,
      conditionalLogic: editForm.value.conditionalLogic || null,
      validationRules: editForm.value.validationRules || null,
      options: options as typeof options & HackOManiaApiEndpointsOrganizersHackathonRegistrationQuestionsUpdateUpdateOptionDto[],
    }
    await updateMutation.mutateAsync({
      questionId: editingId.value,
      data: updateData,
    })
    editingId.value = null
  }

  await invalidateQuestions()
}

async function deleteQuestion(questionId: string) {
  if (!confirm('Are you sure you want to delete this question?')) return
  await deleteMutation.mutateAsync(questionId)
  await invalidateQuestions()
}

async function deleteAllQuestions() {
  const count = questions.value.length
  if (!confirm(`Are you sure you want to delete all ${count} questions? This action cannot be undone.`)) return

  for (const question of questions.value) {
    if (question.id) {
      await deleteMutation.mutateAsync(question.id)
    }
  }

  await invalidateQuestions()
}

function getTypeName(type: number | null | undefined): string {
  return questionTypes.find(t => t.value === type)?.label ?? 'Unknown'
}
</script>

<template>
  <UCard>
    <template #header>
      <div class="flex items-center justify-between gap-2">
        <div class="flex items-center gap-2">
          <h3 class="text-sm font-semibold">
            Questions
          </h3>
          <UBadge
            v-if="questions.length"
            variant="subtle"
            size="xs"
          >
            {{ questions.length }}
          </UBadge>
        </div>
        <div
          v-if="questions.length && !isCreating && !editingId"
          class="flex gap-2"
        >
          <UButton
            size="xs"
            variant="ghost"
            color="error"
            icon="i-lucide-trash-2"
            :loading="deleteMutation.isPending.value"
            @click="deleteAllQuestions"
          >
            Delete All
          </UButton>
          <UButton
            size="xs"
            icon="i-lucide-plus"
            @click="startCreating"
          >
            Add Question
          </UButton>
        </div>
      </div>
    </template>

    <div
      v-if="isLoading"
      class="text-(--ui-text-muted) text-sm"
    >
      Loading questions...
    </div>

    <div
      v-else-if="!questions.length"
      class="text-(--ui-text-muted) text-sm flex items-center justify-between"
    >
      <span>No registration questions yet.</span>
      <UButton
        size="xs"
        :loading="initMutation.isPending.value"
        :disabled="!hackathonId"
        @click="initializeQuestions"
      >
        Get standard questions
      </UButton>
    </div>

    <div
      v-else
      class="divide-y divide-(--ui-border)"
    >
      <!-- Create new question form -->
      <div
        v-if="isCreating"
        class="py-2"
      >
        <form
          class="space-y-3"
          @submit.prevent="saveQuestion"
        >
          <UFormField label="Question Text">
            <UTextarea
              v-model="editForm.questionText"
              size="sm"
              autoresize
              class="w-full"
            />
          </UFormField>

          <UFormField label="Question Key">
            <UInput
              v-model="editForm.questionKey"
              size="sm"
              placeholder="e.g. email, full_name, university"
            />
          </UFormField>

          <UFormField label="Type">
            <USelect
              :model-value="editForm.type"
              :items="questionTypes.map(t => ({ label: t.label, value: t.value }))"
              size="sm"
              class="w-full"
              @update:model-value="editForm.type = Number($event)"
            />
          </UFormField>

          <!-- Options JSON editor for choice-based types -->
          <UFormField
            v-if="showOptions"
            label="Options (JSON)"
            :error="optionsJsonError"
          >
            <UTextarea
              v-model="editForm.optionsJson"
              size="sm"
              :rows="6"
              placeholder="[{ &quot;optionText&quot;: &quot;Label&quot;, &quot;optionValue&quot;: &quot;value&quot;, &quot;displayOrder&quot;: 0 }]"
              class="w-full font-mono text-xs"
            />
          </UFormField>

          <UFormField label="Help Text">
            <UInput
              v-model="editForm.helpText"
              size="sm"
              placeholder="Optional help text"
            />
          </UFormField>

          <UFormField label="Category">
            <UInput
              v-model="editForm.category"
              size="sm"
              placeholder="e.g. Personal Info, Preferences"
            />
          </UFormField>

          <UFormField label="Display Order">
            <UInput
              v-model.number="editForm.displayOrder"
              type="number"
              size="sm"
            />
          </UFormField>

          <UFormField label="Conditional Logic">
            <UInput
              v-model="editForm.conditionalLogic"
              size="sm"
              placeholder="e.g. {&quot;questionKey&quot;: &quot;value&quot;}"
            />
          </UFormField>

          <UFormField label="Validation Rules">
            <UInput
              v-model="editForm.validationRules"
              size="sm"
              placeholder="Optional validation rules"
            />
          </UFormField>

          <label class="flex items-center gap-2 text-sm">
            <UCheckbox v-model="editForm.isRequired" />
            Required
          </label>

          <div class="flex justify-end gap-2">
            <UButton
              size="xs"
              variant="ghost"
              @click="cancelEditing"
            >
              Cancel
            </UButton>
            <UButton
              size="xs"
              type="submit"
              :loading="createMutation.isPending.value"
            >
              Create
            </UButton>
          </div>
        </form>
      </div>

      <div
        v-for="question in questions"
        :key="question.id ?? ''"
        class="py-2"
      >
        <!-- View mode -->
        <div
          v-if="editingId !== question.id"
          class="flex items-center justify-between"
        >
          <div class="flex-1 min-w-0">
            <p class="text-sm font-medium">
              {{ question.questionText }}
              <UBadge
                v-if="question.isRequired"
                color="error"
                variant="subtle"
                size="xs"
                class="ml-1"
              >
                Required
              </UBadge>
            </p>
            <p
              v-if="question.helpText"
              class="text-xs text-(--ui-text-muted)"
            >
              {{ question.helpText }}
            </p>
            <div class="flex flex-wrap gap-2 mt-1">
              <UBadge
                variant="subtle"
                size="xs"
              >
                {{ getTypeName(question.type) }}
              </UBadge>
              <UBadge
                v-if="question.questionKey"
                variant="outline"
                size="xs"
              >
                {{ question.questionKey }}
              </UBadge>
              <UBadge
                v-if="question.category"
                variant="outline"
                size="xs"
              >
                {{ question.category }}
              </UBadge>
              <UBadge
                variant="outline"
                size="xs"
              >
                Order: {{ question.displayOrder ?? 0 }}
              </UBadge>
            </div>
            <div
              v-if="question.options?.length"
              class="flex flex-wrap gap-1 mt-1"
            >
              <UBadge
                v-for="opt in question.options"
                :key="opt.id ?? ''"
                variant="subtle"
                color="neutral"
                size="xs"
              >
                {{ opt.optionText }}
              </UBadge>
            </div>
          </div>
          <div class="flex gap-1">
            <UButton
              size="xs"
              variant="ghost"
              icon="i-lucide-pencil"
              @click="startEditing(question)"
            />
            <UButton
              size="xs"
              variant="ghost"
              icon="i-lucide-trash-2"
              color="error"
              :loading="deleteMutation.isPending.value"
              @click="deleteQuestion(question.id ?? '')"
            />
          </div>
        </div>

        <!-- Edit mode -->
        <form
          v-else
          class="space-y-3"
          @submit.prevent="saveQuestion"
        >
          <UFormField label="Question Text">
            <UTextarea
              v-model="editForm.questionText"
              size="sm"
              autoresize
              class="w-full"
            />
          </UFormField>

          <UFormField label="Question Key">
            <UInput
              v-model="editForm.questionKey"
              size="sm"
              disabled
              placeholder="Cannot be changed after creation"
            />
          </UFormField>

          <UFormField label="Type">
            <USelect
              :model-value="editForm.type"
              :items="questionTypes.map(t => ({ label: t.label, value: t.value }))"
              size="sm"
              class="w-full"
              @update:model-value="editForm.type = Number($event)"
            />
          </UFormField>

          <!-- Options JSON editor for choice-based types -->
          <UFormField
            v-if="showOptions"
            label="Options (JSON)"
            :error="optionsJsonError"
          >
            <UTextarea
              v-model="editForm.optionsJson"
              size="sm"
              :rows="6"
              placeholder="[{ &quot;optionText&quot;: &quot;Label&quot;, &quot;optionValue&quot;: &quot;value&quot;, &quot;displayOrder&quot;: 0 }]"
              class="w-full font-mono text-xs"
            />
          </UFormField>

          <UFormField label="Help Text">
            <UInput
              v-model="editForm.helpText"
              size="sm"
              placeholder="Optional help text"
            />
          </UFormField>

          <UFormField label="Category">
            <UInput
              v-model="editForm.category"
              size="sm"
              placeholder="e.g. Personal Info, Preferences"
            />
          </UFormField>

          <UFormField label="Display Order">
            <UInput
              v-model.number="editForm.displayOrder"
              type="number"
              size="sm"
            />
          </UFormField>

          <UFormField label="Conditional Logic">
            <UInput
              v-model="editForm.conditionalLogic"
              size="sm"
              placeholder="e.g. {&quot;questionKey&quot;: &quot;value&quot;}"
            />
          </UFormField>

          <UFormField label="Validation Rules">
            <UInput
              v-model="editForm.validationRules"
              size="sm"
              placeholder="Optional validation rules"
            />
          </UFormField>

          <label class="flex items-center gap-2 text-sm">
            <UCheckbox v-model="editForm.isRequired" />
            Required
          </label>

          <div class="flex justify-end gap-2">
            <UButton
              size="xs"
              variant="ghost"
              @click="cancelEditing"
            >
              Cancel
            </UButton>
            <UButton
              size="xs"
              type="submit"
              :loading="updateMutation.isPending.value"
            >
              Save
            </UButton>
          </div>
        </form>
      </div>
    </div>
  </UCard>
</template>
