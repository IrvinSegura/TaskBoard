import { LayoutGrid, Plus } from 'lucide-react'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { EmptyState, ErrorState, Skeleton } from '@/components/ui/feedback'
import { useTeamOutlet } from '@/features/teams/team-layout'
import { BoardTile } from './board-tile'
import { useTeamBoards } from './board-queries'
import { CreateBoardDialog } from './create-board-dialog'

export function TeamBoardsPanel() {
  const team = useTeamOutlet()
  const boards = useTeamBoards(team.id)
  const [creating, setCreating] = useState(false)

  if (boards.isPending) {
    return (
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {Array.from({ length: 4 }, (_, i) => (
          <Skeleton key={i} className="h-36 rounded-xl" />
        ))}
      </div>
    )
  }
  if (boards.isError) {
    return <ErrorState error={boards.error} onRetry={() => boards.refetch()} />
  }

  return (
    <section>
      {boards.data.length === 0 ? (
        <EmptyState
          icon={LayoutGrid}
          title="Este equipo no tiene tableros"
          description="Crea un tablero para organizar el trabajo en columnas y tarjetas."
          action={
            <Button onClick={() => setCreating(true)}>
              <Plus />
              Crear tablero
            </Button>
          }
        />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {boards.data.map((board) => (
            <BoardTile key={board.id} board={board} />
          ))}
          <button
            type="button"
            onClick={() => setCreating(true)}
            className="flex min-h-36 cursor-pointer flex-col items-center justify-center gap-2 rounded-xl border border-dashed border-line-strong text-sm font-medium text-muted transition-colors hover:border-primary hover:bg-primary-soft hover:text-primary"
          >
            <Plus className="size-5" />
            Nuevo tablero
          </button>
        </div>
      )}

      <CreateBoardDialog teamId={team.id} open={creating} onOpenChange={setCreating} />
    </section>
  )
}
