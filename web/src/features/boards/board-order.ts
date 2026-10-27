/**
 * The API places a card or column between two neighbors instead of taking an index:
 * these helpers turn the order the user sees into those neighbor ids, and back into positions.
 */

export interface Neighbors {
  previousId: string | null
  nextId: string | null
}

/** Neighbors of `id` in `orderedIds`, the list as it looks after the move. */
export function neighborsOf(orderedIds: readonly string[], id: string): Neighbors {
  const index = orderedIds.indexOf(id)
  if (index === -1) {
    throw new Error(`${id} is not in the list`)
  }
  return {
    previousId: index > 0 ? orderedIds[index - 1]! : null,
    nextId: index < orderedIds.length - 1 ? orderedIds[index + 1]! : null,
  }
}

// PositionCalculator.Step in the API.
const step = 1024

interface Positioned {
  id: string
  position: number
}

/**
 * A provisional position between the neighbors for the optimistic update; the server answers with the real one.
 * Matches PositionCalculator: one step (1024) past the ends, the midpoint in between.
 */
export function positionBetween<T extends Positioned>(items: readonly T[], neighbors: Neighbors): number {
  const previous = items.find((item) => item.id === neighbors.previousId)?.position
  const next = items.find((item) => item.id === neighbors.nextId)?.position
  if (previous !== undefined && next !== undefined) {
    return (previous + next) / 2
  }
  if (previous !== undefined) {
    return previous + step
  }
  if (next !== undefined) {
    return next - step
  }
  return step
}

export function byPosition<T extends { position: number }>(a: T, b: T): number {
  return a.position - b.position
}
