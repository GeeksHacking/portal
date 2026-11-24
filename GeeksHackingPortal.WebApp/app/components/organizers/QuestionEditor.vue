<script setup lang="ts">
import type { GeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListQuestionDto } from '@geekshacking/portal-sdk'
import type { QuestionCondition, QuestionLimits, QuestionType } from '~/utils/registration-question'

/**
 * Add or edit one registration question. Shared by hackathons and workshops: the only
 * difference is where the question can go (fixed form pages for hackathons, free sections for
 * workshops). The parent does the saving through `save`, which throws on failure.
 */
type Question = GeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListQuestionDto

export interface QuestionPayload {
  questionText: string
  questionKey: string
  helpText: string
  isRequired: boolean
  type: QuestionType
  category: string
  conditionalLogic: string
  validationRules: string
  options: { id?: string, optionText: string, optionValue: string, displayOrder: number, hasFollowUpText: boolean, followUpPlaceholder: string | null }[]
}

const props = defineProps<{
  kind: 'hackathon' | 'workshop'
  /** null to add a question */
  question: Question | null
  questions: Question[]
  save: (payload: QuestionPayload) => Promise<void>
}>()
const open = defineModel<boolean>('open', { default: false })

interface OptionRow { uid: number, id?: string, label: string, value: string, isNew: boolean, hasFollowUp: boolean, followUpPlaceholder: string }

let uid = 0
const form = reactive({
  text: '',
  helpText: '',
  type: 'Text' as QuestionType,
  isRequired: false,
  category: '',
  key: '',
  keyTouched: false,
  options: [] as OptionRow[],
  conditionsOn: false,
  conditions: [] as QuestionCondition[],
  conditionsRaw: null as string | null,
  limits: {} as QuestionLimits,
  limitsExtra: {} as Record<string, unknown>,
  limitsRaw: null as string | null,
})
const errors = reactive<Record<string, string>>({})
const limitErrors = ref<Partial<Record<keyof QuestionLimits, string>>>({})
const formError = ref('')
const isSaving = ref(false)

const isNew = computed(() => !props.question)
const otherQuestions = computed(() => props.questions.filter(q => q.id !== props.question?.id))
const takenKeys = computed(() => otherQuestions.value.map(q => q.questionKey ?? '').filter(Boolean))
const typeInfo = computed(() => QUESTION_TYPES.find(t => t.value === form.type))
const typeHint = computed(() => (props.kind === 'hackathon' ? typeInfo.value?.hackathonHint : undefined) ?? typeInfo.value?.hint)
const showsOptions = computed(() => isChoiceType(form.type))
const limitKeys = computed(() => limitKeysFor(form.type))

function reset() {
  const q = props.question
  for (const key of Object.keys(errors))
    delete errors[key]
  limitErrors.value = {}
  formError.value = ''
  const conditions = parseConditions(q?.conditionalLogic)
  const limits = parseLimits(q?.validationRules)
  Object.assign(form, {
    text: q?.questionText ?? '',
    helpText: q?.helpText ?? '',
    type: q?.type ?? 'Text',
    isRequired: q?.isRequired ?? false,
    category: q?.category ?? '',
    key: q?.questionKey ?? '',
    keyTouched: !!q,
    options: [...(q?.options ?? [])]
      .sort((a, b) => (a.displayOrder ?? 0) - (b.displayOrder ?? 0))
      .map(o => ({
        uid: ++uid,
        id: o.id,
        label: o.optionText ?? '',
        value: o.optionValue ?? '',
        isNew: false,
        hasFollowUp: o.hasFollowUpText ?? false,
        followUpPlaceholder: o.followUpPlaceholder ?? '',
      })),
    conditionsOn: !!q?.conditionalLogic?.trim(),
    conditions: conditions ?? [],
    conditionsRaw: conditions === null ? (q?.conditionalLogic ?? '') : null,
    limits: limits?.limits ?? {},
    limitsExtra: limits?.extra ?? {},
    limitsRaw: limits === null ? (q?.validationRules ?? '') : null,
  })
  if (showsOptions.value && !form.options.length)
    addOption()
  if (form.conditionsOn && !form.conditions.length && form.conditionsRaw === null)
    addCondition()
}
watch(open, (isOpen) => {
  if (isOpen)
    reset()
}, { immediate: true })

// the key follows the question text until someone edits it
watch(() => form.text, (text) => {
  if (isNew.value && !form.keyTouched)
    form.key = toQuestionKey(text)
})

// a fixed field drops its error
watch(() => form.text, () => delete errors.text)
watch(() => form.key, () => delete errors.key)
watch(() => form.category, () => delete errors.category)

watch(() => form.type, () => {
  if (showsOptions.value && !form.options.length)
    addOption()
})

// ---- where it goes ----
const hackathonCategoryItems = computed(() => {
  const items = HACKATHON_FORM_PAGES.flatMap(page => page.categories.map(c => ({ label: `${page.name} › ${c}`, value: c })))
  const current = props.question?.category
  if (current && !HACKATHON_CATEGORIES.includes(current))
    items.push({ label: `${current} (not on the form)`, value: current })
  return items
})
const workshopCategories = computed(() => [...new Set(props.questions.map(q => q.category).filter((c): c is string => !!c))])

// ---- options ----
function addOption() {
  form.options.push({ uid: ++uid, label: '', value: '', isNew: true, hasFollowUp: false, followUpPlaceholder: '' })
  const index = form.options.length - 1
  if (index > 0)
    nextTick(() => document.querySelector<HTMLInputElement>(`#qe-option-${form.options[index]!.uid}`)?.focus())
}
function removeOption(index: number) {
  form.options.splice(index, 1)
}
function moveOption(index: number, by: -1 | 1) {
  const target = index + by
  if (target < 0 || target >= form.options.length)
    return
  const [row] = form.options.splice(index, 1)
  form.options.splice(target, 0, row!)
}
function optionValueOf(row: OptionRow) {
  return row.isNew ? toOptionValue(row.label) : row.value
}

// ---- show only when ----
function addCondition() {
  form.conditions.push({ key: '', values: [] })
}
function sourceOf(key: string) {
  return otherQuestions.value.find(q => q.questionKey === key)
}
function valueItemsFor(key: string) {
  const source = sourceOf(key)
  if (!source)
    return null
  if (source.options?.length)
    return [...source.options].sort((a, b) => (a.displayOrder ?? 0) - (b.displayOrder ?? 0)).map(o => ({ label: o.optionText ?? o.optionValue ?? '', value: o.optionValue ?? '' }))
  if (source.type === 'Boolean')
    return [{ label: 'Yes', value: 'true' }, { label: 'No', value: 'false' }]
  return null
}
const conditionSourceItems = computed(() => otherQuestions.value
  .filter(q => q.questionKey)
  .map(q => ({ label: q.questionText || q.questionKey!, value: q.questionKey! })))

watch(() => form.conditionsOn, (on) => {
  if (on && !form.conditions.length && form.conditionsRaw === null)
    addCondition()
})

// ---- saving ----
function validate() {
  for (const key of Object.keys(errors))
    delete errors[key]
  if (!form.text.trim())
    errors.text = 'Write the question.'
  if (isNew.value) {
    const keyError = checkQuestionKey(form.key, takenKeys.value)
    if (keyError)
      errors.key = keyError
  }
  if (props.kind === 'hackathon' && !form.category)
    errors.category = 'Pick where it goes on the form.'
  if (showsOptions.value) {
    if (!form.options.length)
      errors.options = 'Add at least one option.'
    form.options.forEach((row) => {
      if (!row.label.trim())
        errors[`option-${row.uid}`] = 'Enter the option, or remove it.'
    })
    const values = form.options.map(optionValueOf)
    const duplicate = form.options.find((row, i) => row.label.trim() && values.indexOf(optionValueOf(row)) !== i)
    if (duplicate)
      errors[`option-${duplicate.uid}`] = 'Two options are the same.'
  }
  if (form.conditionsOn && form.conditionsRaw === null) {
    form.conditions.forEach((c, i) => {
      if (!c.key)
        errors[`cond-${i}`] = 'Pick a question.'
      else if (!c.values.some(v => v.trim()))
        errors[`cond-${i}`] = 'Pick or enter the answer it depends on.'
    })
    if (!form.conditions.length)
      errors.conditions = 'Add a condition, or turn this off.'
  }
  if (form.conditionsOn && form.conditionsRaw !== null) {
    try {
      JSON.parse(form.conditionsRaw)
    }
    catch {
      errors.conditionsRaw = 'This isn\'t valid JSON.'
    }
  }
  if (form.limitsRaw !== null && form.limitsRaw.trim()) {
    try {
      JSON.parse(form.limitsRaw)
    }
    catch {
      errors.limitsRaw = 'This isn\'t valid JSON.'
    }
  }
  limitErrors.value = form.limitsRaw === null ? checkLimits(form.type, form.limits) : {}
  return !Object.keys(errors).length && !Object.keys(limitErrors.value).length
}

async function submit() {
  if (isSaving.value)
    return
  formError.value = ''
  if (!validate()) {
    nextTick(() => document.querySelector<HTMLElement>('#qe-form [aria-invalid="true"]')?.focus())
    return
  }
  const conditionalLogic = !form.conditionsOn
    ? ''
    : form.conditionsRaw !== null ? form.conditionsRaw.trim() : buildConditions(form.conditions)
  const validationRules = form.limitsRaw !== null
    ? form.limitsRaw.trim()
    : buildLimits(form.type, form.limits, form.limitsExtra)
  const payload: QuestionPayload = {
    questionText: form.text.trim(),
    questionKey: form.key.trim(),
    helpText: form.helpText.trim(),
    isRequired: form.isRequired,
    type: form.type,
    category: form.category.trim(),
    conditionalLogic,
    validationRules,
    options: showsOptions.value
      ? form.options.map((row, index) => ({
          id: row.id,
          optionText: row.label.trim(),
          optionValue: optionValueOf(row),
          displayOrder: index,
          hasFollowUpText: row.hasFollowUp,
          followUpPlaceholder: row.hasFollowUp ? (row.followUpPlaceholder.trim() || null) : null,
        }))
      : [],
  }
  isSaving.value = true
  try {
    await props.save(payload)
    open.value = false
  }
  catch (error) {
    const bag = getApiValidationErrors(error)
    errors.key = getApiFieldError(bag, 'questionKey') ?? ''
    errors.text = getApiFieldError(bag, 'questionText') ?? ''
    if (!errors.key)
      delete errors.key
    if (!errors.text)
      delete errors.text
    if (!Object.keys(errors).length)
      formError.value = getApiErrorMessage(error, 'Nothing was saved. Try again.')
  }
  finally {
    isSaving.value = false
  }
}

const limitLabels: Record<keyof QuestionLimits, string> = {
  min: 'Lowest number',
  max: 'Highest number',
  minLength: 'Fewest characters',
  maxLength: 'Most characters',
  pattern: 'Must match (regular expression)',
  patternMessage: 'Message when it doesn\'t match',
  minSelections: 'Fewest choices',
  maxSelections: 'Most choices',
  minDate: 'Earliest date',
  maxDate: 'Latest date',
}
const rangeLimitKeys = computed(() => limitKeys.value.filter(k => k !== 'pattern' && k !== 'patternMessage'))
const limitNumber = (key: keyof QuestionLimits) => computed({
  get: () => (form.limits[key] ?? null) as number | string | null,
  set: (v) => {
    const isText = key === 'pattern' || key === 'patternMessage' || key === 'minDate' || key === 'maxDate'
    const blank = v === '' || v === null || v === undefined
    ;(form.limits as Record<string, unknown>)[key] = blank ? null : isText ? String(v) : Number(v)
  },
})
const limitModels = Object.fromEntries((Object.keys(limitLabels) as (keyof QuestionLimits)[]).map(k => [k, limitNumber(k)])) as Record<keyof QuestionLimits, ReturnType<typeof limitNumber>>
</script>

<template>
  <GModal
    v-model:open="open"
    size="lg"
    :title="isNew ? 'Add a question' : 'Edit question'"
    :dismissible="!isSaving"
  >
    <form
      id="qe-form"
      class="qe"
      novalidate
      @submit.prevent="submit"
    >
      <!-- the question -->
      <GFormField
        label="Question"
        required
        :error="errors.text"
      >
        <GTextarea
          v-model="form.text"
          :rows="2"
          autoresize
          autofocus
        />
      </GFormField>

      <GFormField
        label="Help text"
        hint="Shown under the question. Optional."
      >
        <GInput v-model="form.helpText" />
      </GFormField>

      <div class="qe-pair">
        <GFormField
          label="Answer type"
          :hint="typeHint"
        >
          <GSelect
            v-model="form.type"
            :items="QUESTION_TYPES"
          />
        </GFormField>

        <GFormField
          v-if="kind === 'hackathon'"
          label="Where it goes"
          hint="The form page and section it shows in."
          required
          :error="errors.category"
        >
          <GSelect
            v-model="form.category"
            :items="hackathonCategoryItems"
            placeholder="Pick a section"
          />
        </GFormField>
        <GFormField
          v-else
          label="Section"
          hint="Questions with the same section are shown together. Optional."
        >
          <GInput
            v-model="form.category"
            list="qe-sections"
            placeholder="e.g. About you"
          />
          <datalist id="qe-sections">
            <option
              v-for="c in workshopCategories"
              :key="c"
              :value="c"
            />
          </datalist>
        </GFormField>
      </div>

      <GCheckbox
        v-model="form.isRequired"
        label="Required"
        description="People can't submit the form without answering."
      />

      <GFormField
        v-if="isNew"
        label="Answer key"
        hint="Names this answer in exports and conditions. It can't be changed later."
        required
        :error="errors.key"
      >
        <GInput
          v-model="form.key"
          class="qe-mono"
          autocomplete="off"
          spellcheck="false"
          @input="form.keyTouched = true"
        />
      </GFormField>
      <p
        v-else
        class="qe-muted"
      >
        Answer key <code class="qe-code">{{ form.key }}</code> (can't be changed)
      </p>

      <!-- options -->
      <fieldset
        v-if="showsOptions"
        class="qe-group"
      >
        <legend class="qe-group__title">
          Options
        </legend>
        <p class="qe-muted">
          Changing an option's wording keeps the answers people already gave.
        </p>
        <ol class="qe-options">
          <li
            v-for="(row, index) in form.options"
            :key="row.uid"
            class="qe-option"
          >
            <div class="qe-option__line">
              <GFormField
                :label="`Option ${index + 1}`"
                hide-label
                :error="errors[`option-${row.uid}`]"
                class="qe-option__field"
              >
                <GInput
                  :id="`qe-option-${row.uid}`"
                  v-model="row.label"
                  :placeholder="`Option ${index + 1}`"
                />
              </GFormField>
              <div class="qe-option__tools">
                <GButton
                  variant="ghost"
                  size="sm"
                  icon="material-symbols:arrow-upward-rounded"
                  :aria-label="`Move option ${index + 1} up`"
                  :disabled="index === 0"
                  @click="moveOption(index, -1)"
                />
                <GButton
                  variant="ghost"
                  size="sm"
                  icon="material-symbols:arrow-downward-rounded"
                  :aria-label="`Move option ${index + 1} down`"
                  :disabled="index === form.options.length - 1"
                  @click="moveOption(index, 1)"
                />
                <GButton
                  variant="ghost"
                  size="sm"
                  icon="material-symbols:close-rounded"
                  :aria-label="`Remove option ${index + 1}`"
                  @click="removeOption(index)"
                />
              </div>
            </div>
            <GCheckbox
              v-model="row.hasFollowUp"
              label="Ask for more detail when picked"
            />
            <GFormField
              v-if="row.hasFollowUp"
              label="Detail box label"
              class="qe-option__detail"
            >
              <GInput
                v-model="row.followUpPlaceholder"
                :placeholder="`Tell us more about “${row.label || 'this'}”`"
              />
            </GFormField>
          </li>
        </ol>
        <p
          v-if="errors.options"
          class="qe-error"
          role="alert"
        >
          {{ errors.options }}
        </p>
        <GButton
          variant="secondary"
          size="sm"
          icon="material-symbols:add-rounded"
          label="Add option"
          class="qe-fit"
          @click="addOption"
        />
      </fieldset>

      <!-- show only when -->
      <fieldset class="qe-group">
        <legend class="qe-group__title">
          Show only when
        </legend>
        <GCheckbox
          v-model="form.conditionsOn"
          label="Only ask this after a certain answer"
          description="Hidden until every condition matches. Once shown, it has to be answered."
          :disabled="!conditionSourceItems.length && form.conditionsRaw === null"
        />
        <template v-if="form.conditionsOn">
          <GFormField
            v-if="form.conditionsRaw !== null"
            label="Condition (JSON)"
            hint="This condition isn't in a shape the builder can show, so it's shown as stored."
            :error="errors.conditionsRaw"
          >
            <GTextarea
              v-model="form.conditionsRaw"
              :rows="3"
              class="qe-mono"
            />
          </GFormField>
          <template v-else>
            <div
              v-for="(condition, index) in form.conditions"
              :key="index"
              class="qe-cond"
            >
              <span
                class="qe-cond__word"
                aria-hidden="true"
              >{{ index === 0 ? 'When' : 'and' }}</span>
              <GFormField
                :label="`Condition ${index + 1}: question`"
                hide-label
                class="qe-cond__field"
                :error="errors[`cond-${index}`]"
              >
                <GSelect
                  v-model="condition.key"
                  :items="conditionSourceItems"
                  placeholder="Pick a question"
                  @update:model-value="condition.values = []"
                />
              </GFormField>
              <span
                class="qe-cond__word"
                aria-hidden="true"
              >is</span>
              <GFormField
                :label="`Condition ${index + 1}: answer`"
                hide-label
                class="qe-cond__field"
              >
                <GSelect
                  v-if="valueItemsFor(condition.key)"
                  v-model="condition.values"
                  :items="valueItemsFor(condition.key)!"
                  multiple
                  placeholder="Any of…"
                />
                <GInput
                  v-else
                  :model-value="condition.values[0] ?? ''"
                  placeholder="The answer"
                  :disabled="!condition.key"
                  @update:model-value="condition.values = [String($event ?? '')]"
                />
              </GFormField>
              <GButton
                variant="ghost"
                size="sm"
                icon="material-symbols:close-rounded"
                :aria-label="`Remove condition ${index + 1}`"
                @click="form.conditions.splice(index, 1)"
              />
            </div>
            <p
              v-if="errors.conditions"
              class="qe-error"
              role="alert"
            >
              {{ errors.conditions }}
            </p>
            <GButton
              variant="secondary"
              size="sm"
              icon="material-symbols:add-rounded"
              label="Add condition"
              class="qe-fit"
              @click="addCondition"
            />
          </template>
        </template>
      </fieldset>

      <!-- limits -->
      <fieldset
        v-if="limitKeys.length || form.limitsRaw !== null"
        class="qe-group"
      >
        <legend class="qe-group__title">
          Answer limits
        </legend>
        <GFormField
          v-if="form.limitsRaw !== null"
          label="Rules (JSON)"
          hint="These rules aren't in a shape the builder can show, so they're shown as stored."
          :error="errors.limitsRaw"
        >
          <GTextarea
            v-model="form.limitsRaw"
            :rows="3"
            class="qe-mono"
          />
        </GFormField>
        <template v-else>
          <p class="qe-muted">
            Optional. Leave blank for no limit.
          </p>
          <div class="qe-pair">
            <GFormField
              v-for="key in rangeLimitKeys"
              :key="key"
              :label="limitLabels[key]"
              :error="limitErrors[key]"
            >
              <GInput
                v-model="limitModels[key].value"
                :type="form.type === 'Date' ? 'date' : 'number'"
                :inputmode="form.type === 'Date' ? undefined : 'numeric'"
              />
            </GFormField>
          </div>
          <details
            v-if="limitKeys.includes('pattern')"
            class="qe-advanced"
            :open="!!form.limits.pattern"
          >
            <summary>Pattern</summary>
            <div class="qe-advanced__body">
              <GFormField
                :label="limitLabels.pattern"
                hint="e.g. ^@\w+$ for a Telegram handle."
                :error="limitErrors.pattern"
              >
                <GInput
                  v-model="limitModels.pattern.value"
                  class="qe-mono"
                  spellcheck="false"
                />
              </GFormField>
              <GFormField :label="limitLabels.patternMessage">
                <GInput
                  v-model="limitModels.patternMessage.value"
                  placeholder="e.g. Start with @"
                />
              </GFormField>
            </div>
          </details>
        </template>
      </fieldset>

      <GAlert
        v-if="formError"
        tone="err"
        title="Not saved"
        :description="formError"
      />
    </form>

    <template #footer>
      <GButton
        variant="ghost"
        label="Cancel"
        :disabled="isSaving"
        @click="open = false"
      />
      <GButton
        variant="primary"
        type="submit"
        form="qe-form"
        :label="isNew ? 'Add question' : 'Save changes'"
        :loading="isSaving"
        loading-label="Saving"
      />
    </template>
  </GModal>
</template>

<style scoped>
.qe {
  display: flex;
  flex-direction: column;
  gap: 18px;
}
.qe-pair {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(14rem, 1fr));
  gap: 16px;
}
.qe-muted {
  margin: 0;
  font-size: 14px;
  color: var(--gh-text-muted);
}
.qe-code,
.qe-mono :deep(input),
.qe-mono :deep(textarea) {
  font-family: var(--font-mono);
}
.qe-code { color: var(--gh-text); }
.qe-group {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin: 0;
  padding: 16px 0 0;
  border: 0;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.qe-group__title {
  float: left;
  width: 100%;
  margin: 0 0 4px;
  padding: 0;
  font-size: 16px;
  font-weight: 700;
}
.qe-options {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.qe-option {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 10px 12px;
  border: var(--gh-border-width) solid var(--gh-line-soft);
  border-radius: var(--radius-gh);
}
.qe-option__line {
  display: flex;
  align-items: flex-start;
  gap: 8px;
}
.qe-option__field {
  flex: 1;
  min-width: 0;
}
.qe-option__tools {
  display: flex;
  flex: none;
  gap: 2px;
}
.qe-cond {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) auto minmax(0, 1fr) auto;
  align-items: start;
  gap: 8px;
}
.qe-cond__word {
  padding-top: 10px;
  font-weight: 600;
  white-space: nowrap;
}
@media (max-width: 40rem) {
  .qe-cond { grid-template-columns: auto minmax(0, 1fr) auto; }
  .qe-cond > :nth-child(3) { grid-column: 1; }
  .qe-cond > :nth-child(4) { grid-column: 2; }
}
.qe-fit { align-self: flex-start; }
.qe-error {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
  color: var(--gh-err);
}
.qe-advanced summary {
  width: fit-content;
  font-weight: 600;
  cursor: pointer;
}
.qe-advanced summary:focus-visible {
  outline: 2px solid var(--gh-focus);
  outline-offset: 2px;
}
.qe-advanced__body {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-top: 12px;
}
</style>
