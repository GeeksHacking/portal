<script setup lang="ts">
import type { HackOManiaApiEndpointsParticipantsHackathonRegistrationQuestionsListResponse } from '~/api-client/models'
import { getCategoriesForPage, getPageName } from '~/config/registration-pages'
import { useQuery } from '@tanstack/vue-query'

const props = defineProps<{
  questions: HackOManiaApiEndpointsParticipantsHackathonRegistrationQuestionsListResponse
}>()

const router = useRouter()
const route = useRoute()

// Get authenticated user data for prefilling
const { data: userData } = useQuery(authQueries.whoAmI)

const currentPageIndex = computed(() => {
  const index = route.query.index
  return index ? Number(index) : 0
})

const currentPageCategories = computed(() => getCategoriesForPage(currentPageIndex.value))

const formattedPageQuestions = computed(() => {
  if (!props.questions?.categories) return []

  return props.questions.categories
    .filter(cat => currentPageCategories.value.includes(cat.name ?? ''))
    .map(cat => ({
      ...cat,
      questions: (cat.questions || []).map((q) => {
        const items = (q.options || []).map(opt => ({
          label: String(opt.optionText ?? ''),
          value: String(opt.optionValue ?? ''),
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
    }))
})

const pageTitle = computed(() => getPageName(currentPageIndex.value))
const state = reactive<Record<string, string | string[]>>({})
const isDataReady = ref(false)

watch([() => props.questions, userData], ([newVal]) => {
  if (newVal?.categories) {
    newVal.categories.forEach((category) => {
      category.questions?.forEach((question) => {
        if (question.questionKey) {
          const isMulti = question.type === 4
          const val = question.currentSubmission?.value

          // Prefill GitHub profile with authenticated user's GitHub login
          if (question.questionKey === 'github_profile' && !val && userData.value?.gitHubLogin) {
            state[question.questionKey] = `https://github.com/${userData.value.gitHubLogin}`
          }
          else if (state[question.questionKey] === undefined) {
            state[question.questionKey] = isMulti
              ? (Array.isArray(val) ? val : [])
              : (val ? String(val) : '')
          }
        }
      })
    })
    isDataReady.value = true
  }
}, { immediate: true, deep: true })

const isSubmitting = ref(false)
const submissionError = ref(false)
const fieldErrors = ref<Record<string, string>>({})

const onSubmit = async () => {
  submissionError.value = false
  fieldErrors.value = {}
  const nextIndex = currentPageIndex.value + 1
  if (nextIndex < 3) {
    router.push({ path: '/registration/form', query: { index: String(nextIndex) } })
  }
  else {
    try {
      isSubmitting.value = true

      const submissions = Object.entries(state)
        .filter(([_key, value]) => {
          if (Array.isArray(value)) {
            return value.length > 0
          }
          return value !== '' && value !== null && value !== undefined
        })
        .map(([questionKey, value]) => {
          const question = props.questions.categories
            ?.flatMap(cat => cat.questions || [])
            .find(q => q.questionKey === questionKey)

          if (!question?.id) return null

          return {
            questionId: question.id,
            value: Array.isArray(value) ? JSON.stringify(value) : String(value),
            followUpValue: null,
          }
        })
        .filter((submission): submission is NonNullable<typeof submission> => submission !== null)

      console.log('📤 [SUBMIT] Submitting', submissions.length, 'submissions:', submissions)

      const hackathonId = useHackathonId()
      if (!hackathonId.value) {
        throw new Error('Hackathon ID not found')
      }

      // Use fetch directly to get error details because i cannot get it from the response body!!
      const config = useRuntimeConfig()
      const response = await fetch(`${config.public.api}/participants/hackathons/${hackathonId.value}/registration/submissions`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        credentials: 'include',
        body: JSON.stringify({ submissions }),
      })

      if (!response.ok) {
        const errorData = await response.json()
        throw errorData
      }

      router.push('/registration/complete')
    }
    catch (error: unknown) {
      submissionError.value = true

      // TRYING TO MATCH THE ERROR WORDS WITH THE QUESTION WORDS SO IK WHICH FIELD TOPUT THE ERROR
      try {
        const err = error as Record<string, unknown>

        const errorData = err
        if (errorData && typeof errorData === 'object') {
          const errData = errorData as Record<string, unknown>

          if (errData.errors && typeof errData.errors === 'object') {
            const errors = errData.errors as Record<string, string[]>

            const allQuestions = props.questions.categories?.flatMap(cat => cat.questions || []) || []

            if (errors.generalErrors && Array.isArray(errors.generalErrors)) {
              for (const errorMessage of errors.generalErrors) {
                let matched = false
                for (const question of allQuestions) {
                  const questionText = question.questionText?.toLowerCase() || ''
                  const errorLower = errorMessage.toLowerCase()

                  if (questionText && errorLower.includes(`'${questionText}'`)) {
                    fieldErrors.value[question.questionKey || ''] = errorMessage
                    matched = true
                    break
                  }
                  else if (questionText && questionText.length > 5 && errorLower.includes(questionText)) {
                    fieldErrors.value[question.questionKey || ''] = errorMessage
                    matched = true
                    break
                  }
                }

                if (!matched) {
                  for (const question of allQuestions) {
                    const questionKey = question.questionKey?.toLowerCase() || ''
                    const errorLower = errorMessage.toLowerCase()

                    // idk hwo else to do this but everytime got validation then need to add here
                    if ((questionKey.includes('phone') && errorLower.includes('phone'))
                      || (questionKey.includes('telegram') && errorLower.includes('telegram'))
                      || (questionKey.includes('age') && errorLower.includes('age'))
                      || (questionKey.includes('experience') && errorLower.includes('experience'))
                      || (questionKey.includes('email') && errorLower.includes('email'))
                      || (questionKey.includes('github') && errorLower.includes('github'))) {
                      fieldErrors.value[question.questionKey || ''] = errorMessage
                      matched = true
                      break
                    }
                  }
                }
              }
            }
          }
        }
      }
      catch (parseError) {
        console.error('[DYNAMICFORM] response error:', parseError)
      }
    }
    finally {
      isSubmitting.value = false
    }
  }
}

const goToPrevious = () => {
  const prevIndex = currentPageIndex.value - 1
  if (prevIndex >= 0) {
    router.push({ path: '/registration/form', query: { index: String(prevIndex) } })
  }
}

const showPreviousButton = computed(() => currentPageIndex.value > 0)
const isLastPage = computed(() => currentPageIndex.value === 2)
const submitButtonText = computed(() => isLastPage.value ? 'SUBMIT' : 'NEXT')

// splitting them into the pages based on their categories, cuz i cant just use the displayorder itself in case
const getCategoryGroupName = (categoryName: string) => {
  if (currentPageIndex.value !== 1) return null

  const skillsCategories = ['Skills & Interests', 'Online Profiles']
  const logisticsCategories = ['Hackathon Preferences', 'Dietary & Preferences']

  if (skillsCategories.includes(categoryName)) return 'Skills'
  if (logisticsCategories.includes(categoryName)) return 'Logistics'
  return null
}

const showMainPageTitle = computed(() => currentPageIndex.value !== 1)

const getSectionIcon = (sectionName: string | null) => {
  if (sectionName === 'Skills') return '/logos/skills.svg'
  if (sectionName === 'Logistics') return '/logos/logistics.svg'
  if (currentPageIndex.value === 2) return '/logos/outreach.svg'
  return '/logos/personaldetails.svg'
}

const isCurrentPageValid = computed(() => {
  if (!props.questions?.categories) return false

  const currentPageRequiredQuestions = formattedPageQuestions.value
    .flatMap(cat => cat.questions || [])
    .filter(q => q.isRequired)

  return currentPageRequiredQuestions.every((question) => {
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
  <div class="min-h-screen flex flex-col items-center bg-white px-4 pt-8 md:pt-16 pb-24">
    <div class="w-full max-w-[600px]">
      <div
        v-if="showMainPageTitle"
        class="flex items-center mb-10"
      >
        <img
          :src="getSectionIcon(null)"
          alt="icon"
          class="w-8 h-8 md:w-10 md:h-10"
        >
        <h2 class="ml-3 font-raleway text-lg md:text-xl font-normal text-black">
          {{ pageTitle }}
        </h2>
      </div>

      <UForm
        v-if="isDataReady"
        :key="currentPageIndex"
        :state="state"
        @submit="onSubmit"
      >
        <template
          v-for="(category, index) in formattedPageQuestions"
          :key="category.name"
        >
          <div
            v-if="getCategoryGroupName(category.name ?? '') && (index === 0 || getCategoryGroupName(category.name ?? '') !== getCategoryGroupName(formattedPageQuestions[index - 1]?.name ?? ''))"
            class="flex items-center mb-6 mt-6 first:mt-0"
          >
            <img
              :src="getSectionIcon(getCategoryGroupName(category.name ?? ''))"
              alt="icon"
              class="w-8 h-8 md:w-10 md:h-10"
            >
            <h2 class="ml-3 font-raleway text-lg md:text-xl font-normal text-black">
              {{ getCategoryGroupName(category.name ?? '') }}
            </h2>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-x-6 gap-y-4 mb-6">
            <template
              v-for="question in category.questions"
              :key="question.id ?? ''"
            >
              <UFormField
                :name="question.questionKey || ''"
                :class="[question.isLong ? 'md:col-span-2' : 'col-span-1']"
              >
                <template #label>
                  <span class="font-raleway text-[16px] font-normal text-black">
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
                    base: 'bg-white rounded-lg min-h-[42px] border border-black/50 text-black font-raleway',
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
                    base: 'bg-white rounded-lg h-[42px] border border-black/50 text-black font-raleway',
                  }"
                />

                <UInput
                  v-else
                  v-model="state[question.questionKey ?? ''] as string"
                  type="text"
                  class="w-full"
                  :ui="{
                    base: 'bg-white rounded-lg h-[42px] border border-black/50 text-black font-raleway',
                  }"
                />

                <div
                  v-if="fieldErrors[question.questionKey ?? '']"
                  class="mt-1"
                >
                  <span class="font-raleway text-[12px] text-red-600">
                    {{ fieldErrors[question.questionKey ?? ''] }}
                  </span>
                </div>
              </UFormField>
            </template>
          </div>
        </template>

        <div class="mt-12">
          <div class="flex justify-between items-center">
            <button
              v-if="showPreviousButton"
              type="button"
              class="font-raleway text-[16px] font-normal text-black underline cursor-pointer"
              @click="goToPrevious"
            >
              Previous
            </button>
            <div v-else />
            <UButton
              type="submit"
              size="xl"
              :disabled="!isCurrentPageValid || isSubmitting"
              :loading="isSubmitting"
              class="bg-black text-white px-8 rounded-md font-raleway disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {{ submitButtonText }}
            </UButton>
          </div>
          <div
            v-if="submissionError && isLastPage"
            class="mt-2 text-right"
          >
            <span class="font-raleway text-[14px] text-red-600">
              One or more errors in the fields, please check!
            </span>
          </div>
        </div>
      </UForm>
    </div>
  </div>
</template>
