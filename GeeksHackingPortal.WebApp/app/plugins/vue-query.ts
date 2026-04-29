import type {
  DehydratedState,
  VueQueryPluginOptions,
} from '@tanstack/vue-query'
import { setConfig } from '@geekshacking/portal-sdk/client/fetch'
import {
  dehydrate,
  hydrate,
  QueryClient,
  VueQueryPlugin,
} from '@tanstack/vue-query'

export default defineNuxtPlugin((nuxt) => {
  const runtimeConfig = useRuntimeConfig()
  setConfig({
    baseURL: runtimeConfig.public.api,
    withCredentials: true,
  })

  const vueQueryState = useState<DehydratedState | null>('vue-query')

  const queryClient = new QueryClient({
    defaultOptions: { queries: { staleTime: 5000 } },
  })
  const options: VueQueryPluginOptions = { queryClient }

  nuxt.vueApp.use(VueQueryPlugin, options)

  if (import.meta.server) {
    nuxt.hooks.hook('app:rendered', () => {
      vueQueryState.value = dehydrate(queryClient)
    })
  }

  if (import.meta.client) {
    hydrate(queryClient, vueQueryState.value)
  }
})
