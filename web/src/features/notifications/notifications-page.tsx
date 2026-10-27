import { BellOff, CheckCheck, Trash2 } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import { PageContainer, PageHeader } from '@/components/layout/page-header'
import { Button } from '@/components/ui/button'
import { EmptyState, ErrorState, Skeleton } from '@/components/ui/feedback'
import { Tabs } from '@/components/ui/tabs'
import { NotificationItem } from './notification-item'
import { errorMessage } from '@/lib/api/api-error'
import {
  useDeleteNotification,
  useMarkAllAsRead,
  useNotifications,
  useOpenNotification,
} from './notification-queries'

type Filter = 'all' | 'unread'

export function NotificationsPage() {
  const notifications = useNotifications()
  const markAll = useMarkAllAsRead()
  const remove = useDeleteNotification()
  const openNotification = useOpenNotification()
  const [filter, setFilter] = useState<Filter>('all')

  const all = notifications.data ?? []
  const unread = all.filter((n) => !n.isRead)
  const shown = filter === 'unread' ? unread : all

  return (
    <PageContainer className="max-w-3xl">
      <PageHeader
        title="Notificaciones"
        description="Asignaciones, invitaciones a equipos y tableros."
        actions={
          unread.length > 0 && (
            <Button variant="secondary" loading={markAll.isPending} onClick={() => markAll.mutate()}>
              <CheckCheck />
              Marcar todo como leído
            </Button>
          )
        }
      />

      <Tabs
        className="mt-8"
        value={filter}
        onChange={setFilter}
        items={[
          { value: 'all', label: 'Todas', count: all.length },
          { value: 'unread', label: 'Sin leer', count: unread.length },
        ]}
      />

      <div className="mt-6">
        {notifications.isPending ? (
          <div className="space-y-2">
            {Array.from({ length: 4 }, (_, i) => (
              <Skeleton key={i} className="h-16 rounded-xl" />
            ))}
          </div>
        ) : notifications.isError ? (
          <ErrorState error={notifications.error} onRetry={() => notifications.refetch()} />
        ) : shown.length === 0 ? (
          <EmptyState
            icon={BellOff}
            title={filter === 'unread' ? 'Estás al día' : 'No tienes notificaciones'}
            description={
              filter === 'unread'
                ? 'No hay notificaciones sin leer.'
                : 'Te avisaremos cuando te asignen una tarjeta o te agreguen a un equipo o tablero.'
            }
          />
        ) : (
          <ul className="divide-y divide-line overflow-hidden rounded-xl border border-line bg-surface shadow-card">
            {shown.map((notification) => (
              <li key={notification.id} className="group flex items-center">
                <div className="min-w-0 flex-1">
                  <NotificationItem notification={notification} onOpen={(n) => void openNotification(n)} />
                </div>
                <Button
                  variant="ghost"
                  size="icon-sm"
                  className="mr-3 shrink-0 text-faint opacity-100 hover:text-danger sm:opacity-0 sm:group-hover:opacity-100 sm:focus-visible:opacity-100"
                  aria-label="Eliminar notificación"
                  title="Eliminar"
                  onClick={() => remove.mutate(notification.id, { onError: (error) => toast.error(errorMessage(error)) })}
                >
                  <Trash2 />
                </Button>
              </li>
            ))}
          </ul>
        )}
      </div>
    </PageContainer>
  )
}
