// The API only honors workers.dev origins. Other origins are ignored.
export function githubLoginUrl(apiBase: string, redirectPath?: string) {
  const url = new URL(`${apiBase.replace(/\/$/, '')}/auth/login`)
  if (redirectPath)
    url.searchParams.set('redirect_uri', redirectPath)
  if (import.meta.client)
    url.searchParams.set('return_origin', window.location.origin)
  return url.toString()
}
