import { dueStatus, formatBytes, formatRelative, fromDateInputValue, initials, toDateInputValue } from './format'

describe('formatRelative', () => {
  const now = new Date('2026-10-26T12:00:00Z')

  it('says "justo ahora" for less than a minute', () => {
    expect(formatRelative('2026-10-26T11:59:40Z', now)).toBe('justo ahora')
  })

  it('uses the largest unit that fits', () => {
    expect(formatRelative('2026-10-26T09:00:00Z', now)).toBe('hace 3 horas')
    expect(formatRelative('2026-10-25T12:00:00Z', now)).toBe('ayer')
  })
})

describe('formatBytes', () => {
  it.each([
    [512, '512 B'],
    [1536, '1.5 KB'],
    [200 * 1024, '200 KB'],
    [3 * 1024 * 1024, '3.0 MB'],
  ])('formats %d as %s', (bytes, expected) => {
    expect(formatBytes(bytes)).toBe(expected)
  })
})

describe('initials', () => {
  it('takes the first letter of the first and last word', () => {
    expect(initials('Ana María López')).toBe('AL')
    expect(initials('irvin')).toBe('I')
    expect(initials('   ')).toBe('?')
  })
})

describe('dueStatus', () => {
  const now = new Date('2026-10-26T12:00:00Z')

  it('classifies past, near and far dates', () => {
    expect(dueStatus('2026-10-25T12:00:00Z', now)).toBe('overdue')
    expect(dueStatus('2026-10-27T12:00:00Z', now)).toBe('soon')
    expect(dueStatus('2026-11-10T12:00:00Z', now)).toBe('later')
  })
})

describe('date inputs', () => {
  it('round-trips a day through the end of that local day', () => {
    const iso = fromDateInputValue('2026-11-03')!
    expect(new Date(iso).getHours()).toBe(23)
    expect(toDateInputValue(iso)).toBe('2026-11-03')
  })

  it('maps empty values to null and back', () => {
    expect(fromDateInputValue('')).toBeNull()
    expect(toDateInputValue(null)).toBe('')
  })
})
