<script setup lang="ts">
import type { GeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationQuestionsListEndpointStatus200 } from '@geekshacking/portal-sdk'
import {
  geeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsSubmitEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import { useProfileName } from '~/composables/useProfileName'
import { registrationPageConfig } from '~/config/registration-pages'
import { getApiValidationErrors } from '~/utils/api-errors'

const props = defineProps<{
  hackathonId: string
  questions: GeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationQuestionsListEndpointStatus200
  /** inside the portal shell: no intro heading (the panel has one) */
  embedded?: boolean
}>()

const router = useRouter()
const route = useRoute()
const { data: hackathon } = useGeeksHackingPortalApiEndpointsParticipantsHackathonGetEndpoint({ path: computed(() => ({ hackathonIdOrShortCode: props.hackathonId })) })
const queryClient = useQueryClient()

const registrationPath = computed(() => hackathon.value ? `/${hackathon.value.shortCode}/registration` : `/${props.hackathonId}/registration`)

// Get authenticated user data for prefilling
const { data: userData } = useGeeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()

// The name lives on the user profile, not in this hackathon's registration answers
const { profileName, isProfileNameValid, saveProfileName, isSavingProfileName } = useProfileName(userData)

const allQuestions = computed(() =>
  props.questions.categories?.flatMap(cat => cat.questions ?? []) ?? [],
)

// Format ALL questions grouped by section (page config)
const formattedSections = computed(() => {
  if (!props.questions?.categories)
    return []

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
            isMultiSelect: q.type === 'MultipleChoice',
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

// Get selected options that have follow-up text for a question
interface FormattedItem { label: string, value: string, hasFollowUpText: boolean, followUpPlaceholder: string }
function getSelectedFollowUpOptions(questionKey: string, items: FormattedItem[]): FormattedItem[] {
  const selectedValue = state[questionKey]
  if (!selectedValue)
    return []

  const selectedValues = Array.isArray(selectedValue) ? selectedValue : [selectedValue]
  return items.filter(item => selectedValues.includes(item.value) && item.hasFollowUpText)
}

// Parse and evaluate conditional logic for a question
// ConditionalLogic format: {"questionKey": "value"} or {"questionKey": ["value1", "value2"]}
function isQuestionVisible(conditionalLogic: string | null | undefined): boolean {
  if (!conditionalLogic)
    return true

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

// PREFILL github profile if sign up with github
watch([() => props.questions, userData], ([newVal]) => {
  if (newVal?.categories) {
    newVal.categories.forEach((category) => {
      category.questions?.forEach((question) => {
        if (question.questionKey) {
          const isMulti = question.type === 'MultipleChoice'
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

const fieldErrors = ref<Record<string, string>>({})
const submissionError = ref(false)
const submitRegistrationMutation = useGeeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsSubmitEndpoint()
const isSubmitting = computed(() => submitRegistrationMutation.isPending.value || isSavingProfileName.value)

async function onSubmit() {
  const submissions = Object.entries(state)
    .filter(([questionKey, value]) => {
      // Only submit if the question is visible
      const question = allQuestions.value.find(q => q.questionKey === questionKey)
      if (!question || !isQuestionVisible(question.conditionalLogic))
        return false

      if (Array.isArray(value))
        return value.length > 0
      return value !== '' && value !== null && value !== undefined
    })
    .map(([questionKey, value]) => {
      const question = allQuestions.value.find(q => q.questionKey === questionKey)
      if (!question?.id)
        return null

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
    await saveProfileName()

    fieldErrors.value = {}
    submissionError.value = false
    await submitRegistrationMutation.mutateAsync({ path: { hackathonId: props.hackathonId }, body: { submissions } })

    // Remove cached submissions so the status page fetches fresh data
    queryClient.removeQueries({
      queryKey: geeksHackingPortalApiEndpointsParticipantsHackathonRegistrationSubmissionsListEndpointQueryKey({ path: { hackathonId: props.hackathonId } }),
    })

    router.push({
      path: props.embedded ? `/dash/${props.hackathonId}/registration/complete` : `${registrationPath.value}/complete`,
      query: route.query.joinCode ? { joinCode: route.query.joinCode } : undefined,
    })
  }
  catch (error) {
    submissionError.value = true
    parseErrorsToFields(error)
    console.error('[FORM] Submission failed:', error)
    // take the user to the first answer that needs fixing
    await nextTick()
    document.querySelector<HTMLElement>('.reg-form [aria-invalid="true"]')?.focus()
  }
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

// Check if a question should span full width
function shouldBeFullWidth(question: { id?: string | null, isLong: boolean, conditionalLogic?: string | null }, categoryQuestions: { id?: string | null, isLong: boolean, conditionalLogic?: string | null }[]) {
  if (question.isLong)
    return true

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

// Required = marked required, or a follow-up question that is currently shown
function isRequiredQuestion(question: { isRequired?: boolean | null, conditionalLogic?: string | null }) {
  return !!question.isRequired || !!question.conditionalLogic
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
    const isMainValueFilled = Array.isArray(value)
      ? value.length > 0
      : value !== '' && value !== null && value !== undefined
    if (!isMainValueFilled) {
      missing.push(key)
      continue
    }

    // selected options that ask for more detail need that text too
    const selectedValues = Array.isArray(value) ? value : [value]
    for (const option of question.options ?? []) {
      const optionValue = String(option.optionValue ?? '')
      if (option.hasFollowUpText && selectedValues.includes(optionValue) && !followUpState[`${key}:${optionValue}`]?.trim())
        missing.push(`${key}:${optionValue}`)
    }
  }
  if (!profileName.firstName.trim())
    missing.push('profile_first_name')
  if (!profileName.lastName.trim())
    missing.push('profile_last_name')
  return missing
})

const isFormValid = computed(() => !!props.questions?.categories && isProfileNameValid.value && missingAnswers.value.length === 0)

const isTshirtImageModalOpen = ref(false)

// free-text question types → the matching input, so phones get the right keyboard
function inputTypeFor(type: string | null | undefined) {
  switch (type) {
    case 'Url': return 'url'
    case 'Email': return 'email'
    case 'Phone': return 'tel'
    case 'Number': return 'number'
    case 'Date': return 'date'
    default: return 'text'
  }
}

const followLinks = [
  { name: 'Instagram', url: 'https://www.instagram.com/geekshacking', icon: 'fa6-brands:instagram' },
  { name: 'LinkedIn', url: 'https://www.linkedin.com/company/geekshacking/', icon: 'fa6-brands:linkedin' },
  { name: 'Facebook', url: 'https://www.facebook.com/GeeksHacking', icon: 'fa6-brands:facebook' },
]
</script>

<template>
  <div
    class="reg-form"
    :class="{ 'reg-form--embedded': embedded }"
  >
    <form
      v-if="isDataReady"
      class="reg-form__inner"
      novalidate
      @submit.prevent="onSubmit"
    >
      <div
        v-if="!embedded"
        class="reg-form__intro"
      >
        <GLabelStrip label="Registration" />
        <h1 class="reg-form__title">
          {{ hackathon?.name ? `Register for ${hackathon.name}` : 'Register' }}
        </h1>
        <p class="reg-form__lead">
          It takes about five minutes. Questions marked <span aria-hidden="true">*</span><span class="sr-only">with an asterisk</span> are required, and your answers are only shared with the organisers.
        </p>
      </div>

      <section
        v-for="section in formattedSections"
        :key="section.sectionIndex"
        class="reg-form__section"
        :aria-labelledby="`reg-section-${section.sectionIndex}`"
      >
        <GSectionHeading
          :id="`reg-section-${section.sectionIndex}`"
          :numeral="section.sectionIndex + 1"
          :label="`Part ${section.sectionIndex + 1} of ${formattedSections.length}`"
          :title="section.pageName"
          :level="2"
        />

        <!-- profile name: lives on the user profile, shown in Personal Details -->
        <div
          v-if="section.sectionIndex === 0"
          class="reg-form__grid"
        >
          <GFormField
            label="First name"
            required
          >
            <GInput
              v-model="profileName.firstName"
              autocomplete="given-name"
            />
          </GFormField>
          <GFormField
            label="Last name"
            required
          >
            <GInput
              v-model="profileName.lastName"
              autocomplete="family-name"
            />
          </GFormField>
          <p class="reg-form__note">
            This is your GeeksHacking profile name. Organizers of every hackathon and workshop you join see it, and changing it here updates it everywhere.
          </p>
        </div>

        <div class="reg-form__grid">
          <template
            v-for="category in section.categories"
            :key="category.name"
          >
            <template
              v-for="question in category.questions"
              :key="question.id ?? ''"
            >
              <div
                v-if="isQuestionVisible(question.conditionalLogic)"
                class="reg-form__question"
                :class="{ 'reg-form__question--wide': shouldBeFullWidth(question, category.questions) }"
              >
                <!-- a single option (e.g. consent): a labelled checkbox under the question; the checkbox carries the required mark -->
                <fieldset
                  v-if="question.hasOptions && !question.isMultiSelect && question.items.length === 1"
                  class="reg-form__fieldset"
                  :aria-describedby="[question.helpText ? `q-${question.id}-hint` : '', fieldErrors[question.questionKey ?? ''] ? `q-${question.id}-error` : ''].filter(Boolean).join(' ') || undefined"
                >
                  <legend class="reg-form__legend">
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
                    :id="`q-${question.id}-hint`"
                    class="reg-form__hint"
                  >
                    {{ question.helpText }}
                  </p>
                  <p
                    v-if="fieldErrors[question.questionKey ?? '']"
                    :id="`q-${question.id}-error`"
                    class="reg-form__error"
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
                  <GTextarea
                    v-else-if="question.type === 'LongText'"
                    v-model="state[question.questionKey ?? ''] as string"
                    :rows="4"
                    autoresize
                  />
                  <GInput
                    v-else
                    v-model="state[question.questionKey ?? ''] as string"
                    :type="inputTypeFor(question.type)"
                    :inputmode="question.type === 'Number' ? 'numeric' : undefined"
                    :autocomplete="question.type === 'Phone' ? 'tel' : question.type === 'Email' ? 'email' : undefined"
                  />
                </GFormField>

                <!-- options that ask for more detail -->
                <GFormField
                  v-for="followUpOption in getSelectedFollowUpOptions(question.questionKey ?? '', question.items)"
                  :key="`${question.questionKey}-${followUpOption.value}-followup`"
                  :label="followUpOption.followUpPlaceholder || `Tell us more about “${followUpOption.label}”`"
                  required
                  class="reg-form__followup"
                >
                  <GInput v-model="followUpState[`${question.questionKey}:${followUpOption.value}`]" />
                </GFormField>

                <!-- T-shirt size guide -->
                <button
                  v-if="question.questionKey === 'tshirt_size'"
                  type="button"
                  class="reg-form__guide"
                  @click="isTshirtImageModalOpen = true"
                >
                  <img
                    src="/appendix/tshirtsize.jpg"
                    alt=""
                    class="reg-form__guide-img"
                  >
                  <span class="reg-form__guide-label">
                    <Icon
                      name="material-symbols:zoom-in-rounded"
                      aria-hidden="true"
                    />
                    View the T-shirt size guide
                  </span>
                </button>
              </div>
            </template>
          </template>
        </div>
      </section>

      <!-- follow us -->
      <section
        class="reg-form__follow"
        aria-labelledby="reg-follow"
      >
        <h2
          id="reg-follow"
          class="reg-form__follow-title"
        >
          Follow GeeksHacking
        </h2>
        <p class="reg-form__note">
          Event updates and new workshops go out on our socials first.
        </p>
        <ul class="flex gap-2">
          <li
            v-for="social in followLinks"
            :key="social.name"
          >
            <GButton
              :href="social.url"
              target="_blank"
              variant="secondary"
              :icon="social.icon"
              :aria-label="`${social.name} (opens in a new tab)`"
            />
          </li>
        </ul>
      </section>

      <GAlert
        v-if="submissionError"
        tone="err"
        title="Some answers need fixing"
        description="Check the highlighted questions, then submit again."
        class="-mb-6"
      />

      <!-- submit -->
      <div class="reg-form__submit">
        <div class="reg-form__submit-row">
          <p
            class="reg-form__remaining"
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
            label="Submit registration"
            :disabled="!isFormValid"
            :loading="isSubmitting"
            loading-label="Submitting"
          />
        </div>
      </div>
    </form>

    <!-- T-shirt size guide, full size -->
    <GModal
      v-model:open="isTshirtImageModalOpen"
      title="T-shirt size guide"
      size="lg"
    >
      <div class="flex flex-col gap-3">
        <a
          href="/appendix/tshirtsize.jpg"
          target="_blank"
          rel="noopener noreferrer"
          class="reg-form__guide-full"
        >
          <img
            src="/appendix/tshirtsize.jpg"
            alt="T-shirt size guide: chest width and body length for each size"
            class="h-auto w-full"
          >
          <span class="sr-only">(opens full size in a new tab)</span>
        </a>
        <p class="gh-small text-gh-muted">
          Select the image to open it full size, or pinch to zoom on a phone.
        </p>
      </div>
      <template #footer>
        <GButton
          href="/appendix/tshirtsize.jpg"
          download="tshirt-size-guide.jpg"
          variant="secondary"
          icon="material-symbols:download-rounded"
          label="Download"
        />
        <GButton
          variant="primary"
          label="Done"
          @click="isTshirtImageModalOpen = false"
        />
      </template>
    </GModal>
  </div>
</template>

<style scoped>
.reg-form {
  display: flex;
  justify-content: center;
  padding: 32px 16px 96px;
  color: var(--gh-text);
  font-family: var(--font-body);
  letter-spacing: var(--gh-tracking);
}
.reg-form--embedded {
  padding: 0 0 32px;
}
.reg-form__inner {
  display: flex;
  flex-direction: column;
  gap: 48px;
  width: 100%;
  max-width: 48rem;
}

.reg-form__intro {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 12px;
}
.reg-form__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: clamp(28px, 5vw, 40px);
  font-weight: 700;
  line-height: 1.1;
}
.reg-form__lead {
  max-width: 60ch;
  margin: 0;
  color: var(--gh-text-muted);
}

.reg-form__section {
  display: flex;
  flex-direction: column;
  gap: 24px;
}
.reg-form__grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 20px 24px;
}
@media (min-width: 768px) {
  .reg-form__grid { grid-template-columns: 1fr 1fr; }
  .reg-form__question--wide,
  .reg-form__note { grid-column: 1 / -1; }
}
.reg-form__question {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-width: 0;
}
.reg-form__note {
  margin: -8px 0 0;
  font-size: 13px;
  color: var(--gh-text-muted);
}
.reg-form__followup {
  padding-left: 14px;
  border-left: 3px solid var(--gh-accent);
}

/* single-checkbox question */
.reg-form__fieldset {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 0;
  margin: 0;
  padding: 0;
  border: 0;
}
.reg-form__legend {
  padding: 0;
  margin-bottom: 8px;
  font-size: 14px;
  font-weight: 600;
}
.reg-form__required,
.reg-form__error {
  color: var(--gh-err);
}
.reg-form__hint,
.reg-form__error {
  margin: 0;
  font-size: 13px;
}
.reg-form__hint {
  color: var(--gh-text-muted);
}

/* T-shirt guide */
.reg-form__guide {
  display: flex;
  flex-direction: column;
  gap: 0;
  overflow: hidden;
  padding: 0;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh);
  background: var(--gh-surface);
  color: var(--gh-text);
  font: inherit;
  text-align: left;
  cursor: zoom-in;
}
.reg-form__guide:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: 2px;
}
.reg-form__guide-img {
  display: block;
  width: 100%;
  max-height: 220px;
  object-fit: cover;
  object-position: top;
}
.reg-form__guide-label {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 10px 12px;
  border-top: var(--gh-border-width) solid var(--gh-line);
  font-size: 14px;
  font-weight: 600;
}
.reg-form__guide:hover .reg-form__guide-label {
  background: var(--gh-ghost-hover);
}
.reg-form__guide-full {
  display: block;
  overflow: hidden;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh);
  cursor: zoom-in;
}
.reg-form__guide-full:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: 2px;
}

.reg-form__follow {
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding-top: 20px;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.reg-form__follow .reg-form__note {
  margin: 0;
}
.reg-form__follow-title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 18px;
  font-weight: 700;
}

.reg-form__submit {
  position: sticky;
  bottom: 0;
  z-index: 5;
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin: 0 -16px -32px;
  padding: 16px;
  border-top: var(--gh-border-width) solid var(--gh-line);
  background: var(--gh-bg);
}
.reg-form__submit-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}
.reg-form__remaining {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
  color: var(--gh-text-muted);
}
</style>
