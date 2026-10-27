import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'
import { ApiError, errorMessage } from './api/api-error'

/**
 * Puts the API's validation messages on the matching fields. Returns the message for the form itself
 * when the error does not belong to any field, or null when every message found a field.
 */
export function applyServerErrors<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
  fields: readonly Path<T>[],
): string | null {
  if (!(error instanceof ApiError)) {
    return errorMessage(error)
  }
  let matched = false
  for (const field of fields) {
    const message = error.fieldErrors[field]
    if (message) {
      setError(field, { type: 'server', message })
      matched = true
    }
  }
  return matched ? null : error.message
}
