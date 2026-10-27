import { ListChecks } from 'lucide-react'
import { Link } from 'react-router'
import { DueDateChip, PriorityBadge } from '@/features/boards/card-meta'
import { boardCover } from '@/features/boards/board-cover'
import { LabelChip } from '@/features/labels/label-chip'
import { cn } from '@/lib/utils/cn'
import type { MyCard } from './my-tasks-api'

/** One assigned card: where it lives, its labels and what is left to do. */
export function MyTaskRow({ card, compact = false }: { card: MyCard; compact?: boolean }) {
  return (
    <Link
      to={`/boards/${card.boardId}/cards/${card.id}`}
      className="group flex items-center gap-3 px-4 py-3 transition-colors hover:bg-subtle sm:px-5"
    >
      <span className={cn('h-9 w-1 shrink-0 rounded-full bg-linear-to-b', boardCover(card.boardId))} aria-hidden="true" />
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium group-hover:text-primary">{card.title}</p>
        <p className="mt-0.5 truncate text-xs text-muted">
          {card.boardName} · {card.columnName}
          {!compact && <span className="text-faint"> · {card.teamName}</span>}
        </p>
        {!compact && card.labels.length > 0 && (
          <div className="mt-1.5 flex flex-wrap gap-1">
            {card.labels.map((label) => (
              <LabelChip key={label.id} label={label} className="px-1.5 py-0 text-[11px]" />
            ))}
          </div>
        )}
      </div>
      <div className="flex shrink-0 items-center gap-1.5">
        {card.checklist && (
          <span className="hidden items-center gap-1 text-xs text-muted tabular-nums sm:inline-flex">
            <ListChecks className="size-3.5" />
            {card.checklist.done}/{card.checklist.total}
          </span>
        )}
        <PriorityBadge priority={card.priority} className={compact ? 'hidden sm:inline-flex' : undefined} />
        {card.dueDate && <DueDateChip dueDate={card.dueDate} />}
      </div>
    </Link>
  )
}
