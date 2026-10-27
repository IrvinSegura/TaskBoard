import { Lock, Users } from 'lucide-react'
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

export function BoardTile({ board, subtitle }: { board: BoardSummary; subtitle?: string }) {
  return (
    <Link
      to={`/boards/${board.id}`}
      className="group overflow-hidden rounded-xl border border-line bg-surface shadow-card transition-all hover:-translate-y-0.5 hover:border-line-strong hover:shadow-raised"
    >
      <div className={cn('relative h-20 bg-linear-to-br', boardCover(board.id))}>
        <div className="absolute inset-x-3 bottom-2.5 flex gap-1.5 opacity-70" aria-hidden="true">
          <span className="h-6 w-1/3 rounded bg-white/25" />
          <span className="h-4 w-1/3 rounded bg-white/20" />
          <span className="h-5 w-1/4 rounded bg-white/15" />
        </div>
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
