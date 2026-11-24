/**
 * Plain-word labels for the OpenIddict values the OAuth API returns (permissions, requirements,
 * sign-in statuses, client and consent types). Unknown values fall back to the raw code, so a new
 * one still shows. The portal gives every app the same permissions (OAuthApplicationMapper); Native
 * apps must also use PKCE.
 */

export type OAuthPermissionGroup = 'endpoint' | 'grant' | 'response' | 'scope' | 'other'

const PERMISSION_GROUPS: Record<string, { group: OAuthPermissionGroup, title: string }> = {
  ept: { group: 'endpoint', title: 'Can call' },
  gt: { group: 'grant', title: 'Ways to get tokens' },
  rst: { group: 'response', title: 'Sign-in responses' },
  scp: { group: 'scope', title: 'Can ask for' },
}

const PERMISSION_LABELS: Record<string, string> = {
  'ept:authorization': 'Sign-in page',
  'ept:token': 'Token endpoint',
  'ept:end_session': 'Sign-out',
  'ept:logout': 'Sign-out',
  'ept:revocation': 'Token revocation',
  'ept:introspection': 'Token introspection',
  'ept:device': 'Device sign-in',
  'gt:authorization_code': 'Sign-in code',
  'gt:refresh_token': 'Refresh tokens (stay signed in)',
  'gt:client_credentials': 'Client credentials (no user)',
  'gt:password': 'Username and password',
  'gt:implicit': 'Implicit',
  'rst:code': 'Code',
  'rst:token': 'Token',
  'rst:id_token': 'ID token',
  'scp:email': 'Email address',
  'scp:profile': 'Name and profile',
  'scp:roles': 'Roles',
  'scp:phone': 'Phone number',
  'scp:address': 'Address',
}

export interface OAuthPermissionGroupView {
  group: OAuthPermissionGroup
  title: string
  items: { code: string, label: string }[]
}

/** Group an app's permissions for display, in a fixed order. */
export function groupOAuthPermissions(permissions: readonly string[] | null | undefined): OAuthPermissionGroupView[] {
  const order: OAuthPermissionGroup[] = ['scope', 'endpoint', 'grant', 'response', 'other']
  const groups = new Map<OAuthPermissionGroup, OAuthPermissionGroupView>()
  for (const code of permissions ?? []) {
    const prefix = code.split(':')[0] ?? ''
    const meta = PERMISSION_GROUPS[prefix] ?? { group: 'other' as const, title: 'Other' }
    const view = groups.get(meta.group) ?? { group: meta.group, title: meta.title, items: [] }
    view.items.push({ code, label: PERMISSION_LABELS[code] ?? (code.slice(prefix.length + 1) || code) })
    groups.set(meta.group, view)
  }
  return order.flatMap(group => (groups.has(group) ? [groups.get(group)!] : []))
}

const REQUIREMENT_LABELS: Record<string, string> = {
  'ft:pkce': 'Must use PKCE (proof key) when signing in',
}
export const oauthRequirementLabel = (code: string) => REQUIREMENT_LABELS[code] ?? code

/** Status of one sign-in (an OpenIddict authorization). */
export function oauthStatusBadge(status: string | null | undefined): { label: string, tone: 'ok' | 'err' | 'neutral' } {
  switch (status) {
    case 'valid': return { label: 'Allowed', tone: 'ok' }
    case 'revoked': return { label: 'Revoked', tone: 'err' }
    case 'rejected': return { label: 'Rejected', tone: 'err' }
    case 'inactive': return { label: 'Inactive', tone: 'neutral' }
    case 'redeemed': return { label: 'Used', tone: 'neutral' }
    default: return { label: status || 'Unknown', tone: 'neutral' }
  }
}

export function oauthClientTypeLabel(value: string | null | undefined) {
  if (value === 'confidential')
    return 'Confidential: has a client secret'
  if (value === 'public')
    return 'Public: no client secret'
  return value || '–'
}

export function oauthConsentTypeLabel(value: string | null | undefined) {
  switch (value) {
    case 'explicit': return 'Asks each person to allow it'
    case 'implicit': return 'Allowed without asking'
    case 'external': return 'Allowed by an admin'
    case 'systematic': return 'Asks every time'
    default: return value || '–'
  }
}

/** Scopes come as a JSON array string ('["openid","email"]') from some endpoints, an array from others. */
export function oauthScopesOf(value: string | readonly string[] | null | undefined): string[] {
  if (!value)
    return []
  if (Array.isArray(value))
    return value.map(String)
  try {
    const parsed = JSON.parse(value as string)
    if (Array.isArray(parsed))
      return parsed.map(String)
  }
  catch {}
  return (value as string).split(/[\s,]+/).filter(Boolean)
}
