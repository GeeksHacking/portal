<script setup lang="ts">
import type { HackOManiaApiEndpointsParticipantsHackathonRegistrationQuestionsListResponse } from '~/api-client/models'
import { registrationPageConfig } from '~/config/registration-pages'
import { useQuery, useQueryClient } from '@tanstack/vue-query'
import { useSubmitRegistrationMutation } from '~/composables/question'
import { useUpdateUserMutation } from '~/composables/user'

const props = defineProps<{
  hackathonId: string
  questions: HackOManiaApiEndpointsParticipantsHackathonRegistrationQuestionsListResponse
}>()

const router = useRouter()
const route = useRoute()
const hackathon = useRouteHackathon()
const queryClient = useQueryClient()

const registrationPath = computed(() => hackathon.value ? `/${hackathon.value.shortCode}/registration` : `/${props.hackathonId}/registration`)

// Get authenticated user data for prefilling
const { data: userData } = useQuery(authQueries.whoAmI)

const allQuestions = computed(() =>
  props.questions.categories?.flatMap(cat => cat.questions ?? []) ?? [],
)

// Format ALL questions grouped by section (page config)
const formattedSections = computed(() => {
  if (!props.questions?.categories) return []

  return registrationPageConfig.map((pageConfig, sectionIndex) => {
    const sectionCategories = props.questions.categories
      ?.filter(cat => (pageConfig.categories as readonly string[]).includes(cat.name ?? ''))
      .map(cat => ({
        ...cat,
        questions: (cat.questions || []).map((q) => {
          const items = (q.options || []).map(opt => ({
            label: String(opt.optionText ?? ''),
            value: String(opt.optionValue ?? ''),
            hasFollowUpText: opt.hasFollowUpText ?? false,
            followUpPlaceholder: opt.followUpPlaceholder ?? '',
          }))
          const isLong = (q.questionText?.length ?? 0) > 30
          return {
            ...q,
            items,
            isMultiSelect: q.type === 4,
            hasOptions: items.length > 0,
            isLong,
          }
        }),
      })) ?? []

    return {
      sectionIndex,
      pageName: pageConfig.pageName,
      categories: sectionCategories,
    }
  })
})

const state = reactive<Record<string, string | string[]>>({})
const followUpState = reactive<Record<string, string>>({}) // key: "questionKey:optionValue" -> follow-up text
const isDataReady = ref(false)
const isTshirtImageModalOpen = ref(false)

// Get selected options that have follow-up text for a question
type FormattedItem = { label: string, value: string, hasFollowUpText: boolean, followUpPlaceholder: string }
const getSelectedFollowUpOptions = (questionKey: string, items: FormattedItem[]): FormattedItem[] => {
  const selectedValue = state[questionKey]
  if (!selectedValue) return []

  const selectedValues = Array.isArray(selectedValue) ? selectedValue : [selectedValue]
  return items.filter(item => selectedValues.includes(item.value) && item.hasFollowUpText)
}

// Parse and evaluate conditional logic for a question
// ConditionalLogic format: {"questionKey": "value"} or {"questionKey": ["value1", "value2"]}
const isQuestionVisible = (conditionalLogic: string | null | undefined): boolean => {
  if (!conditionalLogic) return true

  try {
    const conditions = JSON.parse(conditionalLogic) as Record<string, string | string[]>

    // All conditions must be met (AND logic)
    return Object.entries(conditions).every(([questionKey, expectedValue]) => {
      const currentValue = state[questionKey]

      // If expected value is an array, check if current value matches any
      if (Array.isArray(expectedValue)) {
        if (Array.isArray(currentValue)) {
          // Multi-select: check if any expected value is in current selection
          return expectedValue.some(ev => currentValue.includes(ev))
        }
        // Single value: check if it matches any expected value
        return expectedValue.includes(currentValue as string)
      }

      // Single expected value
      if (Array.isArray(currentValue)) {
        // Multi-select current value: check if expected value is selected
        return currentValue.includes(expectedValue)
      }

      // Both are single values: direct comparison (case-insensitive)
      return currentValue?.toString().toLowerCase() === expectedValue.toLowerCase()
    })
  }
  catch (e) {
    console.warn('Failed to parse conditionalLogic:', conditionalLogic, e)
    return true // Show question if parsing fails
  }
}

// Prefill first name and last name from user data
watch(userData, (user) => {
  if (user && state.first_name === undefined) {
    // If firstName is available, use it; otherwise fallback to name
    if (user.firstName) {
      state.first_name = user.firstName
    }
    else if (user.name) {
      state.first_name = user.name
    }
    else {
      state.first_name = ''
    }
  }
  if (user && state.last_name === undefined) {
    state.last_name = user.lastName ?? ''
  }
}, { immediate: true })

// PREFILL github profile if sign up with github
watch([() => props.questions, userData], ([newVal]) => {
  if (newVal?.categories) {
    newVal.categories.forEach((category) => {
      category.questions?.forEach((question) => {
        if (question.questionKey) {
          const isMulti = question.type === 4
          const val = question.currentSubmission?.value
          const followUpVal = question.currentSubmission?.followUpValue

          // Prefill GitHub profile with authenticated user's GitHub login
          if (question.questionKey === 'github_profile' && !val && userData.value?.gitHubLogin) {
            state[question.questionKey] = `https://github.com/${userData.value.gitHubLogin}`
          }
          else if (state[question.questionKey] === undefined) {
            state[question.questionKey] = isMulti
              ? (Array.isArray(val) ? val : [])
              : (val ? String(val) : '')
          }

          // Prefill follow-up values
          if (followUpVal && question.questionKey) {
            try {
              // Try parsing as JSON object (multiple follow-ups)
              const parsed = JSON.parse(followUpVal) as Record<string, string>
              for (const [optionValue, text] of Object.entries(parsed)) {
                followUpState[`${question.questionKey}:${optionValue}`] = text
              }
            }
            catch {
              // Single follow-up value - associate with selected option
              const selectedValue = Array.isArray(val) ? val[0] : val
              if (selectedValue) {
                followUpState[`${question.questionKey}:${selectedValue}`] = followUpVal
              }
            }
          }
        }
      })
    })
    isDataReady.value = true
  }
}, { immediate: true, deep: true })

// Submit mutation with built-in error handling
const { mutateAsync, isPending: isSubmitting, fieldErrors, submissionError } = useSubmitRegistrationMutation(
  toRef(props, 'hackathonId'),
  allQuestions,
)

// User profile update mutation
const updateUserMutation = useUpdateUserMutation()

const onSubmit = async () => {
  const submissions = Object.entries(state)
    .filter(([questionKey, value]) => {
      // Only submit if the question is visible
      const question = allQuestions.value.find(q => q.questionKey === questionKey)
      if (!question || !isQuestionVisible(question.conditionalLogic)) return false

      if (Array.isArray(value)) return value.length > 0
      return value !== '' && value !== null && value !== undefined
    })
    .map(([questionKey, value]) => {
      const question = allQuestions.value.find(q => q.questionKey === questionKey)
      if (!question?.id) return null

      // Collect follow-up values for this question
      const selectedValues = Array.isArray(value) ? value : [value]
      const followUpValues: Record<string, string> = {}
      for (const optionValue of selectedValues) {
        const followUpKey = `${questionKey}:${optionValue}`
        if (followUpState[followUpKey]) {
          followUpValues[optionValue] = followUpState[followUpKey]
        }
      }

      // Serialize follow-up values: single value as string, multiple as JSON
      let followUpValue: string | null = null
      const followUpEntries = Object.entries(followUpValues)
      if (followUpEntries.length === 1) {
        followUpValue = followUpEntries[0][1]
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
    .filter((s): s is NonNullable<typeof s> => s !== null)

  try {
    // Update user profile with first name and last name
    const firstName = state.first_name as string
    const lastName = state.last_name as string
    if (firstName || lastName) {
      await updateUserMutation.mutateAsync({
        firstName: firstName || null,
        lastName: lastName || null,
      })
    }

    await mutateAsync({ submissions })

    // Remove cached submissions so status page fetches fresh data
    queryClient.removeQueries({
      queryKey: ['hackathons', props.hackathonId, 'registration', 'submissions'],
    })

    router.push({
      path: `${registrationPath.value}/complete`,
      query: route.query.joinCode ? { joinCode: route.query.joinCode } : undefined,
    })
  }
  catch (error) {
    console.error('[FORM] Submission failed:', error)
  }
}

const getCategoryGroupName = (sectionIndex: number, categoryName: string) => {
  const logisticsCategories = ['Hackathon Preferences', 'Dietary & Preferences']

  if (sectionIndex === 1 && logisticsCategories.includes(categoryName)) return 'Logistics'
  return null
}

const getSectionIcon = (sectionIndex: number, sectionName: string | null) => {
  if (sectionName === 'Skills') return '/logos/skills.svg'
  if (sectionName === 'Logistics') return '/logos/logistics.svg'
  if (sectionIndex === 0) return '/logos/personaldetails.svg'
  if (sectionIndex === 1) return '/logos/logistics.svg'
  if (sectionIndex === 2) return '/logos/outreach.svg'
  return '/logos/personaldetails.svg'
}

// Check if a question should span full width
const shouldBeFullWidth = (question: { id?: string | null, isLong: boolean, conditionalLogic?: string | null }, categoryQuestions: { id?: string | null, isLong: boolean, conditionalLogic?: string | null }[]) => {
  if (question.isLong) return true

  // Get visible questions that are not already full-width (long)
  const visibleRegularQuestions = categoryQuestions.filter(q =>
    isQuestionVisible(q.conditionalLogic) && !q.isLong,
  )

  // If odd number of regular questions, the last one should be full width
  if (visibleRegularQuestions.length % 2 === 1) {
    const lastQuestion = visibleRegularQuestions[visibleRegularQuestions.length - 1]
    return question.id === lastQuestion?.id
  }

  return false
}

const isFormValid = computed(() => {
  if (!props.questions?.categories) return false

  // Validate First Name and Last Name are filled
  const firstName = state.first_name as string
  const lastName = state.last_name as string
  if (!firstName || firstName.trim() === '') return false
  if (!lastName || lastName.trim() === '') return false

  // Only validate required questions that are currently visible
  const allRequiredQuestions = allQuestions.value.filter(q =>
    q.isRequired && isQuestionVisible(q.conditionalLogic),
  )

  return allRequiredQuestions.every((question) => {
    const key = question.questionKey
    if (!key) return false

    const value = state[key]

    if (Array.isArray(value)) {
      return value.length > 0
    }

    return value !== '' && value !== null && value !== undefined
  })
})
</script>

<template>
  <div class="min-h-screen flex flex-col items-center bg-white dark:bg-gray-900 px-4 pt-8 md:pt-16 pb-24">
    <div class="w-full max-w-2xl">
      <UForm
        v-if="isDataReady"
        :state="state"
        @submit="onSubmit"
      >
        <!-- Loop through all sections -->
        <div class="space-y-12">
          <template
            v-for="section in formattedSections"
            :key="section.sectionIndex"
          >
            <div class="space-y-8">
              <!-- Section header (for sections 0 and 2, show main title) -->
              <div
                v-if="section.sectionIndex !== 1"
                class="flex items-center"
              >
                <img
                  :src="getSectionIcon(section.sectionIndex, null)"
                  alt="icon"
                  class="w-8 h-8 md:w-10 md:h-10"
                >
                <h2 class="ml-3 font-raleway text-lg md:text-xl font-normal text-gray-900 dark:text-gray-100">
                  {{ section.pageName }}
                </h2>
              </div>

              <!-- First Name and Last Name fields (in Personal Details section) -->
              <div
                v-if="section.sectionIndex === 0"
                class="grid grid-cols-1 md:grid-cols-2 gap-6"
              >
                <UFormField
                  name="first_name"
                  class="col-span-1"
                >
                  <template #label>
                    <span class="font-raleway text-base font-normal text-gray-900 dark:text-gray-100">
                      First Name
                    </span>
                  </template>
                  <UInput
                    v-model="state.first_name as string"
                    type="text"
                    class="w-full"
                    :ui="{
                      base: 'bg-white dark:bg-gray-800 rounded-lg h-11 border border-gray-400 dark:border-gray-600 text-gray-900 dark:text-gray-100 font-raleway',
                    }"
                  />
                </UFormField>

                <UFormField
                  name="last_name"
                  class="col-span-1"
                >
                  <template #label>
                    <span class="font-raleway text-base font-normal text-gray-900 dark:text-gray-100">
                      Last Name
                    </span>
                  </template>
                  <UInput
                    v-model="state.last_name as string"
                    type="text"
                    class="w-full"
                    :ui="{
                      base: 'bg-white dark:bg-gray-800 rounded-lg h-11 border border-gray-400 dark:border-gray-600 text-gray-900 dark:text-gray-100 font-raleway',
                    }"
                  />
                </UFormField>
              </div>

              <!-- Categories within each section -->
              <template
                v-for="(category, catIndex) in section.categories"
                :key="category.name"
              >
                <!-- For section 1, show Skills/Logistics subheadings -->
                <div
                  v-if="getCategoryGroupName(section.sectionIndex, category.name ?? '') && (catIndex === 0 || getCategoryGroupName(section.sectionIndex, category.name ?? '') !== getCategoryGroupName(section.sectionIndex, section.categories[catIndex - 1]?.name ?? ''))"
                  class="flex items-center"
                >
                  <img
                    :src="getSectionIcon(section.sectionIndex, getCategoryGroupName(section.sectionIndex, category.name ?? ''))"
                    alt="icon"
                    class="w-8 h-8 md:w-10 md:h-10"
                  >
                  <h2 class="ml-3 font-raleway text-lg md:text-xl font-normal text-gray-900 dark:text-gray-100">
                    {{ getCategoryGroupName(section.sectionIndex, category.name ?? '') }}
                  </h2>
                </div>

                <!-- Questions grid -->
                <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <template
                    v-for="question in category.questions"
                    :key="question.id ?? ''"
                  >
                    <UFormField
                      v-if="isQuestionVisible(question.conditionalLogic)"
                      :name="question.questionKey || ''"
                      :class="[shouldBeFullWidth(question, category.questions) ? 'md:col-span-2' : 'col-span-1']"
                    >
                      <template #label>
                        <span class="font-raleway text-base font-normal text-gray-900 dark:text-gray-100">
                          {{ question.questionText ?? '' }}
                        </span>
                      </template>

                      <USelectMenu
                        v-if="question.hasOptions && question.isMultiSelect"
                        v-model="state[question.questionKey ?? ''] as string[]"
                        :items="question.items"
                        multiple
                        :search="false"
                        value-key="value"
                        label-key="label"
                        placeholder="Select options"
                        class="w-full"
                        :ui="{
                          base: 'bg-white dark:bg-gray-800 rounded-lg min-h-11 border border-gray-400 dark:border-gray-600 text-gray-900 dark:text-gray-100 font-raleway',
                        }"
                        :ui-menu="{
                          option: {
                            base: 'whitespace-normal break-words py-2',
                          },
                        }"
                      >
                        <template #option="{ option }">
                          <span class="block whitespace-normal break-words">{{ option.label }}</span>
                        </template>
                      </USelectMenu>

                      <!-- Single checkbox for single-option questions -->
                      <UCheckbox
                        v-else-if="question.hasOptions && question.items.length === 1"
                        :model-value="state[question.questionKey ?? ''] === question.items[0]?.value"
                        :label="question.items[0]?.label"
                        class="font-raleway text-base text-gray-900 dark:text-gray-100"
                        @update:model-value="(checked) => {
                          const value = question.items[0]?.value
                          if (value !== undefined) {
                            state[question.questionKey ?? ''] = checked ? value : ''
                          }
                        }"
                      />

                      <USelect
                        v-else-if="question.hasOptions"
                        v-model="state[question.questionKey ?? ''] as string"
                        :items="question.items"
                        value-key="value"
                        label-key="label"
                        placeholder="Select one"
                        class="w-full"
                        :ui="{
                          base: 'bg-white dark:bg-gray-800 rounded-lg h-11 border border-gray-400 dark:border-gray-600 text-gray-900 dark:text-gray-100 font-raleway',
                        }"
                        :ui-menu="{
                          option: {
                            base: 'whitespace-normal break-words py-2',
                          },
                        }"
                      >
                        <template #option="{ option }">
                          <span class="block whitespace-normal break-words">{{ option.label }}</span>
                        </template>
                      </USelect>

                      <UInput
                        v-else
                        v-model="state[question.questionKey ?? ''] as string"
                        type="text"
                        class="w-full"
                        :ui="{
                          base: 'bg-white dark:bg-gray-800 rounded-lg h-11 border border-gray-400 dark:border-gray-600 text-gray-900 dark:text-gray-100 font-raleway',
                        }"
                      />

                      <!-- Follow-up text fields for selected options -->
                      <div
                        v-for="followUpOption in getSelectedFollowUpOptions(question.questionKey ?? '', question.items)"
                        :key="`${question.questionKey}-${followUpOption.value}-followup`"
                        class="mt-3"
                      >
                        <label class="block font-raleway text-base font-normal text-gray-900 dark:text-gray-100 mb-1">
                          {{ followUpOption.followUpPlaceholder || `Please specify for "${followUpOption.label}"` }}
                        </label>
                        <UInput
                          v-model="followUpState[`${question.questionKey}:${followUpOption.value}`]"
                          type="text"
                          class="w-full"
                          :ui="{
                            base: 'bg-white dark:bg-gray-800 rounded-lg h-11 border border-gray-400 dark:border-gray-600 text-gray-900 dark:text-gray-100 font-raleway',
                          }"
                        />
                      </div>

                      <!-- T-shirt size guide image -->
                      <div
                        v-if="question.questionKey === 'tshirt_size'"
                        class="mt-2"
                      >
                        <div
                          role="button"
                          tabindex="0"
                          class="cursor-pointer"
                          @click="() => {
                            console.log('Image clicked, opening modal')
                            isTshirtImageModalOpen = true
                            console.log('Modal state:', isTshirtImageModalOpen)
                          }"
                          @keydown.enter="() => { isTshirtImageModalOpen = true }"
                          @keydown.space.prevent="() => { isTshirtImageModalOpen = true }"
                        >
                          <img
                            src="/appendix/tshirtsize.jpg"
                            alt="T-shirt size guide"
                            class="rounded-lg max-w-full hover:opacity-90 transition-opacity"
                          >
                          <p class="text-xs text-gray-500 dark:text-gray-400 mt-1 font-raleway">
                            <span class="md:hidden">Tap to view larger</span>
                            <span class="hidden md:inline">Click to view larger or download</span>
                          </p>
                        </div>
                      </div>

                      <!-- Help text below the field -->
                      <p
                        v-if="question.helpText"
                        class="mt-1 text-xs text-gray-500 dark:text-gray-400 font-raleway"
                      >
                        {{ question.helpText }}
                      </p>

                      <div
                        v-if="fieldErrors[question.questionKey ?? '']"
                        class="mt-1"
                      >
                        <span class="font-raleway text-sm text-red-600 dark:text-red-400">
                          {{ fieldErrors[question.questionKey ?? ''] }}
                        </span>
                      </div>
                    </UFormField>
                  </template>
                </div>
              </template>
            </div>
          </template>
        </div>

        <!-- Social media links -->
        <div class="mt-8 font-raleway">
          <p class="text-md mb-3">
            Please take some time to follow us!
          </p>
          <div class="flex gap-6">
            <a
              href="https://www.instagram.com/geekshacking"
              target="_blank"
              rel="noopener noreferrer"
              class="text-gray-700 dark:text-gray-300 hover:text-pink-500 dark:hover:text-pink-400 transition-colors"
            >
              <Icon
                name="fa6-brands:instagram"
                class="size-8"
              />
            </a>
            <a
              href="https://www.linkedin.com/company/geekshacking/"
              target="_blank"
              rel="noopener noreferrer"
              class="text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 transition-colors"
            >
              <Icon
                name="fa6-brands:linkedin"
                class="size-8"
              />
            </a>
            <a
              href="https://www.facebook.com/GeeksHacking"
              target="_blank"
              rel="noopener noreferrer"
              class="text-gray-700 dark:text-gray-300 hover:text-blue-700 dark:hover:text-blue-500 transition-colors"
            >
              <Icon
                name="fa6-brands:facebook"
                class="size-8"
              />
            </a>
          </div>
        </div>

        <!-- Submit button at the bottom -->
        <div class="mt-12">
          <div class="flex justify-end">
            <UButton
              type="submit"
              size="xl"
              :disabled="!isFormValid || isSubmitting"
              :loading="isSubmitting"
              class="bg-gray-900 dark:bg-gray-100 text-white dark:text-gray-900 px-8 rounded-md font-raleway disabled:opacity-50 disabled:cursor-not-allowed"
            >
              SUBMIT
            </UButton>
          </div>
          <div
            v-if="submissionError"
            class="mt-2 text-right"
          >
            <span class="font-raleway text-sm text-red-600 dark:text-red-400">
              One or more errors in the fields, please check!
            </span>
          </div>
        </div>
      </UForm>
    </div>

    <!-- T-shirt size image modal -->
    <UModal
      v-model:open="isTshirtImageModalOpen"
      :ui="{ container: 'items-center' }"
    >
      <template #content>
        <div class="p-6 max-w-6xl mx-auto">
          <div class="flex justify-between items-center mb-4">
            <h3 class="text-lg font-raleway font-semibold text-gray-900 dark:text-gray-100">
              T-shirt Size Guide
            </h3>
            <button
              type="button"
              class="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"
              @click="isTshirtImageModalOpen = false"
            >
              ✕
            </button>
          </div>

          <div class="overflow-auto max-h-[70vh]">
            <a
              href="/appendix/tshirtsize.jpg"
              target="_blank"
              rel="noopener noreferrer"
            >
              <img
                src="/appendix/tshirtsize.jpg"
                alt="T-shirt size guide"
                class="w-full h-auto rounded-lg cursor-zoom-in hover:opacity-90 transition-opacity"
                title="Click to open full size in new tab"
              >
            </a>
          </div>

          <div class="mt-4 flex justify-between items-center text-xs text-gray-500 dark:text-gray-400 font-raleway">
            <div>
              <span class="hidden sm:hidden md:block">Click image to open full size in new tab</span>
              <span class="block md:hidden">Pinch to zoom</span>
            </div>
            <a
              href="/appendix/tshirtsize.jpg"
              download="tshirt-size-guide.jpg"
              class="text-gray-700 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100 underline"
            >
              Download
            </a>
          </div>
        </div>
      </template>
    </UModal>
  </div>
</template>

<style>
/* Global styles for dropdown options to enable text wrapping */
[role="option"],
[role="option"] span,
.ui-select-menu-option,
.ui-select-option {
  white-space: normal !important;
  word-wrap: break-word !important;
  word-break: break-word !important;
  padding-top: 0.5rem !important;
  padding-bottom: 0.5rem !important;
  line-height: 1.4 !important;
  min-height: auto !important;
}

/* Target Nuxt UI specific classes */
[data-headlessui-state] {
  white-space: normal !important;
}
</style>
