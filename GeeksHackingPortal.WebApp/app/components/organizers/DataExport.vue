<script setup lang="ts">
import {
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint1,
  useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint2,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonGetEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersHackathonTeamsListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint,
  useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsListEndpoint,
} from '@geekshacking/portal-sdk/hooks'
import type { ReviewState } from '~/utils/participant-review'
import { useClipboard } from '@vueuse/core'

/**
 * Downloads for one event: everyone's registration answers, a contact list and (hackathons)
 * teams. Shared by hackathons and workshops. Spreadsheets are one row per person by default;
 * "one column per person" keeps the layout the old hackathon export used.
 */
const props = defineProps<{
  kind: 'hackathon' | 'workshop'
  eventId: string
}>()

const toast = useGToast()
const { copy, isSupported: canCopy } = useClipboard({ legacy: true })
const isHackathon = props.kind === 'hackathon'
const enabled = { query: { enabled: computed(() => !!props.eventId) } }
const hackathonPath = computed(() => ({ hackathonId: props.eventId }))
const activityPath = computed(() => ({ activityId: props.eventId }))

interface Answer { questionId?: string, questionText?: string, value?: string, followUpValue?: string | null }
interface Person {
  id: string
  name: string
  email: string
  statusKey: string
  status: string
  team: string
  registeredAt: string
  withdrawnAt: string
  answers: Answer[]
  raw: unknown
}

// ---- data for this kind of event ----
const hackathonParticipants = isHackathon ? useGeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListEndpoint({ path: hackathonPath }, enabled) : null
const workshopParticipants = isHackathon ? null : useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsParticipantsListEndpoint({ path: computed(() => ({ standaloneWorkshopId: props.eventId })) }, enabled)
const teamsQuery = isHackathon ? useGeeksHackingPortalApiEndpointsOrganizersHackathonTeamsListEndpoint({ path: hackathonPath }, enabled) : null
const challengesQuery = isHackathon ? useGeeksHackingPortalApiEndpointsOrganizersHackathonChallengesListEndpoint({ path: hackathonPath }, enabled) : null
const hackathonQuery = isHackathon ? useGeeksHackingPortalApiEndpointsOrganizersHackathonGetEndpoint({ path: hackathonPath }, enabled) : null
const workshopsQuery = isHackathon ? null : useGeeksHackingPortalApiEndpointsOrganizersStandaloneWorkshopsListEndpoint()
const questionsQuery = isHackathon
  ? useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint1({ path: activityPath }, enabled)
  : useGeeksHackingPortalApiEndpointsOrganizersActivitiesRegistrationQuestionsListEndpoint2({ path: activityPath }, enabled)

const participantsQuery = (hackathonParticipants ?? workshopParticipants)!
const isLoadingPeople = computed(() => participantsQuery.isLoading.value)
const peopleError = computed(() => participantsQuery.error.value)

const eventTitle = computed(() => isHackathon
  ? (hackathonQuery?.data.value as { name?: string } | undefined)?.name ?? ''
  : workshopsQuery?.data.value?.standaloneWorkshops?.find(w => w.id === props.eventId)?.title ?? '')

const people = computed<Person[]>(() => {
  if (hackathonParticipants) {
    return (hackathonParticipants.data.value?.participants ?? []).map((p) => {
      const statusKey = p.isWithdrawn ? 'withdrawn' : reviewStateOf(p)
      return {
        id: p.id ?? '',
        name: p.name ?? '',
        email: p.email ?? '',
        statusKey,
        status: statusKey === 'withdrawn' ? 'Withdrew' : reviewStateBadge[statusKey as ReviewState].label,
        team: p.teamName ?? '',
        registeredAt: p.createdAt ?? '',
        withdrawnAt: p.withdrawnAt ?? '',
        answers: p.registrationSubmissions ?? [],
        raw: p,
      }
    })
  }
  return (workshopParticipants?.data.value?.participants ?? []).map(p => ({
    id: p.registrationId ?? p.userId ?? '',
    name: p.name ?? '',
    email: p.email ?? '',
    statusKey: (p.status ?? 'Registered').toLowerCase(),
    status: p.status === 'Withdrawn' ? 'Withdrew' : (p.status ?? 'Registered'),
    team: '',
    registeredAt: p.registeredAt ?? '',
    withdrawnAt: p.withdrawnAt ?? '',
    answers: p.registrationSubmissions ?? [],
    raw: p,
  }))
})

// ---- who to include ----
const STATUS_ORDER = isHackathon
  ? [['approved', 'Approved'], ['pending', 'To review'], ['rejected', 'Rejected'], ['incomplete', 'Incomplete'], ['withdrawn', 'Withdrew']]
  : [['registered', 'Registered'], ['withdrawn', 'Withdrew']]
const who = ref('all')
const whoItems = computed(() => [
  { value: 'all', label: `Everyone (${people.value.length})` },
  ...STATUS_ORDER.map(([value, label]) => {
    const count = people.value.filter(p => p.statusKey === value).length
    return { value: value!, label: `${label} (${count})`, disabled: !count }
  }),
])
const whoLabel = computed(() => whoItems.value.find(i => i.value === who.value)?.label.replace(/ \(\d+\)$/, '') ?? 'Everyone')
const included = computed(() => (who.value === 'all' ? people.value : people.value.filter(p => p.statusKey === who.value)))
const peopleWord = (n: number) => `${n} ${n === 1 ? 'person' : 'people'}`

// ---- columns ----
const questionOrder = computed(() => {
  const list = [...(questionsQuery.data.value?.questions ?? [])].sort((a, b) => (a.displayOrder ?? 0) - (b.displayOrder ?? 0))
  return new Map(list.map((q, i) => [q.id ?? '', i]))
})
/** each question once, in form order, with a unique header */
const questionColumns = computed(() => {
  const seen = new Map<string, string>()
  for (const person of people.value) {
    for (const answer of person.answers) {
      const id = answer.questionId ?? answer.questionText ?? ''
      if (id && !seen.has(id))
        seen.set(id, answer.questionText || 'Question')
    }
  }
  const columns = [...seen].map(([id, text]) => ({ id, text }))
  columns.sort((a, b) => (questionOrder.value.get(a.id) ?? Number.MAX_SAFE_INTEGER) - (questionOrder.value.get(b.id) ?? Number.MAX_SAFE_INTEGER))
  const used = new Set<string>()
  return columns.map((c) => {
    let header = c.text
    for (let n = 2; used.has(header); n++)
      header = `${c.text} (${n})`
    used.add(header)
    return { ...c, header }
  })
})

const answerText = (a: Answer | undefined) => {
  if (!a)
    return ''
  const value = a.value?.trim() ?? ''
  const more = a.followUpValue?.trim()
  return more ? `${value}${value ? '\n' : ''}More detail: ${more}` : value
}
const dateCell = (iso: string) => (iso ? formatOrganizerDateTime(iso) : '')

function registrationRows(list: Person[]) {
  return list.map((p) => {
    const row: Record<string, string> = {
      Name: p.name,
      Email: p.email,
      Status: p.status,
    }
    if (isHackathon)
      row.Team = p.team
    row.Registered = dateCell(p.registeredAt)
    row.Withdrew = dateCell(p.withdrawnAt)
    const byId = new Map(p.answers.map(a => [a.questionId ?? a.questionText ?? '', a]))
    for (const column of questionColumns.value)
      row[column.header] = answerText(byId.get(column.id))
    return row
  })
}

const registrationsSummary = computed(() => {
  const scope = who.value === 'all' ? '' : ` (${whoLabel.value})`
  const questions = questionColumns.value.length ? ` to ${questionColumns.value.length} questions` : ''
  return `${peopleWord(included.value.length)}${scope}, with every answer${questions}${isHackathon ? ', plus status and team' : ''}.`
})

const contactSummary = computed(() => {
  const fields = ['Name', 'email', ...(telegramColumn.value ? ['Telegram'] : []), 'status', ...(isHackathon ? ['team'] : [])]
  return `${fields.slice(0, -1).join(', ')} and ${fields.at(-1)} for ${peopleWord(included.value.length)}.`
})

const telegramColumn = computed(() => questionColumns.value.find(c => /telegram/i.test(c.text)))
function contactRows(list: Person[]) {
  return list.map((p) => {
    const row: Record<string, string> = { Name: p.name, Email: p.email, Status: p.status }
    if (telegramColumn.value)
      row.Telegram = answerText(p.answers.find(a => (a.questionId ?? a.questionText) === telegramColumn.value!.id))
    if (isHackathon)
      row.Team = p.team
    return row
  })
}

function summaryRows(list: Person[]) {
  const rows: (string | number)[][] = [
    ['Event', eventTitle.value],
    ['Exported', formatOrganizerDateTime(new Date())],
    ['Included', `${whoLabel.value}: ${peopleWord(list.length)}`],
    [],
    ['Status', 'People'],
  ]
  for (const [key, label] of STATUS_ORDER)
    rows.push([label!, list.filter(p => p.statusKey === key).length])
  return rows
}

// ---- files ----
function fileName(name: string, ext: 'xlsx' | 'json') {
  const slug = (eventTitle.value || props.kind).toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '')
  const scope = who.value === 'all' || name === 'teams' ? '' : `-${who.value}`
  return `${slug}-${name}${scope}-${new Date().toISOString().slice(0, 10)}.${ext}`
}

function saveJson(data: unknown, name: string) {
  const url = URL.createObjectURL(new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' }))
  const link = document.createElement('a')
  link.href = url
  link.download = fileName(name, 'json')
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}

const busy = ref<string | null>(null)
const exportError = ref('')

async function saveWorkbook(name: string, sheets: { name: string, rows?: Record<string, string>[], aoa?: (string | number)[][] }[]) {
  busy.value = name
  exportError.value = ''
  try {
    const XLSX = await import('xlsx')
    const book = XLSX.utils.book_new()
    for (const sheet of sheets) {
      const ws = sheet.aoa ? XLSX.utils.aoa_to_sheet(sheet.aoa) : XLSX.utils.json_to_sheet(sheet.rows ?? [])
      XLSX.utils.book_append_sheet(book, ws, sheet.name)
    }
    XLSX.writeFile(book, fileName(name, 'xlsx'))
    return true
  }
  catch {
    exportError.value = 'The spreadsheet couldn\'t be made. Try again, or download JSON instead.'
    return false
  }
  finally {
    busy.value = null
  }
}

const layout = ref<'rows' | 'columns'>('rows')

/** fields down the side, one column per person */
function transpose(rows: Record<string, string>[]) {
  const fields = Object.keys(rows[0] ?? {})
  return [
    ['Field', ...rows.map((r, i) => r.Name || `Person ${i + 1}`)],
    ...fields.filter(f => f !== 'Name').map(f => [f, ...rows.map(r => r[f] ?? '')]),
  ]
}

async function downloadRegistrations(format: 'xlsx' | 'json') {
  const list = included.value
  if (format === 'json') {
    saveJson(list.map(p => p.raw), 'registrations')
  }
  else {
    const rows = registrationRows(list)
    const ok = await saveWorkbook('registrations', [
      layout.value === 'rows' ? { name: 'Registrations', rows } : { name: 'Registrations', aoa: transpose(rows) },
      { name: 'Summary', aoa: summaryRows(list) },
    ])
    if (!ok)
      return
  }
  markSeen('participants', people.value.length)
}

async function downloadContacts() {
  await saveWorkbook('contacts', [{ name: 'Contacts', rows: contactRows(included.value) }])
}

const emailsToCopy = computed(() => [...new Set(included.value.map(p => p.email.trim()).filter(Boolean))])
async function copyEmails() {
  try {
    await copy(emailsToCopy.value.join(', '))
    toast.add({ tone: 'ok', title: `Copied ${emailsToCopy.value.length} email ${emailsToCopy.value.length === 1 ? 'address' : 'addresses'}`, description: 'Paste them into Bcc so people don\'t see each other\'s addresses.' })
  }
  catch {
    toast.add({ tone: 'err', title: 'Couldn\'t copy', description: 'Download the contact list instead.' })
  }
}

// ---- teams (hackathons) ----
const teams = computed(() => teamsQuery?.data.value?.teams ?? [])
const challengeTitle = computed(() => new Map((challengesQuery?.data.value?.challenges ?? []).map(c => [c.id ?? '', c.title ?? ''])))
function teamRows() {
  return teams.value.map(team => ({
    'Team': team.name ?? '',
    'Challenge': team.challengeId ? (challengeTitle.value.get(team.challengeId) || 'Unknown challenge') : '',
    'Members': String(team.memberCount ?? 0),
    'Member names': people.value.filter(p => p.team && (p.raw as { teamId?: string }).teamId === team.id).map(p => p.name).join(', '),
    'Description': team.description ?? '',
    'Created': dateCell(team.createdAt ?? ''),
  }))
}
async function downloadTeams(format: 'xlsx' | 'json') {
  if (format === 'json')
    saveJson(teams.value, 'teams')
  else if (!(await saveWorkbook('teams', [{ name: 'Teams', rows: teamRows() }])))
    return
  markSeen('teams', teams.value.length)
}

// ---- "new since your last download" (same storage keys as the old export page) ----
const seen = reactive<Record<string, number | null>>({ participants: null, teams: null })
const storageKey = (type: string) => `infopack-count-${props.eventId}-${type}`
function readSeen() {
  for (const type of ['participants', 'teams']) {
    const stored = localStorage.getItem(storageKey(type))
    seen[type] = stored === null ? null : Number.parseInt(stored, 10)
  }
}
function markSeen(type: string, count: number) {
  localStorage.setItem(storageKey(type), String(count))
  seen[type] = count
}
onMounted(readSeen)
watch(() => props.eventId, readSeen)
const newSince = (type: string, count: number) => {
  const last = seen[type]
  return last === null || last === undefined || Number.isNaN(last) || count <= last ? 0 : count - last
}
const eventNoun = isHackathon ? 'hackathon' : 'workshop'
</script>

<template>
  <GPanel :id="isHackathon ? 'infopack' : 'standalone-infopack'">
    <template #header>
      <GPanelHeader title="Data export" />
    </template>

    <template #body>
      <div class="dx">
        <p class="dx-muted">
          Download what people entered when they signed up for this {{ eventNoun }}. The files hold personal details: keep them private and delete them when you're done.
        </p>

        <GAlert
          v-if="peopleError && !people.length"
          tone="err"
          title="Couldn't load registrations"
          :description="getApiErrorMessage(peopleError, 'Refresh the page to try again.')"
        />
        <GAlert
          v-if="exportError"
          tone="err"
          title="Download failed"
          :description="exportError"
          dismissible
          @close="exportError = ''"
        />

        <div class="dx-who">
          <GFormField
            label="Who to include"
            hint="Applies to registrations and the contact list."
          >
            <!-- keyed so the shown label picks up the counts once they load -->
            <GSelect
              :key="people.length"
              v-model="who"
              :items="whoItems"
              :disabled="isLoadingPeople || !people.length"
            />
          </GFormField>
        </div>

        <ul class="dx-list">
          <!-- registrations -->
          <li class="dx-item">
            <div class="dx-item__head">
              <Icon
                name="material-symbols:assignment-ind-rounded"
                class="dx-item__icon"
                aria-hidden="true"
              />
              <div class="dx-item__text">
                <h2 class="dx-item__title">
                  Registrations
                  <GBadge
                    v-if="newSince('participants', people.length)"
                    tone="pending"
                    variant="solid"
                    size="sm"
                    :label="`${newSince('participants', people.length)} new since your last download`"
                  />
                </h2>
                <p class="dx-muted">
                  <template v-if="isLoadingPeople">
                    Loading…
                  </template>
                  <template v-else>
                    {{ registrationsSummary }}
                  </template>
                </p>
              </div>
            </div>
            <fieldset class="dx-layout">
              <legend class="dx-layout__legend">
                Spreadsheet layout
              </legend>
              <label class="dx-radio">
                <input
                  v-model="layout"
                  type="radio"
                  name="dx-layout"
                  value="rows"
                >
                One row per person
              </label>
              <label class="dx-radio">
                <input
                  v-model="layout"
                  type="radio"
                  name="dx-layout"
                  value="columns"
                >
                One column per person
              </label>
            </fieldset>
            <div class="dx-item__actions">
              <GButton
                variant="primary"
                icon="material-symbols:table-view-rounded"
                label="Excel"
                :aria-label="`Download registrations as Excel`"
                :disabled="isLoadingPeople || !included.length || !!busy"
                :loading="busy === 'registrations'"
                loading-label="Making file"
                @click="downloadRegistrations('xlsx')"
              />
              <GButton
                variant="secondary"
                icon="material-symbols:data-object-rounded"
                label="JSON"
                :aria-label="`Download registrations as JSON`"
                :disabled="isLoadingPeople || !included.length"
                @click="downloadRegistrations('json')"
              />
            </div>
          </li>

          <!-- contacts -->
          <li class="dx-item">
            <div class="dx-item__head">
              <Icon
                name="material-symbols:contact-mail-rounded"
                class="dx-item__icon"
                aria-hidden="true"
              />
              <div class="dx-item__text">
                <h2 class="dx-item__title">
                  Contact list
                </h2>
                <p class="dx-muted">
                  {{ contactSummary }}
                </p>
              </div>
            </div>
            <div class="dx-item__actions">
              <GButton
                variant="primary"
                icon="material-symbols:table-view-rounded"
                label="Excel"
                aria-label="Download contact list as Excel"
                :disabled="isLoadingPeople || !included.length || !!busy"
                :loading="busy === 'contacts'"
                loading-label="Making file"
                @click="downloadContacts"
              />
              <GButton
                v-if="canCopy"
                variant="secondary"
                icon="material-symbols:content-copy-rounded"
                :label="`Copy ${emailsToCopy.length} ${emailsToCopy.length === 1 ? 'email' : 'emails'}`"
                :disabled="isLoadingPeople || !emailsToCopy.length"
                @click="copyEmails"
              />
            </div>
          </li>

          <!-- teams -->
          <li
            v-if="isHackathon"
            class="dx-item"
          >
            <div class="dx-item__head">
              <Icon
                name="material-symbols:groups-rounded"
                class="dx-item__icon"
                aria-hidden="true"
              />
              <div class="dx-item__text">
                <h2 class="dx-item__title">
                  Teams
                  <GBadge
                    v-if="newSince('teams', teams.length)"
                    tone="pending"
                    variant="solid"
                    size="sm"
                    :label="`${newSince('teams', teams.length)} new since your last download`"
                  />
                </h2>
                <p class="dx-muted">
                  <template v-if="teamsQuery?.isLoading.value">
                    Loading…
                  </template>
                  <template v-else>
                    {{ teams.length }} {{ teams.length === 1 ? 'team' : 'teams' }}, with challenge and members. Everyone is included.
                  </template>
                </p>
              </div>
            </div>
            <div class="dx-item__actions">
              <GButton
                variant="primary"
                icon="material-symbols:table-view-rounded"
                label="Excel"
                aria-label="Download teams as Excel"
                :disabled="teamsQuery?.isLoading.value || !teams.length || !!busy"
                :loading="busy === 'teams'"
                loading-label="Making file"
                @click="downloadTeams('xlsx')"
              />
              <GButton
                variant="secondary"
                icon="material-symbols:data-object-rounded"
                label="JSON"
                aria-label="Download teams as JSON"
                :disabled="teamsQuery?.isLoading.value || !teams.length"
                @click="downloadTeams('json')"
              />
            </div>
          </li>
        </ul>
      </div>
    </template>
  </GPanel>
</template>

<style scoped>
.dx {
  display: flex;
  flex-direction: column;
  gap: 20px;
  width: 100%;
  max-width: 64rem;
  margin: 0 auto;
}
.dx-muted {
  margin: 0;
  color: var(--gh-text-muted);
}
.dx-who { max-width: 22rem; }
.dx-list {
  display: flex;
  flex-direction: column;
  margin: 0;
  padding: 0;
  list-style: none;
  border: var(--gh-border-width) solid var(--gh-line);
  border-radius: var(--radius-gh-lg);
  background: var(--gh-surface);
}
.dx-item {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 18px 20px;
  border-top: var(--gh-border-width) solid var(--gh-line-soft);
}
.dx-item:first-child { border-top: 0; }
.dx-item__head {
  display: flex;
  align-items: flex-start;
  gap: 14px;
}
.dx-item__icon {
  flex: none;
  width: 28px;
  height: 28px;
}
.dx-item__text {
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 0;
}
.dx-item__title {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px 10px;
  margin: 0;
  font-size: 18px;
  font-weight: 700;
}
.dx-item__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  padding-left: 42px;
}
.dx-layout {
  display: flex;
  flex-wrap: wrap;
  gap: 6px 20px;
  margin: 0;
  padding: 0 0 0 42px;
  border: 0;
}
.dx-layout__legend {
  float: left;
  width: 100%;
  margin-bottom: 4px;
  padding: 0;
  font-size: 14px;
  font-weight: 600;
}
.dx-radio {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  min-height: 32px;
  cursor: pointer;
}
.dx-radio input {
  width: 18px;
  height: 18px;
  margin: 0;
  accent-color: var(--gh-text);
}
.dx-radio input:focus-visible {
  outline: 2px solid var(--gh-focus);
  outline-offset: 2px;
}
@media (max-width: 40rem) {
  .dx-item__actions,
  .dx-layout { padding-left: 0; }
}
</style>
