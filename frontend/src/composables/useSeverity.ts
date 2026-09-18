/**
 * Central place for the severity → colour mapping used across the map UI.
 * Values follow a five-step gradient from green (least severe) to red (most).
 * Both the marker fill and any UI legend / badge should read from here so
 * the palette stays consistent.
 */
export const severityColors: Record<number, string> = {
  1: '#22c55e', // green-500 — reported / minor
  2: '#a3e635', // lime-400
  3: '#facc15', // yellow-400
  4: '#f97316', // orange-500
  5: '#ef4444', // red-500 — most severe
}

export const severityLabels: Record<number, string> = {
  1: 'Minor',
  2: 'Low',
  3: 'Moderate',
  4: 'High',
  5: 'Critical',
}

export function colorForSeverity(severity: number): string {
  return severityColors[severity] ?? severityColors[3]!
}

export function labelForSeverity(severity: number): string {
  return severityLabels[severity] ?? `Severity ${severity}`
}
