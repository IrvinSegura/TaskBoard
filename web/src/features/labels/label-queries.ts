import { useMutation, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '@/lib/api/query-keys'
import { labelsApi, type LabelColor } from './labels-api'

/**
 * Labels show on the board, in its card searches and in every open card detail,
 * so label changes refresh all three.
 */
function useLabelMutation<TVariables>(boardId: string, mutationFn: (variables: TVariables) => Promise<unknown>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn,
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.board(boardId), exact: true }),
        queryClient.invalidateQueries({ queryKey: queryKeys.allBoardCards(boardId) }),
        queryClient.invalidateQueries({ queryKey: ['cards'] }),
      ]),
  })
}

export function useCreateLabel(boardId: string) {
  return useLabelMutation(boardId, ({ name, color }: { name: string; color: LabelColor }) =>
    labelsApi.create(boardId, name, color),
  )
}

export function useUpdateLabel(boardId: string) {
  return useLabelMutation(boardId, ({ id, name, color }: { id: string; name: string; color: LabelColor }) =>
    labelsApi.update(boardId, id, name, color),
  )
}

export function useDeleteLabel(boardId: string) {
  return useLabelMutation(boardId, (labelId: string) => labelsApi.delete(boardId, labelId))
}

export function useToggleCardLabel(cardId: string, boardId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ labelId, added }: { labelId: string; added: boolean }) =>
      added ? labelsApi.removeFromCard(cardId, labelId) : labelsApi.addToCard(cardId, labelId),
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.card(cardId), exact: true }),
        queryClient.invalidateQueries({ queryKey: queryKeys.board(boardId), exact: true }),
        queryClient.invalidateQueries({ queryKey: queryKeys.allBoardCards(boardId) }),
      ]),
  })
}
