/**
 * A challenge's "selection criteria" is a JavaScript rule the API runs (with Jint) when a team
 * submits to that challenge: the submission is refused unless it evaluates to `true`.
 * Variables the API provides:
 *   teamSize                 members in the submitting team
 *   currentTeamsInChallenge  teams that have already submitted to this challenge
 *   totalParticipants        participants in the hackathon
 *   totalSubmissions         old name for currentTeamsInChallenge
 *   challenge                the challenge itself
 * An invalid rule counts as false, so it blocks every submission.
 *
 * The organiser form offers presets for the common rules and falls back to a custom rule.
 */
export type ChallengeRuleKind = 'any' | 'minTeamSize' | 'firstTeams' | 'custom'

export interface ChallengeRule {
  kind: ChallengeRuleKind
  /** for minTeamSize and firstTeams */
  count: number
  /** for custom */
  expression: string
}

export const CHALLENGE_RULE_VARIABLES = [
  { name: 'teamSize', meaning: 'members in the team submitting' },
  { name: 'currentTeamsInChallenge', meaning: 'teams that have already submitted to this challenge' },
  { name: 'totalParticipants', meaning: 'participants in the hackathon' },
] as const

const MIN_TEAM_SIZE = /^\s*teamSize\s*>=\s*(\d+)\s*;?\s*$/
const FIRST_TEAMS = /^\s*(?:currentTeamsInChallenge|totalSubmissions)\s*<\s*(\d+)\s*;?\s*$/

/** Reads a stored rule back into the form's shape. Anything unrecognised is a custom rule. */
export function parseChallengeRule(stored: string | null | undefined): ChallengeRule {
  const text = (stored ?? '').trim()
  if (!text || /^true;?$/.test(text))
    return { kind: 'any', count: 3, expression: '' }
  const minSize = text.match(MIN_TEAM_SIZE)
  if (minSize)
    return { kind: 'minTeamSize', count: Number(minSize[1]), expression: '' }
  const firstTeams = text.match(FIRST_TEAMS)
  if (firstTeams)
    return { kind: 'firstTeams', count: Number(firstTeams[1]), expression: '' }
  return { kind: 'custom', count: 3, expression: text }
}

/** The rule to store for the form's choice. */
export function buildChallengeRule(rule: ChallengeRule): string {
  switch (rule.kind) {
    case 'minTeamSize': return `teamSize >= ${rule.count}`
    case 'firstTeams': return `currentTeamsInChallenge < ${rule.count}`
    case 'custom': return rule.expression.trim()
    default: return 'true'
  }
}

/**
 * The rule in plain words, for participants and organiser lists. Custom rules aren't
 * described to participants (`forParticipants`), since we can't put them into words.
 */
export function describeChallengeRule(stored: string | null | undefined, forParticipants = false): string {
  const rule = parseChallengeRule(stored)
  switch (rule.kind) {
    case 'minTeamSize': return `Teams of ${rule.count} or more`
    case 'firstTeams': return rule.count === 1 ? 'Only the first team to submit' : `Only the first ${rule.count} teams to submit`
    case 'custom': return forParticipants ? '' : 'Custom rule'
    default: return forParticipants ? '' : 'Any team'
  }
}

/**
 * A syntax check for custom rules, without running them. Returns an error message, or '' if
 * the rule parses (or the browser won't let us check, e.g. under a strict CSP).
 */
export function checkChallengeRuleSyntax(expression: string): string {
  // Jint accepts a trailing semicolon; the wrapper below wouldn't
  const text = expression.trim().replace(/;+$/, '').trim()
  if (!text)
    return 'Write a rule, or choose another option.'
  try {
    // compiles only; never called
    // eslint-disable-next-line no-new-func
    new Function('teamSize', 'currentTeamsInChallenge', 'totalParticipants', 'totalSubmissions', 'challenge', `return (${text});`)
    return ''
  }
  catch (error) {
    if (error instanceof SyntaxError)
      return `This isn't valid JavaScript: ${error.message}`
    return ''
  }
}
