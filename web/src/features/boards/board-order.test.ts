import { neighborsOf, positionBetween } from './board-order'

describe('neighborsOf', () => {
  it('finds both neighbors in the middle', () => {
    expect(neighborsOf(['a', 'b', 'c'], 'b')).toEqual({ previousId: 'a', nextId: 'c' })
  })

  it('has no previous at the start and no next at the end', () => {
    expect(neighborsOf(['a', 'b'], 'a')).toEqual({ previousId: null, nextId: 'b' })
    expect(neighborsOf(['a', 'b'], 'b')).toEqual({ previousId: 'a', nextId: null })
  })

  it('has no neighbors when alone', () => {
    expect(neighborsOf(['a'], 'a')).toEqual({ previousId: null, nextId: null })
  })

  it('throws for an id that is not in the list', () => {
    expect(() => neighborsOf(['a'], 'z')).toThrow()
  })
})

describe('positionBetween', () => {
  const items = [
    { id: 'a', position: 1000 },
    { id: 'b', position: 2000 },
  ]

  it('takes the midpoint between two neighbors', () => {
    expect(positionBetween(items, { previousId: 'a', nextId: 'b' })).toBe(1500)
  })

  it('steps past the ends', () => {
    expect(positionBetween(items, { previousId: 'b', nextId: null })).toBe(3024)
    expect(positionBetween(items, { previousId: null, nextId: 'a' })).toBe(-24)
  })

  it('starts an empty list at one step', () => {
    expect(positionBetween([], { previousId: null, nextId: null })).toBe(1024)
  })
})
