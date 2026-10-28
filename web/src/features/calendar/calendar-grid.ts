// Month grids in local time. Weeks start on Monday, as is usual in Spanish-speaking countries.

export const weekdayLabels = ['lun', 'mar', 'mié', 'jue', 'vie', 'sáb', 'dom']

const monthFormat = new Intl.DateTimeFormat('es', { month: 'long', year: 'numeric' })

export interface MonthGrid {
  /** Always six weeks, so the grid keeps its height from month to month. */
  days: Date[]
  /** Start of the first day shown. */
  from: Date
  /** Start of the day after the last one shown. */
  to: Date
}

export function startOfMonth(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), 1)
}

export function addMonths(date: Date, months: number): Date {
  return new Date(date.getFullYear(), date.getMonth() + months, 1)
}

/** The 42 days around the month of `date`, from the Monday on or before its first day. */
export function monthGrid(date: Date): MonthGrid {
  const first = startOfMonth(date)
  const offset = (first.getDay() + 6) % 7
  const days = Array.from(
    { length: 42 },
    (_, i) => new Date(first.getFullYear(), first.getMonth(), 1 - offset + i),
  )
  const last = days[days.length - 1]!
  return { days, from: days[0]!, to: new Date(last.getFullYear(), last.getMonth(), last.getDate() + 1) }
}

/** yyyy-mm-dd of the local day an instant falls on; groups cards and activity by day. */
export function dayKey(value: string | Date): string {
  const date = new Date(value)
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

export function sameMonth(a: Date, b: Date): boolean {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth()
}

/** "Octubre de 2026". */
export function monthLabel(date: Date): string {
  const label = monthFormat.format(date)
  return label.charAt(0).toUpperCase() + label.slice(1)
}

/** Groups items by the local day of the date `pick` returns. */
export function groupByDay<T>(items: T[], pick: (item: T) => string): Map<string, T[]> {
  const groups = new Map<string, T[]>()
  for (const item of items) {
    const key = dayKey(pick(item))
    const group = groups.get(key)
    if (group) {
      group.push(item)
    } else {
      groups.set(key, [item])
    }
  }
  return groups
}
