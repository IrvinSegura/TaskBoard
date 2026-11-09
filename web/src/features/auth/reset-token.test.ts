import { describe, expect, it } from 'vitest'
import { readResetToken } from './reset-token'

describe('readResetToken', () => {
  it('reads the token from the fragment', () => {
    expect(readResetToken('#token=abc_-123')).toBe('abc_-123')
  })

  it('returns null when there is no token', () => {
    expect(readResetToken('')).toBeNull()
    expect(readResetToken('#')).toBeNull()
    expect(readResetToken('#token=')).toBeNull()
    expect(readResetToken('#other=1')).toBeNull()
  })
})
