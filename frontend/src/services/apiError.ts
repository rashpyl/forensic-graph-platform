import axios from 'axios'

/**
 * Extracts a human-readable message from an axios error whose body follows
 * ASP.NET Core's RFC 7807 ProblemDetails shape:
 *   { title, detail, errors?: { field: string[] } }
 * FluentValidation surfaces field-level messages under <c>errors</c>; the
 * rest of the API (NotFoundException, ConflictException, 500s) uses
 * <c>title</c> + <c>detail</c>. We flatten everything into a single string
 * suitable for an inline error banner.
 */
export function formatApiError(err: unknown): string {
  if (!axios.isAxiosError(err)) {
    return err instanceof Error ? err.message : 'Unexpected error.'
  }

  const data = err.response?.data as
    | { title?: string; detail?: string; errors?: Record<string, string[]> }
    | undefined

  if (data?.errors) {
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
