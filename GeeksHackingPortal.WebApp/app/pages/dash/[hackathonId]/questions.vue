<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsCreateCreateOptionDto,
  GeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListQuestionDto,
  GeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsUpdateUpdateOptionDto,
  GeeksHackingPortalApiEntitiesQuestionType,
} from '@geekshacking/portal-sdk'
import {
  geeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint1QueryKey,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsCreateEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsDeleteEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsInitializeEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsUpdateEndpoint1,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import { computed } from 'vue'

type Question = GeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListQuestionDto
type QuestionType = GeeksHackingPortalApiEntitiesQuestionType

const questionTypeValues = {
  Text: 'Text',
  LongText: 'LongText',
  Number: 'Number',
  SingleChoice: 'SingleChoice',
  MultipleChoice: 'MultipleChoice',
  Boolean: 'Boolean',
  Email: 'Email',
  Url: 'Url',
  Phone: 'Phone',
  Date: 'Date',
  Dropdown: 'Dropdown',
} as const satisfies Record<string, QuestionType>

const props = withDefaults(defineProps<{
  hackathonId?: string
}>(), {
  hackathonId: '',
})
const route = useRoute()
const hackathonId = computed(() => props.hackathonId || (route.params.hackathonId as string | undefined) || '')

const queryClient = useQueryClient()

const { data: questionsData, isLoading } = useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint1(
  computed(() => hackathonId.value),
)

const questions = computed(() => questionsData.value?.questions ?? [])

const editingId = ref<string | null>(null)
const isCreating = ref(false)
const editForm = ref({
  questionText: '',
  questionKey: '',
  helpText: '',
  isRequired: false,
  type: questionTypeValues.Text as QuestionType,
  displayOrder: 0,
  category: '',
  conditionalLogic: '',
  validationRules: '',
  optionsJson: '',
})

const typesWithOptions: QuestionType[] = [
  questionTypeValues.SingleChoice,
  questionTypeValues.MultipleChoice,
  questionTypeValues.Dropdown,
]
const showOptions = computed(() => typesWithOptions.includes(editForm.value.type))
const optionsJsonError = ref('')

const questionTypes = [
  { value: questionTypeValues.Text, label: 'Text' },
  { value: questionTypeValues.LongText, label: 'Long Text' },
  { value: questionTypeValues.Number, label: 'Number' },
  { value: questionTypeValues.SingleChoice, label: 'Single Choice' },
  { value: questionTypeValues.MultipleChoice, label: 'Multiple Choice' },
  { value: questionTypeValues.Boolean, label: 'Boolean' },
  { value: questionTypeValues.Email, label: 'Email' },
  { value: questionTypeValues.Url, label: 'URL' },
  { value: questionTypeValues.Phone, label: 'Phone' },
  { value: questionTypeValues.Date, label: 'Date' },
  { value: questionTypeValues.Dropdown, label: 'Dropdown' },
] as const

const updateMutation = useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsUpdateEndpoint1()
const initMutation = useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsInitializeEndpoint1()
const createMutation = useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsCreateEndpoint1()
const deleteMutation = useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsDeleteEndpoint1()

async function invalidateQuestions() {
  await queryClient.invalidateQueries({
    queryKey: geeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint1QueryKey(hackathonId.value),
  })
}

async function initializeQuestions() {
  if (!hackathonId.value)
    return
  await initMutation.mutateAsync({ activityId: hackathonId.value })
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
    type: question.type ?? questionTypeValues.Text,
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
    type: questionTypeValues.Text,
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
  if (!hasOptions)
    return null
  const raw = editForm.value.optionsJson.trim()
  if (!raw || raw === '[]')
    return []
  const parsed = JSON.parse(raw)
  if (!Array.isArray(parsed))
    throw new Error('Options must be an array')
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
      questionKey: editForm.value.questionKey || undefined,
      helpText: editForm.value.helpText || null,
      isRequired: editForm.value.isRequired,
      type: editForm.value.type,
      displayOrder: editForm.value.displayOrder,
      category: editForm.value.category || null,
      conditionalLogic: editForm.value.conditionalLogic || null,
      validationRules: editForm.value.validationRules || null,
      options: options as typeof options & GeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsCreateCreateOptionDto[],
    }
    await createMutation.mutateAsync({ activityId: hackathonId.value, data: createData })
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
      options: options as typeof options & GeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsUpdateUpdateOptionDto[],
    }
    await updateMutation.mutateAsync({
      activityId: hackathonId.value,
      questionId: editingId.value,
      data: updateData,
    })
    editingId.value = null
  }

  await invalidateQuestions()
}

async function deleteQuestion(questionId: string) {
  if (!confirm('Are you sure you want to delete this question?'))
    return
  await deleteMutation.mutateAsync({ activityId: hackathonId.value, questionId })
  await invalidateQuestions()
}

async function deleteAllQuestions() {
  const count = questions.value.length
  if (!confirm(`Are you sure you want to delete all ${count} questions? This action cannot be undone.`))
    return

  for (const question of questions.value) {
    if (question.id) {
      await deleteMutation.mutateAsync({ activityId: hackathonId.value, questionId: question.id })
    }
  }

  await invalidateQuestions()
}

function getTypeName(type: QuestionType | null | undefined): string {
  return questionTypes.find(t => t.value === type)?.label ?? 'Unknown'
}
</script>

<template>
  <UDashboardPanel id="questions">
    <template #header>
      <UDashboardNavbar title="Questions">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="space-y-3">
        <div class="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
          <UBadge
            v-if="questions.length"
            variant="subtle"
            size="xs"
            class="w-fit"
          >
            {{ questions.length }}
          </UBadge>
          <div
            v-if="questions.length && !isCreating && !editingId"
            class="flex flex-col gap-2 sm:flex-row"
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

        <div class="rounded-xl border border-(--ui-border) bg-(--ui-bg) px-4 py-3">
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
                    @update:model-value="editForm.type = $event as QuestionType"
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
                    @update:model-value="editForm.type = $event as QuestionType"
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
        </div>
      </div>
    </template>
  </UDashboardPanel>
</template>
