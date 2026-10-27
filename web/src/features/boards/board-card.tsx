import { useSortable } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import type { KeyboardEvent } from 'react'
import { AvatarStack } from '@/components/ui/avatar'
import { cn } from '@/lib/utils/cn'
import type { BoardMember, CardSummary } from './boards-api'
import { DueDateChip, PriorityBadge } from './card-meta'

interface BoardCardViewProps {
  card: CardSummary
  members: BoardMember[]
  dragging?: boolean
  overlay?: boolean
}

/** The card tile itself, shared by the list and the drag overlay. */
export function BoardCardView({ card, members, dragging, overlay }: BoardCardViewProps) {
  const assignees = card.assigneeIds
    .map((id) => members.find((member) => member.userId === id))
    .filter((member) => member !== undefined)
    .map((member) => ({ id: member.userId, name: member.displayName }))
  const hasMeta = card.priority !== 'None' || card.dueDate || assignees.length > 0

  return (
    <div
      className={cn(
        'rounded-lg border border-line bg-surface px-3 py-2.5 text-left shadow-card transition-[box-shadow,border-color]',
        'group-hover/card:border-line-strong group-hover/card:shadow-raised',
        dragging && 'opacity-40',
        overlay && 'rotate-2 cursor-grabbing border-primary/40 shadow-overlay',
      )}
    >
      <p className="text-sm leading-snug font-medium break-words">{card.title}</p>
      {hasMeta && (
        <div className="mt-2.5 flex flex-wrap items-center gap-1.5">
          <PriorityBadge priority={card.priority} />
          {card.dueDate && <DueDateChip dueDate={card.dueDate} />}
          {assignees.length > 0 && (
            <div className="ml-auto">
              <AvatarStack people={assignees} size="xs" max={3} />
            </div>
          )}
        </div>
      )}
    </div>
  )
}

interface BoardCardProps {
  card: CardSummary
  columnId: string
  members: BoardMember[]
  draggable: boolean
  onOpen: (cardId: string) => void
}

export function BoardCard({ card, columnId, members, draggable, onOpen }: BoardCardProps) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: card.id,
    data: { type: 'card', columnId },
    disabled: !draggable,
  })

  const onKeyDown = (event: KeyboardEvent) => {
    // Space and arrows belong to the keyboard drag sensor; Enter opens the card.
    if (event.key === 'Enter') {
      onOpen(card.id)
    }
  }

  return (
    <li
      ref={setNodeRef}
      style={{ transform: CSS.Translate.toString(transform), transition }}
      className="group/card list-none"
    >
      <div
        {...attributes}
        {...listeners}
        role="button"
        tabIndex={0}
        aria-roledescription={draggable ? 'tarjeta arrastrable' : 'tarjeta'}
        aria-label={card.title}
        onClick={() => onOpen(card.id)}
        onKeyDown={(event) => {
          listeners?.onKeyDown?.(event)
          onKeyDown(event)
        }}
        className={cn('cursor-pointer rounded-lg outline-none focus-visible:ring-2 focus-visible:ring-ring', draggable && 'touch-manipulation')}
      >
        <BoardCardView card={card} members={members} dragging={isDragging} />
      </div>
    </li>
  )
}
