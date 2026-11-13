import axios from 'axios'

/**
 * Turns an axios error into a single message for an inline error banner.
 * The API returns ProblemDetails: `{ title, detail, errors? }`, where `errors`
 * is either a map of field to messages (validation) or a plain list (password policy).
 */
export function formatApiError(err: unknown): string {
  if (!axios.isAxiosError(err)) {
    return err instanceof Error ? err.message : 'Unexpected error.'
  }

  const data = err.response?.data as
    | { title?: string; detail?: string; errors?: Record<string, string[]> | string[] }
    | undefined

  if (Array.isArray(data?.errors) && data.errors.length > 0) {
    return data.errors.join('\n')
  }

  if (data?.errors && !Array.isArray(data.errors)) {
    const parts: string[] = []
    for (const [field, msgs] of Object.entries(data.errors)) {
      for (const msg of msgs) {
        parts.push(`${field}: ${msg}`)
      }
    }
    if (parts.length > 0) {
      return parts.join('\n')
    }
  }

  if (data?.detail) return data.detail
  if (data?.title) return data.title
  return err.message
}
