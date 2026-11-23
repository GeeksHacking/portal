<script setup lang="ts">
/**
 * Which Postmark template to send for each email the portal triggers for this event.
 * `v-model` is `{ [eventKey]: templateAliasOrId }`; rows missing either half are left out.
 *
 * - One row per email: event key + template alias/ID, with Remove.
 * - "Emails this event sends" lists the known keys; "Add" fills in a row for one.
 * - "Template variables" lists the model keys a Postmark template can use.
 */
interface TemplateRow {
  id: number
  key: string
  templateId: string
}

const props = withDefaults(defineProps<{
  modelValue?: Record<string, string>
  eventKind?: 'hackathon' | 'standalone'
}>(), {
  modelValue: () => ({}),
  eventKind: 'standalone',
})

const emit = defineEmits<{
  'update:modelValue': [value: Record<string, string>]
}>()

const knownTemplateKeys = [
  {
    key: 'participant.review.accepted',
    label: 'Participant accepted',
    description: 'Sent when an organiser accepts a participant.',
    scope: 'hackathon',
  },
  {
    key: 'participant.review.rejected',
    label: 'Participant not accepted',
    description: 'Sent when an organiser rejects a participant.',
    scope: 'hackathon',
  },
] as const

const templateVariables = [
  'participant_name',
  'participant_first_name',
  'participant_last_name',
  'participant_email',
  'hackathon_name',
  'hackathon_short_code',
  'hackathon_venue',
  'event_start_date_formatted',
  'event_end_date_formatted',
  'review_status',
  'reason',
  'has_reason',
]

let nextId = 0
const rows = ref<TemplateRow[]>([])

function toValue(list: TemplateRow[]) {
  return Object.fromEntries(
    list
      .map(row => [row.key.trim().toLowerCase(), row.templateId.trim()] as const)
      .filter(([key, templateId]) => key && templateId),
  )
}

// take the parent's value, unless it's just our own rows coming back (keeps half-filled rows)
watch(
  () => props.modelValue,
  (value) => {
    if (JSON.stringify(value ?? {}) === JSON.stringify(toValue(rows.value)))
      return
    rows.value = Object.entries(value ?? {}).map(([key, templateId]) => ({ id: nextId++, key, templateId }))
  },
  { immediate: true },
)

watch(rows, (list) => {
  const value = toValue(list)
  if (JSON.stringify(value) !== JSON.stringify(props.modelValue ?? {}))
    emit('update:modelValue', value)
}, { deep: true })

const availableTemplateKeys = computed(() =>
  knownTemplateKeys.filter(item => item.scope === props.eventKind),
)

function addTemplate(key = '') {
  rows.value = [...rows.value, { id: nextId++, key, templateId: '' }]
}

function removeTemplate(id: number) {
  rows.value = rows.value.filter(row => row.id !== id)
}

function hasKey(key: string) {
  return rows.value.some(row => row.key.trim().toLowerCase() === key)
}
</script>

<template>
  <div class="et">
    <ul
      v-if="rows.length"
      class="et-rows"
    >
      <li
        v-for="(row, index) in rows"
        :key="row.id"
        class="et-row"
      >
        <GFormField
          label="Email"
          :hide-label="index > 0"
          class="et-row__key"
        >
          <GInput
            v-model="row.key"
            placeholder="participant.review.accepted"
            spellcheck="false"
            autocapitalize="off"
          />
        </GFormField>
        <GFormField
          label="Postmark template alias or ID"
          :hide-label="index > 0"
          class="et-row__template"
        >
          <GInput
            v-model="row.templateId"
            placeholder="participant-accepted"
            spellcheck="false"
            autocapitalize="off"
          />
        </GFormField>
        <GButton
          variant="ghost"
          icon="material-symbols:delete-rounded"
          :aria-label="`Remove ${row.key || `email ${index + 1}`}`"
          title="Remove"
          class="et-row__remove"
          @click="removeTemplate(row.id)"
        />
      </li>
    </ul>
    <p
      v-else
      class="et-muted"
    >
      No templates yet. Without one, an email isn't sent.
    </p>

    <GButton
      variant="secondary"
      size="sm"
      icon="material-symbols:add-rounded"
      label="Add template"
      class="self-start"
      @click="addTemplate()"
    />

    <div class="et-help">
      <div class="et-help__col">
        <h3 class="et-help__title">
          Emails this event sends
        </h3>
        <ul
          v-if="availableTemplateKeys.length"
          class="et-keys"
        >
          <li
            v-for="item in availableTemplateKeys"
            :key="item.key"
            class="et-key"
          >
            <div class="et-key__text">
              <p class="et-key__label">
                {{ item.label }}
              </p>
              <p class="et-key__desc">
                {{ item.description }}
              </p>
              <code class="et-code">{{ item.key }}</code>
            </div>
            <GButton
              variant="ghost"
              size="sm"
              :icon="hasKey(item.key) ? 'material-symbols:check-rounded' : 'material-symbols:add-rounded'"
              :label="hasKey(item.key) ? 'Added' : 'Add'"
              :aria-label="hasKey(item.key) ? `${item.label} added` : `Add ${item.label}`"
              :disabled="hasKey(item.key)"
              @click="addTemplate(item.key)"
            />
          </li>
        </ul>
        <p
          v-else
          class="et-muted"
        >
          Workshops don't send any template emails yet.
        </p>
      </div>
      <div class="et-help__col">
        <h3 class="et-help__title">
          Template variables
        </h3>
        <p class="et-muted">
          Use these in the Postmark template, e.g. <code class="et-code">{{ '{' }}{{ '{' }}participant_first_name}}</code>.
        </p>
        <ul class="et-vars">
          <li
            v-for="variable in templateVariables"
            :key="variable"
          >
            <code class="et-code">{{ variable }}</code>
          </li>
        </ul>
      </div>
    </div>
  </div>
</template>

<style scoped>
.et {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.et-muted {
  margin: 0;
  font-size: 14px;
  color: var(--gh-text-muted);
}
.et-rows {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.et-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 8px;
  align-items: end;
}
.et-row__key,
.et-row__template { grid-column: 1; }
.et-row__remove { grid-column: 2; grid-row: 1 / span 2; align-self: center; }
@media (min-width: 640px) {
  .et-row { grid-template-columns: minmax(0, 1fr) minmax(0, 1fr) auto; }
  .et-row__key { grid-column: 1; }
  .et-row__template { grid-column: 2; }
  .et-row__remove { grid-column: 3; grid-row: auto; align-self: end; }
}

.et-help {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 16px;
  margin-top: 4px;
  padding: 16px;
  border: var(--gh-border-width) solid var(--gh-line-soft);
  border-radius: var(--radius-gh-sm);
  background: var(--gh-surface-sunken);
}
.et-help__col {
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-width: 0;
}
.et-help__title {
  margin: 0;
  font-size: 14px;
  font-weight: 700;
}
.et-keys {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.et-key {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: space-between;
  gap: 8px;
  padding: 10px 12px;
  border: var(--gh-border-width) solid var(--gh-line-soft);
  border-radius: var(--radius-gh-sm);
  background: var(--gh-surface);
}
.et-key__text {
  flex: 1 1 15rem;
  min-width: 0;
}
.et-key__label {
  margin: 0;
  font-size: 14px;
  font-weight: 700;
}
.et-key__desc {
  margin: 2px 0 4px;
  font-size: 13px;
  color: var(--gh-text-muted);
}
.et-vars {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin: 0;
  padding: 0;
  list-style: none;
}
.et-code {
  padding: 1px 6px;
  border: var(--gh-border-width) solid var(--gh-line-soft);
  border-radius: 4px;
  background: var(--gh-surface);
  font-family: var(--font-mono, ui-monospace, monospace);
  font-size: 12px;
  overflow-wrap: anywhere;
}
</style>
