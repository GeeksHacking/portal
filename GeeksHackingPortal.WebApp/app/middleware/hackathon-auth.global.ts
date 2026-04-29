import { geeksHackingPortalApiEndpointsAuthWhoAmIEndpoint } from '@geekshacking/portal-sdk/hooks'

export default defineNuxtRouteMiddleware(async (to) => {
  const config = useRuntimeConfig()

  // Define public routes that don't require authentication
  const publicRoutes = ['/', '/login']
  const isPublicRoute = publicRoutes.includes(to.path) || to.path.startsWith('/workshops/')

  if (isPublicRoute) {
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
    await geeksHackingPortalApiEndpointsAuthWhoAmIEndpoint()
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
