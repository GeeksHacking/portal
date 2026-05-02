<script setup lang="ts">
import type {
  GeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsCreateCreateOptionDto,
  GeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListQuestionDto,
  GeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsUpdateUpdateOptionDto,
  GeeksHackingPortalApiEntitiesQuestionType,
} from '@geekshacking/portal-sdk'
import {
  geeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint2QueryKey,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsCreateEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsDeleteEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsInitializeEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsUpdateEndpoint2,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'

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

const route = useRoute()
const queryClient = useQueryClient()
const toast = useToast()

const standaloneWorkshopId = computed(() => (route.params.standaloneWorkshopId as string | undefined) ?? '')

const { data: questionsData, isLoading } = useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint2(
  standaloneWorkshopId,
)

const questions = computed(() => questionsData.value?.questions ?? [])

const editingId = ref<string | null>(null)
const isCreating = ref(false)
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

const choiceTypes: QuestionType[] = [
  questionTypeValues.SingleChoice,
  questionTypeValues.MultipleChoice,
  questionTypeValues.Dropdown,
]

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
  optionsJson: '[]',
})

const showOptions = computed(() => choiceTypes.includes(editForm.value.type))

const createMutation = useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsCreateEndpoint2()
const updateMutation = useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsUpdateEndpoint2()
const deleteMutation = useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsDeleteEndpoint2()
const initMutation = useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsInitializeEndpoint2()

async function invalidateQuestions() {
  await queryClient.invalidateQueries({
    queryKey: geeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint2QueryKey(
      standaloneWorkshopId.value,
    ),
  })
}

async function initializeDefaultQuestions() {
  if (!standaloneWorkshopId.value || questions.value.length)
    return

  try {
    const result = await initMutation.mutateAsync({
      activityId: standaloneWorkshopId.value,
    })

    await invalidateQuestions()
    toast.add({
      title: 'Default questions created',
      description: result.message ?? `${result.questionsCreated ?? 0} workshop registration questions were added.`,
      color: 'success',
    })
  }
  catch (error) {
    console.error('Failed to initialize default workshop questions', error)
    toast.add({
      title: 'Failed to create default questions',
      description: 'Please try again or add questions manually.',
      color: 'error',
    })
  }
}

function startCreating() {
  isCreating.value = true
  editingId.value = null
  optionsJsonError.value = ''

  const nextDisplayOrder = questions.value.length
    ? Math.max(...questions.value.map(question => question.displayOrder ?? 0)) + 1
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

function startEditing(question: Question) {
  editingId.value = question.id ?? null
  isCreating.value = false
  optionsJsonError.value = ''

  const options = (question.options ?? []).map((option, index) => ({
    optionText: option.optionText ?? '',
    optionValue: option.optionValue ?? '',
    displayOrder: option.displayOrder ?? index,
    hasFollowUpText: option.hasFollowUpText ?? false,
    followUpPlaceholder: option.followUpPlaceholder ?? null,
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

function cancelEditing() {
  editingId.value = null
  isCreating.value = false
  optionsJsonError.value = ''
}

function parseOptions() {
  if (!showOptions.value)
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
  if (!standaloneWorkshopId.value)
    return

  optionsJsonError.value = ''

  let options: Record<string, unknown>[] | null
  try {
    options = parseOptions()
  }
  catch (error) {
    optionsJsonError.value = error instanceof Error ? error.message : 'Invalid JSON'
    return
  }

  if (isCreating.value) {
    await createMutation.mutateAsync({
      activityId: standaloneWorkshopId.value,
      data: {
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
      },
    })
    isCreating.value = false
  }
  else if (editingId.value) {
    await updateMutation.mutateAsync({
      activityId: standaloneWorkshopId.value,
      questionId: editingId.value,
      data: {
        questionText: editForm.value.questionText || null,
        helpText: editForm.value.helpText || null,
        isRequired: editForm.value.isRequired,
        type: editForm.value.type,
        displayOrder: editForm.value.displayOrder,
        category: editForm.value.category || null,
        conditionalLogic: editForm.value.conditionalLogic || null,
        validationRules: editForm.value.validationRules || null,
        options: options as typeof options & GeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsUpdateUpdateOptionDto[],
      },
    })
    editingId.value = null
  }

  await invalidateQuestions()
}

async function deleteQuestion(questionId: string) {
  if (!standaloneWorkshopId.value)
    return
  if (!confirm('Are you sure you want to delete this question?'))
    return

  await deleteMutation.mutateAsync({
    activityId: standaloneWorkshopId.value,
    questionId,
  })

  await invalidateQuestions()
}

async function deleteAllQuestions() {
  if (!standaloneWorkshopId.value || !questions.value.length)
    return
  if (!confirm(`Are you sure you want to delete all ${questions.value.length} questions? This action cannot be undone.`))
    return

  for (const question of questions.value) {
    if (question.id) {
      await deleteMutation.mutateAsync({
        activityId: standaloneWorkshopId.value,
        questionId: question.id,
      })
    }
  }

  await invalidateQuestions()
}

function getTypeLabel(type: QuestionType | null | undefined) {
  return questionTypes.find(questionType => questionType.value === type)?.label ?? 'Unknown'
}
</script>

<template>
  <UDashboardPanel id="standalone-questions">
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
            class="flex flex-col gap-2 sm:flex-row"
          >
            <UButton
              v-if="questions.length && !isCreating && !editingId"
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
              v-if="!isCreating && !editingId"
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
            class="text-sm text-(--ui-text-muted)"
          >
            Loading questions...
          </div>

          <div
            v-else-if="!questions.length && !isCreating"
            class="flex flex-col gap-3 text-sm text-(--ui-text-muted) sm:flex-row sm:items-center sm:justify-between"
          >
            <span>No registration questions yet.</span>
            <div class="flex flex-col gap-2 sm:flex-row">
              <UButton
                size="xs"
                :loading="initMutation.isPending.value"
                :disabled="!standaloneWorkshopId"
                @click="initializeDefaultQuestions"
              >
                Get standard questions
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

          <div
            v-else
            class="divide-y divide-(--ui-border)"
          >
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
                    placeholder="e.g. company, experience_level"
                  />
                </UFormField>

                <UFormField label="Type">
                  <USelect
                    :model-value="editForm.type"
                    :items="questionTypes.map(questionType => ({ label: questionType.label, value: questionType.value }))"
                    size="sm"
                    class="w-full"
                    @update:model-value="editForm.type = $event as QuestionType"
                  />
                </UFormField>

                <UFormField
                  v-if="showOptions"
                  label="Options (JSON)"
                  :error="optionsJsonError"
                >
                  <UTextarea
                    v-model="editForm.optionsJson"
                    size="sm"
                    :rows="6"
                    placeholder="[{ &quot;optionText&quot;: &quot;Beginner&quot;, &quot;optionValue&quot;: &quot;beginner&quot;, &quot;displayOrder&quot;: 0 }]"
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
                    placeholder="e.g. Background, Preferences"
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
                    placeholder="Optional conditional JSON"
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
              <div
                v-if="editingId !== question.id"
                class="flex items-center justify-between gap-3"
              >
                <div class="min-w-0 flex-1">
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
                  <div class="mt-1 flex flex-wrap gap-2">
                    <UBadge
                      variant="subtle"
                      size="xs"
                    >
                      {{ getTypeLabel(question.type) }}
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
                    class="mt-1 flex flex-wrap gap-1"
                  >
                    <UBadge
                      v-for="option in question.options"
                      :key="option.id ?? option.optionValue ?? option.optionText ?? ''"
                      variant="subtle"
                      size="xs"
                    >
                      {{ option.optionText }}
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
                    color="error"
                    icon="i-lucide-trash-2"
                    :loading="deleteMutation.isPending.value"
                    @click="deleteQuestion(question.id ?? '')"
                  />
                </div>
              </div>

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
                  />
                </UFormField>

                <UFormField label="Type">
                  <USelect
                    :model-value="editForm.type"
                    :items="questionTypes.map(questionType => ({ label: questionType.label, value: questionType.value }))"
                    size="sm"
                    class="w-full"
                    @update:model-value="editForm.type = $event as QuestionType"
                  />
                </UFormField>

                <UFormField
                  v-if="showOptions"
                  label="Options (JSON)"
                  :error="optionsJsonError"
                >
                  <UTextarea
                    v-model="editForm.optionsJson"
                    size="sm"
                    :rows="6"
                    placeholder="[{ &quot;optionText&quot;: &quot;Beginner&quot;, &quot;optionValue&quot;: &quot;beginner&quot;, &quot;displayOrder&quot;: 0 }]"
                    class="w-full font-mono text-xs"
                  />
                </UFormField>

                <UFormField label="Help Text">
                  <UInput
                    v-model="editForm.helpText"
                    size="sm"
                  />
                </UFormField>

                <UFormField label="Category">
                  <UInput
                    v-model="editForm.category"
                    size="sm"
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
                  />
                </UFormField>

                <UFormField label="Validation Rules">
                  <UInput
                    v-model="editForm.validationRules"
                    size="sm"
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
