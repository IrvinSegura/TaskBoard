import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { api, type PagedResponse } from '@/lib/api/client'
import { queryKeys } from '@/lib/api/query-keys'
import type { CardPriority } from './boards-api'

export interface CardSearchResult {
  id: string
  columnId: string
  title: string
  priority: CardPriority
  dueDate: string | null
  isArchived: boolean
  assigneeIds: string[]
}

export type DueFilter = 'overdue' | 'week' | null

export interface CardFilters {
  text: string
  priority: CardPriority | null
  assigneeId: string | null
  due: DueFilter
}

export const emptyFilters: CardFilters = { text: '', priority: null, assigneeId: null, due: null }

export function hasActiveFilters(filters: CardFilters): boolean {
  return filters.text.trim() !== '' || filters.priority !== null || filters.assigneeId !== null || filters.due !== null
}

/** The API caps pageSize at 200; a board with more matching cards shows the first 200. */
const maxPageSize = 200

function dueBefore(due: DueFilter): string | undefined {
  if (due === 'overdue') {
    return new Date().toISOString()
  }
  if (due === 'week') {
    return new Date(Date.now() + 7 * 24 * 3600 * 1000).toISOString()
  }
  return undefined
}

export function searchCards(boardId: string, filters: Partial<CardFilters> & { archived?: boolean; page?: number }) {
  return api.get<PagedResponse<CardSearchResult>>(`/boards/${boardId}/cards`, {
    query: {
      text: filters.text?.trim(),
      priority: filters.priority,
      assigneeId: filters.assigneeId,
      dueBefore: dueBefore(filters.due ?? null),
      archived: filters.archived,
      page: filters.page,
      pageSize: maxPageSize,
    },
  })
}

/** The ids of the cards matching the filters, or null while no filter is set. */
export function useFilteredCardIds(boardId: string, filters: CardFilters) {
  const active = hasActiveFilters(filters)
  const query = useQuery({
    queryKey: queryKeys.boardCards(boardId, filters),
    queryFn: () => searchCards(boardId, filters),
    enabled: active,
    placeholderData: keepPreviousData,
    select: (page) => new Set(page.items.map((card) => card.id)),
  })
  return { ids: active ? (query.data ?? null) : null, isFetching: active && query.isFetching }
}
