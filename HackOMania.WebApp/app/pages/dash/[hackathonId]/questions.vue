<script setup lang="ts">
import { computed } from 'vue'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { registrationQuestionOrganizerQueries, useUpdateQuestionMutation, useInitQuestionMutation } from '~/composables/question'
import type {
  HackOManiaApiEndpointsOrganizersHackathonRegistrationQuestionsListQuestionDto,
  HackOManiaApiEndpointsOrganizersHackathonRegistrationQuestionsUpdateUpdateOptionDto,
} from '~/api-client/models'

type Question = HackOManiaApiEndpointsOrganizersHackathonRegistrationQuestionsListQuestionDto
type OptionForm = Required<Pick<HackOManiaApiEndpointsOrganizersHackathonRegistrationQuestionsUpdateUpdateOptionDto, 'optionText' | 'optionValue' | 'displayOrder' | 'hasFollowUpText'>> & { id?: string | null, followUpPlaceholder?: string | null }

const props = defineProps<{
  hackathonId: string
  isOrganizer: boolean
}>()

const queryClient = useQueryClient()

const { data: questionsData, isLoading } = useQuery(
  computed(() => ({
    ...registrationQuestionOrganizerQueries.list(props.hackathonId),
    enabled: !!props.hackathonId && props.isOrganizer,
  })),
)

const questions = computed(() => questionsData.value?.questions ?? [])

const editingId = ref<string | null>(null)
const editForm = ref({
  questionText: '',
  helpText: '',
  isRequired: false,
  type: 0 as number,
  displayOrder: 0,
  category: '',
  conditionalLogic: '',
  validationRules: '',
  options: [] as OptionForm[],
})

const typesWithOptions = [3, 4, 10] // SingleChoice, MultipleChoice, Dropdown
const showOptions = computed(() => typesWithOptions.includes(editForm.value.type))

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

const updateMutation = useUpdateQuestionMutation(props.hackathonId)
const initMutation = useInitQuestionMutation()

async function invalidateQuestions() {
  await queryClient.invalidateQueries({
    queryKey: ['hackathons', props.hackathonId, 'registration', 'questions', 'organizer'],
  })
}

async function initializeQuestions() {
  if (!props.hackathonId) return
  await initMutation.mutateAsync(props.hackathonId)
  await invalidateQuestions()
}

function startEditing(question: Question) {
  editingId.value = question.id ?? null
  editForm.value = {
    questionText: question.questionText ?? '',
    helpText: question.helpText ?? '',
    isRequired: question.isRequired ?? false,
    type: question.type ?? 0,
    displayOrder: question.displayOrder ?? 0,
    category: question.category ?? '',
    conditionalLogic: question.conditionalLogic ?? '',
    validationRules: question.validationRules ?? '',
    options: (question.options ?? []).map((o, i) => ({
      id: o.id,
      optionText: o.optionText ?? '',
      optionValue: o.optionValue ?? '',
      displayOrder: o.displayOrder ?? i,
      hasFollowUpText: o.hasFollowUpText ?? false,
      followUpPlaceholder: o.followUpPlaceholder ?? null,
    })),
  }
}

function addOption() {
  editForm.value.options.push({
    optionText: '',
    optionValue: '',
    displayOrder: editForm.value.options.length,
    hasFollowUpText: false,
    followUpPlaceholder: null,
  })
}

function removeOption(index: number) {
  editForm.value.options.splice(index, 1)
  editForm.value.options.forEach((o, i) => o.displayOrder = i)
}

function cancelEditing() {
  editingId.value = null
}

async function saveQuestion() {
  if (!editingId.value) return
  const hasOptions = typesWithOptions.includes(editForm.value.type)
  await updateMutation.mutateAsync({
    questionId: editingId.value,
    data: {
      questionText: editForm.value.questionText,
      helpText: editForm.value.helpText || null,
      isRequired: editForm.value.isRequired,
      type: editForm.value.type,
      displayOrder: editForm.value.displayOrder,
      category: editForm.value.category || null,
      conditionalLogic: editForm.value.conditionalLogic || null,
      validationRules: editForm.value.validationRules || null,
      options: hasOptions
        ? editForm.value.options.map(o => ({
            id: o.id,
            optionText: o.optionText,
            optionValue: o.optionValue,
            displayOrder: o.displayOrder,
            hasFollowUpText: o.hasFollowUpText,
            followUpPlaceholder: o.followUpPlaceholder,
          }))
        : null,
    },
  })
  editingId.value = null
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
          <UButton
            size="xs"
            variant="ghost"
            icon="i-lucide-pencil"
            @click="startEditing(question)"
          />
        </div>

        <!-- Edit mode -->
        <form
          v-else
          class="space-y-3"
          @submit.prevent="saveQuestion"
        >
          <UFormField label="Question Text">
            <UInput
              v-model="editForm.questionText"
              size="sm"
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

          <!-- Options editor for choice-based types -->
          <div
            v-if="showOptions"
            class="space-y-2"
          >
            <div class="flex items-center justify-between">
              <span class="text-sm font-medium">Options</span>
              <UButton
                size="xs"
                variant="soft"
                icon="i-lucide-plus"
                @click="addOption"
              >
                Add Option
              </UButton>
            </div>
            <div
              v-for="(option, idx) in editForm.options"
              :key="idx"
              class="flex items-start gap-2 p-2 rounded border border-(--ui-border)"
            >
              <div class="flex-1 space-y-2">
                <div class="flex gap-2">
                  <UInput
                    v-model="option.optionText"
                    size="sm"
                    placeholder="Display text"
                    class="flex-1"
                  />
                  <UInput
                    v-model="option.optionValue"
                    size="sm"
                    placeholder="Value"
                    class="flex-1"
                  />
                </div>
                <div class="flex items-center gap-3">
                  <label class="flex items-center gap-1 text-xs">
                    <UCheckbox
                      :model-value="option.hasFollowUpText ?? false"
                      size="xs"
                      @update:model-value="option.hasFollowUpText = Boolean($event)"
                    />
                    Follow-up text
                  </label>
                  <UInput
                    v-if="option.hasFollowUpText"
                    v-model="option.followUpPlaceholder"
                    size="sm"
                    placeholder="Follow-up placeholder"
                    class="flex-1"
                  />
                </div>
              </div>
              <UButton
                size="xs"
                variant="ghost"
                color="error"
                icon="i-lucide-trash-2"
                @click="removeOption(idx)"
              />
            </div>
            <p
              v-if="!editForm.options.length"
              class="text-xs text-(--ui-text-muted)"
            >
              No options yet. Add at least one option.
            </p>
          </div>

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
