export default defineNuxtRouteMiddleware(async (to) => {
  const { $apiClient } = useNuxtApp()
  const config = useRuntimeConfig()

  // Define public routes that don't require authentication
  const publicRoutes = ['/', '/login']
  const isPublicRoute = publicRoutes.includes(to.path)

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
    await $apiClient.auth.whoami.get()
  }
  catch (error) {
    // User is not authenticated, redirect to login with return URL
    const loginUrl = `${config.public.api}/auth/login?redirect_uri=${encodeURIComponent(to.fullPath)}`
    return navigateTo(loginUrl, { external: true })
  }
})
