import { LayoutGrid, MessageSquare, UserCheck, Users } from 'lucide-react'
import { cn } from '@/lib/utils/cn'
import { formatRelative } from '@/lib/utils/format'
import { notificationText } from './notification-text'
import type { AppNotification, NotificationType } from './notifications-api'

const typeMeta: Record<NotificationType, { icon: typeof Users; className: string; label: string }> = {
  CardAssigned: { icon: UserCheck, className: 'bg-primary-soft text-primary', label: 'Asignación' },
  CommentAdded: { icon: MessageSquare, className: 'bg-sky-500/12 text-sky-600 dark:text-sky-400', label: 'Comentario' },
  AddedToBoard: { icon: LayoutGrid, className: 'bg-emerald-500/12 text-emerald-600 dark:text-emerald-400', label: 'Tablero' },
  AddedToTeam: { icon: Users, className: 'bg-amber-500/15 text-amber-600 dark:text-amber-400', label: 'Equipo' },
}

/** Icon, message and time of a notification, without the clickable wrapper. */
export function NotificationContent({ notification }: { notification: AppNotification }) {
  const { icon: Icon, className, label } = typeMeta[notification.type]
  return (
    <>
      <span className={cn('mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-lg', className)}>
        <Icon className="size-4" />
      </span>
      <span className="min-w-0 flex-1">
        <span className={cn('block text-sm leading-snug', notification.isRead ? 'text-muted' : 'font-medium text-fg')}>
          {notificationText(notification.message)}
        </span>
        <span className="mt-1 block text-xs text-faint">
          {label} · {formatRelative(notification.createdAt)}
        </span>
      </span>
      {!notification.isRead && <span className="mt-2 size-2 shrink-0 rounded-full bg-primary" aria-label="Sin leer" />}
    </>
  )
}

interface NotificationItemProps {
  notification: AppNotification
  onOpen: (notification: AppNotification) => void
}

export function NotificationItem({ notification, onOpen }: NotificationItemProps) {
  return (
    <button
      type="button"
      onClick={() => onOpen(notification)}
      className="flex w-full cursor-pointer items-start gap-3 px-5 py-4 text-left transition-colors hover:bg-subtle"
    >
      <NotificationContent notification={notification} />
    </button>
  )
}
