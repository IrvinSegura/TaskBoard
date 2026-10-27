import { ArrowUpRight, Lock, Users } from 'lucide-react'
import { Link } from 'react-router'
import { cn } from '@/lib/utils/cn'
import { boardCover } from './board-cover'
import type { BoardSummary } from './boards-api'

export function VisibilityLabel({ visibility, className }: { visibility: BoardSummary['visibility']; className?: string }) {
  const Icon = visibility === 'Private' ? Lock : Users
  return (
    <span className={cn('inline-flex items-center gap-1.5', className)}>
      <Icon className="size-3.5" />
      {visibility === 'Private' ? 'Privado' : 'Equipo'}
    </span>
  )
}

/** The board's monogram on its cover gradient; used by tiles and the board header. */
export function BoardMark({ board, className }: { board: Pick<BoardSummary, 'id' | 'name'>; className?: string }) {
  return (
    <span
      className={cn(
        'inline-flex shrink-0 items-center justify-center rounded-lg bg-linear-to-br font-semibold text-white shadow-card',
        boardCover(board.id),
        className,
      )}
      aria-hidden="true"
    >
      {board.name.trim().charAt(0).toUpperCase() || '?'}
    </span>
  )
}

export function BoardTile({ board, subtitle }: { board: BoardSummary; subtitle?: string }) {
  return (
    <Link
      to={`/boards/${board.id}`}
      className="group flex flex-col overflow-hidden rounded-xl border border-line bg-surface shadow-card transition-all hover:-translate-y-0.5 hover:border-line-strong hover:shadow-raised"
    >
      <div className={cn('relative h-24 bg-linear-to-br', boardCover(board.id))}>
        <div className="bg-dots absolute inset-0 opacity-60" aria-hidden="true" />
        <span className="absolute bottom-3 left-4 flex size-10 items-center justify-center rounded-lg bg-white/20 text-lg font-semibold text-white ring-1 ring-white/30 backdrop-blur-sm ring-inset">
          {board.name.trim().charAt(0).toUpperCase() || '?'}
        </span>
        {/* A tiny kanban: three columns of different heights. */}
        <div className="absolute right-4 bottom-3 flex items-end gap-1" aria-hidden="true">
          <span className="h-8 w-2.5 rounded-sm bg-white/40" />
          <span className="h-5 w-2.5 rounded-sm bg-white/30" />
          <span className="h-6 w-2.5 rounded-sm bg-white/25" />
        </div>
        <ArrowUpRight className="absolute top-3 right-3 size-4 text-white opacity-0 transition-opacity group-hover:opacity-90" />
      </div>
      <div className="px-4 py-3">
        <h3 className="truncate text-sm font-semibold group-hover:text-primary">{board.name}</h3>
        <p className="mt-1 flex items-center gap-2 text-xs text-muted">
          <VisibilityLabel visibility={board.visibility} />
          {subtitle && (
            <>
              <span aria-hidden="true">·</span>
              <span className="truncate">{subtitle}</span>
            </>
          )}
        </p>
      </div>
    </Link>
  )
}
