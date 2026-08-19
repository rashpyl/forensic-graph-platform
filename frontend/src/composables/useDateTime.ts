/**
 * Formats an ISO-8601 UTC datetime for display in the map UI using the
 * European day-first convention (dd.MM.yyyy HH:mm). Falls back to the raw
 * string if parsing fails.
 */
export function formatDateTimeEU(iso: string): string {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) {
    return iso
  }
  const dd = String(d.getDate()).padStart(2, '0')
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const yyyy = d.getFullYear()
  const hh = String(d.getHours()).padStart(2, '0')
  const min = String(d.getMinutes()).padStart(2, '0')
  return `${dd}.${mm}.${yyyy} ${hh}:${min}`
}

/**
 * Converts a browser <c>&lt;input type="datetime-local"&gt;</c> value
 * (local time, no timezone) into the ISO-8601 UTC string the backend
 * expects. Returns null if the input is empty.
 */
export function localDateTimeToUtcIso(value: string): string | null {
  if (!value) return null
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return null
  return d.toISOString()
}
