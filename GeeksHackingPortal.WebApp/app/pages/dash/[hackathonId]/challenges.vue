<script setup lang="ts">
import type { GeeksHackingPortalApiEndpointsOrganizersHackathonChallengesListResponseResponseChallenge } from '@geekshacking/portal-sdk'
import type { ChallengeRule } from '~/utils/challenge-rule'
import {
  geeksHackingPortalApiEndpointsOrganizersHackathonChallengesListEndpointQueryKey,
  geeksHackingPortalApiEndpointsParticipantsHackathonChallengesListEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesCreateEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesDeleteEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesUpdateEndpoint,
  useGeeksHackingPortalApiEndpointsParticipantsHackathonChallengesListEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import {
  buildChallengeRule,
  CHALLENGE_RULE_VARIABLES,
  checkChallengeRuleSyntax,
  describeChallengeRule,
  parseChallengeRule,
} from '~/utils/challenge-rule'

/**
 * Hackathon challenges: list with status, team counts and who can pick each one; add / edit in
 * a dialog; delete with a named confirm. "Who can pick it" is the rule the API checks when a
 * team submits (see utils/challenge-rule.ts), set with presets or a custom rule.
 */
type Challenge = GeeksHackingPortalApiEndpointsOrganizersHackathonChallengesListResponseResponseChallenge

const props = withDefaults(defineProps<{
  hackathonId?: string
}>(), {
  hackathonId: '',
})
const route = useRoute()
const hackathonId = computed(() => props.hackathonId || (route.params.hackathonId as string | undefined) || '')

const queryClient = useQueryClient()
const toast = useGToast()

const {
  data: challengesData,
  isLoading,
  error: loadError,
} = useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesListEndpoint({ path: computed(() => ({ hackathonId: hackathonId.value })) })
const challenges = computed<Challenge[]>(() => challengesData.value?.challenges ?? [])
const publishedCount = computed(() => challenges.value.filter(c => c.isPublished).length)

// team counts come from the participant list (published challenges only)
const { data: participantChallengesData } = useGeeksHackingPortalApiEndpointsParticipantsHackathonChallengesListEndpoint({ path: computed(() => ({ hackathonId: hackathonId.value })) })
const teamCountById = computed(() => new Map((participantChallengesData.value?.challenges ?? [])
  .filter(c => c.id)
  .map(c => [c.id as string, c.teamCount ?? 0])))

function teamCountLabel(challenge: Challenge) {
  const count = teamCountById.value.get(challenge.id ?? '') ?? 0
  return count === 1 ? '1 team' : `${count} teams`
}

async function refreshLists() {
  await Promise.all([
    queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsOrganizersHackathonChallengesListEndpointQueryKey({ path: { hackathonId: hackathonId.value } }) }),
    queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsParticipantsHackathonChallengesListEndpointQueryKey({ path: { hackathonId: hackathonId.value } }) }),
  ])
}

// ---- add / edit ----
const createMutation = useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesCreateEndpoint()
const updateMutation = useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesUpdateEndpoint()
const isSaving = computed(() => createMutation.isPending.value || updateMutation.isPending.value)

const isFormOpen = ref(false)
const editingId = ref<string | null>(null)
const form = reactive({
  title: '',
  sponsor: '',
  description: '',
  isPublished: false,
})
const rule = reactive<ChallengeRule>({ kind: 'any', count: 3, expression: '' })
const errors = reactive<Record<'title' | 'description' | 'sponsor' | 'rule' | 'count' | 'form', string>>({
  title: '',
  description: '',
  sponsor: '',
  rule: '',
  count: '',
  form: '',
})

function clearErrors() {
  for (const key of Object.keys(errors) as (keyof typeof errors)[])
    errors[key] = ''
}

function openCreate() {
  editingId.value = null
  Object.assign(form, { title: '', sponsor: '', description: '', isPublished: false })
  Object.assign(rule, { kind: 'any', count: 3, expression: '' })
  clearErrors()
  isFormOpen.value = true
}

function openEdit(challenge: Challenge) {
  editingId.value = challenge.id ?? null
  Object.assign(form, {
    title: challenge.title ?? '',
    sponsor: challenge.sponsor ?? '',
    description: challenge.description ?? '',
    isPublished: !!challenge.isPublished,
  })
  Object.assign(rule, parseChallengeRule(challenge.criteria))
  clearErrors()
  isFormOpen.value = true
}

const ruleOptions = [
  { kind: 'any', label: 'Any team' },
  { kind: 'minTeamSize', label: 'Teams with at least', unit: 'members' },
  { kind: 'firstTeams', label: 'Only the first', unit: 'teams to submit' },
  { kind: 'custom', label: 'Custom rule' },
] as const

function validate() {
  clearErrors()
  if (!form.title.trim())
    errors.title = 'Give the challenge a title.'
  if (rule.kind === 'minTeamSize' || rule.kind === 'firstTeams') {
    if (!Number.isInteger(Number(rule.count)) || Number(rule.count) < 1)
      errors.count = 'Enter a whole number of 1 or more.'
  }
  if (rule.kind === 'custom')
    errors.rule = checkChallengeRuleSyntax(rule.expression)
  return !errors.title && !errors.count && !errors.rule
}

async function save() {
  if (isSaving.value)
    return
  if (!validate()) {
    nextTick(() => document.querySelector<HTMLElement>('.ch-form [aria-invalid="true"]')?.focus())
    return
  }
  const criteria = buildChallengeRule({ ...rule, count: Number(rule.count) })
  const common = {
    title: form.title.trim(),
    sponsor: form.sponsor.trim(),
    description: form.description.trim(),
    isPublished: form.isPublished,
  }
  try {
    if (editingId.value) {
      await updateMutation.mutateAsync({
        path: { hackathonId: hackathonId.value, challengeId: editingId.value },
        body: { ...common, criteria },
      })
    }
    else {
      // the create request names the rule `selectionCriteriaStmt` (update calls it `criteria`)
      await createMutation.mutateAsync({
        path: { hackathonId: hackathonId.value },
        body: { ...common, selectionCriteriaStmt: criteria },
      })
    }
    await refreshLists()
    isFormOpen.value = false
    toast.add({ tone: 'ok', title: editingId.value ? `Saved ${common.title}` : `Added ${common.title}` })
  }
  catch (error) {
    const bag = getApiValidationErrors(error)
    errors.title = getApiFieldError(bag, 'title') ?? ''
    errors.sponsor = getApiFieldError(bag, 'sponsor') ?? ''
    errors.description = getApiFieldError(bag, 'description') ?? ''
    errors.rule = getApiFieldError(bag, 'criteria') ?? getApiFieldError(bag, 'selectionCriteriaStmt') ?? ''
    if (!errors.title && !errors.sponsor && !errors.description && !errors.rule)
      errors.form = getApiErrorMessage(error, 'The challenge wasn\'t saved. Try again.')
  }
}

// ---- delete ----
const deleteMutation = useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesDeleteEndpoint()
const deleting = ref<Challenge | null>(null)
const deleteError = ref('')
const isDeleteOpen = computed({
  get: () => !!deleting.value,
  set: (value) => {
    if (!value && !deleteMutation.isPending.value)
      deleting.value = null
  },
})

function askDelete(challenge: Challenge) {
  deleteError.value = ''
  deleting.value = challenge
}

async function confirmDelete() {
  const challenge = deleting.value
  if (!challenge?.id)
    return
  deleteError.value = ''
  try {
    await deleteMutation.mutateAsync({ path: { hackathonId: hackathonId.value, challengeId: challenge.id } })
    await refreshLists()
    deleting.value = null
    toast.add({ tone: 'ok', title: `Deleted ${challenge.title || 'the challenge'}` })
  }
  catch (error) {
    deleteError.value = getApiErrorMessage(error, 'It wasn\'t deleted. Try again.')
  }
}
</script>

<template>
  <GPanel id="challenges">
    <template #header>
      <GPanelHeader title="Challenges">
        <template #right>
          <GButton
            variant="primary"
            icon="material-symbols:add-rounded"
            label="Add challenge"
            @click="openCreate"
          />
        </template>
      </GPanelHeader>
    </template>

    <template #body>
      <div class="ch">
        <p
          v-if="challenges.length"
          class="ch-muted"
        >
          {{ challenges.length }} {{ challenges.length === 1 ? 'challenge' : 'challenges' }} · {{ publishedCount }} published.
          Participants only see published ones.
        </p>

        <GAlert
          v-if="loadError && !challenges.length"
          tone="err"
          title="Couldn't load challenges"
          :description="getApiErrorMessage(loadError, 'Refresh the page to try again.')"
        />

        <div
          v-if="isLoading"
          class="ch-list"
          aria-busy="true"
        >
          <GSkeleton
            v-for="i in 3"
            :key="i"
            class="ch-skeleton"
          />
        </div>

        <GEmptyState
          v-else-if="!challenges.length && !loadError"
          icon="material-symbols:trophy-rounded"
          title="No challenges yet"
          description="Teams pick a challenge to work on. Add one, then publish it so participants can see it."
        >
          <template #actions>
            <GButton
              variant="primary"
              icon="material-symbols:add-rounded"
              label="Add challenge"
              @click="openCreate"
            />
          </template>
        </GEmptyState>

        <ul
          v-else
          class="ch-list"
        >
          <li
            v-for="challenge in challenges"
            :key="challenge.id"
            class="ch-card"
          >
            <div class="ch-card__main">
              <p
                v-if="challenge.sponsor"
                class="ch-card__sponsor"
              >
                {{ challenge.sponsor }}
              </p>
              <h2 class="ch-card__title">
                {{ challenge.title || 'Untitled challenge' }}
              </h2>
              <div class="ch-card__meta">
                <GBadge
                  :tone="challenge.isPublished ? 'ok' : 'neutral'"
                  size="sm"
                  :icon="challenge.isPublished ? 'material-symbols:public' : 'material-symbols:draft-rounded'"
                  :label="challenge.isPublished ? 'Published' : 'Draft'"
                />
                <span>{{ teamCountLabel(challenge) }}</span>
                <span aria-hidden="true">·</span>
                <span>{{ describeChallengeRule(challenge.criteria) }}</span>
              </div>
              <p
                v-if="challenge.description"
                class="ch-card__desc"
              >
                {{ challenge.description }}
              </p>
            </div>
            <div class="ch-card__actions">
              <GButton
                variant="secondary"
                size="sm"
                icon="material-symbols:edit-rounded"
                label="Edit"
                :aria-label="`Edit ${challenge.title || 'challenge'}`"
                @click="openEdit(challenge)"
              />
              <GButton
                variant="ghost"
                size="sm"
                icon="material-symbols:delete-rounded"
                :aria-label="`Delete ${challenge.title || 'challenge'}`"
                title="Delete"
                @click="askDelete(challenge)"
              />
            </div>
          </li>
        </ul>
      </div>

      <!-- add / edit -->
      <GModal
        v-model:open="isFormOpen"
        :title="editingId ? 'Edit challenge' : 'Add a challenge'"
        size="lg"
      >
        <form
          id="ch-form"
          class="ch-form"
          novalidate
          @submit.prevent="save"
        >
          <GFormField
            label="Title"
            required
            :error="errors.title"
          >
            <GInput
              v-model="form.title"
              placeholder="e.g. Greener commutes"
              autofocus
            />
          </GFormField>
          <GFormField
            label="Sponsor"
            hint="Shown above the title. Leave empty if there isn't one."
            :error="errors.sponsor"
          >
            <GInput
              v-model="form.sponsor"
              placeholder="e.g. GovTech"
            />
          </GFormField>
          <GFormField
            label="Challenge statement"
            hint="What teams should solve. Participants see it on the Challenges page."
            :error="errors.description"
          >
            <GTextarea
              v-model="form.description"
              :rows="4"
              autoresize
              :max-rows="12"
            />
          </GFormField>

          <fieldset
            class="ch-rule"
            :aria-describedby="errors.count || errors.rule ? 'ch-rule-error' : 'ch-rule-hint'"
          >
            <legend class="ch-rule__legend">
              Who can pick it
            </legend>
            <p
              id="ch-rule-hint"
              class="ch-muted ch-rule__hint"
            >
              Checked when a team submits: teams that don't qualify can't submit to this challenge.
            </p>
            <div
              v-for="option in ruleOptions"
              :key="option.kind"
              class="ch-rule__option"
              :class="{ 'is-on': rule.kind === option.kind }"
            >
              <label class="ch-rule__label">
                <input
                  v-model="rule.kind"
                  type="radio"
                  name="ch-rule"
                  :value="option.kind"
                >
                <span>{{ option.label }}</span>
              </label>
              <template v-if="'unit' in option">
                <GInput
                  v-model.number="rule.count"
                  type="number"
                  size="sm"
                  min="1"
                  inputmode="numeric"
                  class="ch-rule__count"
                  :disabled="rule.kind !== option.kind"
                  :invalid="rule.kind === option.kind && !!errors.count"
                  :aria-label="`Number of ${option.unit.split(' ')[0]}`"
                />
                <span>{{ option.unit }}</span>
              </template>
            </div>

            <div
              v-if="rule.kind === 'custom'"
              class="ch-rule__custom"
            >
              <GTextarea
                v-model="rule.expression"
                :rows="2"
                autoresize
                :max-rows="8"
                class="ch-rule__code"
                aria-label="Custom rule (JavaScript)"
                :invalid="!!errors.rule"
                placeholder="e.g. teamSize >= 3 && currentTeamsInChallenge < 10"
                spellcheck="false"
                autocapitalize="off"
                autocomplete="off"
              />
              <p class="ch-muted ch-rule__vars">
                A JavaScript expression that must be <code>true</code> for a team to submit. You can use:
              </p>
              <ul class="ch-rule__varlist">
                <li
                  v-for="variable in CHALLENGE_RULE_VARIABLES"
                  :key="variable.name"
                >
                  <code>{{ variable.name }}</code> {{ variable.meaning }}
                </li>
              </ul>
            </div>
            <p
              v-if="errors.count || errors.rule"
              id="ch-rule-error"
              class="ch-error"
            >
              {{ errors.count || errors.rule }}
            </p>
          </fieldset>

          <GCheckbox
            v-model="form.isPublished"
            label="Published"
            description="Participants can see it and teams can pick it."
          />

          <GAlert
            v-if="errors.form"
            tone="err"
            title="Not saved"
            :description="errors.form"
          />
        </form>

        <template #footer>
          <GButton
            variant="ghost"
            label="Cancel"
            :disabled="isSaving"
            @click="isFormOpen = false"
          />
          <GButton
            variant="primary"
            type="submit"
            form="ch-form"
            :label="editingId ? 'Save changes' : 'Add challenge'"
            :loading="isSaving"
            loading-label="Saving"
          />
        </template>
      </GModal>

      <!-- delete -->
      <GModal
        v-model:open="isDeleteOpen"
        :title="`Delete ${deleting?.title || 'this challenge'}?`"
        description="This can't be undone. Challenges that already have submissions can't be deleted."
        size="sm"
      >
        <p
          v-if="deleting && (teamCountById.get(deleting.id ?? '') ?? 0) > 0"
          class="ch-delete-note"
        >
          {{ teamCountLabel(deleting) }} picked it.
        </p>
        <GAlert
          v-if="deleteError"
          tone="err"
          title="Not deleted"
          :description="deleteError"
        />
        <template #footer>
          <GButton
            variant="ghost"
            label="Keep it"
            :disabled="deleteMutation.isPending.value"
            @click="isDeleteOpen = false"
          />
          <GButton
            variant="danger"
            icon="material-symbols:delete-rounded"
            label="Delete challenge"
            :loading="deleteMutation.isPending.value"
            loading-label="Deleting"
            @click="confirmDelete"
          />
        </template>
      </GModal>
    </template>
  </GPanel>
</template>

<style scoped>
.ch {
  display: flex;
  flex-direction: column;
  gap: 16px;
  width: 100%;
  max-width: 64rem;
  margin: 0 auto;
}
.ch-muted {
  margin: 0;
  color: var(--gh-text-muted);
}
.ch-error {
  margin: 0;
  font-size: 14px;
  color: var(--gh-err);
}
.ch-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.ch-skeleton {
  display: block;
  height: 120px;
}
.ch-card {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px 24px;
  padding: 20px 24px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.ch-card__main {
  display: flex;
  flex: 1 1 24rem;
  flex-direction: column;
  gap: 6px;
  min-width: 0;
}
.ch-card__sponsor {
  margin: 0;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--gh-text-muted);
}
.ch-card__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 22px;
  font-weight: 700;
  line-height: 1.25;
  overflow-wrap: anywhere;
}
.ch-card__meta {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px 8px;
  font-size: 14px;
  color: var(--gh-text-muted);
}
.ch-card__desc {
  display: -webkit-box;
  margin: 4px 0 0;
  overflow: hidden;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
  line-clamp: 2;
  white-space: pre-line;
}
.ch-card__actions {
  display: flex;
  align-items: center;
  gap: 4px;
}

/* form */
.ch-form {
  display: flex;
  flex-direction: column;
  gap: 20px;
}
.ch-rule {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin: 0;
  padding: 0;
  border: 0;
}
.ch-rule__legend {
  padding: 0;
  font-size: 14px;
  font-weight: 600;
}
.ch-rule__hint { font-size: 14px; }
.ch-rule__option {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  min-height: 48px;
  padding: 6px 12px;
  border: var(--gh-border-width) solid var(--gh-line-soft);
  border-radius: var(--radius-gh);
}
.ch-rule__option.is-on { border-color: var(--gh-line); }
.ch-rule__label {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  cursor: pointer;
}
.ch-rule__label input {
  width: 18px;
  height: 18px;
  margin: 0;
  accent-color: var(--gh-text);
}
.ch-rule__count { width: 5.5rem; }
.ch-rule__custom {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding-left: 12px;
  border-left: 3px solid var(--gh-line-soft);
}
.ch-rule__code :deep(textarea) {
  font-family: var(--font-mono, ui-monospace, monospace);
  font-size: 14px;
}
.ch-rule__vars { font-size: 14px; }
.ch-rule__varlist {
  display: flex;
  flex-direction: column;
  gap: 2px;
  margin: 0;
  padding-left: 18px;
  font-size: 14px;
  color: var(--gh-text-muted);
}
.ch-rule__varlist code,
.ch-rule__vars code {
  font-family: var(--font-mono, ui-monospace, monospace);
  color: var(--gh-text);
}
.ch-delete-note { margin: 0 0 12px; }
</style>
