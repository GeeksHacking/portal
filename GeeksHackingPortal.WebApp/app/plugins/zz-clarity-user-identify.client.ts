declare global {
  interface Window {
    clarity?: (...args: [string, ...unknown[]]) => void
  }
}

export default defineNuxtPlugin((nuxtApp) => {
  nuxtApp.hook('app:mounted', async () => {
    // Skip identification when Clarity is unavailable (blocked script, CSP, etc.)
    if (typeof window.clarity !== 'function') {
      return
    }

    try {
      const { $apiClient } = useNuxtApp()
      const user = await $apiClient.auth.whoami.get()
      const userId = user?.id

      if (userId) {
        window.clarity('identify', userId)
      }
    }
    catch {
      // User is likely not authenticated; keep Clarity in anonymous mode.
    }
  })
})
