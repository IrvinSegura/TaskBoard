import { useInfiniteQuery } from '@tanstack/react-query'
import { api, type PagedResponse } from '@/lib/api/client'
import { queryKeys } from '@/lib/api/query-keys'

export type ActivityType =
  | 'CardCreated'
  | 'CardUpdated'
  | 'CardMoved'
  | 'CardArchived'
  | 'CardRestored'
  | 'CardDeleted'
  | 'CardAssigned'
  | 'CardUnassigned'
  | 'CommentAdded'
  | 'AttachmentAdded'
  | 'ColumnCreated'
  | 'ColumnRenamed'
  | 'ColumnDeleted'
  | 'BoardRenamed'
  | 'BoardArchived'
  | 'BoardRestored'
  | 'MemberAdded'
  | 'MemberRemoved'

export interface ActivityEntry {
  id: string
  type: ActivityType
  actorId: string | null
  actorName: string | null
  boardId: string
  boardName: string
  cardId: string | null
  subject: string | null
  detail: string | null
  occurredAt: string
}

export type ActivityScope = { kind: 'me' } | { kind: 'team' | 'board' | 'card'; id: string }

const pageSize = 30

function feedPath(scope: ActivityScope): string {
  switch (scope.kind) {
    case 'me':
      return '/me/activity'
    case 'team':
      return `/teams/${scope.id}/activity`
    case 'board':
      return `/boards/${scope.id}/activity`
    case 'card':
      return `/cards/${scope.id}/activity`
  }
}

export function useActivityFeed(scope: ActivityScope, size = pageSize) {
  return useInfiniteQuery({
    queryKey: queryKeys.activity(scope.kind, scope.kind === 'me' ? undefined : scope.id),
    queryFn: ({ pageParam }) =>
      api.get<PagedResponse<ActivityEntry>>(feedPath(scope), { query: { page: pageParam, pageSize: size } }),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.hasNextPage ? last.page + 1 : undefined),
  })
}
