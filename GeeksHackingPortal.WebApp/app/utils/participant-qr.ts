/**
 * Read the participant out of a scanned check-in QR code, for an organiser scanning at an event.
 *
 * Understands our JSON payloads (hackathon: `{ userId }`; workshop:
 * `{ kind, workshopId, userId, registrationId }`) and, for older codes, a bare id. Workshop codes
 * carry their workshop, so a code for another event is refused. A registration or participant id is
 * mapped back to the user id through `participants` when it matches someone there.
 *
 * Throws an Error with a message meant for the organiser when the code can't be used.
 */
export function readParticipantQr(
  decodedText: string,
  activityId: string,
  participants: ReadonlyArray<{ userId?: string | null, participantId?: string | null }>,
): string {
  const text = decodedText.trim()
  let id = text
  let otherEvent = false
  try {
    const data = JSON.parse(text) as Record<string, unknown>
    id = String(data.userId ?? data.participantUserId ?? data.registrationId ?? data.participantId ?? '').trim()
    const eventId = data.workshopId ?? data.hackathonId
    otherEvent = typeof eventId === 'string' && !!eventId && eventId !== activityId
  }
  catch {
    // not JSON: the whole text is the id
  }
  if (!id)
    throw new Error('This QR code has no participant in it.')
  if (otherEvent)
    throw new Error('This QR code is for a different event.')
  const match = participants.find(p => p.userId === id || p.participantId === id)
  return match?.userId ?? id
}
