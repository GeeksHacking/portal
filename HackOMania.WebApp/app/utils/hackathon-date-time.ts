export const HACKATHON_TIME_ZONE = 'Asia/Singapore'
export const HACKATHON_TIME_ZONE_LABEL = 'SGT'
const HACKATHON_TIME_ZONE_OFFSET = '+08:00'

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

export function formatHackathonDateTimeInput(value: Date | string | null | undefined): string {
  if (!value)
    return ''

  const date = value instanceof Date ? value : new Date(value)
  if (Number.isNaN(date.getTime()))
    return ''

  const { year, month, day, hour, minute } = getHackathonDateTimeParts(date)
  return `${year}-${month}-${day}T${hour}:${minute}`
}

export function parseHackathonDateTimeInput(value: string | null | undefined): Date | undefined {
  if (!value)
    return undefined

  return new Date(`${value}:00${HACKATHON_TIME_ZONE_OFFSET}`)
}

export function formatHackathonDate(value: Date | string | null | undefined): string {
  if (!value)
    return 'TBC'

  const date = value instanceof Date ? value : new Date(value)
  if (Number.isNaN(date.getTime()))
    return 'TBC'

  return hackathonDateFormatter.format(date)
}
