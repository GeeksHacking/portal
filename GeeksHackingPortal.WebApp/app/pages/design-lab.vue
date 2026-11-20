<script setup lang="ts">
import type { Rgba } from '~/utils/color-contrast'
import type { GTableColumn, GTableSort } from '~/utils/g-table'
import { contrastRatio, flatten, parseCssColor, toHex } from '~/utils/color-contrast'

useHead({ title: 'Design lab · GeeksHacking Portal' })

type Theme = 'light' | 'dark'
// `?theme=dark` opens the page in dark mode (handy for sharing with designers)
const route = useRoute()
const theme = ref<Theme>(route.query.theme === 'dark' ? 'dark' : 'light')
const themes: Theme[] = ['light', 'dark']

// ---------- token catalogue ----------
const brandPalette = [
  { name: 'Yellow', token: '--color-gh-yellow', note: 'Primary. Solid fill with dark text; never text on light.' },
  { name: 'Yellow pale', token: '--color-gh-yellow-pale', note: 'Light-mode highlight tile and save bar.' },
  { name: 'Yellow tint', token: '--color-gh-yellow-tint', note: 'In the guide, not used by the new UI.' },
  { name: 'Canvas', token: '--color-gh-canvas', note: 'Sunken surfaces, disabled fields.' },
  { name: 'Ink', token: '--color-gh-ink', note: 'Text, outlines, dark page.' },
  { name: 'Ink 2', token: '--color-gh-ink-2', note: 'Dark cards, offset shadow.' },
  { name: 'Ink 3', token: '--color-gh-ink-3', note: 'Dark dividers.' },
  { name: 'Grey', token: '--color-gh-grey', note: 'Muted text on dark.' },
  { name: 'Grey light', token: '--color-gh-grey-light', note: 'Light dividers.' },
  { name: 'Orange', token: '--color-gh-orange', note: 'Hero gradient only.' },
]

const semanticGroups = [
  { title: 'Surfaces', tokens: ['--gh-bg', '--gh-surface', '--gh-surface-sunken', '--gh-surface-raised', '--gh-highlight'] },
  { title: 'Text', tokens: ['--gh-text', '--gh-text-muted', '--gh-text-subtle', '--gh-text-disabled'] },
  { title: 'Lines and focus', tokens: ['--gh-line', '--gh-line-soft', '--gh-highlight-edge', '--gh-focus'] },
  { title: 'Accent and status', tokens: ['--gh-accent', '--gh-on-accent', '--gh-highlight-ink', '--gh-ok', '--gh-ok-soft', '--gh-on-ok', '--gh-err', '--gh-err-soft', '--gh-on-err'] },
  { title: 'Data viz', tokens: ['--gh-viz-1', '--gh-viz-2', '--gh-viz-3', '--gh-viz-edge', '--gh-viz-track'] },
]

interface Pair { label: string, fg: string, bg: string, under?: string, min: number }
const contrastPairs: Pair[] = [
  { label: 'Text on page', fg: '--gh-text', bg: '--gh-bg', min: 4.5 },
  { label: 'Text on card', fg: '--gh-text', bg: '--gh-surface', min: 4.5 },
  { label: 'Muted text on page', fg: '--gh-text-muted', bg: '--gh-bg', min: 4.5 },
  { label: 'Muted text on card', fg: '--gh-text-muted', bg: '--gh-surface', min: 4.5 },
  { label: 'Placeholder in input', fg: '--gh-text-subtle', bg: '--gh-surface', min: 4.5 },
  { label: 'Text on yellow', fg: '--gh-on-accent', bg: '--gh-accent', min: 4.5 },
  { label: 'Number on highlight tile', fg: '--gh-highlight-ink', bg: '--gh-highlight', min: 4.5 },
  { label: 'Muted text on highlight tile', fg: '--gh-text-muted', bg: '--gh-highlight', min: 4.5 },
  { label: 'Success text on card', fg: '--gh-ok', bg: '--gh-surface', min: 4.5 },
  { label: 'Success badge', fg: '--gh-ok', bg: '--gh-ok-soft', under: '--gh-surface', min: 4.5 },
  { label: 'Error text on card', fg: '--gh-err', bg: '--gh-surface', min: 4.5 },
  { label: 'Error badge', fg: '--gh-err', bg: '--gh-err-soft', under: '--gh-surface', min: 4.5 },
  { label: 'Solid success badge', fg: '--gh-on-ok', bg: '--gh-ok', min: 4.5 },
  { label: 'Solid error badge', fg: '--gh-on-err', bg: '--gh-err', min: 4.5 },
  { label: 'Solid neutral badge', fg: '--gh-bg', bg: '--gh-text', min: 4.5 },
  { label: 'Neutral badge', fg: '--gh-text', bg: '--gh-surface-sunken', min: 4.5 },
  { label: 'Success alert body', fg: '--gh-text-muted', bg: '--gh-ok-soft', under: '--gh-surface', min: 4.5 },
  { label: 'Error alert body', fg: '--gh-text-muted', bg: '--gh-err-soft', under: '--gh-surface', min: 4.5 },
  { label: 'Text in menu / modal', fg: '--gh-text', bg: '--gh-surface-raised', min: 4.5 },
  { label: 'Muted text in menu / modal', fg: '--gh-text-muted', bg: '--gh-surface-raised', min: 4.5 },
  { label: 'Highlighted menu option', fg: '--gh-text', bg: '--gh-option-hover', under: '--gh-surface-raised', min: 4.5 },
  { label: 'Sidebar link on hover', fg: '--gh-text', bg: '--gh-ghost-hover', under: '--gh-surface', min: 4.5 },
  { label: 'Outline vs card (UI)', fg: '--gh-line', bg: '--gh-surface', min: 3 },
  { label: 'Outline vs menu / modal (UI)', fg: '--gh-line', bg: '--gh-surface-raised', min: 3 },
  { label: 'Switch thumb (off) vs track (UI)', fg: '--gh-line', bg: '--gh-surface-sunken', min: 3 },
  { label: 'Outline vs page (UI)', fg: '--gh-line', bg: '--gh-bg', min: 3 },
  { label: 'Focus ring vs page (UI)', fg: '--gh-focus', bg: '--gh-bg', min: 3 },
  { label: 'Highlight edge vs card (UI)', fg: '--gh-highlight-edge', bg: '--gh-surface', min: 3 },
  { label: 'Chart 1 vs card (UI)', fg: '--gh-viz-1', bg: '--gh-surface', min: 3 },
  { label: 'Chart 2 vs card (UI)', fg: '--gh-viz-2', bg: '--gh-surface', min: 3 },
]

const typeScale = [
  { cls: 'gh-display', name: 'Display', spec: 'Libre Franklin 56 / 800', sample: 'Changing the world' },
  { cls: 'gh-h1', name: 'H1', spec: 'Libre Franklin 45 / 700', sample: 'GeeksHacking 2026' },
  { cls: 'gh-h2', name: 'H2', spec: 'Libre Franklin 32 / 700', sample: 'Organiser dashboard' },
  { cls: 'gh-h3', name: 'H3', spec: 'Open Sans 22 / 500', sample: 'Tell us about you' },
  { cls: 'gh-h4', name: 'H4', spec: 'Open Sans 18 / 300', sample: 'Step 2 of 4 · takes about two minutes' },
  { cls: 'gh-body', name: 'Body', spec: 'Open Sans 15 / 400 (guide: 300)', sample: 'We\'ll send your ticket and team updates to this address.' },
  { cls: 'gh-small', name: 'Small', spec: 'Open Sans 13 / 400', sample: 'Updated 2 min ago' },
  { cls: 'gh-label', name: 'Label', spec: 'Open Sans 12 / 600, uppercase', sample: 'Checked in now' },
]

const radii = [
  { name: 'rounded-gh-sm', value: '6px' },
  { name: 'rounded-gh', value: '10px' },
  { name: 'rounded-gh-lg', value: '14px' },
  { name: 'rounded-gh-pill', value: '999px' },
]

const shadows = [
  { name: 'shadow-gh-sm', value: '3px 3px 0', use: 'Active nav item, focused input' },
  { name: 'shadow-gh-md', value: '4px 4px 0', use: 'Buttons, actionable cards' },
  { name: 'shadow-gh-lg', value: '8px 8px 0', use: 'Hero card only' },
]

const motion = [
  { name: '--gh-dur-press', value: '80ms', use: 'Button press-in' },
  { name: '--gh-dur-fast', value: '150ms', use: 'Hover tint, colour changes' },
  { name: '--gh-dur-base', value: '250ms', use: 'Theme switch, panels' },
]

// ---------- live values read from the CSS ----------
const lightPanel = ref<HTMLElement | null>(null)
const darkPanel = ref<HTMLElement | null>(null)
const values = ref<Record<Theme, Record<string, string>>>({ light: {}, dark: {} })

const allTokens = computed(() => {
  const names = new Set<string>(brandPalette.map(c => c.token))
  semanticGroups.forEach(g => g.tokens.forEach(t => names.add(t)))
  contrastPairs.forEach((p) => {
    names.add(p.fg)
    names.add(p.bg)
    if (p.under)
      names.add(p.under)
  })
  return [...names]
})

function readTokens() {
  const panels: Record<Theme, HTMLElement | null> = { light: lightPanel.value, dark: darkPanel.value }
  for (const t of themes) {
    const el = panels[t]
    if (!el)
      continue
    const style = getComputedStyle(el)
    const out: Record<string, string> = {}
    for (const name of allTokens.value)
      out[name] = style.getPropertyValue(name).trim()
    values.value[t] = out
  }
}

onMounted(() => nextTick(readTokens))

function parseHex(value: string | undefined): Rgba | null {
  if (!value)
    return null
  if (value === 'transparent')
    return { r: 0, g: 0, b: 0, a: 0 }
  let hex = value.replace('#', '')
  // the CSS minifier shortens #ffffff to #fff, and rgb(0 0 0 / 0.06) to #0000000f
  if (/^[0-9a-f]{3,4}$/i.test(hex))
    hex = hex.split('').map(c => c + c).join('')
  if (!/^(?:[0-9a-f]{6}|[0-9a-f]{8})$/i.test(hex))
    return parseCssColor(value) // unminified rgb() / rgba()
  return {
    r: Number.parseInt(hex.slice(0, 2), 16),
    g: Number.parseInt(hex.slice(2, 4), 16),
    b: Number.parseInt(hex.slice(4, 6), 16),
    a: hex.length === 8 ? Number.parseInt(hex.slice(6, 8), 16) / 255 : 1,
  }
}

function displayValue(t: Theme, token: string) {
  const raw = values.value[t][token]
  if (!raw)
    return '…'
  const parsed = parseHex(raw)
  if (!parsed)
    return raw
  return parsed.a === 0 ? 'transparent' : toHex(parsed)
}

function ratioFor(t: Theme, pair: Pair) {
  const v = values.value[t]
  const fg = parseHex(v[pair.fg])
  let bg = parseHex(v[pair.bg])
  const under = parseHex(pair.under ? v[pair.under] : undefined)
  if (!fg || !bg)
    return null
  if (bg.a < 1)
    bg = flatten(bg, under ?? { r: 255, g: 255, b: 255, a: 1 })
  return contrastRatio(fg, bg)
}

const failures = computed(() =>
  themes.flatMap(t => contrastPairs
    .map(p => ({ t, p, r: ratioFor(t, p) }))
    .filter(x => x.r === null || x.r < x.p.min)),
)

// ---------- component demos ----------
const buttonVariants = ['primary', 'secondary', 'ghost', 'danger'] as const
const buttonSizes = ['sm', 'md', 'lg'] as const

const saving = ref(false)
function fakeSave() {
  saving.value = true
  setTimeout(() => (saving.value = false), 1600)
}

const demo = reactive({ name: 'Nur Aisyah Tan', email: '', teamCode: 'GH-7F2A', search: '' })
const emailTouched = ref(false)
const emailError = computed(() => {
  if (!emailTouched.value || !demo.email)
    return ''
  return demo.email.includes('@') ? '' : 'That doesn\'t look like an email address yet. Check for a missing @.'
})
const teamCodeError = 'That code doesn\'t match a team yet. Check with your teammate, or skip this and join later.'

// ---------- batch 2 demos ----------
const shirtSizes = ['XS', 'S', 'M', 'L', 'XL', 'XXL']
const challengeItems = [
  { value: 'c1', label: 'Smarter public transport' },
  { value: 'c2', label: 'Healthier hawker centres' },
  { value: 'c3', label: 'Closed this year', disabled: true },
  { value: 'c4', label: 'Greener HDB estates' },
]
const form2 = reactive({
  shirt: undefined as string | undefined,
  challenge: 'c2' as string | undefined,
  dietary: undefined as string | undefined,
  pitch: '',
  notes: 'We met at the last workshop and want to keep building the bus-arrival idea.',
  coc: false,
  newsletter: true,
  photos: false,
})
const pitchTouched = ref(false)
const pitchError = computed(() => (pitchTouched.value && form2.pitch.trim().length < 20 ? 'Tell us a bit more: at least 20 characters.' : ''))
const allPerks = ref<boolean[]>([true, false, true])
const allPerksChecked = computed(() => allPerks.value.every(Boolean))
const somePerksChecked = computed(() => allPerks.value.some(Boolean) && !allPerksChecked.value)
function toggleAllPerks(value: boolean) {
  allPerks.value = allPerks.value.map(() => value)
}
const perkLabels = ['Meals', 'Swag', 'Parking']

const badgeTones = ['neutral', 'ok', 'err', 'pending'] as const
const badgeVariants = ['soft', 'solid', 'outline'] as const
const badgeSamples: Record<(typeof badgeTones)[number], { label: string, icon: string }> = {
  neutral: { label: 'Draft', icon: 'material-symbols:draft-rounded' },
  ok: { label: 'Accepted', icon: 'material-symbols:check-rounded' },
  err: { label: 'Rejected', icon: 'material-symbols:close-rounded' },
  pending: { label: 'Pending review', icon: 'material-symbols:schedule-rounded' },
}

const alertDismissed = ref(false)

const modalOpen = ref(false)
const confirmOpen = ref(false)
const customOpen = ref(false)
const lastModalAction = ref('')
function confirmLeave() {
  confirmOpen.value = false
  lastModalAction.value = 'You left the team (demo only).'
}

// ---------- batch 3 demos ----------
// toasts render from app.vue, outside this page's theme wrapper: mirror the theme onto <html> while here
watch(theme, t => document.documentElement.setAttribute('data-gh-theme', t), { immediate: true })
onBeforeUnmount(() => document.documentElement.removeAttribute('data-gh-theme'))

const gToast = useGToast()
const toastDemos = [
  { label: 'Success', run: () => gToast.add({ title: 'Checked in', description: 'Nur Aisyah Tan · Team Bus Uncles', color: 'success' }) },
  { label: 'Error', run: () => gToast.add({ title: 'Couldn\'t save', description: 'The server didn\'t answer. Your changes are still here.', color: 'error', action: { label: 'Retry', onClick: () => gToast.success('Saved') } }) },
  { label: 'Attention', run: () => gToast.add({ title: 'Check-in closes in 10 minutes', color: 'warning' }) },
  { label: 'Neutral', run: () => gToast.add({ title: 'Link copied', icon: 'material-symbols:content-copy-rounded', color: 'neutral' }) },
  { label: 'Replace by id', run: () => gToast.add({ id: 'sync', title: `Synced ${new Date().toLocaleTimeString()}`, color: 'neutral' }) },
]

const interests = ref<string[]>(['web'])
const interestItems = [
  { value: 'web', label: 'Web' },
  { value: 'ai', label: 'AI / ML' },
  { value: 'hw', label: 'Hardware' },
  { value: 'design', label: 'Design' },
  { value: 'data', label: 'Data' },
]

type ViewMode = 'list' | 'cards'
const viewMode = ref<ViewMode>('list')
const viewOptions = [
  { label: 'List', value: 'list', icon: 'material-symbols:list-rounded' },
  { label: 'Cards', value: 'cards', icon: 'material-symbols:grid-view-rounded' },
]
const sectionTab = ref('overview')
const sectionTabs = [
  { label: 'Overview', value: 'overview' },
  { label: 'Participants', value: 'participants', icon: 'material-symbols:group-rounded' },
  { label: 'Judging', value: 'judging' },
  { label: 'Archive', value: 'archive', disabled: true },
]

const settings = reactive({ registrationOpen: true, waitlist: false, publicGallery: true })

const statsLoading = ref(false)
function reloadStats() {
  statsLoading.value = true
  setTimeout(() => (statsLoading.value = false), 1500)
}

// ---------- batch 4 demos ----------
interface DemoParticipant { id: string, name: string, team: string | null, status: 'Accepted' | 'Pending' | 'Rejected', checkedIn: number | null }
const firstNames = ['Aisyah', 'Wei Jie', 'Priya', 'Marcus', 'Hui Min', 'Arjun', 'Siti', 'Daniel', 'Mei Ling', 'Farhan', 'Chloe', 'Ravi']
const lastNames = ['Tan', 'Lim', 'Ng', 'Rahman', 'Kumar', 'Goh', 'Lee', 'Wong', 'Ismail', 'Chua']
const teamNames = ['Bus Uncles', 'Kopi Kode', 'Null Pointers', 'Hawker Hackers', null]
const statuses = ['Accepted', 'Pending', 'Rejected'] as const
function makeParticipants(count: number): DemoParticipant[] {
  return Array.from({ length: count }, (_, i) => ({
    id: `P-${String(i + 1).padStart(4, '0')}`,
    name: `${firstNames[i % firstNames.length]} ${lastNames[(i * 7) % lastNames.length]}`,
    team: teamNames[(i * 3) % teamNames.length] ?? null,
    status: statuses[(i * 5) % 3]!,
    checkedIn: i % 4 === 0 ? null : 8 + ((i * 13) % 4),
  }))
}
const participants = makeParticipants(12)
const manyParticipants = makeParticipants(2000)
const participantColumns: GTableColumn<DemoParticipant>[] = [
  { accessorKey: 'name', header: 'Participant', sortable: true, meta: { class: { th: 'min-w-[12rem]' } } },
  { accessorKey: 'team', header: 'Team', sortable: true },
  { accessorKey: 'status', header: 'Status', sortable: true },
  { accessorKey: 'checkedIn', header: 'Check-in (am)', sortable: true, align: 'right' },
  { id: 'actions', header: '', align: 'right' },
]
const tableSort = ref<GTableSort | null>({ id: 'name', desc: false })
const statusTone = { Accepted: 'ok', Pending: 'pending', Rejected: 'err' } as const
const tableLoading = ref(false)
function reloadTable() {
  tableLoading.value = true
  setTimeout(() => (tableLoading.value = false), 1500)
}

const iconMap = Object.entries(lucideToMaterial)

const shellDemoGroups = [
  { items: [{ label: 'Explore', icon: 'material-symbols:explore-rounded', to: '/design-lab?shell=explore' }] },
  {
    label: 'HackOMania 2026',
    items: [
      { label: 'Team', icon: 'material-symbols:groups-rounded', to: '/design-lab?shell=team' },
      { label: 'Challenges', icon: 'material-symbols:trophy-rounded', to: '/design-lab?shell=challenges' },
      { label: 'Submission', icon: 'material-symbols:upload-file-rounded', to: '/design-lab?shell=submission' },
    ],
  },
  {
    label: 'Organizer',
    items: [
      { label: 'Workspace', icon: 'material-symbols:dashboard-rounded', to: '/design-lab?shell=workspace' },
      {
        label: 'Judging',
        icon: 'material-symbols:balance-rounded',
        children: [
          { label: 'Judges', to: '/design-lab?shell=judges' },
          { label: 'Analytics', to: '/design-lab?shell=analytics' },
        ],
      },
    ],
  },
]
const shellDemoTitle = computed(() => {
  const key = String(route.query.shell ?? 'team')
  return key.charAt(0).toUpperCase() + key.slice(1)
})

const logoBackgrounds = [
  { on: 'light', color: '#f5f5f5', label: '#5e5d5f' },
  { on: 'dark', color: '#1f1f1f', label: '#a09fa0' },
  { on: 'yellow', color: '#fff200', label: '#1c1b1b' },
] as const
</script>

<template>
  <div
    class="gh-base min-h-screen"
    :data-gh-theme="theme"
  >
    <header class="sticky top-0 z-10 border-b-[1.5px] border-gh-line bg-gh-bg">
      <div class="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-6 py-4">
        <div class="flex items-center gap-3">
          <GLogo
            :wordmark="false"
            size="sm"
          />
          <span class="gh-h4">Design lab</span>
        </div>
        <div
          class="inline-flex overflow-hidden rounded-gh border-[1.5px] border-gh-line bg-gh-surface"
          role="group"
          aria-label="Theme"
        >
          <button
            v-for="t in themes"
            :key="t"
            type="button"
            class="px-4 py-2 font-semibold capitalize"
            :class="theme === t ? 'bg-gh-accent text-gh-on-accent' : 'text-gh-text'"
            :aria-pressed="theme === t"
            @click="theme = t"
          >
            {{ t }}
          </button>
        </div>
      </div>
    </header>

    <main class="mx-auto flex max-w-6xl flex-col gap-16 px-6 pt-10 pb-24">
      <section class="flex flex-col gap-3">
        <h1 class="gh-h1">
          Tokens
        </h1>
        <p class="max-w-[70ch] text-gh-muted">
          The foundation for the redesign: colours, type, shape, shadow and motion. Every value comes from
          <code class="gh-mono">app/assets/css/tokens.css</code>. Components built on them are in section 08; more get added as they are built.
        </p>
        <p
          class="max-w-[70ch] rounded-gh border-[1.5px] p-3 gh-small"
          :class="failures.length ? 'border-gh-err text-gh-err' : 'border-gh-ok text-gh-ok'"
          role="status"
        >
          <template v-if="failures.length">
            {{ failures.length }} contrast check(s) fail or could not be read: {{ failures.map(f => `${f.p.label} (${f.t})`).join(', ') }}
          </template>
          <template v-else>
            All {{ contrastPairs.length * 2 }} contrast checks pass in light and dark.
          </template>
        </p>
      </section>

      <!-- 01 Brand palette -->
      <section aria-labelledby="s-palette">
        <GSectionHeading
          id="s-palette"
          numeral="01"
          label="Palette"
          title="Brand colours"
          class="mb-5"
        />
        <p class="mb-4 max-w-[70ch] text-gh-muted">
          Raw colours from the brand guide. Components never use these directly; they use the semantic tokens below so light and dark mode both work.
        </p>
        <ul class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
          <li
            v-for="c in brandPalette"
            :key="c.token"
            class="overflow-hidden rounded-gh border-[1.5px] border-gh-line bg-gh-surface"
          >
            <div
              class="h-16 border-b-[1.5px] border-gh-line"
              :style="{ background: `var(${c.token})` }"
            />
            <div class="p-3">
              <p class="font-semibold">
                {{ c.name }}
              </p>
              <p class="gh-mono gh-small text-gh-muted">
                {{ displayValue('light', c.token) }}
              </p>
              <p class="gh-small mt-1 text-gh-muted">
                {{ c.note }}
              </p>
            </div>
          </li>
        </ul>
      </section>

      <!-- 02 Semantic tokens -->
      <section aria-labelledby="s-semantic">
        <GSectionHeading
          id="s-semantic"
          numeral="02"
          label="Semantic"
          title="Light and dark tokens"
          class="mb-5"
        />
        <p class="mb-4 max-w-[70ch] text-gh-muted">
          Use these in components. Utilities follow the same names, e.g. <code class="gh-mono">bg-gh-surface</code>, <code class="gh-mono">text-gh-muted</code>, <code class="gh-mono">border-gh-line</code>.
        </p>
        <div class="grid gap-4 md:grid-cols-2">
          <div
            v-for="t in themes"
            :key="t"
            :ref="(el) => { if (t === 'light') lightPanel = el as HTMLElement; else darkPanel = el as HTMLElement }"
            :data-gh-theme="t"
            class="rounded-gh-lg border-[1.5px] border-gh-line bg-gh-bg p-5 text-gh-text"
          >
            <p class="gh-label mb-4">
              {{ t }} theme
            </p>
            <div
              v-for="g in semanticGroups"
              :key="g.title"
              class="mb-5 last:mb-0"
            >
              <p class="mb-2 font-semibold">
                {{ g.title }}
              </p>
              <ul class="flex flex-col gap-2">
                <li
                  v-for="token in g.tokens"
                  :key="token"
                  class="flex items-center gap-3"
                >
                  <span
                    class="size-8 shrink-0 rounded-gh-sm border-[1.5px] border-gh-line"
                    :style="{ background: `var(${token})` }"
                  />
                  <code class="gh-mono gh-small flex-1">{{ token }}</code>
                  <span class="gh-mono gh-small text-gh-muted">{{ displayValue(t, token) }}</span>
                </li>
              </ul>
            </div>
          </div>
        </div>
      </section>

      <!-- 03 Contrast -->
      <section aria-labelledby="s-contrast">
        <GSectionHeading
          id="s-contrast"
          numeral="03"
          label="Checks"
          title="Contrast"
          class="mb-5"
        />
        <p class="mb-4 max-w-[70ch] text-gh-muted">
          Worked out live from the tokens. Text needs 4.5:1; outlines, focus rings and chart marks need 3:1. Yellow buttons rely on their dark outline, not on the yellow itself.
        </p>
        <div class="overflow-x-auto rounded-gh border-[1.5px] border-gh-line bg-gh-surface">
          <table class="w-full text-left gh-small">
            <thead>
              <tr class="border-b-[1.5px] border-gh-line">
                <th
                  scope="col"
                  class="gh-label px-4 py-3"
                >
                  Pair
                </th>
                <th
                  scope="col"
                  class="gh-label px-4 py-3"
                >
                  Needs
                </th>
                <th
                  v-for="t in themes"
                  :key="t"
                  scope="col"
                  class="gh-label px-4 py-3"
                >
                  {{ t }}
                </th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="p in contrastPairs"
                :key="p.label"
                class="border-b border-gh-line-soft last:border-0"
              >
                <td class="px-4 py-2.5">
                  {{ p.label }}
                  <span class="gh-mono block text-gh-muted">{{ p.fg }} on {{ p.bg }}</span>
                </td>
                <td class="px-4 py-2.5">
                  {{ p.min }}:1
                </td>
                <td
                  v-for="t in themes"
                  :key="t"
                  class="px-4 py-2.5"
                >
                  <span
                    v-if="ratioFor(t, p) === null"
                    class="font-semibold text-gh-err"
                  >Could not read</span>
                  <span
                    v-else
                    class="inline-flex items-center gap-2"
                  >
                    <span
                      class="rounded-gh-sm border-[1.5px] px-2 py-0.5 font-semibold"
                      :data-gh-theme="t"
                      :style="{ color: `var(${p.fg})`, background: `var(${p.bg})`, borderColor: 'var(--gh-line)' }"
                    >Aa</span>
                    <span class="gh-mono">{{ ratioFor(t, p)!.toFixed(2) }}</span>
                    <span
                      class="rounded-gh-pill border-[1.5px] px-2 font-semibold"
                      :class="ratioFor(t, p)! >= p.min ? 'border-gh-ok text-gh-ok' : 'border-gh-err text-gh-err'"
                    >{{ ratioFor(t, p)! >= p.min ? 'Pass' : 'Fail' }}</span>
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <!-- 04 Typography -->
      <section aria-labelledby="s-type">
        <GSectionHeading
          id="s-type"
          numeral="04"
          label="Type"
          title="Typography"
          class="mb-5"
        />
        <p class="mb-4 max-w-[70ch] text-gh-muted">
          Libre Franklin stands in for Franklin Gothic (logo, display and H1–H2 only). Open Sans for everything else. All styles keep the guide's +0.5px tracking.
        </p>
        <ul class="flex flex-col divide-y divide-gh-line-soft rounded-gh border-[1.5px] border-gh-line bg-gh-surface">
          <li
            v-for="s in typeScale"
            :key="s.cls"
            class="grid gap-2 p-4 md:grid-cols-[180px_1fr] md:items-baseline"
          >
            <div>
              <p class="font-semibold">
                {{ s.name }}
              </p>
              <p class="gh-small text-gh-muted">
                {{ s.spec }} · <code class="gh-mono">{{ s.cls }}</code>
              </p>
            </div>
            <p
              :class="s.cls"
              class="min-w-0 break-words"
            >
              {{ s.sample }}
            </p>
          </li>
        </ul>
        <div class="mt-4 flex flex-wrap items-center gap-6 rounded-gh border-[1.5px] border-gh-line bg-gh-surface p-4">
          <span class="gh-strip">Label strip</span>
          <span class="gh-numeral">07</span>
          <span class="gh-h3">Highlight <mark class="gh-mark">with code</mark></span>
          <span class="gh-mono">GH-7F2A · 09:14</span>
        </div>
      </section>

      <!-- 05 Shape and shadow -->
      <section aria-labelledby="s-shape">
        <GSectionHeading
          id="s-shape"
          numeral="05"
          label="Shape"
          title="Radius, outline and offset shadow"
          class="mb-5"
        />
        <p class="mb-4 max-w-[70ch] text-gh-muted">
          Outlines are 1.5px. The hard offset shadow comes from the logo and only goes on things you can act on, plus the hero; stat tiles, tables and info cards stay flat.
        </p>
        <div class="grid gap-6 md:grid-cols-2">
          <ul class="grid grid-cols-2 gap-4">
            <li
              v-for="r in radii"
              :key="r.name"
              class="flex h-24 flex-col justify-end border-[1.5px] border-gh-line bg-gh-surface p-3"
              :class="r.name"
            >
              <code class="gh-mono gh-small">{{ r.name }}</code>
              <span class="gh-small text-gh-muted">{{ r.value }}</span>
            </li>
          </ul>
          <ul class="grid grid-cols-3 gap-5">
            <li
              v-for="s in shadows"
              :key="s.name"
              class="flex h-24 flex-col justify-end rounded-gh border-[1.5px] border-gh-line bg-gh-surface p-3"
              :class="s.name"
            >
              <code class="gh-mono gh-small">{{ s.name }}</code>
              <span class="gh-small text-gh-muted">{{ s.use }}</span>
            </li>
          </ul>
        </div>
      </section>

      <!-- 06 Motion -->
      <section aria-labelledby="s-motion">
        <GSectionHeading
          id="s-motion"
          numeral="06"
          label="Motion"
          title="Motion"
          class="mb-5"
        />
        <p class="mb-4 max-w-[70ch] text-gh-muted">
          Movement only means "you can click this". Disabled things never move, never cast a shadow and never animate. All durations drop to 0 when the system asks for reduced motion.
        </p>
        <div class="grid gap-6 md:grid-cols-[1fr_auto] md:items-center">
          <ul class="flex flex-col gap-2">
            <li
              v-for="m in motion"
              :key="m.name"
              class="flex flex-wrap gap-x-4"
            >
              <code class="gh-mono w-36">{{ m.name }}</code>
              <span class="w-16 font-semibold">{{ m.value }}</span>
              <span class="text-gh-muted">{{ m.use }}</span>
            </li>
          </ul>
          <div class="flex flex-wrap gap-5">
            <button
              type="button"
              class="motion-demo rounded-gh border-[1.5px] border-gh-line bg-gh-accent px-5 py-2.5 font-semibold text-gh-on-accent shadow-gh-md"
            >
              Press me
            </button>
            <button
              type="button"
              disabled
              class="motion-demo rounded-gh border-[1.5px] px-5 py-2.5 font-semibold"
            >
              Disabled
            </button>
          </div>
        </div>
      </section>

      <!-- 07 Data viz and hero -->
      <section aria-labelledby="s-viz">
        <GSectionHeading
          id="s-viz"
          numeral="07"
          label="Charts"
          title="Data viz and hero"
          class="mb-5"
        />
        <p class="mb-4 max-w-[70ch] text-gh-muted">
          Charts use a three-step neutral ramp. Green and red only when the data is a status. Every segment and legend swatch gets a dark edge so pale shades still read against the card.
        </p>
        <div class="grid gap-4 md:grid-cols-2">
          <div class="flex flex-col gap-3 rounded-gh border-[1.5px] border-gh-line bg-gh-surface p-4">
            <p class="font-semibold">
              Neutral ramp: Registration pipeline
            </p>
            <div
              class="viz-stack"
              role="img"
              aria-label="231 complete, 17 incomplete, 14 withdrawn"
            >
              <i style="width: 88%; background: var(--gh-viz-1)" />
              <i style="width: 7%; background: var(--gh-viz-2)" />
              <i style="width: 5%; background: var(--gh-viz-3)" />
            </div>
            <p class="viz-legend gh-small">
              <span style="--c: var(--gh-viz-1)">Complete <b>231</b></span>
              <span style="--c: var(--gh-viz-2)">Incomplete <b>17</b></span>
              <span style="--c: var(--gh-viz-3)">Withdrawn <b>14</b></span>
            </p>
            <p class="font-semibold">
              Status: Review queue
            </p>
            <div
              class="viz-stack"
              role="img"
              aria-label="176 accepted, 37 pending, 18 rejected"
            >
              <i style="width: 76%; background: var(--gh-ok)" />
              <i style="width: 16%; background: var(--gh-accent)" />
              <i style="width: 8%; background: var(--gh-err)" />
            </div>
            <p class="viz-legend gh-small">
              <span style="--c: var(--gh-ok)">Accepted <b>176</b></span>
              <span style="--c: var(--gh-accent)">Pending <b>37</b></span>
              <span style="--c: var(--gh-err)">Rejected <b>18</b></span>
            </p>
          </div>
          <div
            class="flex flex-col justify-center rounded-gh-lg border-[1.5px] border-gh-line p-6 shadow-gh-lg"
            style="background: var(--gh-hero); color: var(--gh-hero-text)"
          >
            <!-- text stays inside the first ~60%, where the dark gradient is solid black -->
            <span class="gh-strip mb-3 self-start">Registration open</span>
            <p class="gh-h2 max-w-[60%]">
              Hero surface
            </p>
            <p class="gh-small mt-1 max-w-[60%]">
              <code class="gh-mono">--gh-hero</code>: solid yellow in light. In dark, black under the text with the orange → yellow run on the far right only.
            </p>
          </div>
        </div>
      </section>

      <!-- 08 Components -->
      <section aria-labelledby="s-components">
        <GSectionHeading
          id="s-components"
          numeral="08"
          label="Components"
          title="The G* kit"
          class="mb-5"
        />
        <p class="mb-6 max-w-[70ch] text-gh-muted">
          Everything in <code class="gh-mono">app/components/g/</code>: button, input, textarea, select (single and multiple), checkbox, switch, form field, tabs, badge, alert, toast, card, stat tile, empty state, skeleton, modal, table, label strip and section heading. Hover, press, tab to them and type: these are the real components.
        </p>

        <div class="flex flex-col gap-6">
          <!-- Buttons -->
          <div class="rounded-gh border-[1.5px] border-gh-line bg-gh-surface p-5">
            <h3 class="gh-h3 mb-1">
              Button
            </h3>
            <p class="gh-small mb-5 text-gh-muted">
              One primary per group. Ghost never moves. Loading keeps its look but stops moving and ignores clicks. Disabled is flat and grey: no movement, no shadow.
            </p>
            <div class="overflow-x-auto">
              <table class="w-full border-separate border-spacing-x-0 border-spacing-y-4 text-left">
                <thead>
                  <tr>
                    <th
                      scope="col"
                      class="gh-label w-28 text-gh-muted"
                    >
                      Variant
                    </th>
                    <th
                      scope="col"
                      class="gh-label text-gh-muted"
                    >
                      Default
                    </th>
                    <th
                      scope="col"
                      class="gh-label text-gh-muted"
                    >
                      With icon
                    </th>
                    <th
                      scope="col"
                      class="gh-label text-gh-muted"
                    >
                      Icon only
                    </th>
                    <th
                      scope="col"
                      class="gh-label text-gh-muted"
                    >
                      Loading
                    </th>
                    <th
                      scope="col"
                      class="gh-label text-gh-muted"
                    >
                      Disabled
                    </th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="v in buttonVariants"
                    :key="v"
                  >
                    <th
                      scope="row"
                      class="font-semibold capitalize"
                    >
                      {{ v }}
                    </th>
                    <td class="pr-4">
                      <GButton
                        :variant="v"
                        label="Continue"
                      />
                    </td>
                    <td class="pr-4">
                      <GButton
                        :variant="v"
                        icon="material-symbols:check-rounded"
                        label="Check in"
                      />
                    </td>
                    <td class="pr-4">
                      <GButton
                        :variant="v"
                        icon="material-symbols:history-rounded"
                        aria-label="View history"
                      />
                    </td>
                    <td class="pr-4">
                      <GButton
                        :variant="v"
                        label="Saving"
                        loading
                      />
                    </td>
                    <td class="pr-4">
                      <GButton
                        :variant="v"
                        label="Continue"
                        disabled
                      />
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
            <div class="mt-2 flex flex-wrap items-center gap-4 border-t border-gh-line-soft pt-5">
              <GButton
                v-for="s in buttonSizes"
                :key="s"
                variant="primary"
                :size="s"
                :label="`Size ${s}`"
              />
              <GButton
                variant="secondary"
                label="Link to Explore"
                trailing-icon="material-symbols:arrow-forward-rounded"
                to="/dash"
              />
              <GButton
                variant="primary"
                icon="material-symbols:save-rounded"
                :loading="saving"
                :label="saving ? 'Saving' : 'Try loading'"
                @click="fakeSave"
              />
            </div>
            <div class="mt-5 max-w-sm">
              <GButton
                variant="primary"
                size="lg"
                icon="material-symbols:check-rounded"
                label="Block button (full width)"
                block
              />
            </div>
          </div>

          <!-- Inputs and fields -->
          <div class="grid gap-6 lg:grid-cols-2">
            <form
              class="flex flex-col gap-4 rounded-gh border-[1.5px] border-gh-line bg-gh-surface p-5 shadow-gh-md"
              novalidate
              @submit.prevent
            >
              <div>
                <h3 class="gh-h3">
                  Form field
                </h3>
                <p class="gh-small text-gh-muted">
                  A live form: type in the email field, then leave it.
                </p>
              </div>
              <GFormField
                label="Full name"
                required
              >
                <GInput
                  v-model="demo.name"
                  autocomplete="name"
                />
              </GFormField>
              <GFormField
                label="Email"
                hint="We'll send your ticket and team updates here."
                :error="emailError"
                required
              >
                <GInput
                  v-model="demo.email"
                  type="email"
                  placeholder="e.g. you@example.com"
                  autocomplete="email"
                  @blur="emailTouched = true"
                />
              </GFormField>
              <GFormField
                label="Team code"
                :error="teamCodeError"
              >
                <GInput
                  v-model="demo.teamCode"
                  icon="material-symbols:group-rounded"
                />
              </GFormField>
              <GFormField
                label="T-shirt size"
                hint="Opens after you're accepted."
                disabled
              >
                <GInput placeholder="Not available yet" />
              </GFormField>
              <div class="flex items-center justify-between gap-3 pt-2">
                <GButton
                  variant="ghost"
                  label="Back"
                />
                <GButton
                  variant="primary"
                  type="submit"
                  label="Continue"
                />
              </div>
            </form>

            <div class="flex flex-col gap-4 rounded-gh border-[1.5px] border-gh-line bg-gh-surface p-5">
              <div>
                <h3 class="gh-h3">
                  Input states
                </h3>
                <p class="gh-small text-gh-muted">
                  Each one is labelled for screen readers, even where the label is hidden.
                </p>
              </div>
              <GFormField
                label="Default"
                hint="Placeholder text uses the subtle text token."
              >
                <GInput placeholder="Type something" />
              </GFormField>
              <GFormField
                label="Search participants"
                hide-label
              >
                <GInput
                  v-model="demo.search"
                  type="search"
                  icon="material-symbols:search-rounded"
                  placeholder="Search by name (label hidden)"
                />
              </GFormField>
              <GFormField label="Read-only">
                <GInput
                  model-value="GH-7F2A"
                  readonly
                />
              </GFormField>
              <GFormField
                label="With a trailing button"
              >
                <GInput
                  model-value="https://portal.geekshacking.com/gh26"
                  readonly
                >
                  <template #trailing>
                    <GButton
                      variant="ghost"
                      size="sm"
                      icon="material-symbols:content-copy-rounded"
                      aria-label="Copy link"
                    />
                  </template>
                </GInput>
              </GFormField>
              <div class="grid grid-cols-3 gap-3">
                <GFormField label="Small">
                  <GInput
                    size="sm"
                    model-value="sm"
                  />
                </GFormField>
                <GFormField label="Medium">
                  <GInput model-value="md" />
                </GFormField>
                <GFormField label="Large">
                  <GInput
                    size="lg"
                    model-value="lg"
                  />
                </GFormField>
              </div>
            </div>
          </div>

          <!-- Textarea, select, checkbox -->
          <div class="grid gap-6 lg:grid-cols-2">
            <form
              class="flex flex-col gap-4 rounded-gh border-[1.5px] border-gh-line bg-gh-surface p-5"
              novalidate
              @submit.prevent
            >
              <div>
                <h3 class="gh-h3">
                  Select and textarea
                </h3>
                <p class="gh-small text-gh-muted">
                  Open a select with Space or the arrow keys, type to jump, Esc to close.
                </p>
              </div>
              <GFormField
                label="T-shirt size"
                hint="Unisex fit."
                required
              >
                <GSelect
                  v-model="form2.shirt"
                  :items="shirtSizes"
                  placeholder="Choose a size"
                />
              </GFormField>
              <GFormField label="Challenge statement">
                <GSelect
                  v-model="form2.challenge"
                  :items="challengeItems"
                  icon="material-symbols:flag-rounded"
                />
              </GFormField>
              <GFormField
                label="Dietary needs"
                error="Pick one so the caterer can plan."
              >
                <GSelect
                  v-model="form2.dietary"
                  :items="['None', 'Vegetarian', 'Halal', 'Vegan']"
                  placeholder="Select one"
                />
              </GFormField>
              <GFormField
                label="Track"
                hint="Assigned by organisers."
                disabled
              >
                <GSelect
                  :items="['Beginner', 'Open']"
                  placeholder="Not assigned yet"
                />
              </GFormField>
              <GFormField
                label="Your pitch"
                hint="One or two sentences. Grows as you type."
                :error="pitchError"
                required
              >
                <GTextarea
                  v-model="form2.pitch"
                  :rows="2"
                  :max-rows="8"
                  autoresize
                  placeholder="We're building…"
                  @blur="pitchTouched = true"
                />
              </GFormField>
              <GFormField label="Notes for organisers">
                <GTextarea v-model="form2.notes" />
              </GFormField>
              <GFormField
                label="Judge feedback"
                disabled
              >
                <GTextarea placeholder="Shown after judging" />
              </GFormField>
            </form>

            <div class="flex flex-col gap-4 rounded-gh border-[1.5px] border-gh-line bg-gh-surface p-5">
              <div>
                <h3 class="gh-h3">
                  Checkbox
                </h3>
                <p class="gh-small text-gh-muted">
                  Checked is a solid yellow box with a dark tick. Space toggles.
                </p>
              </div>
              <GCheckbox
                v-model="form2.coc"
                label="I agree to the Code of Conduct"
                description="You can't register without this."
                required
                :invalid="!form2.coc"
              />
              <GCheckbox
                v-model="form2.newsletter"
                label="Email me about future events"
              />
              <GCheckbox
                v-model="form2.photos"
                label="I'm happy to appear in event photos"
                description="Opt out any time from your profile."
              />
              <fieldset class="flex flex-col gap-3 border-t border-gh-line-soft pt-4">
                <legend class="gh-label mb-3 text-gh-muted">
                  Indeterminate
                </legend>
                <GCheckbox
                  :model-value="allPerksChecked"
                  :indeterminate="somePerksChecked"
                  label="All perks"
                  @update:model-value="toggleAllPerks"
                />
                <div class="flex flex-col gap-3 pl-8">
                  <GCheckbox
                    v-for="(perk, i) in perkLabels"
                    :key="perk"
                    v-model="allPerks[i]"
                    :label="perk"
                  />
                </div>
              </fieldset>
              <div class="flex flex-col gap-3 border-t border-gh-line-soft pt-4">
                <span class="gh-label text-gh-muted">Disabled</span>
                <GCheckbox
                  label="Locked off"
                  disabled
                />
                <GCheckbox
                  :model-value="true"
                  label="Locked on"
                  disabled
                />
              </div>
            </div>
          </div>

          <!-- Badges -->
          <div class="rounded-gh border-[1.5px] border-gh-line bg-gh-surface p-5">
            <h3 class="gh-h3 mb-1">
              Badge
            </h3>
            <p class="gh-small mb-5 text-gh-muted">
              Status is green or red. Pending is always solid yellow with dark text. Soft badges are tinted in light and outlined in dark.
            </p>
            <div class="overflow-x-auto">
              <table class="w-full border-separate border-spacing-x-0 border-spacing-y-3 text-left">
                <thead>
                  <tr>
                    <th
                      scope="col"
                      class="gh-label w-28 text-gh-muted"
                    >
                      Tone
                    </th>
                    <th
                      v-for="v in badgeVariants"
                      :key="v"
                      scope="col"
                      class="gh-label capitalize text-gh-muted"
                    >
                      {{ v }}
                    </th>
                    <th
                      scope="col"
                      class="gh-label text-gh-muted"
                    >
                      Small
                    </th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="t in badgeTones"
                    :key="t"
                  >
                    <th
                      scope="row"
                      class="font-semibold capitalize"
                    >
                      {{ t }}
                    </th>
                    <td
                      v-for="v in badgeVariants"
                      :key="v"
                      class="pr-4"
                    >
                      <GBadge
                        :tone="t"
                        :variant="v"
                        :icon="badgeSamples[t].icon"
                        :label="badgeSamples[t].label"
                      />
                    </td>
                    <td class="pr-4">
                      <GBadge
                        :tone="t"
                        size="sm"
                        :label="badgeSamples[t].label"
                      />
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- Alerts -->
          <div class="flex flex-col gap-4 rounded-gh border-[1.5px] border-gh-line bg-gh-surface p-5">
            <div>
              <h3 class="gh-h3">
                Alert
              </h3>
              <p class="gh-small text-gh-muted">
                Only the icon and edge carry the tone. Body text stays readable. Errors are announced.
              </p>
            </div>
            <GAlert
              title="Registration closes Friday 23:59"
              description="You can still edit your answers until then."
            />
            <GAlert
              tone="ok"
              title="You're checked in"
              description="Grab your lanyard from the front desk."
            />
            <GAlert
              tone="err"
              title="We couldn't save your submission"
              description="The repository link isn't public. Make it public, then try again."
            >
              <template #actions>
                <GButton
                  size="sm"
                  variant="secondary"
                  icon="material-symbols:refresh-rounded"
                  label="Try again"
                />
              </template>
            </GAlert>
            <GAlert
              v-if="!alertDismissed"
              tone="attention"
              title="Your team needs one more member"
              description="Teams of 2–5 can submit. Invite someone with your team code."
              dismissible
              @close="alertDismissed = true"
            />
            <GButton
              v-else
              variant="ghost"
              size="sm"
              label="Bring the dismissed alert back"
              class="self-start"
              @click="alertDismissed = false"
            />
          </div>

          <!-- Cards -->
          <div class="flex flex-col gap-4">
            <div>
              <h3 class="gh-h3">
                Card
              </h3>
              <p class="gh-small text-gh-muted">
                Plain cards stay still. Action cards are one big link or button: they lift on hover and press in. Disabled ones don't move.
              </p>
            </div>
            <div class="grid gap-4 md:grid-cols-3">
              <GCard>
                <template #header>
                  <span class="gh-label text-gh-muted">Plain</span>
                </template>
                <p class="gh-h3">
                  128
                </p>
                <p class="gh-small text-gh-muted">
                  Registrations
                </p>
                <template #footer>
                  <span class="gh-small text-gh-muted">Updated 2 min ago</span>
                </template>
              </GCard>
              <GCard highlight>
                <span class="gh-label">Highlight</span>
                <p class="gh-h3 text-gh-highlight-ink">
                  42
                </p>
                <p class="gh-small text-gh-muted">
                  Waiting for review: one per view at most
                </p>
              </GCard>
              <GCard
                variant="action"
                to="/dash"
              >
                <span class="gh-label text-gh-muted">Action · link</span>
                <p class="gh-h3">
                  GeeksHacking 2026
                </p>
                <p class="gh-small text-gh-muted">
                  12–14 Jun · Open to all
                </p>
              </GCard>
              <GCard
                variant="action"
                @click="lastModalAction = 'Action card clicked.'"
              >
                <span class="gh-label text-gh-muted">Action · button</span>
                <p class="gh-h3">
                  Intro to Rust
                </p>
                <p class="gh-small text-gh-muted">
                  Workshop · 20 seats left
                </p>
              </GCard>
              <GCard
                variant="action"
                to="/dash"
                disabled
              >
                <span class="gh-label">Action · disabled</span>
                <p class="gh-h3">
                  Past event
                </p>
                <p class="gh-small">
                  Registration closed
                </p>
              </GCard>
            </div>
          </div>

          <!-- Modals -->
          <div class="flex flex-col gap-4 rounded-gh border-[1.5px] border-gh-line bg-gh-surface p-5">
            <div>
              <h3 class="gh-h3">
                Modal
              </h3>
              <p class="gh-small text-gh-muted">
                Focus is trapped inside and returns to the button that opened it. Esc and clicking outside close it, unless it must be answered.
              </p>
            </div>
            <div class="flex flex-wrap gap-3">
              <GButton
                label="Edit team details"
                icon="material-symbols:edit-rounded"
                @click="modalOpen = true"
              />
              <GButton
                variant="danger"
                label="Leave team"
                icon="material-symbols:logout-rounded"
                @click="confirmOpen = true"
              />
              <GButton
                variant="ghost"
                label="Custom content"
                @click="customOpen = true"
              />
            </div>
            <p
              v-if="lastModalAction"
              class="gh-small text-gh-muted"
              role="status"
            >
              {{ lastModalAction }}
            </p>

            <GModal
              v-model:open="modalOpen"
              title="Edit team details"
              description="Everyone on your team sees these."
            >
              <div class="flex flex-col gap-4">
                <GFormField
                  label="Team name"
                  required
                >
                  <GInput model-value="Bus Uncles" />
                </GFormField>
                <GFormField label="What are you building?">
                  <GTextarea model-value="A bus-arrival app for people who can't read small screens." />
                </GFormField>
              </div>
              <template #footer="{ close }">
                <GButton
                  variant="ghost"
                  label="Cancel"
                  @click="close"
                />
                <GButton
                  variant="primary"
                  label="Save"
                  @click="close(); lastModalAction = 'Team details saved (demo only).'"
                />
              </template>
            </GModal>

            <GModal
              v-model:open="confirmOpen"
              size="sm"
              title="Leave Bus Uncles?"
              description="You'll lose access to the team's submission. Your teammates can invite you back."
              :dismissible="false"
            >
              <template #footer>
                <GButton
                  variant="ghost"
                  label="Stay"
                  @click="confirmOpen = false"
                />
                <GButton
                  variant="danger"
                  label="Leave team"
                  @click="confirmLeave"
                />
              </template>
            </GModal>

            <GModal
              v-model:open="customOpen"
              title="Your check-in QR code"
            >
              <template #content="{ close }">
                <div class="flex flex-col items-center gap-4 p-8 text-center">
                  <span class="gh-strip">Check-in</span>
                  <div
                    class="grid size-40 place-items-center rounded-gh border-[1.5px] border-gh-line bg-gh-sunken"
                    aria-hidden="true"
                  >
                    <Icon
                      name="material-symbols:qr-code-2-rounded"
                      class="size-24"
                    />
                  </div>
                  <p class="gh-small text-gh-muted">
                    The content slot replaces the whole inside. The title is still read out to screen readers.
                  </p>
                  <GButton
                    variant="primary"
                    label="Done"
                    @click="close"
                  />
                </div>
              </template>
            </GModal>
          </div>

          <!-- Batch 3: multi-select, switch, tabs -->
          <div class="grid gap-6 lg:grid-cols-2">
            <div class="flex flex-col gap-4 rounded-gh border-[1.5px] border-gh-line bg-gh-surface p-5">
              <div>
                <h3 class="gh-h3">
                  Multiple select and switch
                </h3>
                <p class="gh-small text-gh-muted">
                  The menu stays open while you tick options. Switches apply straight away; checkboxes are for forms you submit.
                </p>
              </div>
              <GFormField
                label="Interests"
                hint="Pick as many as you like."
              >
                <GSelect
                  v-model="interests"
                  :items="interestItems"
                  multiple
                  placeholder="Choose interests"
                />
              </GFormField>
              <p class="gh-small text-gh-muted">
                Value: <code class="gh-mono">{{ JSON.stringify(interests) }}</code>
              </p>
              <div class="flex flex-col gap-4 border-t border-gh-line-soft pt-4">
                <GSwitch
                  v-model="settings.registrationOpen"
                  label="Registration open"
                  description="Participants can sign up from the event page."
                />
                <GSwitch
                  v-model="settings.waitlist"
                  label="Waitlist when full"
                />
                <GSwitch
                  v-model="settings.publicGallery"
                  size="sm"
                  label="Small: public project gallery"
                />
                <GSwitch
                  label="Disabled off"
                  disabled
                />
                <GSwitch
                  :model-value="true"
                  label="Disabled on"
                  disabled
                />
              </div>
            </div>

            <div class="flex flex-col gap-5 rounded-gh border-[1.5px] border-gh-line bg-gh-surface p-5">
              <div>
                <h3 class="gh-h3">
                  Tabs
                </h3>
                <p class="gh-small text-gh-muted">
                  Arrow keys move between tabs. Segmented for view toggles, underline for sections.
                </p>
              </div>
              <div class="flex flex-wrap items-center gap-4">
                <GTabs
                  v-model="viewMode"
                  :items="viewOptions"
                  :content="false"
                  size="sm"
                  aria-label="View"
                />
                <span class="gh-small text-gh-muted">Showing: {{ viewMode }}</span>
              </div>
              <GTabs
                v-model="sectionTab"
                :items="sectionTabs"
                variant="underline"
                aria-label="Event sections"
              >
                <template #overview>
                  <p class="gh-small text-gh-muted">
                    Overview panel. Tab into it from the active tab.
                  </p>
                </template>
                <template #participants>
                  <p class="gh-small text-gh-muted">
                    128 participants, 42 waiting for review.
                  </p>
                </template>
                <template #judging>
                  <p class="gh-small text-gh-muted">
                    Judging opens on Sunday.
                  </p>
                </template>
              </GTabs>
              <GTabs
                :items="viewOptions"
                :content="false"
                block
                model-value="cards"
                aria-label="Block view toggle"
              />
            </div>
          </div>

          <!-- Toasts -->
          <div class="flex flex-col gap-4 rounded-gh border-[1.5px] border-gh-line bg-gh-surface p-5">
            <div>
              <h3 class="gh-h3">
                Toast
              </h3>
              <p class="gh-small text-gh-muted">
                <code class="gh-mono">useGToast().add({ title, description, color })</code>, the same shape as Nuxt UI's. Hover to pause, swipe right or press the × to dismiss, F8 to jump to them. Errors stay up longer.
              </p>
            </div>
            <div class="flex flex-wrap gap-3">
              <GButton
                v-for="d in toastDemos"
                :key="d.label"
                :label="d.label"
                @click="d.run"
              />
            </div>
          </div>

          <!-- Stat tiles -->
          <div class="flex flex-col gap-4">
            <div class="flex flex-wrap items-end justify-between gap-3">
              <div>
                <h3 class="gh-h3">
                  Stat tile
                </h3>
                <p class="gh-small text-gh-muted">
                  One highlighted number per view at most. Changes stay grey: more isn't always good.
                </p>
              </div>
              <GButton
                size="sm"
                icon="material-symbols:refresh-rounded"
                label="Show loading"
                :loading="statsLoading"
                @click="reloadStats"
              />
            </div>
            <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <GStatTile
                label="Registrations"
                :value="1284"
                change="+36 since yesterday"
                trend="up"
                icon="material-symbols:person-add-rounded"
                :loading="statsLoading"
              />
              <GStatTile
                label="Waiting for review"
                :value="42"
                hint="Oldest is 3 days old"
                highlight
                :loading="statsLoading"
              />
              <GStatTile
                label="Checked in"
                :value="87"
                unit="%"
                change="−4 vs last year"
                trend="down"
                :loading="statsLoading"
              />
              <GStatTile
                label="Teams"
                :value="214"
                hint="Open the team list"
                to="/dash"
                :loading="statsLoading"
              />
            </div>
          </div>

          <!-- Empty state + skeleton -->
          <div class="grid gap-6 lg:grid-cols-2">
            <GEmptyState
              title="No submissions yet"
              description="Teams' projects show up here once they submit. Submissions open on Sunday at 9am."
              icon="material-symbols:folder-open-rounded"
            >
              <template #actions>
                <GButton
                  variant="primary"
                  icon="material-symbols:notifications-rounded"
                  label="Remind teams"
                />
                <GButton
                  variant="ghost"
                  label="See the timeline"
                />
              </template>
            </GEmptyState>
            <div
              class="flex flex-col gap-4 rounded-gh border-[1.5px] border-gh-line bg-gh-surface p-5"
              aria-busy="true"
            >
              <div>
                <h3 class="gh-h3">
                  Skeleton
                </h3>
                <p class="gh-small text-gh-muted">
                  Hidden from screen readers; the region is marked busy instead.
                </p>
              </div>
              <div class="flex items-center gap-3">
                <GSkeleton
                  shape="circle"
                  class="w-10"
                />
                <div class="flex flex-1 flex-col gap-2">
                  <GSkeleton
                    shape="text"
                    class="w-1/3"
                  />
                  <GSkeleton
                    shape="text"
                    class="w-2/3"
                  />
                </div>
              </div>
              <GSkeleton class="h-10 w-full" />
              <GSkeleton class="h-10 w-full" />
              <GEmptyState
                size="sm"
                :bordered="false"
                title="No check-ins yet"
                description="Scan a QR code to start."
                icon="material-symbols:qr-code-scanner-rounded"
              />
            </div>
          </div>

          <!-- Table -->
          <div class="flex flex-col gap-4 rounded-gh border-[1.5px] border-gh-line bg-gh-surface p-5">
            <div class="flex flex-wrap items-end justify-between gap-3">
              <div>
                <h3 class="gh-h3">
                  Table
                </h3>
                <p class="gh-small text-gh-muted">
                  Calm: no shadows, thin dividers, a sticky label-style header. Click a header to sort (ascending, descending, off). Empty values stay last.
                </p>
              </div>
              <GButton
                size="sm"
                icon="material-symbols:refresh-rounded"
                label="Show loading"
                :loading="tableLoading"
                @click="reloadTable"
              />
            </div>
            <div class="overflow-hidden rounded-gh border-[1.5px] border-gh-line">
              <GTable
                v-model:sort="tableSort"
                :data="participants"
                :columns="participantColumns"
                row-key="id"
                caption="Participants (demo)"
                :loading="tableLoading"
              >
                <template #name-cell="{ row }">
                  <span class="font-semibold">{{ row.original.name }}</span>
                  <span class="gh-mono block text-gh-muted">{{ row.original.id }}</span>
                </template>
                <template #team-cell="{ value }">
                  <span :class="value ? '' : 'text-gh-muted'">{{ value ?? 'No team' }}</span>
                </template>
                <template #status-cell="{ row }">
                  <GBadge
                    size="sm"
                    :tone="statusTone[row.original.status]"
                    :label="row.original.status"
                  />
                </template>
                <template #checkedIn-cell="{ value }">
                  <span :class="value ? 'gh-mono' : 'text-gh-muted'">{{ value ? `${String(value).padStart(2, '0')}:14` : 'Not yet' }}</span>
                </template>
                <template #actions-cell="{ row }">
                  <GButton
                    variant="ghost"
                    size="sm"
                    icon="material-symbols:history-rounded"
                    :aria-label="`Check-in history for ${row.original.name}`"
                  />
                </template>
              </GTable>
            </div>
            <p class="gh-small text-gh-muted">
              Sort: <code class="gh-mono">{{ tableSort ? `${tableSort.id} ${tableSort.desc ? 'desc' : 'asc'}` : 'none' }}</code>
            </p>

            <div class="grid gap-6 lg:grid-cols-2">
              <div class="flex flex-col gap-2">
                <span class="gh-label text-gh-muted">Virtualised · 2,000 rows</span>
                <div class="overflow-hidden rounded-gh border-[1.5px] border-gh-line">
                  <GTable
                    :data="manyParticipants"
                    :columns="participantColumns.slice(0, 3)"
                    row-key="id"
                    size="sm"
                    caption="2,000 participants, virtualised (demo)"
                    :virtualize="{ estimateSize: 37, overscan: 10 }"
                    class="max-h-80"
                  >
                    <template #status-cell="{ row }">
                      <GBadge
                        size="sm"
                        :tone="statusTone[row.original.status]"
                        :label="row.original.status"
                      />
                    </template>
                  </GTable>
                </div>
              </div>
              <div class="flex flex-col gap-2">
                <span class="gh-label text-gh-muted">Empty</span>
                <div class="overflow-hidden rounded-gh border-[1.5px] border-gh-line">
                  <GTable
                    :data="[]"
                    :columns="participantColumns.slice(0, 3)"
                    caption="No results (demo)"
                    empty-title="No participants match"
                    empty-description="Try a different status filter or clear the search."
                  />
                </div>
              </div>
            </div>
          </div>

          <!-- Brand pieces -->
          <div class="flex flex-col gap-5 rounded-gh border-[1.5px] border-gh-line bg-gh-surface p-5">
            <div>
              <h3 class="gh-h3">
                Label strip and section heading
              </h3>
              <p class="gh-small text-gh-muted">
                The wordmark's letter-spaced bar and the big section numerals. The logo is below.
              </p>
            </div>
            <div class="flex flex-wrap items-center gap-4">
              <GLabelStrip label="Registration" />
              <GLabelStrip
                label="Live now"
                tone="accent"
              />
              <GLabelStrip
                label="Workshop"
                tone="outline"
              />
              <GLabelStrip
                label="Small"
                size="sm"
              />
            </div>
            <GSectionHeading
              :numeral="2"
              label="Step"
              title="Tell us about your team"
              :level="3"
            >
              Numbers are padded to two digits and hidden from screen readers.
            </GSectionHeading>
          </div>

          <!-- Logo -->
          <div class="flex flex-col gap-5 rounded-gh border-[1.5px] border-gh-line bg-gh-surface p-5">
            <div>
              <h3 class="gh-h3">
                Logo
              </h3>
              <p class="gh-small text-gh-muted">
                <code class="gh-mono">GLogo</code>, traced from the brand-guide moodboard until the designers export vector files. Primary has the wordmark bar; secondary is the glasses alone; mono is one colour. Keep it upright, keep its proportions, never recolour it.
              </p>
            </div>
            <div class="grid gap-4 md:grid-cols-3">
              <div
                v-for="bg in logoBackgrounds"
                :key="bg.on"
                class="flex flex-col gap-6 rounded-gh border-[1.5px] border-gh-line p-6"
                :style="{ background: bg.color }"
              >
                <span
                  class="gh-label"
                  :style="{ color: bg.label }"
                >On {{ bg.on }}</span>
                <GLogo
                  :on="bg.on"
                  size="lg"
                  decorative
                />
                <GLogo
                  :on="bg.on"
                  :wordmark="false"
                  size="lg"
                  decorative
                />
                <GLogo
                  :on="bg.on"
                  variant="mono"
                  size="lg"
                  decorative
                />
              </div>
            </div>
            <div class="flex flex-wrap items-end gap-6 border-t border-gh-line-soft pt-5">
              <div class="flex flex-col gap-2">
                <span class="gh-label text-gh-muted">Follows the theme</span>
                <GLogo size="lg" />
              </div>
              <div class="flex flex-col gap-2">
                <span class="gh-label text-gh-muted">With clearance space</span>
                <GLogo
                  size="lg"
                  clearance
                  class="outline-1 outline-dashed outline-gh-subtle"
                />
              </div>
              <div class="flex flex-col gap-2">
                <span class="gh-label text-gh-muted">Small: glasses alone, name as text</span>
                <div class="flex items-center gap-3">
                  <GLogo
                    :wordmark="false"
                    size="sm"
                    decorative
                  />
                  <span class="font-display text-lg font-semibold">GeeksHacking</span>
                  <GLogo
                    :wordmark="false"
                    size="md"
                    class="ml-4"
                  />
                </div>
              </div>
              <div class="flex flex-col gap-2">
                <span class="gh-label text-gh-muted">Primary from lg up</span>
                <GLogo size="xl" />
              </div>
            </div>
          </div>

          <!-- Icons -->
          <div class="flex flex-col gap-4 rounded-gh border-[1.5px] border-gh-line bg-gh-surface p-5">
            <div>
              <h3 class="gh-h3">
                Icons
              </h3>
              <p class="gh-small text-gh-muted">
                Material Symbols Rounded: filled, rounded, one colour. <code class="gh-mono">lucideToMaterial</code> in <code class="gh-mono">utils/g-icons.ts</code> maps the old Lucide names for page migrations.
              </p>
            </div>
            <ul class="grid grid-cols-2 gap-2 sm:grid-cols-3 lg:grid-cols-4">
              <li
                v-for="[lucide, material] in iconMap"
                :key="lucide"
                class="flex items-center gap-3 rounded-gh-sm border border-gh-line-soft p-2"
              >
                <Icon
                  :name="material"
                  class="size-6 flex-none"
                  aria-hidden="true"
                />
                <span class="min-w-0">
                  <span class="gh-small block truncate">{{ material.replace('material-symbols:', '') }}</span>
                  <span class="gh-mono block truncate text-xs text-gh-muted">{{ lucide.replace('i-lucide-', 'lucide ') }}</span>
                </span>
              </li>
            </ul>
          </div>
        </div>
      </section>
      <!-- 09 Shell -->
      <section aria-labelledby="s-shells">
        <GSectionHeading
          id="s-shells"
          numeral="09"
          label="Shell"
          title="Portal shell"
          class="mb-5"
        />
        <p class="mb-6 max-w-[70ch] text-gh-muted">
          <code class="gh-mono">GShell</code> + <code class="gh-mono">GNavList</code> + <code class="gh-mono">GPanel</code> / <code class="gh-mono">GPanelHeader</code>: the frame every signed-in page sits in (<code class="gh-mono">layouts/portal.vue</code>). The sidebar shrinks to icons from the header button and remembers it. Below 1024px it becomes a drawer behind the menu button. The links here only switch this demo.
        </p>
        <div class="shell-preview overflow-hidden rounded-gh border-[1.5px] border-gh-line">
          <GShell
            contained
            label="Demo navigation"
            storage-key="gh-shell-demo"
          >
            <template #header="{ collapsed }">
              <span
                class="flex min-w-0 items-center gap-2.5"
                :class="{ 'mx-auto': collapsed }"
              >
                <GLogo
                  :wordmark="false"
                  size="sm"
                  decorative
                />
                <span
                  v-if="!collapsed"
                  class="flex min-w-0 flex-col whitespace-nowrap font-display leading-[1.15] font-bold"
                >GeeksHacking <span class="text-[11px] font-semibold tracking-[0.12em] text-gh-muted uppercase [font-family:var(--font-body)]">Portal</span></span>
              </span>
            </template>
            <template #default="{ collapsed }">
              <GNavList
                :groups="shellDemoGroups"
                :collapsed="collapsed"
                label="Demo pages"
              />
            </template>
            <template #footer="{ collapsed }">
              <GButton
                variant="ghost"
                size="sm"
                icon="material-symbols:support-agent-rounded"
                :label="collapsed ? undefined : 'Contact us'"
                :aria-label="collapsed ? 'Contact us' : undefined"
                :block="!collapsed"
              />
            </template>
            <template #main>
              <GPanel id="shell-demo-panel">
                <template #header>
                  <GPanelHeader :title="shellDemoTitle">
                    <template #right>
                      <GButton
                        variant="secondary"
                        size="sm"
                        icon="material-symbols:qr-code-2-rounded"
                        label="My QR code"
                      />
                    </template>
                  </GPanelHeader>
                </template>
                <template #body>
                  <GLabelStrip
                    label="HackOMania 2026"
                    class="self-start"
                  />
                  <GCard
                    v-for="n in 4"
                    :key="n"
                  >
                    <p class="text-gh-muted">
                      {{ shellDemoTitle }} content block {{ n }}. The body scrolls under the header.
                    </p>
                  </GCard>
                </template>
              </GPanel>
            </template>
          </GShell>
        </div>
      </section>
    </main>
  </div>
</template>

<style scoped>
.shell-preview {
  height: 640px;
  background: var(--gh-bg);
}
.gh-mark {
  background: linear-gradient(transparent 58%, var(--gh-accent) 58%, var(--gh-accent) 92%, transparent 92%);
  color: inherit;
  padding: 0 0.08em;
}
[data-gh-theme='dark'] .gh-mark {
  background: none;
  color: var(--gh-accent);
}

/* Motion demo: press-in on enabled, nothing at all on disabled */
.motion-demo {
  transition:
    transform var(--gh-dur-press) var(--ease-gh),
    box-shadow var(--gh-dur-press) var(--ease-gh);
}
.motion-demo:not(:disabled):hover {
  transform: translate(-1px, -1px);
  box-shadow: 5px 5px 0 var(--gh-shadow-color);
}
.motion-demo:not(:disabled):active {
  transform: translate(4px, 4px);
  box-shadow: 0 0 0 var(--gh-shadow-color);
}
.motion-demo:disabled {
  cursor: not-allowed;
  transition: none;
  transform: none;
  box-shadow: none;
  background: var(--gh-surface-sunken);
  color: var(--gh-text-disabled);
  border-color: var(--gh-line-disabled);
}
.motion-demo:focus-visible,
button:focus-visible {
  outline: 3px solid var(--gh-focus);
  outline-offset: 2px;
}

.viz-stack {
  display: flex;
  height: 14px;
  overflow: hidden;
  border-radius: 4px;
  border: 1.5px solid var(--gh-viz-edge);
}
[data-gh-theme='dark'] .viz-stack {
  border-color: var(--gh-viz-1);
}
.viz-stack i {
  display: block;
  height: 100%;
  border-right: 1.5px solid var(--gh-viz-edge);
}
.viz-stack i:last-child {
  border-right: 0;
}
.viz-legend {
  display: flex;
  flex-wrap: wrap;
  gap: 6px 14px;
}
.viz-legend span {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}
.viz-legend span::before {
  content: '';
  width: 12px;
  height: 12px;
  border-radius: 2px;
  background: var(--c);
  border: 1.5px solid var(--gh-viz-edge);
}
[data-gh-theme='dark'] .viz-legend span::before {
  border-color: var(--gh-viz-1);
}
</style>
