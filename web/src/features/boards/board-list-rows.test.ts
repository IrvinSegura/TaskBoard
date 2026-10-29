import { boardRows, nextSort, sortRows, type CardRow } from './board-list-rows'
import type { BoardDetail, CardSummary } from './boards-api'

function card(id: string, overrides: Partial<CardSummary> = {}): CardSummary {
  return {
    id,
    title: id,
    priority: 'None',
    dueDate: null,
    position: 0,
    assigneeIds: [],
    labelIds: [],
    checklist: null,
    commentCount: 0,
    attachmentCount: 0,
    ...overrides,
  }
}

const board: BoardDetail = {
  id: 'board',
  name: 'Board',
  visibility: 'Team',
  members: [],
  labels: [],
  columns: [
    {
      id: 'todo',
      name: 'Por hacer',
      position: 1,
      cards: [
        card('b', { priority: 'High', dueDate: '2026-11-10T00:00:00Z', checklist: { done: 1, total: 4 } }),
        card('a', { priority: 'Low' }),
      ],
    },
    {
      id: 'done',
      name: 'Hecho',
      position: 2,
      cards: [card('c', { priority: 'Urgent', dueDate: '2026-11-01T00:00:00Z', checklist: { done: 2, total: 2 } })],
    },
  ],
}

const ids = (rows: CardRow[]) => rows.map((row) => row.card.id)

describe('boardRows', () => {
  it('lists the cards column by column with their column', () => {
    const rows = boardRows(board)
    expect(ids(rows)).toEqual(['b', 'a', 'c'])
    expect(rows[2]).toMatchObject({ columnId: 'done', columnName: 'Hecho', order: 2 })
  })
})

describe('sortRows', () => {
  const rows = boardRows(board)

  it('sorts titles alphabetically both ways', () => {
    expect(ids(sortRows(rows, { key: 'title', direction: 'asc' }))).toEqual(['a', 'b', 'c'])
    expect(ids(sortRows(rows, { key: 'title', direction: 'desc' }))).toEqual(['c', 'b', 'a'])
  })

  it('sorts by priority', () => {
    expect(ids(sortRows(rows, { key: 'priority', direction: 'desc' }))).toEqual(['c', 'b', 'a'])
  })

  it('keeps cards without a due date or checklist last in both directions', () => {
    expect(ids(sortRows(rows, { key: 'dueDate', direction: 'asc' }))).toEqual(['c', 'b', 'a'])
    expect(ids(sortRows(rows, { key: 'dueDate', direction: 'desc' }))).toEqual(['b', 'c', 'a'])
    expect(ids(sortRows(rows, { key: 'checklist', direction: 'asc' }))).toEqual(['b', 'c', 'a'])
  })
})

describe('nextSort', () => {
  it('flips the direction of the sorted column', () => {
    expect(nextSort({ key: 'title', direction: 'asc' }, 'title')).toEqual({ key: 'title', direction: 'desc' })
  })

  it('starts priority highest first and titles from A', () => {
    expect(nextSort({ key: 'title', direction: 'asc' }, 'priority')).toEqual({ key: 'priority', direction: 'desc' })
    expect(nextSort({ key: 'priority', direction: 'desc' }, 'title')).toEqual({ key: 'title', direction: 'asc' })
  })
})
