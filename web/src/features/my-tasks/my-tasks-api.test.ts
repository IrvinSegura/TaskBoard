import { dueGroup, summarize, type MyCard } from './my-tasks-api'

// Tuesday 27 October 2026, 15:00 local time.
const now = new Date(2026, 9, 27, 15, 0)
const at = (day: number, hour = 12) => new Date(2026, 9, day, hour).toISOString()

describe('dueGroup', () => {
  it('puts a passed date in overdue, even earlier the same day', () => {
    expect(dueGroup(at(27, 9), now)).toBe('overdue')
  })

  it('splits the rest by whole local days', () => {
    expect(dueGroup(at(27, 23), now)).toBe('today')
    expect(dueGroup(at(28), now)).toBe('week')
    expect(dueGroup(at(3 + 31), now)).toBe('later')
  })

  it('has a group for cards without a date', () => {
    expect(dueGroup(null, now)).toBe('none')
  })
})

describe('summarize', () => {
  it('counts overdue cards and the ones due this week', () => {
    const card = (dueDate: string | null) => ({ dueDate }) as MyCard
    expect(summarize([card(at(26)), card(at(27, 20)), card(at(30)), card(null)], now)).toEqual({
      total: 4,
      overdue: 1,
      thisWeek: 2,
    })
  })
})
