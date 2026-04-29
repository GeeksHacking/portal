type ValidationErrorValue = string[] | string
type ValidationErrorBag = Record<string, ValidationErrorValue>

type ApiErrorPayload = {
  message?: unknown
  reason?: unknown
  title?: unknown
  errors?: unknown
}

type ApiErrorLike = {
  message?: unknown
  data?: ApiErrorPayload
  response?: {
    data?: ApiErrorPayload
  }
  errors?: unknown
}

function isValidationErrorBag(value: unknown): value is ValidationErrorBag {
  if (!value || typeof value !== 'object' || Array.isArray(value))
    return false

  return Object.values(value).every(entry => typeof entry === 'string' || (Array.isArray(entry) && entry.every(item => typeof item === 'string')))
}

function normalizeValidationErrors(value: unknown): ValidationErrorBag {
  if (!value || typeof value !== 'object')
    return {}

  const candidate = value as {
    additionalData?: unknown
  }

  if (isValidationErrorBag(candidate.additionalData))
    return candidate.additionalData

  if (isValidationErrorBag(value))
    return value

  return {}
}

export function getApiErrorPayload(error: unknown): ApiErrorPayload | undefined {
  if (!error || typeof error !== 'object')
    return undefined

  const candidate = error as ApiErrorLike
  return candidate.response?.data ?? candidate.data
}

export function getApiValidationErrors(error: unknown): ValidationErrorBag {
  const payload = getApiErrorPayload(error)
  if (payload?.errors)
    return normalizeValidationErrors(payload.errors)

  if (error && typeof error === 'object') {
    const candidate = error as ApiErrorLike
    return normalizeValidationErrors(candidate.errors)
  }

  return {}
}

export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const candidate = error as ApiErrorLike
    if (typeof candidate.message === 'string' && candidate.message.trim())
      return candidate.message
  }

  const payload = getApiErrorPayload(error)
  if (payload) {
    if (typeof payload.message === 'string' && payload.message.trim())
      return payload.message
    if (typeof payload.reason === 'string' && payload.reason.trim())
      return payload.reason
    if (typeof payload.title === 'string' && payload.title.trim())
      return payload.title

    const firstFieldError = Object.values(getApiValidationErrors(error)).flatMap(value =>
      Array.isArray(value) ? value : [value],
    )[0]

    if (typeof firstFieldError === 'string' && firstFieldError.trim())
      return firstFieldError
  }

  return fallback
}
