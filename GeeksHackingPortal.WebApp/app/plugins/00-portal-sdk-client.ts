import { client, setConfig } from '@geekshacking/portal-sdk/client'

const previewSessionKey = 'gh-preview-session'

export function readPreviewSession(): string | null {
  if (!import.meta.client)
    return null
  try {
    return sessionStorage.getItem(previewSessionKey)
  }
  catch {
    return null
  }
}

export default defineNuxtPlugin(async () => {
  const runtimeConfig = useRuntimeConfig()
  const api = runtimeConfig.public.api

  if (import.meta.client)
    await consumeLoginHandoff(api)

  setConfig({
    baseURL: api,
    credentials: 'include',
  })

  // setConfig credentials is not applied by older generated clients. Force it on
  // the resolved request so cross-origin whoami actually sends the auth cookie.
  // Preview origins cannot rely on that cookie (third-party blocking), so also
  // attach the bearer minted by /auth/handoff.
  client.interceptors.request.use((request) => {
    request.credentials ?? = 'include'
    applyPreviewSession(request.headers)
    return request
  })

  if (import.meta.client)
    installFetchBridge(api)
})

async function consumeLoginHandoff(api: string) {
  const url = new URL(window.location.href)
  const code = url.searchParams.get('login_handoff')
  if (!code)
    return

  url.searchParams.delete('login_handoff')
  window.history.replaceState(null, '', `${url.pathname}${url.search}${url.hash}`)

  try {
    const response = await fetch(`${api}/auth/handoff`, {
      method: 'POST',
      headers: {
        'accept': 'application/json',
        'content-type': 'application/json',
      },
      body: JSON.stringify({ code }),
    })
    if (!response.ok)
      return
    const body = await response.json() as { token?: string }
    if (body.token)
      sessionStorage.setItem(previewSessionKey, body.token)
  }
  catch {
    // whoami will fail and the login page will show the session error
  }
}

function applyPreviewSession(headers: Headers) {
  const token = readPreviewSession()
  if (token && !headers.has('Authorization'))
    headers.set('Authorization', `Bearer ${token}`)
}

function installFetchBridge(api: string) {
  if ((window.fetch as { __previewSession?: boolean }).__previewSession)
    return

  const nativeFetch = window.fetch.bind(window)
  const bridged = (input: RequestInfo | URL, init?: RequestInit) => {
    const token = readPreviewSession()
    const url = input instanceof Request ? input.url : String(input)
    if (!token || !url.startsWith(api))
      return nativeFetch(input, init)

    const headers = new Headers(init?.headers ?? (input instanceof Request ? input.headers : undefined))
    if (!headers.has('Authorization'))
      headers.set('Authorization', `Bearer ${token}`)
    if (input instanceof Request)
      return nativeFetch(new Request(input, { headers }))
    return nativeFetch(input, { ...init, headers })
  }
  bridged.__previewSession = true
  window.fetch = bridged
}
