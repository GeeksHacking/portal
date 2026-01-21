import { AnonymousAuthenticationProvider } from '@microsoft/kiota-abstractions'
import { FetchRequestAdapter, HttpClient } from '@microsoft/kiota-http-fetchlibrary'
import { createApiClient } from '~/api-client/apiClient'

export default defineNuxtPlugin(() => {
  const config = useRuntimeConfig()
  const authProvider = new AnonymousAuthenticationProvider()
  const httpClient = new HttpClient(async (url, init) => {
    const response = await fetch(url, {
      ...init,
      credentials: 'include',
    })

    return response
  })
  const adapter = new FetchRequestAdapter(authProvider, undefined, undefined, httpClient)
  adapter.baseUrl = config.public.api
  const apiClient = createApiClient(adapter)

  return {
    provide: {
      apiClient,
    },
  }
})
