<script setup lang="ts">
import type { GeeksHackingPortalApiEndpointsOrganizersHackathonJudgesListJudgeItem } from '@geekshacking/portal-sdk'
import {
  geeksHackingPortalApiEndpointsOrganizersHackathonJudgesListEndpointQueryKey,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonJudgesCreateEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonJudgesListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonJudgesUpdateEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'
import { useClipboard } from '@vueuse/core'

/**
 * Hackathon judges: each has a name, an active flag and a secret key. Secrets are hidden in the
 * list (show or copy one at a time). Adding a judge, or issuing a new secret, ends on a screen
 * that shows the secret to copy.
 */
type Judge = GeeksHackingPortalApiEndpointsOrganizersHackathonJudgesListJudgeItem

const props = withDefaults(defineProps<{
  hackathonId?: string
}>(), {
  hackathonId: '',
})
const route = useRoute()
const hackathonId = computed(() => props.hackathonId || (route.params.hackathonId as string | undefined) || '')

const queryClient = useQueryClient()
const toast = useGToast()
const { copy, isSupported: canCopy } = useClipboard({ legacy: true })

const {
  data: judgesData,
  isLoading,
  error: loadError,
} = useGeeksHackingPortalApiEndpointsOrganizersHackathonJudgesListEndpoint(
  { path: computed(() => ({ hackathonId: hackathonId.value })) },
  { query: { enabled: computed(() => !!hackathonId.value) } },
)
const judges = computed<Judge[]>(() => [...(judgesData.value?.judges ?? [])]
  .sort((a, b) => Number(!!b.active) - Number(!!a.active) || (a.name ?? '').localeCompare(b.name ?? '')))
const activeCount = computed(() => judges.value.filter(j => j.active).length)

// ---- secrets in the list ----
const revealedId = ref<string | null>(null)

function maskSecret(secret: string | undefined) {
  if (!secret)
    return '—'
  return `••••••••${secret.slice(-4)}`
}

async function copySecret(secret: string | undefined, name: string | undefined) {
  if (!secret)
    return
  try {
    await copy(secret)
    toast.add({ tone: 'ok', title: `Copied ${name || 'the judge'}'s secret` })
  }
  catch {
    toast.add({ tone: 'err', title: 'Couldn\'t copy', description: 'Show the secret and copy it by hand.' })
  }
}

// ---- add / edit ----
const createMutation = useGeeksHackingPortalApiEndpointsOrganizersHackathonJudgesCreateEndpoint()
const updateMutation = useGeeksHackingPortalApiEndpointsOrganizersHackathonJudgesUpdateEndpoint()
const isSaving = computed(() => createMutation.isPending.value || updateMutation.isPending.value)

const isFormOpen = ref(false)
const editing = ref<Judge | null>(null)
const form = reactive({ name: '', active: true, regenerateSecret: false })
const nameError = ref('')
const formError = ref('')
/** set after saving when there's a secret to hand over: the dialog then shows it */
const issued = ref<{ name: string, secret: string, isNew: boolean } | null>(null)

const dialogTitle = computed(() => {
  if (issued.value)
    return issued.value.isNew ? `${issued.value.name} is a judge` : `New secret for ${issued.value.name}`
  return editing.value ? `Edit ${editing.value.name || 'judge'}` : 'Add a judge'
})

function openCreate() {
  editing.value = null
  issued.value = null
  Object.assign(form, { name: '', active: true, regenerateSecret: false })
  nameError.value = ''
  formError.value = ''
  isFormOpen.value = true
}

function openEdit(judge: Judge) {
  editing.value = judge
  issued.value = null
  Object.assign(form, { name: judge.name ?? '', active: judge.active ?? true, regenerateSecret: false })
  nameError.value = ''
  formError.value = ''
  isFormOpen.value = true
}

async function save() {
  if (isSaving.value)
    return
  nameError.value = ''
  formError.value = ''
  const name = form.name.trim()
  if (!name) {
    nameError.value = 'Enter the judge\'s name.'
    nextTick(() => document.querySelector<HTMLElement>('#jd-form [aria-invalid="true"]')?.focus())
    return
  }
  try {
    let secret: string | undefined
    if (editing.value?.id) {
      const result = await updateMutation.mutateAsync({
        path: { hackathonId: hackathonId.value, judgeId: editing.value.id },
        body: { name, active: form.active, regenerateSecret: form.regenerateSecret },
      })
      secret = form.regenerateSecret ? (result as { secret?: string } | undefined)?.secret : undefined
    }
    else {
      const result = await createMutation.mutateAsync({ path: { hackathonId: hackathonId.value }, body: { name } })
      secret = (result as { secret?: string } | undefined)?.secret
    }
    await queryClient.invalidateQueries({ queryKey: geeksHackingPortalApiEndpointsOrganizersHackathonJudgesListEndpointQueryKey({ path: { hackathonId: hackathonId.value } }) })
    if (secret) {
      issued.value = { name, secret, isNew: !editing.value }
    }
    else {
      isFormOpen.value = false
      toast.add({ tone: 'ok', title: editing.value ? `Saved ${name}` : `Added ${name}` })
    }
  }
  catch (error) {
    nameError.value = getApiFieldError(getApiValidationErrors(error), 'name') ?? ''
    if (!nameError.value)
      formError.value = getApiErrorMessage(error, 'Nothing was saved. Try again.')
  }
}
</script>

<template>
  <GPanel id="judges">
    <template #header>
      <GPanelHeader title="Judges">
        <template #right>
          <GButton
            variant="primary"
            icon="material-symbols:person-add-rounded"
            label="Add judge"
            @click="openCreate"
          />
        </template>
      </GPanelHeader>
    </template>

    <template #body>
      <div class="jd">
        <p
          v-if="judges.length"
          class="jd-muted"
        >
          {{ judges.length }} {{ judges.length === 1 ? 'judge' : 'judges' }} · {{ activeCount }} active.
          Each judge has a secret key: treat it like a password.
        </p>

        <GAlert
          v-if="loadError && !judges.length"
          tone="err"
          title="Couldn't load judges"
          :description="getApiErrorMessage(loadError, 'Refresh the page to try again.')"
        />

        <div
          v-if="isLoading"
          class="jd-list jd-list--loading"
          aria-busy="true"
        >
          <GSkeleton
            v-for="i in 4"
            :key="i"
            class="jd-skeleton"
          />
        </div>

        <GEmptyState
          v-else-if="!judges.length && !loadError"
          icon="material-symbols:balance-rounded"
          title="No judges yet"
          description="Add the people judging this hackathon. Each one gets a secret key."
        >
          <template #actions>
            <GButton
              variant="primary"
              icon="material-symbols:person-add-rounded"
              label="Add judge"
              @click="openCreate"
            />
          </template>
        </GEmptyState>

        <ul
          v-else
          class="jd-list"
        >
          <li
            v-for="judge in judges"
            :key="judge.id"
            class="jd-row"
          >
            <div class="jd-row__who">
              <p class="jd-row__name">
                {{ judge.name || 'Unnamed judge' }}
              </p>
              <GBadge
                :tone="judge.active ? 'ok' : 'neutral'"
                size="sm"
                :label="judge.active ? 'Active' : 'Inactive'"
              />
            </div>
            <div class="jd-row__secret">
              <code
                class="jd-secret"
                :aria-label="revealedId === judge.id ? 'Secret' : 'Secret, hidden'"
              >{{ revealedId === judge.id ? judge.secret : maskSecret(judge.secret) }}</code>
              <GButton
                variant="ghost"
                size="sm"
                :icon="revealedId === judge.id ? 'material-symbols:visibility-off-rounded' : 'material-symbols:visibility-rounded'"
                :aria-label="revealedId === judge.id ? `Hide ${judge.name}'s secret` : `Show ${judge.name}'s secret`"
                :title="revealedId === judge.id ? 'Hide' : 'Show'"
                :aria-pressed="revealedId === judge.id"
                :disabled="!judge.secret"
                @click="revealedId = revealedId === judge.id ? null : (judge.id ?? null)"
              />
              <GButton
                v-if="canCopy"
                variant="ghost"
                size="sm"
                icon="material-symbols:content-copy-rounded"
                :aria-label="`Copy ${judge.name}'s secret`"
                title="Copy"
                :disabled="!judge.secret"
                @click="copySecret(judge.secret, judge.name)"
              />
            </div>
            <GButton
              variant="secondary"
              size="sm"
              icon="material-symbols:edit-rounded"
              label="Edit"
              :aria-label="`Edit ${judge.name || 'judge'}`"
              @click="openEdit(judge)"
            />
          </li>
        </ul>
      </div>

      <GModal
        v-model:open="isFormOpen"
        :title="dialogTitle"
        :description="issued ? 'Copy the secret now and send it to them privately. You can see it again later on this page.' : undefined"
      >
        <!-- the secret to hand over -->
        <div
          v-if="issued"
          class="jd-issued"
        >
          <code class="jd-issued__secret">{{ issued.secret }}</code>
          <GButton
            v-if="canCopy"
            variant="secondary"
            icon="material-symbols:content-copy-rounded"
            label="Copy secret"
            @click="copySecret(issued.secret, issued.name)"
          />
        </div>

        <form
          v-else
          id="jd-form"
          class="jd-form"
          novalidate
          @submit.prevent="save"
        >
          <GFormField
            label="Name"
            required
            :error="nameError"
          >
            <GInput
              v-model="form.name"
              autocomplete="off"
              autofocus
            />
          </GFormField>
          <template v-if="editing">
            <GCheckbox
              v-model="form.active"
              label="Active"
              description="Turn off for judges who are no longer judging."
            />
            <GCheckbox
              v-model="form.regenerateSecret"
              label="Issue a new secret"
              description="Their current secret stops working. You'll see the new one after saving."
            />
          </template>
          <GAlert
            v-if="formError"
            tone="err"
            title="Not saved"
            :description="formError"
          />
        </form>

        <template #footer>
          <GButton
            v-if="issued"
            variant="primary"
            label="Done"
            @click="isFormOpen = false"
          />
          <template v-else>
            <GButton
              variant="ghost"
              label="Cancel"
              :disabled="isSaving"
              @click="isFormOpen = false"
            />
            <GButton
              variant="primary"
              type="submit"
              form="jd-form"
              :label="editing ? 'Save changes' : 'Add judge'"
              :loading="isSaving"
              loading-label="Saving"
            />
          </template>
        </template>
      </GModal>
    </template>
  </GPanel>
</template>

<style scoped>
.jd {
  display: flex;
  flex-direction: column;
  gap: 16px;
  width: 100%;
  max-width: 64rem;
  margin: 0 auto;
}
.jd-muted {
  margin: 0;
  color: var(--gh-text-muted);
}
.jd-list {
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0;
  list-style: none;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.jd-list--loading {
  gap: 8px;
  padding: 12px;
}
.jd-skeleton {
  display: block;
  height: 48px;
}
.jd-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px 16px;
  padding: 12px 16px;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.jd-row:first-child { border-top: 0; }
.jd-row__who {
  display: flex;
  flex: 1 1 12rem;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px 10px;
  min-width: 0;
}
.jd-row__name {
  margin: 0;
  font-weight: 700;
  overflow-wrap: anywhere;
}
.jd-row__secret {
  display: flex;
  align-items: center;
  gap: 2px;
  min-width: 0;
}
.jd-secret {
  max-width: 16rem;
  margin-right: 4px;
  overflow: hidden;
  font-family: var(--font-mono);
  font-size: 14px;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: var(--gh-text-muted);
}
.jd-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.jd-issued {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 12px;
}
.jd-issued__secret {
  display: block;
  width: 100%;
  padding: 12px;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh);
  background: var(--gh-surface-sunken);
  font-family: var(--font-mono);
  font-size: 15px;
  overflow-wrap: anywhere;
  user-select: all;
}
</style>
