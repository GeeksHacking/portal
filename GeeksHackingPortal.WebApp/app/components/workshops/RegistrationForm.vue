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

const toast = useToast()
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

const visibleQuestions = computed(() =>
  allQuestions.value.filter(question => isQuestionVisible(question.conditionalLogic)),
)

const requiredQuestions = computed(() =>
  visibleQuestions.value.filter(question => Boolean(question.isRequired || question.conditionalLogic)),
)

const answeredRequiredCount = computed(() => {
  return requiredQuestions.value.filter((question) => {
    const key = question.questionKey
    if (!key)
      return false
    const value = state[key]
    const hasValue = Array.isArray(value) ? value.length > 0 : value !== '' && value !== null && value !== undefined
    if (!hasValue)
      return false
    const items = (question.options ?? []).map(option => ({
      label: String(option.optionText ?? ''),
      value: String(option.optionValue ?? ''),
      hasFollowUpText: option.hasFollowUpText ?? false,
      followUpPlaceholder: option.followUpPlaceholder ?? '',
    }))
    return getSelectedFollowUpOptions(key, items).every((item) => {
      const followUpValue = followUpState[`${key}:${item.value}`]
      return followUpValue && followUpValue.trim() !== ''
    })
  }).length
})

const requiredProgress = computed(() => ({
  answered: answeredRequiredCount.value,
  total: requiredQuestions.value.length,
}))

const isFormValid = computed(() => {
  const requiredQuestions = allQuestions.value.filter(question =>
    isQuestionVisible(question.conditionalLogic) && Boolean(question.isRequired || question.conditionalLogic),
  )

  return requiredQuestions.every((question) => {
    const key = question.questionKey
    if (!key)
      return false

    const value = state[key]
    const hasValue = Array.isArray(value) ? value.length > 0 : value !== '' && value !== null && value !== undefined
    if (!hasValue)
      return false

    const items = (question.options ?? []).map(option => ({
      label: String(option.optionText ?? ''),
      value: String(option.optionValue ?? ''),
      hasFollowUpText: option.hasFollowUpText ?? false,
      followUpPlaceholder: option.followUpPlaceholder ?? '',
    }))

    return getSelectedFollowUpOptions(key, items).every((item) => {
      const followUpValue = followUpState[`${key}:${item.value}`]
      return followUpValue && followUpValue.trim() !== ''
    })
  })
})

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
      title: 'You are registered',
      description: props.workshopTitle ? `Your spot for ${props.workshopTitle} is confirmed.` : 'Your workshop spot is confirmed.',
      color: 'success',
    })

    emit('submitted')
  }
  catch (error) {
    submissionError.value = true
    parseErrorsToFields(error)
    console.error('Failed to submit standalone workshop registration', error)
  }
}
</script>

<template>
  <UForm
    :state="state"
    class="space-y-5"
    @submit="submitRegistration"
  >
    <div
      v-for="category in formattedCategories"
      :key="category.name"
      class="rounded-lg bg-default/70 p-4 sm:p-5"
    >
      <div class="space-y-1 border-b border-default pb-3">
        <h3 class="text-sm font-semibold text-default sm:text-base">
          {{ category.name }}
        </h3>
      </div>

      <div class="grid gap-4 pt-4 md:grid-cols-2 md:gap-5">
        <UFormField
          v-for="question in category.questions"
          v-show="isQuestionVisible(question.conditionalLogic)"
          :key="question.key"
          :name="question.questionKey ?? question.key"
          :class="shouldSpanFullWidth(question) ? 'md:col-span-2' : ''"
          :ui="{
            container: 'space-y-2',
            label: 'text-sm leading-6 font-medium text-default',
            help: 'text-xs leading-5 text-muted',
            error: 'text-sm text-error',
          }"
        >
          <template #label>
            <div class="flex flex-wrap items-center gap-2">
              <span class="min-w-0">{{ question.questionText }}</span>
              <UBadge
                v-if="question.isRequired"
                size="xs"
                color="error"
                variant="subtle"
              >
                Required
              </UBadge>
            </div>
          </template>

          <UTextarea
            v-if="question.type === questionTypeValues.LongText && !question.hasOptions"
            v-model="state[question.questionKey ?? ''] as string"
            :rows="4"
            autoresize
            class="w-full"
          />

          <USelectMenu
            v-else-if="question.hasOptions && question.isMultiSelect"
            v-model="state[question.questionKey ?? ''] as string[]"
            :items="question.items"
            multiple
            value-key="value"
            label-key="label"
            :search="false"
            placeholder="Select all that apply"
            class="w-full"
          />

          <USelect
            v-else-if="question.hasOptions"
            v-model="state[question.questionKey ?? ''] as string"
            :items="question.items"
            value-key="value"
            label-key="label"
            placeholder="Choose an option"
            class="w-full"
          />

          <USelect
            v-else-if="question.type === questionTypeValues.Boolean"
            v-model="state[question.questionKey ?? ''] as string"
            :items="[
              { label: 'Yes', value: 'true' },
              { label: 'No', value: 'false' },
            ]"
            value-key="value"
            label-key="label"
            placeholder="Choose yes or no"
            class="w-full"
          />

          <UInput
            v-else
            v-model="state[question.questionKey ?? ''] as string"
            :type="getInputType(question.type)"
            class="w-full"
          />

          <div
            v-for="followUpOption in getSelectedFollowUpOptions(question.questionKey ?? '', question.items)"
            :key="`${question.key}-${followUpOption.value}-follow-up`"
            class="mt-3 rounded-md bg-elevated/70 p-3"
          >
            <UFormField
              :label="followUpOption.followUpPlaceholder || `Please specify for ${followUpOption.label}`"
              :ui="{ label: 'text-xs font-medium text-muted' }"
            >
              <UInput
                v-model="followUpState[`${question.questionKey}:${followUpOption.value}`]"
                class="w-full"
              />
            </UFormField>
          </div>

          <template
            v-if="question.helpText"
            #help
          >
            {{ question.helpText }}
          </template>

          <p
            v-if="fieldErrors[question.questionKey ?? '']"
            class="mt-2 text-sm text-error"
          >
            {{ fieldErrors[question.questionKey ?? ''] }}
          </p>
        </UFormField>
      </div>
    </div>

    <div class="rounded-lg bg-default/70 p-4 sm:p-5">
      <div class="flex flex-col gap-4">
        <div class="flex flex-wrap items-center justify-between gap-2">
          <p class="text-sm leading-6 text-muted">
            Review your answers, then confirm. After this step your registration is locked.
          </p>
          <UBadge
            v-if="requiredProgress.total > 0"
            color="neutral"
            variant="soft"
            size="sm"
          >
            {{ requiredProgress.answered }} of {{ requiredProgress.total }} required
          </UBadge>
        </div>

        <div class="flex flex-col items-stretch gap-2">
          <UButton
            type="submit"
            size="lg"
            icon="i-lucide-check"
            :loading="submitMutation.isPending.value"
            :disabled="!isFormValid"
            class="w-full justify-center"
          >
            Complete registration
          </UButton>

          <p class="text-center text-xs text-muted">
            Next you will see a confirmation page with calendar options and your check-in QR code.
          </p>

          <p
            v-if="submissionError"
            class="text-sm text-error"
          >
            One or more fields need attention.
          </p>
        </div>
      </div>
    </div>
  </UForm>
</template>
