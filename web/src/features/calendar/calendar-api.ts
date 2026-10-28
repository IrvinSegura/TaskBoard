import { keepPreviousData, useQuery } from '@tanstack/react-query'
import type { ActivityEntry } from '@/features/activity/activity-api'
import type { CardPriority } from '@/features/boards/boards-api'
import { api } from '@/lib/api/client'
import { queryKeys } from '@/lib/api/query-keys'

export interface CalendarCard {
  id: string
  title: string
  priority: CardPriority
  dueDate: string
  boardId: string
  boardName: string
  columnName: string
  isAssignedToMe: boolean
}

export interface CalendarData {
  cards: CalendarCard[]
  activity: ActivityEntry[]
  /** The range had more activity than the API returns at once; the newest entries are the ones kept. */
  activityTruncated: boolean
}

/** Due dates and activity between two instants, on every board the user can see. */
export function useCalendar(from: Date, to: Date) {
  const fromIso = from.toISOString()
  const toIso = to.toISOString()
  return useQuery({
    queryKey: queryKeys.calendar(fromIso, toIso),
    queryFn: () => api.get<CalendarData>('/me/calendar', { query: { from: fromIso, to: toIso } }),
    // Keeps the previous month on screen while the next one loads.
    placeholderData: keepPreviousData,
  })
}
