import * as Menu from '@radix-ui/react-dropdown-menu'
import { Bell, BellOff, CheckCheck } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/feedback'
import { NotificationContent } from './notification-item'
import { useMarkAllAsRead, useNotifications, useOpenNotification } from './notification-queries'

/** The bell in the top bar, with the latest notifications. */
export function NotificationsMenu() {
  const [open, setOpen] = useState(false)
  const notifications = useNotifications()
  const markAll = useMarkAllAsRead()
  const openNotification = useOpenNotification()
  const unread = notifications.data?.filter((n) => !n.isRead) ?? []
  const latest = notifications.data?.slice(0, 8) ?? []

  return (
    <Menu.Root open={open} onOpenChange={setOpen}>
      <Menu.Trigger asChild>
        <Button variant="ghost" size="icon" className="relative" aria-label={`Notificaciones (${unread.length} sin leer)`}>
          <Bell />
          {unread.length > 0 && (
            <span className="absolute top-1 right-1 flex min-w-4 items-center justify-center rounded-full bg-danger px-1 text-[10px] leading-4 font-semibold text-white ring-2 ring-surface">
              {unread.length > 9 ? '9+' : unread.length}
            </span>
          )}
        </Button>
      </Menu.Trigger>
      <Menu.Portal>
        <Menu.Content
          align="end"
          sideOffset={8}
          className="z-50 w-[min(24rem,calc(100vw-1rem))] rounded-xl border border-line bg-surface shadow-overlay data-[state=open]:animate-fade-in"
        >
          <div className="flex items-center justify-between border-b border-line px-4 py-3">
            <p className="text-sm font-semibold">Notificaciones</p>
            {unread.length > 0 && (
              <Button
                variant="ghost"
                size="sm"
                loading={markAll.isPending}
                onClick={() => markAll.mutate(unread.map((n) => n.id))}
              >
                <CheckCheck />
                Marcar todo como leído
              </Button>
            )}
          </div>
          <div className="scrollbar-thin max-h-96 overflow-y-auto p-1.5">
            {notifications.isPending ? (
              <div className="space-y-2 p-2">
                <Skeleton className="h-12" />
                <Skeleton className="h-12" />
              </div>
            ) : latest.length === 0 ? (
              <div className="flex flex-col items-center gap-2 px-4 py-10 text-center text-sm text-muted">
                <BellOff className="size-6 text-faint" />
                No tienes notificaciones
              </div>
            ) : (
              latest.map((notification) => (
                <Menu.Item
                  key={notification.id}
                  onSelect={() => void openNotification(notification)}
                  className="flex cursor-pointer items-start gap-3 rounded-lg px-2.5 py-2.5 outline-none data-[highlighted]:bg-subtle"
                >
                  <NotificationContent notification={notification} />
                </Menu.Item>
              ))
            )}
          </div>
          <div className="border-t border-line p-1.5">
            <Link
              to="/notifications"
              onClick={() => setOpen(false)}
              className="block rounded-lg px-3 py-2 text-center text-[13px] font-medium text-primary hover:bg-primary-soft"
            >
              Ver todas
            </Link>
          </div>
        </Menu.Content>
      </Menu.Portal>
    </Menu.Root>
  )
}
