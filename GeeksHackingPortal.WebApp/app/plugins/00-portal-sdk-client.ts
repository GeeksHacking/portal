import { client, setConfig } from '@geekshacking/portal-sdk/client'
import { readPreviewSession, storePreviewSession, trackPreviewSession } from '~/utils/preview-session'

export default defineNuxtPlugin(async () => {
  const runtimeConfig = useRuntimeConfig()
  const api = runtimeConfig.public.api

  const handoff = import.meta.client ? consumeLoginHandoff(api) : Promise.resolve()
  await trackPreviewSession(handoff)

  setConfig({
    baseURL: api,
    credentials: 'include',
  })

  // setConfig credentials is not applied by older generated clients. Force it on
  // the resolved request so cross-origin whoami actually sends the auth cookie.
  // Preview origins cannot rely on that cookie (third-party blocking), so also
  // attach the bearer minted by /auth/handoff.
  client.interceptors.request.use((request) => {
    if (request.credentials == null)
      request.credentials = 'include'
    const headers = new Headers(request.headers)
    applyPreviewSession(headers)
    request.headers = Object.fromEntries(headers.entries())
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
    if (!body.token)
      return

    storePreviewSession(body.token)
    url.searchParams.delete('login_handoff')
    window.history.replaceState(null, '', `${url.pathname}${url.search}${url.hash}`)
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
  const fetchWithSession = window.fetch as typeof window.fetch & { __previewSession?: boolean }
  if (fetchWithSession.__previewSession)
    return

  const nativeFetch = window.fetch.bind(window)
  const bridged = (input: RequestInfo | URL, init?: RequestInit) => {
    const token = readPreviewSession()
    const url = input instanceof Request ? input.url : String(input)
    if (!token || !isApiOriginRequest(api, url))
      return nativeFetch(input, init)

    const request = new Request(input, init)
    const headers = new Headers(request.headers)
    if (!headers.has('Authorization'))
      headers.set('Authorization', `Bearer ${token}`)
    return nativeFetch(new Request(request, { headers }))
  }
  bridged.__previewSession = true
  window.fetch = bridged
}

function isApiOriginRequest(api: string, requestUrl: string) {
  try {
    return new URL(requestUrl, window.location.href).origin === new URL(api, window.location.href).origin
  }
  catch {
    return false
  }
}
