/**
 * Fallback timetable (HackOMania), shown on the Schedule page only when the hackathon has no
 * timeline items in the API yet.
 */
export interface FallbackScheduleItem {
  time: string
  event: string
}

export const day1Items: FallbackScheduleItem[] = [
  { time: '10:00 AM', event: 'Registration & Team Forming' },
  { time: '11:00 AM', event: 'Introduction' },
  { time: '11:45 AM', event: 'Safety Briefing' },
  { time: '12:00 PM', event: 'Title Sponsor Key Note' },
  { time: '12:15 PM', event: 'Hackathon Briefing (challenges, house rules)' },
  { time: '12:30 PM', event: 'Lunch' },
  { time: '01:00 PM', event: 'Hackathon Kick Off' },
  { time: '02:00 PM', event: 'Challenge Picking' },
  { time: '04:00 PM', event: 'Challenge Lock-in' },
  { time: '04:30 PM', event: 'Snackpack Distribution' },
  { time: '07:30 PM', event: 'Dinner' },
  { time: '11:00 PM', event: 'Snack Time!' },
]

export const day2Items: FallbackScheduleItem[] = [
  { time: '08:00 AM', event: 'Breakfast' },
  { time: '09:00 AM', event: 'Submission' },
  { time: '11:30 AM', event: 'Submission Close' },
  { time: '12:00 PM', event: 'Lunch' },
  { time: '12:30 PM', event: 'Judges Intro' },
  { time: '01:30 PM', event: 'Judging Period' },
  { time: '03:30 PM', event: 'Deliberation' },
  { time: '04:00 PM', event: 'Prize Presentation' },
  { time: '04:30 PM', event: 'End of GeeksHacking Portal event' },
]
