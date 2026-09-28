import { setConfig } from '@geekshacking/portal-sdk/client'

export default defineNuxtPlugin(() => {
  const runtimeConfig = useRuntimeConfig()

  setConfig({
    baseURL: runtimeConfig.public.api,
    credentials: 'include',
  })
})
