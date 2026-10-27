import {
  Archive,
  ArchiveRestore,
  ArrowRightLeft,
  Columns3,
  MessageSquare,
  Paperclip,
  Pencil,
  Plus,
  Trash2,
  UserMinus,
  UserPlus,
  type LucideIcon,
} from 'lucide-react'
import { Link } from 'react-router'
import { Avatar } from '@/components/ui/avatar'
import { cn } from '@/lib/utils/cn'
import { formatRelative } from '@/lib/utils/format'
import type { ActivityEntry, ActivityType } from './activity-api'
import { activityText } from './activity-text'

const icons: Record<ActivityType, { icon: LucideIcon; tone: string }> = {
  CardCreated: { icon: Plus, tone: 'bg-success-soft text-success' },
  CardUpdated: { icon: Pencil, tone: 'bg-muted-surface text-muted' },
  CardMoved: { icon: ArrowRightLeft, tone: 'bg-info-soft text-info' },
  CardArchived: { icon: Archive, tone: 'bg-warning-soft text-warning' },
  CardRestored: { icon: ArchiveRestore, tone: 'bg-success-soft text-success' },
  CardDeleted: { icon: Trash2, tone: 'bg-danger-soft text-danger' },
  CardAssigned: { icon: UserPlus, tone: 'bg-primary-soft text-primary' },
  CardUnassigned: { icon: UserMinus, tone: 'bg-muted-surface text-muted' },
  CommentAdded: { icon: MessageSquare, tone: 'bg-info-soft text-info' },
  AttachmentAdded: { icon: Paperclip, tone: 'bg-muted-surface text-muted' },
  ColumnCreated: { icon: Columns3, tone: 'bg-success-soft text-success' },
  ColumnRenamed: { icon: Pencil, tone: 'bg-muted-surface text-muted' },
  ColumnDeleted: { icon: Trash2, tone: 'bg-danger-soft text-danger' },
  BoardRenamed: { icon: Pencil, tone: 'bg-muted-surface text-muted' },
  BoardArchived: { icon: Archive, tone: 'bg-warning-soft text-warning' },
  BoardRestored: { icon: ArchiveRestore, tone: 'bg-success-soft text-success' },
  MemberAdded: { icon: UserPlus, tone: 'bg-primary-soft text-primary' },
  MemberRemoved: { icon: UserMinus, tone: 'bg-muted-surface text-muted' },
}

interface ActivityItemProps {
  entry: ActivityEntry
  /** Feeds that span boards say which board each line is about. */
  showBoard?: boolean
}

export function ActivityItem({ entry, showBoard = false }: ActivityItemProps) {
  const { icon: Icon, tone } = icons[entry.type]
  const { action, subject, detail } = activityText(entry)
  const actor = entry.actorName ?? 'Alguien'
  const cardLink = entry.cardId && entry.type !== 'CardDeleted' ? `/boards/${entry.boardId}/cards/${entry.cardId}` : null

  return (
    <li className="flex gap-3 py-3">
      <div className="relative shrink-0">
        <Avatar id={entry.actorId ?? actor} name={actor} />
        <span
          className={cn('absolute -right-1 -bottom-1 flex size-4.5 items-center justify-center rounded-full ring-2 ring-surface', tone)}
          aria-hidden="true"
        >
          <Icon className="size-2.5" />
        </span>
      </div>
      <div className="min-w-0 flex-1 text-sm leading-snug">
        <p className="break-words text-muted">
          <span className="font-medium text-fg">{actor}</span> {action}{' '}
          {subject &&
            (cardLink ? (
              <Link to={cardLink} className="font-medium text-fg hover:text-primary hover:underline">
                {subject}
              </Link>
            ) : (
              <span className="font-medium text-fg">{subject}</span>
            ))}
          {detail && entry.type !== 'CommentAdded' && <span> {detail}</span>}
        </p>
        {entry.type === 'CommentAdded' && detail && (
          <p className="mt-1.5 line-clamp-2 rounded-lg rounded-tl-sm bg-subtle px-2.5 py-1.5 text-[13px] text-fg">{detail}</p>
        )}
        <p className="mt-1 text-xs text-faint">
          {formatRelative(entry.occurredAt)}
          {showBoard && (
            <>
              {' · '}
              <Link to={`/boards/${entry.boardId}`} className="hover:text-fg hover:underline">
                {entry.boardName}
              </Link>
            </>
          )}
        </p>
      </div>
    </li>
  )
}
