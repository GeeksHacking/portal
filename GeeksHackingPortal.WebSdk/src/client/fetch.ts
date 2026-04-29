import axios from 'axios'

export type RequestConfig<TData = unknown> = {
  baseURL?: string
  url?: string
  method?: 'GET' | 'PUT' | 'PATCH' | 'POST' | 'DELETE' | 'OPTIONS' | 'HEAD'
  params?: unknown
  data?: TData | FormData
  responseType?: 'arraybuffer' | 'blob' | 'document' | 'json' | 'text' | 'stream'
  signal?: AbortSignal
  withCredentials?: boolean
  validateStatus?: (status: number) => boolean
  headers?: import('axios').AxiosRequestConfig['headers']
}

export type ResponseConfig<TData = unknown> = {
  data: TData
  status: number
  statusText: string
  headers: import('axios').AxiosResponse['headers']
}

export type ResponseErrorConfig<TError = unknown> = import('axios').AxiosError<TError>

export type Client = <TData, _TError = unknown, TVariables = unknown>(
  config: RequestConfig<TVariables>,
) => Promise<ResponseConfig<TData>>

type ConfigurableClient = Client & {
  getConfig: typeof getConfig
  setConfig: typeof setConfig
}

let config: Partial<RequestConfig> = {}

export const getConfig = () => config

export const setConfig = (nextConfig: RequestConfig) => {
  config = nextConfig
  axiosInstance.defaults = {
    ...axiosInstance.defaults,
    ...nextConfig,
    headers: {
      ...axiosInstance.defaults.headers,
      ...nextConfig.headers,
    },
  }

  return getConfig()
}

export const mergeConfig = <T extends RequestConfig>(...configs: Array<Partial<T>>): Partial<T> => {
  return configs.reduce<Partial<T>>((merged, current) => ({
    ...merged,
    ...current,
    headers: {
      ...merged.headers,
      ...current.headers,
    },
  }), {})
}

export const axiosInstance = axios.create(getConfig())

export const fetch = (async <TData, TError = unknown, TVariables = unknown>(
  requestConfig: RequestConfig<TVariables>,
): Promise<ResponseConfig<TData>> => {
  return axiosInstance.request<TData, ResponseConfig<TData>>(mergeConfig(getConfig(), requestConfig)).catch((error: import('axios').AxiosError<TError>) => {
    throw error
  })
}) as ConfigurableClient

fetch.getConfig = getConfig
fetch.setConfig = setConfig

export const client = fetch

export default fetch
