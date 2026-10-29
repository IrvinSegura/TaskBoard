import type { BoardDetail, CardSummary } from '@/features/boards/boards-api'
import { buildReport, percent, upcomingDays } from './report-data'

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

const now = new Date(2026, 9, 29, 12)
const local = (day: number) => new Date(2026, 9, day, 18).toISOString()

const board: BoardDetail = {
  id: 'b1',
  name: 'Lanzamiento',
  visibility: 'Team',
  members: [
    { userId: 'ana', displayName: 'Ana', role: 'Admin' },
    { userId: 'luis', displayName: 'Luis', role: 'Editor' },
  ],
  labels: [],
  columns: [
    {
      id: 'todo',
      name: 'Por hacer',
      position: 1,
      cards: [
        card('late', { priority: 'High', dueDate: local(20), assigneeIds: ['ana'] }),
        card('soon', { priority: 'Urgent', dueDate: local(30), assigneeIds: ['ana', 'luis'] }),
        card('free', { checklist: { done: 1, total: 3 } }),
      ],
    },
    {
      id: 'done',
      name: 'Hecho',
      position: 2,
      // Done cards never count as overdue or as open work.
      cards: [card('shipped', { priority: 'High', dueDate: local(1), assigneeIds: ['luis'], checklist: { done: 2, total: 2 } })],
    },
  ],
}

describe('buildReport', () => {
  const report = buildReport([{ board, teamName: 'Producto' }], now)

  it('counts done cards in the last column and the rest as open', () => {
    expect(report).toMatchObject({ total: 4, done: 1, open: 3, overdue: 1, unassigned: 1 })
    expect(report.boards).toEqual([{ id: 'b1', name: 'Lanzamiento', teamName: 'Producto', total: 4, done: 1, overdue: 1 }])
  })

  it('splits open cards by priority, highest first', () => {
    expect(report.byPriority.map((p) => p.priority)).toEqual(['Urgent', 'High', 'Medium', 'Low', 'None'])
    expect(report.byPriority.map((p) => p.count)).toEqual([1, 1, 0, 0, 1])
  })

  it('ranks people by their open cards', () => {
    expect(report.people).toEqual([
      { userId: 'ana', name: 'Ana', open: 2, overdue: 1 },
      { userId: 'luis', name: 'Luis', open: 1, overdue: 0 },
    ])
  })

  it('counts upcoming due dates from today on', () => {
    expect(report.upcoming).toHaveLength(upcomingDays)
    expect(report.upcoming[1]!.count).toBe(1)
    expect(report.upcoming.reduce((sum, day) => sum + day.count, 0)).toBe(1)
  })

  it('adds up checklist items of every card', () => {
    expect(report.checklist).toEqual({ done: 3, total: 5 })
  })

  it('treats a single-column board as all open', () => {
    const single = buildReport([{ board: { ...board, columns: [board.columns[1]!] }, teamName: 'Producto' }], now)
    expect(single).toMatchObject({ total: 1, done: 0, open: 1 })
  })
})

describe('percent', () => {
  it('rounds and avoids dividing by zero', () => {
    expect(percent(1, 3)).toBe(33)
    expect(percent(0, 0)).toBe(0)
  })
})
