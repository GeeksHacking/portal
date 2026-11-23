import type {
  GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListQuestionDto,
  GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListResponse,
  GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListResponse,
  GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListSubmissionDto,
} from '@geekshacking/portal-sdk'
import type { MaybeRefOrGetter } from 'vue'
import { HACKATHON_TIME_ZONE, HACKATHON_TIME_ZONE_LABEL, isSameHackathonDay } from '~/utils/hackathon-date-time'

export interface WorkshopAnswerDetail {
  key: string
  question: string
  answers: string[]
  followUps: { label: string, value: string }[]
}

export interface WorkshopAnswerGroup {
  name: string
  details: WorkshopAnswerDetail[]
}

/**
 * A participant's saved workshop answers, ready to show: grouped by category, option values
 * turned back into their labels, Yes/No for booleans, follow-up texts attached. Used by the
 * workshop page and the "registered" page.
 */
export function useWorkshopAnswers(
  questionsData: MaybeRefOrGetter<GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListResponse | undefined>,
  submissionsData: MaybeRefOrGetter<GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListResponse | undefined>,
) {
  const questionById = computed(() => {
    const questions = toValue(questionsData)?.categories?.flatMap(category => category.questions ?? []) ?? []
    return new Map(questions.filter(question => question.id).map(question => [question.id as string, question]))
  })

  const allQuestions = computed(() =>
    toValue(questionsData)?.categories?.flatMap(category => category.questions ?? []) ?? [],
  )

  function parseJsonValue(value: string | null | undefined) {
    if (!value)
      return null

    try {
      return JSON.parse(value) as unknown
    }
    catch {
      return null
    }
  }

  function optionLabel(question: GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListQuestionDto | undefined, value: string) {
    const option = question?.options?.find(item => item.optionValue === value)
    return option?.optionText ?? value
  }

  function formatAnswerValue(
    value: string | null | undefined,
    question: GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListQuestionDto | undefined,
  ) {
    const rawValue = value ?? ''
    const parsed = parseJsonValue(rawValue)

    if (Array.isArray(parsed))
      return parsed.map(item => optionLabel(question, String(item)))

    if (rawValue === 'true')
      return ['Yes']
    if (rawValue === 'false')
      return ['No']

    return [optionLabel(question, rawValue)]
  }

  function formatAnswers(submission: GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListSubmissionDto) {
    const question = submission.questionId ? questionById.value.get(submission.questionId) : undefined
    return formatAnswerValue(submission.value, question)
  }

  function formatFollowUpValue(
    followUpValue: string | null | undefined,
    question: GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationQuestionsListQuestionDto | undefined,
  ) {
    if (!followUpValue)
      return []

    const parsed = parseJsonValue(followUpValue)
    if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
      return Object.entries(parsed as Record<string, string>)
        .filter(([, value]) => Boolean(value))
        .map(([optionValue, value]) => ({
          label: optionLabel(question, optionValue),
          value,
        }))
    }

    return [{ label: 'Additional details', value: followUpValue }]
  }

  function formatFollowUps(submission: GeeksHackingPortalApiEndpointsParticipantsStandaloneWorkshopsRegistrationSubmissionsListSubmissionDto) {
    const question = submission.questionId ? questionById.value.get(submission.questionId) : undefined
    return formatFollowUpValue(submission.followUpValue, question)
  }


  function addAnswerDetail(groups: Map<string, WorkshopAnswerDetail[]>, category: string, detail: WorkshopAnswerDetail) {
    const details = groups.get(category) ?? []
    details.push(detail)
    groups.set(category, details)
  }

  const answerGroups = computed<WorkshopAnswerGroup[]>(() => {
    const groups = new Map<string, WorkshopAnswerDetail[]>()
    const submissions = toValue(submissionsData)?.submissions ?? []

    if (submissions.length > 0) {
      for (const submission of submissions) {
        const category = submission.category || 'Registration details'
        addAnswerDetail(groups, category, {
          key: submission.questionId ?? `${category}-${groups.get(category)?.length ?? 0}`,
          question: submission.questionText ?? submission.questionKey ?? 'Question',
          answers: formatAnswers(submission),
          followUps: formatFollowUps(submission),
        })
      }
    }
    else {
      for (const category of toValue(questionsData)?.categories ?? []) {
        for (const question of category.questions ?? []) {
          const submission = question.currentSubmission
          if (!submission?.value)
            continue

          addAnswerDetail(groups, category.name ?? 'Registration details', {
            key: question.id ?? question.questionKey ?? `${category.name}-${groups.get(category.name ?? '')?.length ?? 0}`,
            question: question.questionText ?? question.questionKey ?? 'Question',
            answers: formatAnswerValue(submission.value, question),
            followUps: formatFollowUpValue(submission.followUpValue, question),
          })
        }
      }
    }

    return Array.from(groups.entries()).map(([name, details]) => ({ name, details }))
  })

  const savedAnswersCount = computed(() => {
    const fallbackCount = answerGroups.value.reduce((count, group) => count + group.details.length, 0)
    const apiCount = toValue(submissionsData)?.answeredQuestions ?? 0
    return apiCount > 0 ? apiCount : fallbackCount
  })

  const totalQuestionsCount = computed(() => {
    const apiCount = toValue(submissionsData)?.totalQuestions ?? 0
    return apiCount > 0 ? apiCount : allQuestions.value.length
  })

  return { answerGroups, savedAnswersCount, totalQuestionsCount }
}

/** "Thu, Nov 26, 2026" + "3:58 PM to 6:58 PM SGT" (or a two-day range), in event time. */
export function formatWorkshopWhen(startTime: string | Date | null | undefined, endTime: string | Date | null | undefined) {
  if (!startTime || !endTime)
    return { dateLabel: 'Date to be announced', timeLabel: 'Time to be announced' }

  const start = new Date(startTime)
  const end = new Date(endTime)
  const sameDay = isSameHackathonDay(start, end)
  const date = new Intl.DateTimeFormat(undefined, { weekday: 'short', month: 'short', day: 'numeric', timeZone: HACKATHON_TIME_ZONE })
  const dateWithYear = new Intl.DateTimeFormat(undefined, { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric', timeZone: HACKATHON_TIME_ZONE })
  const time = new Intl.DateTimeFormat(undefined, { hour: 'numeric', minute: '2-digit', timeZone: HACKATHON_TIME_ZONE })

  return {
    dateLabel: sameDay ? dateWithYear.format(start) : `${date.format(start)} to ${dateWithYear.format(end)}`,
    timeLabel: `${time.format(start)} to ${time.format(end)} ${HACKATHON_TIME_ZONE_LABEL}`,
  }
}

/** "Nov 23, 2026, 1:58 PM SGT" */
export function formatWorkshopRegisteredAt(value: string | Date | null | undefined) {
  if (!value)
    return 'Registration date unavailable'
  return `${new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short', timeZone: HACKATHON_TIME_ZONE }).format(new Date(value))} ${HACKATHON_TIME_ZONE_LABEL}`
}
