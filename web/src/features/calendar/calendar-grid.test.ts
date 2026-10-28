import { describe, expect, it } from 'vitest'
import { addMonths, dayKey, groupByDay, monthGrid, monthLabel } from './calendar-grid'

describe('monthGrid', () => {
  it('starts on the Monday on or before the first day and spans six weeks', () => {
    // October 2026 starts on a Thursday.
    const grid = monthGrid(new Date(2026, 9, 27))

    expect(grid.days).toHaveLength(42)
    expect(dayKey(grid.from)).toBe('2026-09-28')
    expect(grid.from.getDay()).toBe(1)
    expect(dayKey(grid.days[41]!)).toBe('2026-11-08')
    expect(dayKey(grid.to)).toBe('2026-11-09')
  })

  it('starts on the first day when the month begins on a Monday', () => {
    // June 2026 starts on a Monday.
    expect(dayKey(monthGrid(new Date(2026, 5, 15)).from)).toBe('2026-06-01')
  })
})

describe('addMonths', () => {
  it('moves to the first day of another month, across years', () => {
    expect(dayKey(addMonths(new Date(2026, 11, 31), 1))).toBe('2027-01-01')
    expect(dayKey(addMonths(new Date(2026, 0, 31), -1))).toBe('2025-12-01')
  })
})

describe('groupByDay', () => {
  it('groups by the local day of each date', () => {
    const items = [
      { id: 1, at: new Date(2026, 9, 27, 9).toISOString() },
      { id: 2, at: new Date(2026, 9, 27, 23, 59).toISOString() },
      { id: 3, at: new Date(2026, 9, 28, 0, 1).toISOString() },
    ]

    const groups = groupByDay(items, (item) => item.at)

    expect(groups.get('2026-10-27')?.map((item) => item.id)).toEqual([1, 2])
    expect(groups.get('2026-10-28')?.map((item) => item.id)).toEqual([3])
  })
})

describe('monthLabel', () => {
  it('capitalizes the month name', () => {
    expect(monthLabel(new Date(2026, 9, 1))).toBe('Octubre de 2026')
  })
})
