import { client, setConfig } from '@geekshacking/portal-sdk/client'

export default defineNuxtPlugin(() => {
  const runtimeConfig = useRuntimeConfig()

  setConfig({
    baseURL: runtimeConfig.public.api,
    credentials: 'include',
  })

  // setConfig credentials is not applied by older generated clients. Force it on
  // the resolved request so cross-origin whoami actually sends the auth cookie.
  client.interceptors.request.use((request) => {
    request.credentials ??= 'include'
    return request
  })
})
