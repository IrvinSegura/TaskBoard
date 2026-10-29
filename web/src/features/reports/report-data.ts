import { cardPriorities, type BoardDetail, type CardPriority } from '@/features/boards/boards-api'
import { dayKey } from '@/features/calendar/calendar-grid'

export interface ReportBoard {
  board: BoardDetail
  teamName: string
}

export interface BoardProgress {
  id: string
  name: string
  teamName: string
  total: number
  done: number
  overdue: number
}

export interface PersonLoad {
  userId: string
  name: string
  open: number
  overdue: number
}

export interface DayCount {
  day: Date
  count: number
}

export interface Report {
  total: number
  done: number
  open: number
  overdue: number
  unassigned: number
  /** Open cards by priority, highest first. */
  byPriority: { priority: CardPriority; count: number }[]
  boards: BoardProgress[]
  /** People with open cards, busiest first. */
  people: PersonLoad[]
  /** Open cards due on each of the next `upcomingDays` days, today included. */
  upcoming: DayCount[]
  checklist: { done: number; total: number }
}

export const upcomingDays = 14

/**
 * Counts across boards. A card is done when it sits in the last column of a board with two or more
 * columns (the usual "Hecho"); overdue, unassigned and the workload only count the cards still open.
 */
export function buildReport(boards: ReportBoard[], now: Date = new Date()): Report {
  const priorityCounts = new Map<CardPriority, number>(cardPriorities.map((p) => [p, 0]))
  const people = new Map<string, PersonLoad>()
  const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  const upcoming = Array.from({ length: upcomingDays }, (_, i) => ({
    day: new Date(startOfToday.getFullYear(), startOfToday.getMonth(), startOfToday.getDate() + i),
    count: 0,
  }))
  const upcomingIndex = new Map(upcoming.map((entry, i) => [dayKey(entry.day), i]))

  const report: Report = {
    total: 0,
    done: 0,
    open: 0,
    overdue: 0,
    unassigned: 0,
    byPriority: [],
    boards: [],
    people: [],
    upcoming,
    checklist: { done: 0, total: 0 },
  }

  for (const { board, teamName } of boards) {
    const progress: BoardProgress = { id: board.id, name: board.name, teamName, total: 0, done: 0, overdue: 0 }
    const names = new Map(board.members.map((m) => [m.userId, m.displayName]))
    const doneColumnId = board.columns.length > 1 ? board.columns[board.columns.length - 1]!.id : null

    for (const column of board.columns) {
      for (const card of column.cards) {
        progress.total++
        if (card.checklist) {
          report.checklist.done += card.checklist.done
          report.checklist.total += card.checklist.total
        }
        if (column.id === doneColumnId) {
          progress.done++
          continue
        }

        const overdue = card.dueDate !== null && new Date(card.dueDate).getTime() < now.getTime()
        if (overdue) {
          progress.overdue++
        }
        priorityCounts.set(card.priority, priorityCounts.get(card.priority)! + 1)
        if (card.assigneeIds.length === 0) {
          report.unassigned++
        }
        for (const userId of card.assigneeIds) {
          const person = people.get(userId) ?? { userId, name: names.get(userId) ?? 'Sin acceso', open: 0, overdue: 0 }
          person.open++
          if (overdue) {
            person.overdue++
          }
          people.set(userId, person)
        }
        const day = card.dueDate ? upcomingIndex.get(dayKey(card.dueDate)) : undefined
        if (day !== undefined && !overdue) {
          upcoming[day]!.count++
        }
      }
    }

    report.total += progress.total
    report.done += progress.done
    report.overdue += progress.overdue
    report.boards.push(progress)
  }

  report.open = report.total - report.done
  report.byPriority = [...cardPriorities].reverse().map((priority) => ({ priority, count: priorityCounts.get(priority)! }))
  report.people = [...people.values()].sort((a, b) => b.open - a.open || a.name.localeCompare(b.name, 'es'))
  report.boards.sort((a, b) => b.total - a.total || a.name.localeCompare(b.name, 'es'))
  return report
}

/** Whole percent of a part, 0 when there is nothing to divide. */
export function percent(part: number, total: number): number {
  return total === 0 ? 0 : Math.round((part / total) * 100)
}
