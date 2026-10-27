import { Archive, LayoutGrid } from 'lucide-react'
import { useCallback, useState } from 'react'
import { Link, Outlet, useNavigate, useOutletContext, useParams } from 'react-router'
import { Alert } from '@/components/ui/alert'
import { EmptyState, ErrorState, Skeleton } from '@/components/ui/feedback'
import { useCurrentUser } from '@/features/auth/auth-provider'
import { ApiError } from '@/lib/api/api-error'
import { BoardCanvas } from './board-canvas'
import { BoardFilters } from './board-filters'
import { BoardHeader } from './board-header'
import { boardPermissions, type BoardPermissions } from './board-permissions'
import { useBoard, useBoardLocation, type BoardLocation } from './board-queries'
import type { BoardDetail } from './boards-api'
import { emptyFilters, useFilteredCardIds, type CardFilters } from './card-search'
import { useBoardRealtime } from './use-board-realtime'

export interface BoardOutletContext {
  board: BoardDetail
  location: BoardLocation | null
  permissions: BoardPermissions
}

export function BoardPage() {
  const { boardId = '' } = useParams<{ boardId: string }>()
  const navigate = useNavigate()
  const me = useCurrentUser()
  const board = useBoard(boardId)
  const { location, isPending: locating } = useBoardLocation(boardId)
  const [filters, setFilters] = useState<CardFilters>(emptyFilters)
  const filtered = useFilteredCardIds(boardId, filters)

  useBoardRealtime(boardId, location?.teamId, board.isSuccess)

  const openCard = useCallback((cardId: string) => navigate(`cards/${cardId}`), [navigate])

  if (board.isPending) {
    return <BoardSkeleton />
  }
  if (board.isError) {
    const notFound = board.error instanceof ApiError && (board.error.status === 404 || board.error.status === 403)
    return (
      <div className="p-8">
        {notFound ? (
          <EmptyState
            icon={LayoutGrid}
            title="Tablero no disponible"
            description="No existe o no tienes acceso a él."
            action={
              <Link to="/" className="text-sm font-medium text-primary hover:underline">
                Volver al inicio
              </Link>
            }
          />
        ) : (
          <ErrorState error={board.error} onRetry={() => board.refetch()} />
        )}
      </div>
    )
  }

  // Until the team lists load we do not know whether it is archived; stay read-only meanwhile.
  const archived = location?.archived ?? locating
  const permissions = boardPermissions(board.data, me.id, archived)

  return (
    <div className="flex h-full min-h-0 flex-col">
      <BoardHeader board={board.data} location={location} permissions={permissions} />

      <div className="flex flex-col gap-3 px-4 pt-4 pb-3 sm:px-6">
        {location?.archived && (
          <Alert tone="warning">
            <span className="inline-flex items-center gap-1.5">
              <Archive className="size-4" />
              Este tablero está archivado y es de solo lectura.
              {permissions.canAdmin && ' Puedes restaurarlo desde el menú de opciones.'}
            </span>
          </Alert>
        )}
        <BoardFilters
          filters={filters}
          onChange={setFilters}
          members={board.data.members}
          meId={me.id}
          searching={filtered.isFetching}
          canEdit={permissions.canEdit}
        />
      </div>

      <div className="min-h-0 flex-1">
        {board.data.columns.length === 0 && !permissions.canEdit ? (
          <div className="px-6">
            <EmptyState icon={LayoutGrid} title="Este tablero no tiene columnas todavía" />
          </div>
        ) : (
          <BoardCanvas
            board={board.data}
            canEdit={permissions.canEdit}
            visibleCardIds={filtered.ids}
            onOpenCard={openCard}
          />
        )}
      </div>

      <Outlet context={{ board: board.data, location, permissions } satisfies BoardOutletContext} />
    </div>
  )
}

/** The board the BoardPage above is showing, for its child routes (card detail, archived cards). */
export function useBoardOutlet(): BoardOutletContext {
  return useOutletContext<BoardOutletContext>()
}

function BoardSkeleton() {
  return (
    <div className="flex h-full flex-col">
      <div className="border-b border-line px-6 py-4">
        <Skeleton className="h-3.5 w-28" />
        <Skeleton className="mt-2.5 h-7 w-64" />
      </div>
      <div className="flex gap-3 overflow-hidden p-6">
        {[5, 3, 4, 2].map((cards, i) => (
          <div key={i} className="w-72 shrink-0 space-y-2 rounded-xl bg-subtle p-2">
            <Skeleton className="h-5 w-24" />
            {Array.from({ length: cards }, (_, j) => (
              <Skeleton key={j} className="h-16 rounded-lg" />
            ))}
          </div>
        ))}
      </div>
    </div>
  )
}
