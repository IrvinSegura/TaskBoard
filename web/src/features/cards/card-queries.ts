import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '@/lib/api/query-keys'
import { cardsApi, type CardDetail, type UpdateCardRequest } from './cards-api'

export function useCard(cardId: string) {
  return useQuery({ queryKey: queryKeys.card(cardId), queryFn: () => cardsApi.get(cardId) })
}

export function useCardComments(cardId: string) {
  return useInfiniteQuery({
    queryKey: queryKeys.cardComments(cardId),
    queryFn: ({ pageParam }) => cardsApi.comments(cardId, pageParam),
    initialPageParam: 1,
    getNextPageParam: (last) => (last.hasNextPage ? last.page + 1 : undefined),
  })
}

/**
 * Card changes show on the board (title, priority, assignees...) and in the search results,
 * so every mutation refreshes the card, its board and the board's card searches.
 */
function useCardMutation<TVariables>(
  cardId: string,
  boardId: string,
  mutationFn: (variables: TVariables) => Promise<unknown>,
) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn,
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.card(cardId) }),
        queryClient.invalidateQueries({ queryKey: queryKeys.board(boardId), exact: true }),
        queryClient.invalidateQueries({ queryKey: queryKeys.allBoardCards(boardId) }),
      ]),
  })
}

/** PUT /cards/{id} replaces every editable field, so partial edits are merged with the current card. */
export function useUpdateCard(card: CardDetail) {
  const queryClient = useQueryClient()
  const key = queryKeys.card(card.id)
  return useMutation({
    mutationFn: (changes: Partial<UpdateCardRequest>) => {
      const current = queryClient.getQueryData<CardDetail>(key) ?? card
      return cardsApi.update(card.id, {
        title: current.title,
        description: current.description,
        priority: current.priority,
        dueDate: current.dueDate,
        ...changes,
      })
    },
    onMutate: async (changes) => {
      await queryClient.cancelQueries({ queryKey: key, exact: true })
      const previous = queryClient.getQueryData<CardDetail>(key)
      if (previous) {
        queryClient.setQueryData<CardDetail>(key, { ...previous, ...changes })
      }
      return { previous }
    },
    onError: (_error, _changes, context) => {
      if (context?.previous) {
        queryClient.setQueryData(key, context.previous)
      }
    },
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: key, exact: true }),
        queryClient.invalidateQueries({ queryKey: queryKeys.board(card.boardId), exact: true }),
        queryClient.invalidateQueries({ queryKey: queryKeys.allBoardCards(card.boardId) }),
      ]),
  })
}

export function useArchiveCard(cardId: string, boardId: string) {
  return useCardMutation(cardId, boardId, () => cardsApi.archive(cardId))
}

export function useRestoreCard(cardId: string, boardId: string) {
  return useCardMutation(cardId, boardId, () => cardsApi.restore(cardId))
}

/** The card's own query is left alone: refetching it would only 404 while its detail is still closing. */
export function useDeleteCard(boardId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (cardId: string) => cardsApi.delete(cardId),
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.board(boardId), exact: true }),
        queryClient.invalidateQueries({ queryKey: queryKeys.allBoardCards(boardId) }),
      ]),
  })
}

export function useToggleAssignee(cardId: string, boardId: string) {
  return useCardMutation(cardId, boardId, ({ userId, assigned }: { userId: string; assigned: boolean }) =>
    assigned ? cardsApi.unassign(cardId, userId) : cardsApi.assign(cardId, userId),
  )
}

export function useUploadAttachment(cardId: string, boardId: string) {
  return useCardMutation(cardId, boardId, (file: File) => cardsApi.uploadAttachment(cardId, file))
}

export function useDeleteAttachment(cardId: string, boardId: string) {
  return useCardMutation(cardId, boardId, (attachmentId: string) => cardsApi.deleteAttachment(attachmentId))
}

/** Comments change the comment count of the card too. */
function useCommentMutation<TVariables>(cardId: string, mutationFn: (variables: TVariables) => Promise<unknown>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn,
    onSettled: () => queryClient.invalidateQueries({ queryKey: queryKeys.card(cardId) }),
  })
}

export function useAddComment(cardId: string) {
  return useCommentMutation(cardId, (body: string) => cardsApi.addComment(cardId, body))
}

export function useEditComment(cardId: string) {
  return useCommentMutation(cardId, ({ commentId, body }: { commentId: string; body: string }) =>
    cardsApi.editComment(commentId, body),
  )
}

export function useDeleteComment(cardId: string) {
  return useCommentMutation(cardId, (commentId: string) => cardsApi.deleteComment(commentId))
}
