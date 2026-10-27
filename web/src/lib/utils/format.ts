const locale = 'es'

const relativeFormat = new Intl.RelativeTimeFormat(locale, { numeric: 'auto' })
const dateFormat = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short' })
const dateWithYearFormat = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short', year: 'numeric' })
const longDateFormat = new Intl.DateTimeFormat(locale, { dateStyle: 'long' })

const units: [Intl.RelativeTimeFormatUnit, number][] = [
  ['year', 365 * 24 * 3600],
  ['month', 30 * 24 * 3600],
  ['week', 7 * 24 * 3600],
  ['day', 24 * 3600],
  ['hour', 3600],
  ['minute', 60],
]

/** "hace 5 minutos", "ayer", "en 3 días"... */
export function formatRelative(value: string | Date, now: Date = new Date()): string {
  const seconds = (new Date(value).getTime() - now.getTime()) / 1000
  for (const [unit, size] of units) {
    if (Math.abs(seconds) >= size) {
      return relativeFormat.format(Math.round(seconds / size), unit)
    }
  }
  return 'justo ahora'
}

export function formatShortDate(value: string | Date, now: Date = new Date()): string {
  const date = new Date(value)
  return date.getFullYear() === now.getFullYear() ? dateFormat.format(date) : dateWithYearFormat.format(date)
}

export function formatLongDate(value: string | Date): string {
  return longDateFormat.format(new Date(value))
}

export function formatBytes(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`
  }
  const kb = bytes / 1024
  if (kb < 1024) {
    return `${kb.toFixed(kb < 10 ? 1 : 0)} KB`
  }
  const mb = kb / 1024
  return `${mb.toFixed(mb < 10 ? 1 : 0)} MB`
}

export function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) {
    return '?'
  }
  const first = parts[0]!.charAt(0)
  const last = parts.length > 1 ? parts[parts.length - 1]!.charAt(0) : ''
  return (first + last).toUpperCase()
}

export type DueStatus = 'overdue' | 'soon' | 'later'

/** Overdue once the date has passed, "soon" within the next 48 hours. */
export function dueStatus(dueDate: string, now: Date = new Date()): DueStatus {
  const remaining = new Date(dueDate).getTime() - now.getTime()
  if (remaining < 0) {
    return 'overdue'
  }
  return remaining <= 48 * 3600 * 1000 ? 'soon' : 'later'
}

/** The yyyy-mm-dd value of an <input type="date"> for an ISO date, in local time. */
export function toDateInputValue(value: string | null | undefined): string {
  if (!value) {
    return ''
  }
  const date = new Date(value)
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

/** End of the chosen local day, so a card due "today" is not overdue until the day is over. */
export function fromDateInputValue(value: string): string | null {
  if (!value) {
    return null
  }
  const [year, month, day] = value.split('-').map(Number)
  return new Date(year!, month! - 1, day!, 23, 59, 59).toISOString()
}
