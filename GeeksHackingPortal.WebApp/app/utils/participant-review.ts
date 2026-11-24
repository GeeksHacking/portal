import type {
  GeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListParticipantItem,
  GeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListParticipantReviewItemParticipantReviewStatusKey,
} from '@geekshacking/portal-sdk'
import { HACKATHON_TIME_ZONE, HACKATHON_TIME_ZONE_LABEL } from '~/utils/hackathon-date-time'

/**
 * Shared rules for the hackathon participant review queue (page + OrganizersParticipantReview).
 */
type ParticipantItem = GeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListParticipantItem
type ReviewStatus = GeeksHackingPortalApiEndpointsOrganizersHackathonParticipantsListParticipantReviewItemParticipantReviewStatusKey

export type ReviewState = 'incomplete' | 'pending' | 'approved' | 'rejected'

/** An application waiting this long is overdue and jumps to the top of the queue. */
export const REVIEW_OVERDUE_DAYS = 5
const REVIEW_OVERDUE_MS = REVIEW_OVERDUE_DAYS * 24 * 60 * 60 * 1000

export function reviewStateOf(participant: ParticipantItem): ReviewState {
  if (!participant.registrationSubmissions?.length)
    return 'incomplete'
  if (participant.concludedStatus === 'Accepted')
    return 'approved'
  if (participant.concludedStatus === 'Rejected')
    return 'rejected'
  return 'pending'
}

/** When they last changed an answer (the old page's "Applied"), as epoch ms; 0 if never. */
export function applicationTimeOf(participant: ParticipantItem) {
  return (participant.registrationSubmissions ?? []).reduce((max, submission) => {
    const time = submission.updatedAt ? new Date(submission.updatedAt).getTime() : 0
    return Number.isNaN(time) ? max : Math.max(max, time)
  }, 0)
}

export function isReviewOverdue(participant: ParticipantItem, now = Date.now()) {
  if (reviewStateOf(participant) !== 'pending')
    return false
  const applied = applicationTimeOf(participant)
  return applied > 0 && now - applied >= REVIEW_OVERDUE_MS
}

/** Overdue first, then the rest waiting for review, then everyone else. */
export function reviewPriorityOf(participant: ParticipantItem) {
  if (isReviewOverdue(participant))
    return 0
  return reviewStateOf(participant) === 'pending' ? 1 : 2
}

export const reviewStateBadge: Record<ReviewState, { label: string, tone: 'neutral' | 'ok' | 'err' | 'pending', icon: string }> = {
  incomplete: { label: 'Incomplete', tone: 'neutral', icon: 'material-symbols:edit-note-rounded' },
  pending: { label: 'To review', tone: 'pending', icon: 'material-symbols:hourglass-top-rounded' },
  approved: { label: 'Approved', tone: 'ok', icon: 'material-symbols:check-circle-rounded' },
  rejected: { label: 'Rejected', tone: 'err', icon: 'material-symbols:cancel-rounded' },
}

export function reviewDecisionLabel(status: ReviewStatus | null | undefined) {
  if (status === 'Accepted')
    return 'Approved'
  if (status === 'Rejected')
    return 'Rejected'
  return 'Unknown'
}

const dateTimeFormatter = new Intl.DateTimeFormat(undefined, {
  dateStyle: 'medium',
  timeStyle: 'short',
  timeZone: HACKATHON_TIME_ZONE,
})

/** "24 Nov 2026, 3:05 pm SGT", or "—" for a missing or bad date. */
export function formatOrganizerDateTime(value: Date | string | number | null | undefined) {
  if (value === null || value === undefined || value === '' || value === 0)
    return '—'
  const date = value instanceof Date ? value : new Date(value)
  if (Number.isNaN(date.getTime()))
    return '—'
  return `${dateTimeFormatter.format(date)} ${HACKATHON_TIME_ZONE_LABEL}`
}

/** "today", "yesterday", "3 days ago": how long an application has waited. */
export function waitedFor(epoch: number, now = Date.now()) {
  if (!epoch)
    return ''
  const days = Math.floor((now - epoch) / (24 * 60 * 60 * 1000))
  if (days <= 0)
    return 'today'
  if (days === 1)
    return 'yesterday'
  return `${days} days ago`
}

/** The error's HTTP status, whichever shape the SDK client gave it. */
export function getErrorStatusCode(error: unknown): number | null {
  if (!error || typeof error !== 'object')
    return null
  const candidate = error as { responseStatusCode?: unknown, statusCode?: unknown, status?: unknown, response?: { status?: unknown } }
  for (const value of [candidate.responseStatusCode, candidate.statusCode, candidate.status, candidate.response?.status]) {
    if (typeof value === 'number')
      return value
  }
  return null
}
