<script setup lang="ts">
import type { GeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListQuestionDto } from '@geekshacking/portal-sdk'
import type { QuestionPayload } from './QuestionEditor.vue'
import {
  geeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint1QueryKey,
  geeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint2QueryKey,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsCreateEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsCreateEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsDeleteEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsDeleteEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsInitializeEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsInitializeEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsUpdateEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsUpdateEndpoint2,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'

/**
 * The registration questions of one event, laid out the way the sign-up form shows them.
 * Shared by hackathons and workshops (the API's "1" endpoints are hackathons, "2" workshops).
 * Hackathon questions sit on fixed form pages; a question whose section isn't one of them is
 * listed under "Not on the form", since participants never see it.
 */
type Question = GeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListQuestionDto

const props = defineProps<{
  kind: 'hackathon' | 'workshop'
  eventId: string
}>()

const queryClient = useQueryClient()
const toast = useGToast()
const isHackathon = props.kind === 'hackathon'
const path = computed(() => ({ activityId: props.eventId }))
const enabled = { query: { enabled: computed(() => !!props.eventId) } }

const listQuery = isHackathon
  ? useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint1({ path }, enabled)
  : useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint2({ path }, enabled)
const createMutation = isHackathon
  ? useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsCreateEndpoint1()
  : useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsCreateEndpoint2()
const updateMutation = isHackathon
  ? useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsUpdateEndpoint1()
  : useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsUpdateEndpoint2()
const deleteMutation = isHackathon
  ? useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsDeleteEndpoint1()
  : useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsDeleteEndpoint2()
const initMutation = isHackathon
  ? useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsInitializeEndpoint1()
  : useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsInitializeEndpoint2()

function refresh() {
  return queryClient.invalidateQueries({
    queryKey: isHackathon
      ? geeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint1QueryKey({ path: { activityId: props.eventId } })
      : geeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint2QueryKey({ path: { activityId: props.eventId } }),
  })
}

/** every question in form order (ties keep the API's order) */
const questions = computed<Question[]>(() => (listQuery.data.value?.questions ?? [])
  .map((q, i) => ({ q, i }))
  .sort((a, b) => (a.q.displayOrder ?? 0) - (b.q.displayOrder ?? 0) || a.i - b.i)
  .map(({ q }) => q))
const byKey = computed(() => new Map(questions.value.map(q => [q.questionKey ?? '', q])))

// ---- layout: pages (hackathon) > sections > questions ----
interface Section { name: string, questions: Question[] }
interface Group { id: string, title: string | null, note?: string, offForm?: boolean, sections: Section[] }

function sectionsOf(list: Question[]): Section[] {
  const map = new Map<string, Question[]>()
  for (const q of list) {
    const name = q.category || UNCATEGORISED
    map.set(name, [...(map.get(name) ?? []), q])
  }
  return [...map].map(([name, qs]) => ({ name, questions: qs }))
}

const groups = computed<Group[]>(() => {
  if (!isHackathon)
    return [{ id: 'all', title: null, sections: sectionsOf(questions.value) }]
  const result: Group[] = HACKATHON_FORM_PAGES.map((page, index) => ({
    id: `page-${index}`,
    title: `Page ${index + 1} · ${page.name}`,
    sections: sectionsOf(questions.value.filter(q => page.categories.includes(q.category ?? ''))),
  }))
  const offForm = questions.value.filter(q => !HACKATHON_CATEGORIES.includes(q.category ?? ''))
  if (offForm.length) {
    result.push({
      id: 'off-form',
      title: 'Not on the form',
      note: 'Their section isn\'t one of the form\'s pages, so participants never see them. Edit each one and pick where it goes.',
      offForm: true,
      sections: sectionsOf(offForm),
    })
  }
  return result
})
const offFormCount = computed(() => groups.value.find(g => g.offForm)?.sections.reduce((n, s) => n + s.questions.length, 0) ?? 0)
const requiredCount = computed(() => questions.value.filter(q => q.isRequired).length)

// ---- describing a question ----
function optionSummary(q: Question) {
  const labels = [...(q.options ?? [])].sort((a, b) => (a.displayOrder ?? 0) - (b.displayOrder ?? 0)).map(o => o.optionText ?? '')
  if (!labels.length)
    return ''
  const shown = labels.slice(0, 4).join(', ')
  return labels.length > 4 ? `${shown} +${labels.length - 4} more` : shown
}

function conditionSummary(q: Question) {
  if (!q.conditionalLogic?.trim())
    return ''
  const conditions = parseConditions(q.conditionalLogic)
  if (!conditions)
    return 'Shown on a condition the builder can\'t read'
  return `Shown when ${conditions.map((c) => {
    const source = byKey.value.get(c.key)
    const label = (value: string) => source?.options?.find(o => o.optionValue === value)?.optionText
      ?? (source?.type === 'Boolean' ? (value === 'true' ? 'Yes' : value === 'false' ? 'No' : value) : value)
    return `“${source?.questionText || c.key}” is ${c.values.map(label).join(' or ')}`
  }).join(' and ')}`
}

function conditionIsBroken(q: Question) {
  const conditions = parseConditions(q.conditionalLogic)
  return !!conditions?.some(c => !byKey.value.has(c.key))
}

// ---- add / edit ----
const isEditorOpen = ref(false)
const editing = ref<Question | null>(null)

function openAdd() {
  editing.value = null
  isEditorOpen.value = true
}
function openEdit(q: Question) {
  editing.value = q
  isEditorOpen.value = true
}

async function save(payload: QuestionPayload) {
  const target = editing.value
  if (!target?.id) {
    const nextOrder = questions.value.length ? Math.max(...questions.value.map(q => q.displayOrder ?? 0)) + 1 : 0
    await createMutation.mutateAsync({
      path: { activityId: props.eventId },
      body: {
        questionText: payload.questionText,
        questionKey: payload.questionKey,
        helpText: payload.helpText || null,
        isRequired: payload.isRequired,
        type: payload.type,
        displayOrder: nextOrder,
        category: payload.category || null,
        conditionalLogic: payload.conditionalLogic || null,
        validationRules: payload.validationRules || null,
        options: payload.options.map(({ id: _id, ...option }) => option),
      },
    })
    await refresh()
    toast.add({ tone: 'ok', title: 'Question added' })
    return
  }
  const hadOptions = !!target.options?.length
  await updateMutation.mutateAsync({
    path: { activityId: props.eventId, questionId: target.id },
    // the API leaves null fields unchanged, so blanks are sent as '' to clear them
    body: {
      questionText: payload.questionText,
      helpText: payload.helpText,
      isRequired: payload.isRequired,
      type: payload.type,
      category: payload.category,
      conditionalLogic: payload.conditionalLogic,
      validationRules: payload.validationRules,
      options: payload.options.length || hadOptions ? payload.options.map(o => ({ ...o, id: o.id ?? null })) : null,
    },
  })
  await refresh()
  toast.add({ tone: 'ok', title: 'Question saved' })
}

// ---- reorder within a section ----
const movingId = ref<string | null>(null)
const moveError = ref('')

async function move(section: Section, q: Question, by: -1 | 1) {
  const index = section.questions.indexOf(q)
  const neighbour = section.questions[index + by]
  if (!neighbour || movingId.value)
    return
  moveError.value = ''
  movingId.value = q.id ?? null
  const order = [...questions.value]
  const a = order.indexOf(q)
  const b = order.indexOf(neighbour)
  order[a] = neighbour
  order[b] = q
  try {
    await Promise.all(order
      .map((item, position) => ({ item, position }))
      .filter(({ item, position }) => item.id && item.displayOrder !== position)
      .map(({ item, position }) => updateMutation.mutateAsync({
        path: { activityId: props.eventId, questionId: item.id! },
        body: { displayOrder: position },
      })))
  }
  catch (error) {
    moveError.value = getApiErrorMessage(error, 'The order may be only partly saved. Check it and try again.')
  }
  finally {
    await refresh()
    movingId.value = null
    // keep focus on the row: the same arrow, or the other one once it reaches the end
    nextTick(() => {
      const row = document.querySelector<HTMLElement>(`[data-question="${q.id}"]`)
      ;(row?.querySelector<HTMLElement>(`[data-move="${by}"]:not(:disabled)`)
        ?? row?.querySelector<HTMLElement>(`[data-move="${-by}"]:not(:disabled)`)
        ?? row?.querySelector<HTMLElement>('button:not(:disabled)'))?.focus()
    })
  }
}

// ---- delete ----
const confirmingDelete = ref<string | null>(null)
const deleteError = ref<{ id: string, message: string } | null>(null)

async function remove(q: Question) {
  if (!q.id || deleteMutation.isPending.value)
    return
  deleteError.value = null
  try {
    await deleteMutation.mutateAsync({ path: { activityId: props.eventId, questionId: q.id } })
    confirmingDelete.value = null
    await refresh()
    toast.add({ tone: 'ok', title: 'Question deleted' })
  }
  catch (error) {
    deleteError.value = { id: q.id, message: getApiErrorMessage(error, 'It wasn\'t deleted. Try again.') }
  }
}

const isClearOpen = ref(false)
const clearProgress = ref<{ done: number, total: number } | null>(null)
const clearError = ref('')

async function removeAll() {
  if (clearProgress.value)
    return
  clearError.value = ''
  const list = questions.value.filter(q => q.id)
  clearProgress.value = { done: 0, total: list.length }
  try {
    for (const q of list) {
      await deleteMutation.mutateAsync({ path: { activityId: props.eventId, questionId: q.id! } })
      clearProgress.value.done++
    }
    isClearOpen.value = false
    toast.add({ tone: 'ok', title: `Deleted ${list.length} questions` })
  }
  catch (error) {
    clearError.value = `${clearProgress.value.done} of ${list.length} were deleted before it stopped. ${getApiErrorMessage(error, 'Try again.')}`
  }
  finally {
    clearProgress.value = null
    await refresh()
  }
}

// ---- standard questions ----
const initError = ref('')
async function addStandard() {
  if (initMutation.isPending.value)
    return
  initError.value = ''
  try {
    const result = await initMutation.mutateAsync({ path: { activityId: props.eventId } })
    await refresh()
    toast.add({ tone: 'ok', title: `Added ${result?.questionsCreated ?? 'the'} standard questions`, description: 'Edit or delete any you don\'t need.' })
  }
  catch (error) {
    initError.value = getApiErrorMessage(error, 'No questions were added. Try again, or add your own.')
  }
}

const eventNoun = isHackathon ? 'hackathon' : 'workshop'
</script>

<template>
  <GPanel :id="isHackathon ? 'questions' : 'standalone-questions'">
    <template #header>
      <GPanelHeader title="Registration questions">
        <template #right>
          <GButton
            v-if="questions.length"
            variant="primary"
            icon="material-symbols:add-rounded"
            label="Add question"
            @click="openAdd"
          />
        </template>
      </GPanelHeader>
    </template>

    <template #body>
      <div class="qb">
        <GAlert
          v-if="listQuery.error.value && !questions.length"
          tone="err"
          title="Couldn't load the questions"
          :description="getApiErrorMessage(listQuery.error.value, 'Refresh the page to try again.')"
        />

        <div
          v-if="listQuery.isLoading.value"
          class="qb-loading"
          aria-busy="true"
        >
          <GSkeleton
            v-for="i in 4"
            :key="i"
            class="qb-skeleton"
          />
        </div>

        <GEmptyState
          v-else-if="!questions.length && !listQuery.error.value"
          icon="material-symbols:list-alt-rounded"
          title="No registration questions yet"
          :description="`Start from the standard ${eventNoun} questions and adjust them, or write your own.`"
        >
          <template #actions>
            <GButton
              variant="primary"
              icon="material-symbols:playlist-add-rounded"
              label="Use the standard questions"
              :loading="initMutation.isPending.value"
              loading-label="Adding"
              @click="addStandard"
            />
            <GButton
              variant="secondary"
              icon="material-symbols:add-rounded"
              label="Write my own"
              @click="openAdd"
            />
          </template>
        </GEmptyState>
        <GAlert
          v-if="initError"
          tone="err"
          title="Standard questions not added"
          :description="initError"
        />

        <template v-if="questions.length">
          <p class="qb-muted">
            {{ questions.length }} {{ questions.length === 1 ? 'question' : 'questions' }}, {{ requiredCount }} required.
            They're listed in the order people see them. Names and emails come from each person's account, so you don't need to ask for them.
          </p>
          <GAlert
            v-if="offFormCount"
            tone="attention"
            :title="`${offFormCount} ${offFormCount === 1 ? 'question isn\'t' : 'questions aren\'t'} on the form`"
            description="Their section isn't one of the form's pages. They're listed at the bottom."
          />
          <GAlert
            v-if="moveError"
            tone="err"
            title="Order not saved"
            :description="moveError"
            dismissible
            @close="moveError = ''"
          />

          <section
            v-for="group in groups"
            :key="group.id"
            class="qb-group"
            :class="{ 'qb-group--off': group.offForm }"
            :aria-labelledby="group.title ? `qb-${group.id}` : undefined"
            :aria-label="group.title ? undefined : 'Questions'"
          >
            <div
              v-if="group.title"
              class="qb-group__head"
            >
              <h2
                :id="`qb-${group.id}`"
                class="qb-group__title"
              >
                {{ group.title }}
              </h2>
              <p
                v-if="group.note"
                class="qb-muted"
              >
                {{ group.note }}
              </p>
            </div>
            <p
              v-if="!group.sections.length"
              class="qb-muted qb-empty-page"
            >
              No questions on this page.
            </p>

            <div
              v-for="section in group.sections"
              :key="section.name"
              class="qb-section"
            >
              <h3 class="qb-section__title">
                {{ section.name }}
              </h3>
              <ol class="qb-list">
                <li
                  v-for="(q, index) in section.questions"
                  :key="q.id"
                  class="qb-row"
                  :data-question="q.id"
                  :aria-busy="movingId === q.id"
                >
                  <div class="qb-row__main">
                    <span
                      class="qb-row__num"
                      aria-hidden="true"
                    >{{ index + 1 }}</span>
                    <div class="qb-row__body">
                      <p class="qb-row__text">
                        {{ q.questionText || 'Untitled question' }}
                        <span
                          v-if="q.isRequired"
                          class="qb-required"
                        >Required</span>
                      </p>
                      <p
                        v-if="q.helpText"
                        class="qb-row__help"
                      >
                        {{ q.helpText }}
                      </p>
                      <p class="qb-row__facts">
                        <span>{{ questionTypeLabel(q.type) }}</span>
                        <code class="qb-key">{{ q.questionKey }}</code>
                        <span v-if="describeLimits(q.type, q.validationRules)">{{ describeLimits(q.type, q.validationRules) }}</span>
                      </p>
                      <p
                        v-if="optionSummary(q)"
                        class="qb-row__facts"
                      >
                        <span class="qb-row__label">Options</span> {{ optionSummary(q) }}
                      </p>
                      <p
                        v-if="conditionSummary(q)"
                        class="qb-row__cond"
                        :class="{ 'qb-row__cond--broken': conditionIsBroken(q) }"
                      >
                        <Icon
                          :name="conditionIsBroken(q) ? 'material-symbols:warning-rounded' : 'material-symbols:call-split-rounded'"
                          aria-hidden="true"
                        />
                        <span>
                          {{ conditionSummary(q) }}<template v-if="conditionIsBroken(q)">. That question no longer exists, so this one never shows.</template>
                        </span>
                      </p>
                    </div>
                    <div class="qb-row__tools">
                      <GButton
                        variant="ghost"
                        size="sm"
                        icon="material-symbols:arrow-upward-rounded"
                        data-move="-1"
                        :aria-label="`Move “${q.questionText}” up`"
                        title="Move up"
                        :disabled="index === 0 || !!movingId"
                        @click="move(section, q, -1)"
                      />
                      <GButton
                        variant="ghost"
                        size="sm"
                        icon="material-symbols:arrow-downward-rounded"
                        data-move="1"
                        :aria-label="`Move “${q.questionText}” down`"
                        title="Move down"
                        :disabled="index === section.questions.length - 1 || !!movingId"
                        @click="move(section, q, 1)"
                      />
                      <GButton
                        variant="secondary"
                        size="sm"
                        icon="material-symbols:edit-rounded"
                        label="Edit"
                        :aria-label="`Edit “${q.questionText}”`"
                        @click="openEdit(q)"
                      />
                      <GButton
                        v-if="confirmingDelete !== q.id"
                        variant="ghost"
                        size="sm"
                        icon="material-symbols:delete-rounded"
                        :aria-label="`Delete “${q.questionText}”`"
                        title="Delete"
                        @click="deleteError = null; confirmingDelete = q.id ?? null"
                      />
                    </div>
                  </div>
                  <div
                    v-if="confirmingDelete === q.id"
                    class="qb-confirm"
                    role="group"
                    aria-label="Delete this question?"
                  >
                    <p class="qb-confirm__text">
                      Delete this question? <strong>Everyone's answers to it are deleted too.</strong>
                    </p>
                    <div class="qb-confirm__actions">
                      <GButton
                        variant="ghost"
                        size="sm"
                        label="Keep"
                        :disabled="deleteMutation.isPending.value"
                        @click="confirmingDelete = null"
                      />
                      <GButton
                        variant="danger"
                        size="sm"
                        label="Delete"
                        :loading="deleteMutation.isPending.value"
                        loading-label="Deleting"
                        @click="remove(q)"
                      />
                    </div>
                  </div>
                  <p
                    v-if="deleteError?.id === q.id"
                    class="qb-error"
                    role="alert"
                  >
                    {{ deleteError.message }}
                  </p>
                </li>
              </ol>
            </div>
          </section>

          <p class="qb-muted qb-tip">
            To move a question to another section, edit it and change its section.
          </p>

          <div class="qb-danger">
            <div>
              <p class="qb-danger__title">
                Start over
              </p>
              <p class="qb-muted">
                Delete every question and every answer to them.
              </p>
            </div>
            <GButton
              variant="danger"
              icon="material-symbols:delete-sweep-rounded"
              label="Delete all questions"
              @click="clearError = ''; isClearOpen = true"
            />
          </div>
        </template>
      </div>

      <OrganizersQuestionEditor
        v-model:open="isEditorOpen"
        :kind="kind"
        :question="editing"
        :questions="questions"
        :save="save"
      />

      <GModal
        v-model:open="isClearOpen"
        size="sm"
        :title="`Delete all ${questions.length} questions?`"
        description="Everyone's answers to them are deleted too. This can't be undone."
        :dismissible="!clearProgress"
      >
        <p
          v-if="clearProgress"
          class="qb-muted"
          role="status"
        >
          Deleted {{ clearProgress.done }} of {{ clearProgress.total }}…
        </p>
        <GAlert
          v-if="clearError"
          tone="err"
          title="Stopped"
          :description="clearError"
        />
        <template #footer>
          <GButton
            variant="ghost"
            label="Keep them"
            :disabled="!!clearProgress"
            @click="isClearOpen = false"
          />
          <GButton
            variant="danger"
            label="Delete all"
            :loading="!!clearProgress"
            loading-label="Deleting"
            @click="removeAll"
          />
        </template>
      </GModal>
    </template>
  </GPanel>
</template>

<style scoped>
.qb {
  display: flex;
  flex-direction: column;
  gap: 20px;
  width: 100%;
  max-width: 64rem;
  margin: 0 auto;
}
.qb-muted {
  margin: 0;
  color: var(--gh-text-muted);
}
.qb-loading {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.qb-skeleton {
  display: block;
  height: 64px;
}
.qb-group {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.qb-group__head {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding-bottom: 8px;
  border-bottom: var(--gh-border-width) solid var(--gh-line);
}
.qb-group__title {
  margin: 0;
  font-size: 18px;
  font-weight: 700;
}
.qb-group--off .qb-group__head { border-bottom-color: var(--gh-err); }
.qb-empty-page { font-size: 14px; }
.qb-section {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.qb-section__title {
  margin: 0;
  font-size: 14px;
  font-weight: 700;
  letter-spacing: var(--gh-tracking);
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.qb-list {
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0;
  list-style: none;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.qb-row {
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 12px 16px;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.qb-row:first-child { border-top: 0; }
.qb-row[aria-busy='true'] { opacity: 0.6; }
.qb-row__main {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  gap: 8px 12px;
}
.qb-row__num {
  flex: none;
  width: 1.75rem;
  padding-top: 1px;
  font-family: var(--font-mono);
  font-weight: 700;
  color: var(--gh-text-muted);
}
.qb-row__body {
  display: flex;
  flex: 1 1 18rem;
  flex-direction: column;
  gap: 4px;
  min-width: 0;
}
.qb-row__text {
  margin: 0;
  font-weight: 700;
  overflow-wrap: anywhere;
}
.qb-required {
  margin-left: 8px;
  padding: 1px 6px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-pill);
  font-size: 12px;
  font-weight: 600;
  vertical-align: 2px;
  white-space: nowrap;
}
.qb-row__help {
  margin: 0;
  font-size: 14px;
  color: var(--gh-text-muted);
}
.qb-row__facts {
  display: flex;
  flex-wrap: wrap;
  gap: 2px 12px;
  margin: 0;
  font-size: 14px;
  color: var(--gh-text-muted);
  overflow-wrap: anywhere;
}
.qb-row__label { font-weight: 600; color: var(--gh-text); }
.qb-key {
  font-family: var(--font-mono);
  font-size: 13px;
}
.qb-row__cond {
  display: flex;
  align-items: flex-start;
  gap: 6px;
  margin: 2px 0 0;
  font-size: 14px;
}
.qb-row__cond .iconify,
.qb-row__cond :deep(svg) {
  flex: none;
  width: 18px;
  height: 18px;
  margin-top: 1px;
}
.qb-row__cond--broken { color: var(--gh-err); font-weight: 600; }
.qb-row__tools {
  display: flex;
  flex: none;
  align-items: center;
  gap: 4px;
  margin-left: auto;
}
.qb-confirm {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 8px 16px;
  padding: 10px 12px;
  border: var(--gh-border-width) solid var(--gh-err);
  border-radius: var(--radius-gh);
  background: var(--gh-err-soft);
}
.qb-confirm__text {
  flex: 1 1 16rem;
  margin: 0;
  font-size: 14px;
}
.qb-confirm__actions {
  display: flex;
  gap: 8px;
}
.qb-error {
  margin: 0;
  font-size: 14px;
  font-weight: 600;
  color: var(--gh-err);
}
.qb-tip { font-size: 14px; }
.qb-danger {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 12px 24px;
  margin-top: 12px;
  padding: 16px;
  border: var(--gh-border-width) solid var(--gh-line-soft);
  border-radius: var(--radius-gh-lg);
}
.qb-danger__title {
  margin: 0;
  font-weight: 700;
}
</style>
