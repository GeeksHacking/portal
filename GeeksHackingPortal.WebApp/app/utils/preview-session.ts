const previewSessionKey = 'gh-preview-session'

let previewSessionReady: Promise<void> = Promise.resolve()

export function whenPreviewSessionReady(): Promise<void> {
  return previewSessionReady
}

export function trackPreviewSession(task: Promise<void>): Promise<void> {
  previewSessionReady = task
  return task
}

export function readPreviewSession(): string | null {
  if (!import.meta.client)
    return null
  try {
    return sessionStorage.getItem(previewSessionKey)
  }
  catch {
    return null
  }
}

export function storePreviewSession(token: string) {
  sessionStorage.setItem(previewSessionKey, token)
}
