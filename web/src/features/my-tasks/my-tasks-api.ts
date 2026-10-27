import { useQuery } from '@tanstack/react-query'
import type { CardPriority } from '@/features/boards/boards-api'
import type { BoardLabel } from '@/features/labels/labels-api'
import { api } from '@/lib/api/client'
import { queryKeys } from '@/lib/api/query-keys'

export interface MyCard {
  id: string
  title: string
  priority: CardPriority
  dueDate: string | null
  boardId: string
  boardName: string
  columnName: string
  teamName: string
  labels: BoardLabel[]
  checklist: { done: number; total: number } | null
}

export function useMyCards() {
  return useQuery({ queryKey: queryKeys.myCards(), queryFn: () => api.get<MyCard[]>('/me/cards') })
}

export type DueGroup = 'overdue' | 'today' | 'week' | 'later' | 'none'

export const dueGroupLabels: Record<DueGroup, string> = {
  overdue: 'Vencidas',
  today: 'Hoy',
  week: 'Próximos 7 días',
  later: 'Más adelante',
  none: 'Sin fecha',
}

/** Which bucket a due date falls in, counted in whole local days. */
export function dueGroup(dueDate: string | null, now: Date = new Date()): DueGroup {
  if (!dueDate) {
    return 'none'
  }
  const due = new Date(dueDate)
  if (due.getTime() < now.getTime()) {
    return 'overdue'
  }
  const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  const days = Math.floor((due.getTime() - startOfToday.getTime()) / (24 * 3600 * 1000))
  if (days < 1) {
    return 'today'
  }
  return days < 7 ? 'week' : 'later'
}

/** Counts for the dashboard: what is late and what comes up this week. */
export function summarize(cards: MyCard[], now: Date = new Date()) {
  const groups = cards.map((card) => dueGroup(card.dueDate, now))
  return {
    total: cards.length,
    overdue: groups.filter((g) => g === 'overdue').length,
    thisWeek: groups.filter((g) => g === 'today' || g === 'week').length,
  }
}
