import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Archive, ArchiveRestore } from 'lucide-react'
import { Link } from 'react-router'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { EmptyState, ErrorState, Skeleton } from '@/components/ui/feedback'
import { useTeamOutlet } from '@/features/teams/team-layout'
import { errorMessage } from '@/lib/api/api-error'
import { queryKeys } from '@/lib/api/query-keys'
import { cn } from '@/lib/utils/cn'
import { boardCover } from './board-cover'
import { VisibilityLabel } from './board-tile'
import { useTeamBoards } from './board-queries'
import { boardsApi, type BoardSummary } from './boards-api'

export function ArchivedBoardsPanel() {
  const team = useTeamOutlet()
  const queryClient = useQueryClient()
  const boards = useTeamBoards(team.id, true)
  // Only board admins can restore; the API answers 403 to anyone else.
  const restore = useMutation({
    mutationFn: (board: BoardSummary) => boardsApi.restore(board.id),
    onSuccess: (_result, board) => {
      toast.success(`«${board.name}» se restauró`)
      void queryClient.invalidateQueries({ queryKey: queryKeys.allTeamBoards(team.id) })
      void queryClient.invalidateQueries({ queryKey: queryKeys.board(board.id) })
    },
    onError: (error) => toast.error(errorMessage(error)),
  })

  if (boards.isPending) {
    return <Skeleton className="h-40 rounded-xl" />
  }
  if (boards.isError) {
    return <ErrorState error={boards.error} onRetry={() => boards.refetch()} />
  }
  if (boards.data.length === 0) {
    return (
      <EmptyState
        icon={Archive}
        title="No hay tableros archivados"
        description="Los tableros archivados quedan de solo lectura y se pueden restaurar desde aquí."
      />
    )
  }

  return (
    <ul className="divide-y divide-line overflow-hidden rounded-xl border border-line bg-surface shadow-card">
      {boards.data.map((board) => (
        <li key={board.id} className="flex items-center gap-4 px-4 py-3 sm:px-5">
          <span className={cn('size-9 shrink-0 rounded-lg bg-linear-to-br opacity-60', boardCover(board.id))} />
          <div className="min-w-0 flex-1">
            <Link to={`/boards/${board.id}`} className="truncate text-sm font-medium hover:text-primary">
              {board.name}
            </Link>
            <VisibilityLabel visibility={board.visibility} className="mt-0.5 flex text-xs text-muted" />
          </div>
          <Button
            variant="secondary"
            size="sm"
            loading={restore.isPending && restore.variables?.id === board.id}
            onClick={() => restore.mutate(board)}
          >
            <ArchiveRestore />
            Restaurar
          </Button>
        </li>
      ))}
    </ul>
  )
}
