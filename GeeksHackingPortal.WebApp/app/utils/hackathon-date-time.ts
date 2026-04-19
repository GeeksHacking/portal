export const HACKATHON_TIME_ZONE = 'Asia/Singapore'
export const HACKATHON_TIME_ZONE_LABEL = 'SGT'
const HACKATHON_TIME_ZONE_OFFSET = '+08:00'
const DATE_TIME_OFFSET_PATTERN = /(?:Z|[+-]\d{2}:\d{2})$/i

const hackathonDateTimeFormatter = new Intl.DateTimeFormat('en-GB', {
  timeZone: HACKATHON_TIME_ZONE,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
})

const hackathonDateFormatter = new Intl.DateTimeFormat(undefined, {
  timeZone: HACKATHON_TIME_ZONE,
  dateStyle: 'medium',
})

function getHackathonDateTimeParts(date: Date) {
  const parts = hackathonDateTimeFormatter.formatToParts(date)

  return {
    year: parts.find(part => part.type === 'year')?.value ?? '0000',
    month: parts.find(part => part.type === 'month')?.value ?? '01',
    day: parts.find(part => part.type === 'day')?.value ?? '01',
    hour: parts.find(part => part.type === 'hour')?.value ?? '00',
    minute: parts.find(part => part.type === 'minute')?.value ?? '00',
  }
}

export function parseHackathonDateTimeValue(value: Date | string | null | undefined): Date | undefined {
  if (!value)
    return undefined

  if (value instanceof Date)
    return Number.isNaN(value.getTime()) ? undefined : value

  const normalizedValue = DATE_TIME_OFFSET_PATTERN.test(value) ? value : `${value}Z`
  const date = new Date(normalizedValue)

  return Number.isNaN(date.getTime()) ? undefined : date
}

export function formatHackathonDateTimeInput(value: Date | string | null | undefined): string {
  const date = parseHackathonDateTimeValue(value)
  if (!date)
    return ''

  const { year, month, day, hour, minute } = getHackathonDateTimeParts(date)
  return `${year}-${month}-${day}T${hour}:${minute}`
}

export function serializeHackathonDateTimeInput(value: string | null | undefined): string | undefined {
  if (!value)
    return undefined

  return `${value}:00${HACKATHON_TIME_ZONE_OFFSET}`
}

export function formatHackathonDate(value: Date | string | null | undefined): string {
  const date = parseHackathonDateTimeValue(value)
  if (!date)
    return 'TBC'

  return hackathonDateFormatter.format(date)
}
