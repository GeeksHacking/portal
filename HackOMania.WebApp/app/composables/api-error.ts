type ErrorMessageValue = string | string[] | null | undefined

type FastEndpointsErrorLike = {
  messageEscaped?: string | null
  errors?: {
    additionalData?: Record<string, ErrorMessageValue>
  } | null
}

function toErrorMessageList(value: ErrorMessageValue): string[] {
  if (Array.isArray(value))
    return value.map(message => String(message)).filter(Boolean)

  if (typeof value === 'string' && value.trim())
    return [value]

  return []
}

function toFastEndpointsError(error: unknown): FastEndpointsErrorLike | null {
  if (!error || typeof error !== 'object')
    return null

  return error as FastEndpointsErrorLike
}

export function getFastEndpointsErrorBag(error: unknown): Record<string, string[]> {
  const errorBag = toFastEndpointsError(error)?.errors?.additionalData ?? {}

  return Object.fromEntries(
    Object.entries(errorBag)
      .map(([key, value]) => [key, toErrorMessageList(value)])
      .filter(([, messages]) => messages.length > 0),
  )
}

export function getFastEndpointsErrorMessage(
  error: unknown,
  preferredFields: string[] = [],
): string | null {
  const errorBag = getFastEndpointsErrorBag(error)

  for (const field of preferredFields) {
    const message = errorBag[field]?.[0]
    if (message)
      return message
  }

  const firstFieldMessage = Object.values(errorBag)[0]?.[0]
  if (firstFieldMessage)
    return firstFieldMessage

  const message = toFastEndpointsError(error)?.messageEscaped?.trim()
  if (message)
    return message

  return null
}
