import { cardPriorities, type BoardDetail, type CardSummary } from './boards-api'

/** A card with the column it sits in, as one row of the list view. */
export interface CardRow {
  card: CardSummary
  columnId: string
  columnName: string
  /** Board order: the column's index, then the card's position inside it. */
  order: number
}

export type ListSortKey = 'title' | 'column' | 'priority' | 'dueDate' | 'checklist'
export type SortDirection = 'asc' | 'desc'

export interface ListSort {
  key: ListSortKey
  direction: SortDirection
}

export const defaultListSort: ListSort = { key: 'column', direction: 'asc' }

/** Every card of the board, column by column, in the order the canvas shows them. */
export function boardRows(board: BoardDetail): CardRow[] {
  let order = 0
  return board.columns.flatMap((column) =>
    column.cards.map((card) => ({ card, columnId: column.id, columnName: column.name, order: order++ })),
  )
}

const collator = new Intl.Collator('es', { sensitivity: 'base', numeric: true })

/** The value a row sorts by, or null when it has none (those rows go last in both directions). */
function sortValue(row: CardRow, key: ListSortKey): number | string | null {
  switch (key) {
    case 'title':
      return row.card.title
    case 'column':
      return row.order
    case 'priority':
      return cardPriorities.indexOf(row.card.priority)
    case 'dueDate':
      return row.card.dueDate ? new Date(row.card.dueDate).getTime() : null
    case 'checklist':
      return row.card.checklist && row.card.checklist.total > 0
        ? row.card.checklist.done / row.card.checklist.total
        : null
  }
}

export function sortRows(rows: CardRow[], sort: ListSort): CardRow[] {
  const sign = sort.direction === 'asc' ? 1 : -1
  return [...rows].sort((a, b) => {
    const left = sortValue(a, sort.key)
    const right = sortValue(b, sort.key)
    if (left === null || right === null) {
      if (left !== right) {
        return left === null ? 1 : -1
      }
    } else {
      const compared =
        typeof left === 'string' ? collator.compare(left, right as string) : left - (right as number)
      if (compared !== 0) {
        return compared * sign
      }
    }
    // Ties keep the board order.
    return a.order - b.order
  })
}

/** Clicking the sorted column flips it; clicking another sorts by it (priority and progress highest first). */
export function nextSort(current: ListSort, key: ListSortKey): ListSort {
  if (current.key === key) {
    return { key, direction: current.direction === 'asc' ? 'desc' : 'asc' }
  }
  return { key, direction: key === 'priority' || key === 'checklist' ? 'desc' : 'asc' }
}
