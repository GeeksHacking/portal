export const registrationSetup = async () => {
  console.log('🚀 [REGIS-INIT] Starting registration setup...')

  const { $apiClient } = useNuxtApp()
  const hackathonId = useHackathonId()

  // THIS SHOULD NOT BE HERE but im just putting it here for now because i need hackathon id
  // REMOVE THIS FILE AFTER THERE'S ANOTHER WAY OF GETTING HACKATHON ID!!!!
  try {
    const response = await $apiClient.organizers.hackathons.get()

    const hasHackathons = response?.hackathons && response.hackathons.length > 0

    if (!hasHackathons) {
      // random ass christmas dates
      const created = await hackathonCreate.create({
        name: 'HackOMania 2026',
        shortCode: 'hackomania2026',
        description: 'HackOMania 2026 - Annual hackathon event',
        eventStartDate: new Date('2025-12-25'),
        eventEndDate: new Date('2025-12-2'),
        submissionsStartDate: new Date('2025-12-2'),
        submissionsEndDate: new Date('2025-12-2'),
        judgingStartDate: new Date('2025-12-2'),
        judgingEndDate: new Date('2025-12-2'),
        isPublished: true,
        venue: 'TBD',
        homepageUri: 'https://hackomania.geekshacking.com/',
      })

      // get this guid that will be used everywhere!!
      hackathonId.value = created?.id ?? ''
    }
    else {
      // IDK WHICH ONE SO ILL JUST USE THE FIRST ONE, but maybe ill check by year or something ?
      hackathonId.value = response?.hackathons?.[0]?.id ?? ''
    }

    // Join the hackathon as a participant BEFORE checking questions
    if (hackathonId.value) {
      try {
        await hackathonActions.join(hackathonId.value)
      }
      catch (joinError) {
        console.error('[REGIS-INIT] Error joining hackathon (may already be joined):', joinError)
      }
    }

    // Check if registration questions exist, if not initialize them
    const questionsResponse = await $apiClient.participants.hackathons
      .byHackathonIdOrShortCodeId(hackathonId.value)
      .registration.questions.get()

    const totalQuestions = questionsResponse?.categories?.reduce((sum, cat) => sum + (cat.questions?.length ?? 0), 0) ?? 0
    const hasQuestions = totalQuestions > 0

    if (!hasQuestions) {
      await questionActions.initialize(hackathonId.value)
    }

    return {
      success: true,
      hasHackathons,
      hackathonId: hackathonId.value,
      hackathons: response?.hackathons,
    }
  }
  catch (error) {
    console.error('[REGIS-INIT] Registration setup failed:', error)
    console.error('[REGIS-INIT] Error details:', JSON.stringify(error, null, 2))
    return {
      success: false,
      error,
    }
  }
}
