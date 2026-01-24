export default defineNuxtRouteMiddleware(async (to) => {
  const { $apiClient } = useNuxtApp()

  // Check if this is a hackathon route (matches /[hackathonId] or /[hackathonId]/*)
  const pathSegments = to.path.split('/').filter(Boolean)

  // Skip if no path segments
  if (pathSegments.length === 0) return

  const firstSegment = pathSegments[0]

  // Skip known non-hackathon routes
  if (firstSegment === 'dash' || firstSegment === 'login') return

  // Extract hackathon ID from the first path segment
  const hackathonId = firstSegment

  // Validate that it looks like a hackathon ID (not empty)
  if (!hackathonId) return

  try {
    // Try to fetch the hackathon to verify it exists
    await $apiClient.participants.hackathons.byHackathonIdOrShortCodeId(hackathonId).get()
  }
  catch (error: any) {
    // Check multiple possible error structures from the API client
    const status = error?.status || error?.response?.status || error?.statusCode
    const isNotFound = status === 404 ||
                       error?.message?.includes('404') ||
                       error?.message?.includes('not found')

    if (isNotFound) {
      throw createError({
        statusCode: 404,
        statusMessage: 'Hackathon not found',
        message: 'The hackathon you\'re looking for doesn\'t exist.',
      })
    }

    // For other errors, rethrow to prevent access
    throw createError({
      statusCode: error?.status || 500,
      statusMessage: 'Error loading hackathon',
      message: 'Unable to load hackathon information.',
    })
  }
})
