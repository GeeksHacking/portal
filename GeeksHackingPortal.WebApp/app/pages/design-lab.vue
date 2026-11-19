<script setup lang="ts">
import type { Rgba } from '~/utils/color-contrast'
import { contrastRatio, flatten, toHex } from '~/utils/color-contrast'

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
  { title: 'Surfaces', tokens: ['--gh-bg', '--gh-surface', '--gh-surface-sunken', '--gh-highlight'] },
  { title: 'Text', tokens: ['--gh-text', '--gh-text-muted', '--gh-text-subtle', '--gh-text-disabled'] },
  { title: 'Lines and focus', tokens: ['--gh-line', '--gh-line-soft', '--gh-highlight-edge', '--gh-focus'] },
  { title: 'Accent and status', tokens: ['--gh-accent', '--gh-on-accent', '--gh-highlight-ink', '--gh-ok', '--gh-ok-soft', '--gh-err', '--gh-err-soft'] },
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
  { label: 'Outline vs card (UI)', fg: '--gh-line', bg: '--gh-surface', min: 3 },
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
  // the CSS minifier shortens #ffffff to #fff
  if (/^[0-9a-f]{3}$/i.test(hex))
    hex = hex.split('').map(c => c + c).join('')
  if (!/^[0-9a-f]{6}$/i.test(hex))
    return null
  return {
    r: Number.parseInt(hex.slice(0, 2), 16),
    g: Number.parseInt(hex.slice(2, 4), 16),
    b: Number.parseInt(hex.slice(4, 6), 16),
    a: 1,
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
</script>

<template>
  <div
    class="gh-base min-h-screen"
    :data-gh-theme="theme"
  >
    <header class="sticky top-0 z-10 border-b-[1.5px] border-gh-line bg-gh-bg">
      <div class="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-6 py-4">
        <div class="flex items-center gap-3">
          <span class="gh-strip">GeeksHacking</span>
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
        <div class="mb-5 flex items-end gap-5 border-t-2 border-gh-text pt-3">
          <span class="gh-numeral">01</span>
          <div>
            <span class="gh-strip mb-2">Palette</span>
            <h2
              id="s-palette"
              class="gh-h2"
            >
              Brand colours
            </h2>
          </div>
        </div>
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
        <div class="mb-5 flex items-end gap-5 border-t-2 border-gh-text pt-3">
          <span class="gh-numeral">02</span>
          <div>
            <span class="gh-strip mb-2">Semantic</span>
            <h2
              id="s-semantic"
              class="gh-h2"
            >
              Light and dark tokens
            </h2>
          </div>
        </div>
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
        <div class="mb-5 flex items-end gap-5 border-t-2 border-gh-text pt-3">
          <span class="gh-numeral">03</span>
          <div>
            <span class="gh-strip mb-2">Checks</span>
            <h2
              id="s-contrast"
              class="gh-h2"
            >
              Contrast
            </h2>
          </div>
        </div>
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
        <div class="mb-5 flex items-end gap-5 border-t-2 border-gh-text pt-3">
          <span class="gh-numeral">04</span>
          <div>
            <span class="gh-strip mb-2">Type</span>
            <h2
              id="s-type"
              class="gh-h2"
            >
              Typography
            </h2>
          </div>
        </div>
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
        <div class="mb-5 flex items-end gap-5 border-t-2 border-gh-text pt-3">
          <span class="gh-numeral">05</span>
          <div>
            <span class="gh-strip mb-2">Shape</span>
            <h2
              id="s-shape"
              class="gh-h2"
            >
              Radius, outline and offset shadow
            </h2>
          </div>
        </div>
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
        <div class="mb-5 flex items-end gap-5 border-t-2 border-gh-text pt-3">
          <span class="gh-numeral">06</span>
          <div>
            <span class="gh-strip mb-2">Motion</span>
            <h2
              id="s-motion"
              class="gh-h2"
            >
              Motion
            </h2>
          </div>
        </div>
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
        <div class="mb-5 flex items-end gap-5 border-t-2 border-gh-text pt-3">
          <span class="gh-numeral">07</span>
          <div>
            <span class="gh-strip mb-2">Charts</span>
            <h2
              id="s-viz"
              class="gh-h2"
            >
              Data viz and hero
            </h2>
          </div>
        </div>
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
        <div class="mb-5 flex items-end gap-5 border-t-2 border-gh-text pt-3">
          <span class="gh-numeral">08</span>
          <div>
            <span class="gh-strip mb-2">Components</span>
            <h2
              id="s-components"
              class="gh-h2"
            >
              Button, input and form field
            </h2>
          </div>
        </div>
        <p class="mb-6 max-w-[70ch] text-gh-muted">
          <code class="gh-mono">GButton</code>, <code class="gh-mono">GInput</code> and <code class="gh-mono">GFormField</code> in <code class="gh-mono">app/components/g/</code>. Hover, press, tab to them and type: these are the real components.
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
                        icon="i-lucide-check"
                        label="Check in"
                      />
                    </td>
                    <td class="pr-4">
                      <GButton
                        :variant="v"
                        icon="i-lucide-history"
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
                trailing-icon="i-lucide-arrow-right"
                to="/dash"
              />
              <GButton
                variant="primary"
                icon="i-lucide-save"
                :loading="saving"
                :label="saving ? 'Saving' : 'Try loading'"
                @click="fakeSave"
              />
            </div>
            <div class="mt-5 max-w-sm">
              <GButton
                variant="primary"
                size="lg"
                icon="i-lucide-check"
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
                  icon="i-lucide-users"
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
                  icon="i-lucide-search"
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
                      icon="i-lucide-copy"
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
        </div>
      </section>
    </main>
  </div>
</template>

<style scoped>
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
