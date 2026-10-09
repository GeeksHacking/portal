import { geeksHackingPortalApiEndpointsAuthWhoAmIEndpointQueryOptions } from '@geekshacking/portal-sdk/hooks'
import { useQueryClient } from '@tanstack/vue-query'

export default defineNuxtRouteMiddleware(async (to) => {
  const config = useRuntimeConfig()
  const queryClient = useQueryClient()

  // Just came back from the GitHub callback. If the session still isn't visible,
  // stop here — sending the user to /auth/login would challenge GitHub and return forever.
  if (to.query.login_return === '1') {
    try {
      await queryClient.fetchQuery(geeksHackingPortalApiEndpointsAuthWhoAmIEndpointQueryOptions())
      const query = { ...to.query }
      delete query.login_return
      delete query.login_handoff
      return navigateTo({ path: to.path, query, replace: true })
    }
    catch (error) {
      const status = getErrorStatusCode(error)
      if (status && status !== 401)
        return

      return navigateTo({ path: '/login', query: { error: 'session' } })
    }
  }

  // Define public routes that don't require authentication
  const publicRoutes = ['/', '/login']
  const isPublicRoute = publicRoutes.includes(to.path) || to.path.startsWith('/workshops/')

  if (isPublicRoute) {
    return
  }

  // API OIDC endpoints must not be bounced back to /auth/login from the frontend.
  // That round-trip is a login callback loop when a GitHub return lands here.
  if (to.path === '/connect' || to.path.startsWith('/connect/')) {
    return
  }

  // Check if this is a registration route - these should be public
  const isRegistrationRoute = to.path.match(/^\/[^/]+\/registration/)

  if (isRegistrationRoute) {
    return
  }

  // All other routes require authentication
  // This includes:
  // - /dash and /dash/*
  // - /[hackathonId] and /[hackathonId]/* (except registration)
  try {
    await queryClient.fetchQuery(geeksHackingPortalApiEndpointsAuthWhoAmIEndpointQueryOptions())
  }
  catch (error) {
    const status = getErrorStatusCode(error)
    if (status && status !== 401) {
      return
    }

    // User is not authenticated, redirect to login with return URL
    const loginUrl = `${config.public.api}/auth/login?redirect_uri=${encodeURIComponent(to.fullPath)}`
    return navigateTo(loginUrl, { external: true })
  }
})

function getErrorStatusCode(error: unknown): number | null {
  if (!error || typeof error !== 'object')
    return null

  const unknownError = error as {
    responseStatusCode?: unknown
    statusCode?: unknown
    status?: unknown
    response?: { status?: unknown }
  }

  if (typeof unknownError.responseStatusCode === 'number')
    return unknownError.responseStatusCode
  if (typeof unknownError.statusCode === 'number')
    return unknownError.statusCode
  if (typeof unknownError.status === 'number')
    return unknownError.status
  if (typeof unknownError.response?.status === 'number')
    return unknownError.response.status
  return null
}
