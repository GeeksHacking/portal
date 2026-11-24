<script setup lang="ts">
import type {
  GeeksHackingPortalApiEntitiesQuestionTypeKey,
  GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListQuestionDto,
  GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListResponse,
} from '@geekshacking/portal-sdk'
import {
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsSubmitEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import { getApiValidationErrors } from '~/utils/api-errors'

/**
 * The workshop's registration questions, grouped by category. Mirrors the hackathon form
 * (RegistrationDynamicForm): required marks, follow-up fields for options that ask for detail,
 * "N required answers left", and on a failed submit an alert plus focus on the first bad answer.
 * Answers are final once submitted.
 */
const props = defineProps<{
  standaloneWorkshopId: string
  workshopTitle?: string
  questions: GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListResponse
}>()

const emit = defineEmits<{
  submitted: []
}>()

type Question = GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListQuestionDto
type QuestionType = GeeksHackingPortalApiEntitiesQuestionTypeKey

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

const toast = useGToast()
const queryClient = useQueryClient()

const { data: userData } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint({
  query: {
    retry: false,
    staleTime: 0,
    gcTime: 0,
  },
})

const submitMutation = useGeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsSubmitEndpoint()

const state = reactive<Record<string, string | string[]>>({})
const followUpState = reactive<Record<string, string>>({})
const fieldErrors = ref<Record<string, string>>({})
const submissionError = ref(false)

const allQuestions = computed<Question[]>(() =>
  props.questions.categories?.flatMap(category => category.questions ?? []) ?? [],
)

const formattedCategories = computed(() => {
  return (props.questions.categories ?? []).map((category, categoryIndex) => ({
    name: category.name ?? `Section ${categoryIndex + 1}`,
    questions: (category.questions ?? []).map((question, questionIndex) => {
      const items = (question.options ?? []).map(option => ({
        label: String(option.optionText ?? ''),
        value: String(option.optionValue ?? ''),
        hasFollowUpText: option.hasFollowUpText ?? false,
        followUpPlaceholder: option.followUpPlaceholder ?? '',
      }))

      return {
        ...question,
        key: question.id ?? `${categoryIndex}-${questionIndex}`,
        items,
        hasOptions: items.length > 0,
        isMultiSelect: question.type === questionTypeValues.MultipleChoice,
        isLong: question.type === questionTypeValues.LongText || (question.questionText?.length ?? 0) > 55,
      }
    }),
  }))
})

function standaloneWorkshopStatusQueryKey(standaloneWorkshopId: string) {
  return [{ url: '/participants/standalone-workshops/:standaloneWorkshopId/status', params: { standaloneWorkshopId } }] as const
}

function standaloneWorkshopRegistrationQuestionsQueryKey(standaloneWorkshopId: string) {
  return [{ url: '/participants/standalone-workshops/:standaloneWorkshopId/registration/questions', params: { standaloneWorkshopId } }] as const
}

function standaloneWorkshopRegistrationSubmissionsQueryKey(standaloneWorkshopId: string) {
  return [{ url: '/participants/standalone-workshops/:standaloneWorkshopId/registration/submissions', params: { standaloneWorkshopId } }] as const
}

function parseQuestionValue(question: Question) {
  const rawValue = question.currentSubmission?.value
  if (!rawValue)
    return question.type === questionTypeValues.MultipleChoice ? [] : ''

  if (question.type === questionTypeValues.MultipleChoice) {
    try {
      const parsed = JSON.parse(rawValue)
      return Array.isArray(parsed) ? parsed.map(value => String(value)) : []
    }
    catch {
      return []
    }
  }

  return String(rawValue)
}

function seedQuestionState() {
  for (const question of allQuestions.value) {
    if (!question.questionKey)
      continue

    if (state[question.questionKey] === undefined) {
      state[question.questionKey] = parseQuestionValue(question)
    }

    if (question.questionKey === 'github_profile' && !question.currentSubmission?.value && userData.value?.gitHubLogin) {
      state[question.questionKey] = `https://github.com/${userData.value.gitHubLogin}`
    }

    const followUpValue = question.currentSubmission?.followUpValue
    if (!followUpValue)
      continue

    try {
      const parsed = JSON.parse(followUpValue) as Record<string, string>
      for (const [optionValue, text] of Object.entries(parsed)) {
        followUpState[`${question.questionKey}:${optionValue}`] = text
      }
    }
    catch {
      const currentValue = state[question.questionKey]
      const selectedValue = Array.isArray(currentValue) ? currentValue[0] : currentValue
      if (selectedValue) {
        followUpState[`${question.questionKey}:${selectedValue}`] = followUpValue
      }
    }
  }
}

watch([allQuestions, userData], seedQuestionState, { immediate: true, deep: true })

function isQuestionVisible(conditionalLogic: string | null | undefined): boolean {
  if (!conditionalLogic)
    return true

  try {
    const conditions = JSON.parse(conditionalLogic) as Record<string, string | string[]>
    return Object.entries(conditions).every(([questionKey, expectedValue]) => {
      const currentValue = state[questionKey]

      if (Array.isArray(expectedValue)) {
        if (Array.isArray(currentValue))
          return expectedValue.some(value => currentValue.includes(value))
        return expectedValue.includes(String(currentValue ?? ''))
      }

      if (Array.isArray(currentValue))
        return currentValue.includes(expectedValue)

      return String(currentValue ?? '').toLowerCase() === expectedValue.toLowerCase()
    })
  }
  catch {
    return true
  }
}

function getSelectedFollowUpOptions(questionKey: string, items: { value: string, hasFollowUpText: boolean, followUpPlaceholder: string, label: string }[]) {
  const currentValue = state[questionKey]
  if (!currentValue)
    return []

  const selectedValues = Array.isArray(currentValue) ? currentValue : [currentValue]
  return items.filter(item => selectedValues.includes(item.value) && item.hasFollowUpText)
}

function getInputType(type: QuestionType | undefined) {
  switch (type) {
    case questionTypeValues.Number:
      return 'number'
    case questionTypeValues.Email:
      return 'email'
    case questionTypeValues.Url:
      return 'url'
    case questionTypeValues.Phone:
      return 'tel'
    case questionTypeValues.Date:
      return 'date'
    default:
      return 'text'
  }
}

function shouldSpanFullWidth(question: { isLong: boolean, type?: QuestionType, conditionalLogic?: string | null }) {
  return question.isLong || question.type === questionTypeValues.Boolean
}

// Required = marked required, or a follow-up question that is currently shown
function isRequiredQuestion(question: { isRequired?: boolean | null, conditionalLogic?: string | null }) {
  return !!question.isRequired || !!question.conditionalLogic
}

function parseErrorsToFields(error: unknown) {
  const errorBag = getApiValidationErrors(error)
  const questionById = new Map(
    allQuestions.value
      .filter(question => question.id)
      .map(question => [question.id as string, question]),
  )

  for (const [fieldId, messages] of Object.entries(errorBag)) {
    const question = questionById.get(fieldId)
    if (!question?.questionKey)
      continue

    const message = Array.isArray(messages) ? messages[0] : String(messages)
    if (message)
      fieldErrors.value[question.questionKey] = message
  }
}

// Visible required questions (and their follow-up texts) that still need an answer
const missingAnswers = computed(() => {
  const missing: string[] = []
  for (const question of allQuestions.value) {
    if (!isQuestionVisible(question.conditionalLogic) || !isRequiredQuestion(question))
      continue
    const key = question.questionKey
    if (!key) {
      missing.push(question.id ?? '?')
      continue
    }

    const value = state[key]
    const hasValue = Array.isArray(value) ? value.length > 0 : value !== '' && value !== null && value !== undefined
    if (!hasValue) {
      missing.push(key)
      continue
    }

    const selectedValues = Array.isArray(value) ? value : [value]
    for (const option of question.options ?? []) {
      const optionValue = String(option.optionValue ?? '')
      if (option.hasFollowUpText && selectedValues.includes(optionValue) && !followUpState[`${key}:${optionValue}`]?.trim())
        missing.push(`${key}:${optionValue}`)
    }
  }
  return missing
})

const isFormValid = computed(() => missingAnswers.value.length === 0)

const formEl = ref<HTMLFormElement | null>(null)

async function submitRegistration() {
  const submissions = Object.entries(state)
    .filter(([questionKey, value]) => {
      const question = allQuestions.value.find(item => item.questionKey === questionKey)
      if (!question || !isQuestionVisible(question.conditionalLogic))
        return false

      if (Array.isArray(value))
        return value.length > 0

      return value !== '' && value !== null && value !== undefined
    })
    .map(([questionKey, value]) => {
      const question = allQuestions.value.find(item => item.questionKey === questionKey)
      if (!question?.id)
        return null

      const selectedValues = Array.isArray(value) ? value : [value]
      const followUpValues: Record<string, string> = {}

      for (const optionValue of selectedValues) {
        const followUpKey = `${questionKey}:${optionValue}`
        const followUpValue = followUpState[followUpKey]
        if (followUpValue)
          followUpValues[String(optionValue)] = followUpValue
      }

      let followUpValue: string | null = null
      const followUpEntries = Object.entries(followUpValues)
      const onlyFollowUp = followUpEntries[0]
      if (onlyFollowUp && followUpEntries.length === 1) {
        followUpValue = onlyFollowUp[1]
      }
      else if (followUpEntries.length > 1) {
        followUpValue = JSON.stringify(followUpValues)
      }

      return {
        questionId: question.id,
        value: Array.isArray(value) ? JSON.stringify(value) : String(value),
        followUpValue,
      }
    })
    .filter((submission): submission is NonNullable<typeof submission> => submission !== null)

  try {
    fieldErrors.value = {}
    submissionError.value = false

    await submitMutation.mutateAsync({
      path: { standaloneWorkshopId: props.standaloneWorkshopId },
      body: { submissions },
    })

    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: standaloneWorkshopRegistrationQuestionsQueryKey(props.standaloneWorkshopId),
      }),
      queryClient.invalidateQueries({
        queryKey: standaloneWorkshopRegistrationSubmissionsQueryKey(props.standaloneWorkshopId),
      }),
      queryClient.invalidateQueries({
        queryKey: standaloneWorkshopStatusQueryKey(props.standaloneWorkshopId),
      }),
    ])

    toast.add({
      title: 'Registration saved',
      description: props.workshopTitle ? `Your answers for ${props.workshopTitle} have been saved.` : 'Your answers have been saved.',
      tone: 'ok',
    })

    emit('submitted')
  }
  catch (error) {
    submissionError.value = true
    parseErrorsToFields(error)
    console.error('Failed to submit standalone workshop registration', error)
    // take the user to the first answer that needs fixing
    await nextTick()
    formEl.value?.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus()
  }
}
</script>

<template>
  <form
    ref="formEl"
    class="ws-form"
    novalidate
    @submit.prevent="submitRegistration"
  >
    <p class="ws-form__lead">
      Questions marked <span aria-hidden="true">*</span><span class="sr-only">with an asterisk</span> are required. Your answers are only shared with the organisers and can't be changed after you submit.
    </p>

    <section
      v-for="(category, categoryIndex) in formattedCategories"
      :key="category.name"
      class="ws-form__section"
      :aria-labelledby="`ws-category-${categoryIndex}`"
    >
      <h3
        :id="`ws-category-${categoryIndex}`"
        class="ws-form__section-title"
      >
        {{ category.name }}
      </h3>

      <div class="ws-form__grid">
        <template
          v-for="question in category.questions"
          :key="question.key"
        >
          <div
            v-if="isQuestionVisible(question.conditionalLogic)"
            class="ws-form__question"
            :class="{ 'ws-form__question--wide': shouldSpanFullWidth(question) }"
          >
            <!-- a single option (e.g. consent): a labelled checkbox under the question; the checkbox carries the required mark -->
            <fieldset
              v-if="question.hasOptions && !question.isMultiSelect && question.items.length === 1"
              class="ws-form__fieldset"
              :aria-describedby="[question.helpText ? `ws-q-${question.key}-hint` : '', fieldErrors[question.questionKey ?? ''] ? `ws-q-${question.key}-error` : ''].filter(Boolean).join(' ') || undefined"
            >
              <legend class="ws-form__legend">
                {{ question.questionText ?? '' }}
              </legend>
              <GCheckbox
                :model-value="state[question.questionKey ?? ''] === question.items[0]?.value"
                :label="question.items[0]?.label"
                :required="isRequiredQuestion(question)"
                :invalid="!!fieldErrors[question.questionKey ?? '']"
                @update:model-value="(checked: boolean) => {
                  const value = question.items[0]?.value
                  if (value !== undefined)
                    state[question.questionKey ?? ''] = checked ? value : ''
                }"
              />
              <p
                v-if="question.helpText"
                :id="`ws-q-${question.key}-hint`"
                class="ws-form__hint"
              >
                {{ question.helpText }}
              </p>
              <p
                v-if="fieldErrors[question.questionKey ?? '']"
                :id="`ws-q-${question.key}-error`"
                class="ws-form__error"
              >
                {{ fieldErrors[question.questionKey ?? ''] }}
              </p>
            </fieldset>

            <GFormField
              v-else
              :label="question.questionText ?? ''"
              :hint="question.helpText ?? undefined"
              :error="fieldErrors[question.questionKey ?? '']"
              :required="isRequiredQuestion(question)"
            >
              <GSelect
                v-if="question.hasOptions && question.isMultiSelect"
                v-model="state[question.questionKey ?? ''] as string[]"
                :items="question.items"
                multiple
                placeholder="Select all that apply"
              />
              <GSelect
                v-else-if="question.hasOptions"
                v-model="state[question.questionKey ?? ''] as string"
                :items="question.items"
                placeholder="Select one"
              />
              <GSelect
                v-else-if="question.type === questionTypeValues.Boolean"
                v-model="state[question.questionKey ?? ''] as string"
                :items="[{ label: 'Yes', value: 'true' }, { label: 'No', value: 'false' }]"
                placeholder="Yes or no"
              />
              <GTextarea
                v-else-if="question.type === questionTypeValues.LongText"
                v-model="state[question.questionKey ?? ''] as string"
                :rows="4"
                autoresize
              />
              <GInput
                v-else
                v-model="state[question.questionKey ?? ''] as string"
                :type="getInputType(question.type)"
                :inputmode="question.type === questionTypeValues.Number ? 'numeric' : undefined"
                :autocomplete="question.type === questionTypeValues.Phone ? 'tel' : question.type === questionTypeValues.Email ? 'email' : undefined"
              />
            </GFormField>

            <!-- options that ask for more detail -->
            <GFormField
              v-for="followUpOption in getSelectedFollowUpOptions(question.questionKey ?? '', question.items)"
              :key="`${question.key}-${followUpOption.value}-follow-up`"
              :label="followUpOption.followUpPlaceholder || `Tell us more about “${followUpOption.label}”`"
              required
              class="ws-form__followup"
            >
              <GInput v-model="followUpState[`${question.questionKey}:${followUpOption.value}`]" />
            </GFormField>
          </div>
        </template>
      </div>
    </section>

    <GAlert
      v-if="submissionError"
      tone="err"
      title="Some answers need fixing"
      description="Check the highlighted questions, then submit again."
    />

    <div class="ws-form__submit">
      <p
        class="ws-form__remaining"
        aria-live="polite"
      >
        <template v-if="missingAnswers.length">
          {{ missingAnswers.length === 1 ? '1 required answer left' : `${missingAnswers.length} required answers left` }}
        </template>
        <template v-else>
          All required answers filled in
        </template>
      </p>
      <GButton
        type="submit"
        variant="primary"
        size="lg"
        label="Complete registration"
        :disabled="!isFormValid"
        :loading="submitMutation.isPending.value"
        loading-label="Submitting"
      />
    </div>
  </form>
</template>

<style scoped>
.ws-form {
  display: flex;
  flex-direction: column;
  gap: 32px;
  min-width: 0;
}
.ws-form__lead {
  max-width: 65ch;
  margin: 0;
  color: var(--gh-text-muted);
}
.ws-form__section {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.ws-form__section-title {
  margin: 0;
  padding-bottom: 8px;
  border-bottom: var(--gh-border-width) solid var(--gh-line-soft);
  font-family: var(--font-display);
  font-size: 18px;
  font-weight: 700;
}
.ws-form__grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 20px 24px;
}
@media (min-width: 768px) {
  .ws-form__grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
  .ws-form__question--wide { grid-column: 1 / -1; }
}
.ws-form__question {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
}
.ws-form__followup {
  padding-left: 14px;
  border-left: 3px solid var(--gh-accent);
}
.ws-form__fieldset {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 0;
  margin: 0;
  padding: 0;
  border: 0;
}
.ws-form__legend {
  padding: 0;
  margin-bottom: 8px;
  font-size: 14px;
  font-weight: 600;
}
.ws-form__required,
.ws-form__error {
  color: var(--gh-err);
}
.ws-form__hint,
.ws-form__error {
  margin: 0;
  font-size: 13px;
}
.ws-form__hint {
  color: var(--gh-text-muted);
}
.ws-form__submit {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 12px 16px;
  padding-top: 20px;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.ws-form__remaining {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
  color: var(--gh-text-muted);
}
</style>
