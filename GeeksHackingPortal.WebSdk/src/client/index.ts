import { client } from '../gen/.kubb/client'

export type {
  ClientConfig,
  ClientInstance,
  RequestConfig,
  ResponseError,
} from '../gen/.kubb/client'

export { client, createClient } from '../gen/.kubb/client'

export function getConfig() {
  return client.getConfig()
}

export function setConfig(config: Parameters<typeof client.setConfig>[0]) {
  return client.setConfig(config)
}
