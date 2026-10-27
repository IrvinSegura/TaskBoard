import { useMutation, useQueries, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '@/lib/api/query-keys'
import { useTeams } from '@/features/teams/team-queries'
import { byPosition, positionBetween, type Neighbors } from './board-order'
import { boardsApi, type BoardDetail, type BoardRole, type BoardVisibility } from './boards-api'

export function useTeamBoards(teamId: string, archived = false) {
  return useQuery({
    queryKey: queryKeys.teamBoards(teamId, archived),
    queryFn: () => boardsApi.listForTeam(teamId, archived),
  })
}

/** Columns and cards always sorted by position, so the cache can be rendered and patched in order. */
async function fetchBoard(boardId: string): Promise<BoardDetail> {
  const board = await boardsApi.get(boardId)
  return {
    ...board,
    columns: board.columns
      .map((column) => ({ ...column, cards: [...column.cards].sort(byPosition) }))
      .sort(byPosition),
  }
}

export function useBoard(boardId: string) {
  return useQuery({ queryKey: queryKeys.board(boardId), queryFn: () => fetchBoard(boardId) })
}

export interface BoardLocation {
  teamId: string
  teamName: string
  archived: boolean
}

/**
 * GET /boards/{id} says neither the team nor whether the board is archived; both come from the
 * team board lists, which the sidebar and dashboard have usually loaded already.
 */
export function useBoardLocation(boardId: string): { location: BoardLocation | null; isPending: boolean } {
  const teams = useTeams()
  const lists = useQueries({
    queries: (teams.data ?? []).flatMap((team) =>
      [false, true].map((archived) => ({
        queryKey: queryKeys.teamBoards(team.id, archived),
        queryFn: () => boardsApi.listForTeam(team.id, archived),
      })),
    ),
  })

  const pairs = (teams.data ?? []).flatMap((team) => [false, true].map((archived) => ({ team, archived })))
  for (let i = 0; i < pairs.length; i++) {
    if (lists[i]?.data?.some((board) => board.id === boardId)) {
      const { team, archived } = pairs[i]!
      return { location: { teamId: team.id, teamName: team.name, archived }, isPending: false }
    }
  }
  return { location: null, isPending: teams.isPending || lists.some((list) => list.isPending) }
}

export function useCreateBoard(teamId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ name, visibility }: { name: string; visibility: BoardVisibility }) =>
      boardsApi.create(teamId, name, visibility),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.allTeamBoards(teamId) }),
  })
}

/** Board-level changes also show in the team lists (name, archived or not). */
function useBoardMutation<TVariables, TResult = void>(
  boardId: string,
  teamId: string | undefined,
  mutationFn: (variables: TVariables) => Promise<TResult>,
) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn,
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.board(boardId) }),
        teamId && queryClient.invalidateQueries({ queryKey: queryKeys.allTeamBoards(teamId) }),
      ]),
  })
}

export function useRenameBoard(boardId: string, teamId: string | undefined) {
  return useBoardMutation(boardId, teamId, (name: string) => boardsApi.rename(boardId, name))
}

export function useArchiveBoard(boardId: string, teamId: string | undefined) {
  return useBoardMutation(boardId, teamId, () => boardsApi.archive(boardId))
}

export function useRestoreBoard(boardId: string, teamId: string | undefined) {
  return useBoardMutation(boardId, teamId, () => boardsApi.restore(boardId))
}

export function useAddBoardMember(boardId: string) {
  return useBoardMutation(boardId, undefined, ({ userId, role }: { userId: string; role: BoardRole }) =>
    boardsApi.addMember(boardId, userId, role),
  )
}

export function useRemoveBoardMember(boardId: string) {
  return useBoardMutation(boardId, undefined, (userId: string) => boardsApi.removeMember(boardId, userId))
}

export function useCreateColumn(boardId: string) {
  return useBoardMutation(boardId, undefined, (name: string) => boardsApi.createColumn(boardId, name))
}

export function useRenameColumn(boardId: string) {
  return useBoardMutation(boardId, undefined, ({ columnId, name }: { columnId: string; name: string }) =>
    boardsApi.renameColumn(columnId, name),
  )
}

export function useCreateCard(boardId: string) {
  return useBoardMutation(boardId, undefined, ({ columnId, title }: { columnId: string; title: string }) =>
    boardsApi.createCard(columnId, title),
  )
}

/**
 * Applies the new order to the cache right away and rolls it back if the API refuses the move.
 * `apply` receives the board as it was and returns it as it should look.
 */
function useOptimisticBoardMutation<TVariables>(
  boardId: string,
  mutationFn: (variables: TVariables) => Promise<unknown>,
  apply: (board: BoardDetail, variables: TVariables) => BoardDetail,
) {
  const queryClient = useQueryClient()
  const key = queryKeys.board(boardId)
  return useMutation({
    mutationFn,
    onMutate: async (variables: TVariables) => {
      await queryClient.cancelQueries({ queryKey: key })
      const previous = queryClient.getQueryData<BoardDetail>(key)
      if (previous) {
        queryClient.setQueryData(key, apply(previous, variables))
      }
      return { previous }
    },
    onError: (_error, _variables, context) => {
      if (context?.previous) {
        queryClient.setQueryData(key, context.previous)
      }
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: key }),
  })
}

export interface MoveColumnVariables {
  columnId: string
  neighbors: Neighbors
}

export function useMoveColumn(boardId: string) {
  return useOptimisticBoardMutation(
    boardId,
    ({ columnId, neighbors }: MoveColumnVariables) =>
      boardsApi.moveColumn(columnId, neighbors.previousId, neighbors.nextId),
    (board, { columnId, neighbors }) => {
      const others = board.columns.filter((column) => column.id !== columnId)
      const moving = board.columns.find((column) => column.id === columnId)
      if (!moving) {
        return board
      }
      const moved = { ...moving, position: positionBetween(others, neighbors) }
      return { ...board, columns: [...others, moved].sort(byPosition) }
    },
  )
}

export interface MoveCardVariables {
  cardId: string
  targetColumnId: string
  neighbors: Neighbors
}

export function useMoveCard(boardId: string) {
  return useOptimisticBoardMutation(
    boardId,
    ({ cardId, targetColumnId, neighbors }: MoveCardVariables) =>
      boardsApi.moveCard(cardId, targetColumnId, neighbors.previousId, neighbors.nextId),
    (board, { cardId, targetColumnId, neighbors }) => moveCardInBoard(board, cardId, targetColumnId, neighbors),
  )
}

export function moveCardInBoard(
  board: BoardDetail,
  cardId: string,
  targetColumnId: string,
  neighbors: Neighbors,
): BoardDetail {
  const card = board.columns.flatMap((column) => column.cards).find((c) => c.id === cardId)
  if (!card) {
    return board
  }
  return {
    ...board,
    columns: board.columns.map((column) => {
      const cards = column.cards.filter((c) => c.id !== cardId)
      if (column.id !== targetColumnId) {
        return cards.length === column.cards.length ? column : { ...column, cards }
      }
      const moved = { ...card, position: positionBetween(cards, neighbors) }
      return { ...column, cards: [...cards, moved].sort(byPosition) }
    }),
  }
}
