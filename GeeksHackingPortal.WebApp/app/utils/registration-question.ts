import type { GeeksHackingPortalApiEntitiesQuestionTypeKey } from '@geekshacking/portal-sdk'
import { registrationPageConfig } from '~/config/registration-pages'

/**
 * Rules for the organiser's registration-questions builder, shared by hackathons and workshops.
 *
 * How the stored fields are read by the sign-up forms and the API:
 * - `category` groups questions into sections. On the hackathon form it also picks the page
 *   (config/registration-pages.ts): a category that isn't listed there never appears.
 *   Workshops show every category, in order of each one's first question.
 * - `conditionalLogic` is JSON `{ "<questionKey>": "value" | ["value", ...] }`; every entry has to
 *   match (AND) for the question to show. A shown conditional question is always required.
 * - `validationRules` is JSON read by the API (RegistrationValidationService), case-insensitive:
 *   min/max (numbers), minLength/maxLength/pattern/patternMessage (text),
 *   minSelections/maxSelections (several choices), minDate/maxDate (dates).
 * - On update the API treats `null` as "leave unchanged", so clearing a field sends ''.
 */
export type QuestionType = GeeksHackingPortalApiEntitiesQuestionTypeKey

export const QUESTION_TYPES: { value: QuestionType, label: string, hint?: string, hackathonHint?: string }[] = [
  { value: 'Text', label: 'Short text' },
  { value: 'LongText', label: 'Long text', hint: 'A box for a few sentences.' },
  { value: 'Number', label: 'Number' },
  { value: 'Email', label: 'Email address' },
  { value: 'Url', label: 'Link' },
  { value: 'Phone', label: 'Phone number' },
  { value: 'Date', label: 'Date' },
  { value: 'SingleChoice', label: 'Pick one', hint: 'A list to pick one from. With a single option it shows as a checkbox, e.g. for consent.' },
  { value: 'Dropdown', label: 'Pick one (dropdown)', hint: 'Shows the same as Pick one.' },
  { value: 'MultipleChoice', label: 'Pick any', hint: 'A list to pick any number from.' },
  { value: 'Boolean', label: 'Yes or no', hackathonHint: 'The hackathon form shows this as a text box. Use Pick one with Yes and No options instead.' },
]

export const CHOICE_TYPES: QuestionType[] = ['SingleChoice', 'Dropdown', 'MultipleChoice']
export const isChoiceType = (type: QuestionType | null | undefined) => !!type && CHOICE_TYPES.includes(type)

export function questionTypeLabel(type: QuestionType | null | undefined) {
  return QUESTION_TYPES.find(t => t.value === type)?.label ?? 'Unknown type'
}

/** Keys the API refuses: names come from each person's profile. */
export const RESERVED_QUESTION_KEYS = ['first_name', 'last_name']

/** `What's your T-shirt size?` → `whats_your_t_shirt_size` (max 40 characters) */
export function toQuestionKey(text: string) {
  return text
    .normalize('NFKD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .replace(/['’]/g, '')
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '')
    .slice(0, 40)
    .replace(/_+$/g, '')
}

export function checkQuestionKey(key: string, takenKeys: string[]): string {
  const trimmed = key.trim()
  if (!trimmed)
    return 'Enter a key, e.g. tshirt_size.'
  if (!/^[a-z0-9_]+$/i.test(trimmed))
    return 'Use letters, numbers and underscores only.'
  if (RESERVED_QUESTION_KEYS.includes(trimmed.toLowerCase()))
    return 'Names come from each person\'s profile, so first_name and last_name are taken.'
  if (takenKeys.includes(trimmed))
    return 'Another question already uses this key.'
  return ''
}

// ---- sections ----
/** The hackathon form's pages and the categories on each, in form order. */
export const HACKATHON_FORM_PAGES = registrationPageConfig.map(page => ({
  name: page.pageName,
  categories: [...page.categories] as string[],
}))
export const HACKATHON_CATEGORIES = HACKATHON_FORM_PAGES.flatMap(page => page.categories)

/** The API files questions without a category under this name. */
export const UNCATEGORISED = 'Other'

export function hackathonPageOf(category: string | null | undefined) {
  return HACKATHON_FORM_PAGES.find(page => page.categories.includes(category ?? ''))?.name ?? null
}

// ---- show only when ----
export interface QuestionCondition { key: string, values: string[] }

/** null when the stored text isn't the JSON shape the forms read */
export function parseConditions(stored: string | null | undefined): QuestionCondition[] | null {
  const text = (stored ?? '').trim()
  if (!text)
    return []
  try {
    const parsed = JSON.parse(text) as unknown
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed))
      return null
    const conditions: QuestionCondition[] = []
    for (const [key, value] of Object.entries(parsed)) {
      if (typeof value === 'string')
        conditions.push({ key, values: [value] })
      else if (Array.isArray(value) && value.every(v => typeof v === 'string'))
        conditions.push({ key, values: value as string[] })
      else
        return null
    }
    return conditions
  }
  catch {
    return null
  }
}

export function buildConditions(conditions: QuestionCondition[]): string {
  const entries = conditions
    .filter(c => c.key && c.values.some(v => v.trim()))
    .map(c => [c.key, c.values.length === 1 ? c.values[0]!.trim() : c.values.map(v => v.trim()).filter(Boolean)] as const)
  return entries.length ? JSON.stringify(Object.fromEntries(entries)) : ''
}

// ---- answer limits ----
export interface QuestionLimits {
  min?: number | null
  max?: number | null
  minLength?: number | null
  maxLength?: number | null
  pattern?: string | null
  patternMessage?: string | null
  minSelections?: number | null
  maxSelections?: number | null
  minDate?: string | null
  maxDate?: string | null
}

const LIMIT_KEYS: (keyof QuestionLimits)[] = ['min', 'max', 'minLength', 'maxLength', 'pattern', 'patternMessage', 'minSelections', 'maxSelections', 'minDate', 'maxDate']

/** Which limits apply to a type of answer */
export function limitKeysFor(type: QuestionType): (keyof QuestionLimits)[] {
  switch (type) {
    case 'Number': return ['min', 'max']
    case 'Text': return ['minLength', 'maxLength', 'pattern', 'patternMessage']
    case 'LongText': return ['minLength', 'maxLength']
    case 'MultipleChoice': return ['minSelections', 'maxSelections']
    case 'Date': return ['minDate', 'maxDate']
    default: return []
  }
}

/** The stored rules, with keys normalised to camelCase. `null` when it isn't valid JSON. */
export function parseLimits(stored: string | null | undefined): { limits: QuestionLimits, extra: Record<string, unknown> } | null {
  const text = (stored ?? '').trim()
  if (!text)
    return { limits: {}, extra: {} }
  try {
    const parsed = JSON.parse(text) as unknown
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed))
      return null
    const limits: QuestionLimits = {}
    const extra: Record<string, unknown> = {}
    for (const [rawKey, value] of Object.entries(parsed)) {
      const key = LIMIT_KEYS.find(k => k.toLowerCase() === rawKey.toLowerCase())
      if (key && value !== null)
        (limits as Record<string, unknown>)[key] = value
      else if (!key)
        extra[rawKey] = value
    }
    return { limits, extra }
  }
  catch {
    return null
  }
}

/** Keeps only the limits that apply to the type (plus any keys we don't know), dropping blanks. */
export function buildLimits(type: QuestionType, limits: QuestionLimits, extra: Record<string, unknown> = {}): string {
  const out: Record<string, unknown> = { ...extra }
  for (const key of limitKeysFor(type)) {
    const value = limits[key]
    if (value === null || value === undefined || value === '' || (typeof value === 'number' && Number.isNaN(value)))
      continue
    out[key] = typeof value === 'string' ? value.trim() : value
  }
  return Object.keys(out).length ? JSON.stringify(out) : ''
}

/** Problems a person can fix in the form, by limit key */
export function checkLimits(type: QuestionType, limits: QuestionLimits): Partial<Record<keyof QuestionLimits, string>> {
  const errors: Partial<Record<keyof QuestionLimits, string>> = {}
  const has = (v: unknown) => v !== null && v !== undefined && v !== '' && !(typeof v === 'number' && Number.isNaN(v))
  const pairs: [keyof QuestionLimits, keyof QuestionLimits][] = [['min', 'max'], ['minLength', 'maxLength'], ['minSelections', 'maxSelections'], ['minDate', 'maxDate']]
  const keys = limitKeysFor(type)
  for (const [low, high] of pairs) {
    if (!keys.includes(low))
      continue
    for (const key of [low, high]) {
      const v = limits[key]
      if (has(v) && key !== 'min' && key !== 'max' && typeof v === 'number' && (!Number.isInteger(v) || v < 0))
        errors[key] = 'Enter a whole number, 0 or more.'
    }
    const a = limits[low]
    const b = limits[high]
    if (has(a) && has(b) && !errors[low] && !errors[high] && (a as number | string) > (b as number | string))
      errors[high] = 'This has to be at least the lowest value.'
  }
  if (keys.includes('pattern') && has(limits.pattern)) {
    try {
      // eslint-disable-next-line no-new
      new RegExp(String(limits.pattern))
    }
    catch {
      errors.pattern = 'This isn\'t a valid pattern (regular expression).'
    }
  }
  return errors
}

/** e.g. "2 to 5 choices", "At most 200 characters" */
export function describeLimits(type: QuestionType | null | undefined, stored: string | null | undefined): string {
  if (!type)
    return ''
  const parsed = parseLimits(stored)
  if (!parsed)
    return 'Answer rules (unreadable)'
  const l = parsed.limits
  const range = (low: unknown, high: unknown, unit: string) => {
    const hasLow = low !== undefined && low !== null && low !== ''
    const hasHigh = high !== undefined && high !== null && high !== ''
    if (hasLow && hasHigh)
      return low === high ? `Exactly ${low} ${unit}` : `${low} to ${high} ${unit}`
    if (hasLow)
      return `At least ${low} ${unit}`
    if (hasHigh)
      return `At most ${high} ${unit}`
    return ''
  }
  const parts: string[] = []
  switch (type) {
    case 'Number': {
      const r = range(l.min, l.max, '')
      if (r)
        parts.push(r.trim().replace(/^Exactly/, 'Must be').replace(/^At least/, 'From').replace(/^At most/, 'Up to'))
      break
    }
    case 'Text':
    case 'LongText':
      parts.push(range(l.minLength, l.maxLength, 'characters'))
      if (type === 'Text' && l.pattern)
        parts.push('Must match a pattern')
      break
    case 'MultipleChoice':
      parts.push(range(l.minSelections, l.maxSelections, 'choices'))
      break
    case 'Date':
      if (l.minDate && l.maxDate)
        parts.push(`${l.minDate} to ${l.maxDate}`)
      else if (l.minDate)
        parts.push(`From ${l.minDate}`)
      else if (l.maxDate)
        parts.push(`Up to ${l.maxDate}`)
      break
  }
  return parts.filter(Boolean).join(' · ')
}

/** A stored option value for a new option: its label in lowercase with dashes */
export function toOptionValue(label: string) {
  return label
    .normalize('NFKD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .replace(/['’]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 60) || 'option'
}
