import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArchiveRestore, Layers } from 'lucide-react'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Dialog } from '@/components/ui/dialog'
import { EmptyState, ErrorState, Skeleton } from '@/components/ui/feedback'
import { useBoardOutlet } from '@/features/boards/board-page'
import { PriorityBadge } from '@/features/boards/card-meta'
import { searchCards, type CardSearchResult } from '@/features/boards/card-search'
import { errorMessage } from '@/lib/api/api-error'
import { queryKeys } from '@/lib/api/query-keys'
import { cardsApi } from './cards-api'

/** /boards/:boardId/archived: the board's archived cards, restorable by editors. */
export function ArchivedCardsRoute() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { board, permissions } = useBoardOutlet()
  const archived = useQuery({
    queryKey: queryKeys.boardCards(board.id, { archived: true }),
    queryFn: () => searchCards(board.id, { archived: true }),
  })
  const restore = useMutation({
    mutationFn: (card: CardSearchResult) => cardsApi.restore(card.id),
    onSuccess: (_result, card) => toast.success(`«${card.title}» volvió al tablero`),
    onError: (error) => toast.error(errorMessage(error)),
    onSettled: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.allBoardCards(board.id) }),
        queryClient.invalidateQueries({ queryKey: queryKeys.board(board.id), exact: true }),
      ]),
  })

  const close = () => navigate(`/boards/${board.id}`)
  const columnName = (columnId: string) => board.columns.find((c) => c.id === columnId)?.name ?? '—'

  return (
    <Dialog
      open
      onOpenChange={(open) => !open && close()}
      title="Tarjetas archivadas"
      description="Al restaurar una tarjeta vuelve a su columna original."
      size="lg"
    >
      {archived.isPending ? (
        <div className="space-y-2">
          <Skeleton className="h-14" />
          <Skeleton className="h-14" />
        </div>
      ) : archived.isError ? (
        <ErrorState error={archived.error} onRetry={() => archived.refetch()} className="border-0" />
      ) : archived.data.items.length === 0 ? (
        <EmptyState icon={Layers} title="No hay tarjetas archivadas" className="border-0 py-8" />
      ) : (
        <ul className="divide-y divide-line">
          {archived.data.items.map((card) => (
            <li key={card.id} className="flex items-center gap-3 py-2.5">
              <button
                type="button"
                className="min-w-0 flex-1 cursor-pointer text-left"
                onClick={() => navigate(`/boards/${board.id}/cards/${card.id}`)}
              >
                <p className="truncate text-sm font-medium hover:text-primary">{card.title}</p>
                <p className="mt-0.5 flex items-center gap-2 text-xs text-muted">
                  en {columnName(card.columnId)}
                  <PriorityBadge priority={card.priority} />
                </p>
              </button>
              {permissions.canEdit && (
                <Button
                  variant="secondary"
                  size="sm"
                  loading={restore.isPending && restore.variables?.id === card.id}
                  onClick={() => restore.mutate(card)}
                >
                  <ArchiveRestore />
                  Restaurar
                </Button>
              )}
            </li>
          ))}
        </ul>
      )}
    </Dialog>
  )
}
