import type {
  DehydratedState,
  VueQueryPluginOptions,
} from '@tanstack/vue-query'
import {
  dehydrate,
  hydrate,
  QueryClient,
  VueQueryPlugin,
} from '@tanstack/vue-query'

export default defineNuxtPlugin((nuxt) => {
  const vueQueryState = useState<DehydratedState | null>('vue-query', () => null)

  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 5000,
        // A 4xx (not found, forbidden…) won't change on a retry: answer at once. Pages use 404 as
        // a normal answer, e.g. "my team" when the user has none. Network/5xx still retry.
        retry: (failureCount, error) => {
          const status = (error as { status?: number } | null)?.status
          if (typeof status === 'number' && status >= 400 && status < 500)
            return false
          return failureCount < 3
        },
      },
    },
  })
  const options: VueQueryPluginOptions = { queryClient }

  nuxt.vueApp.use(VueQueryPlugin, options)

  if (import.meta.server) {
    nuxt.hooks.hook('app:rendered', () => {
      vueQueryState.value = dehydrate(queryClient)
    })
  }

  if (import.meta.client && vueQueryState.value) {
    hydrate(queryClient, vueQueryState.value)
  }
})
