import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { cardsApi } from '@/features/cards/cards-api'
import { errorMessage } from '@/lib/api/api-error'
import { queryKeys } from '@/lib/api/query-keys'
import { useHubEvent } from '@/lib/realtime/realtime-provider'
import { notificationText } from './notification-text'
import { notificationsApi, type AppNotification } from './notifications-api'

export function useNotifications() {
  return useQuery({
    queryKey: queryKeys.notifications(),
    queryFn: notificationsApi.list,
    // Newest first, whatever order the API used.
    select: (items) => [...items].sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt)),
  })
}

export function useUnreadCount(): number {
  const { data } = useNotifications()
  return data?.filter((n) => !n.isRead).length ?? 0
}

export function useMarkAsRead() {
  const queryClient = useQueryClient()
  const key = queryKeys.notifications()
  return useMutation({
    mutationFn: (id: string) => notificationsApi.markAsRead(id),
    onMutate: async (id) => {
      await queryClient.cancelQueries({ queryKey: key })
      const previous = queryClient.getQueryData<AppNotification[]>(key)
      queryClient.setQueryData<AppNotification[]>(key, (items) =>
        items?.map((n) => (n.id === id ? { ...n, isRead: true } : n)),
      )
      return { previous }
    },
    onError: (_error, _id, context) => queryClient.setQueryData(key, context?.previous),
    onSettled: () => queryClient.invalidateQueries({ queryKey: key }),
  })
}

export function useMarkAllAsRead() {
  const queryClient = useQueryClient()
  const key = queryKeys.notifications()
  return useMutation({
    mutationFn: () => notificationsApi.markAllAsRead(),
    onMutate: async () => {
      await queryClient.cancelQueries({ queryKey: key })
      const previous = queryClient.getQueryData<AppNotification[]>(key)
      queryClient.setQueryData<AppNotification[]>(key, (items) => items?.map((n) => ({ ...n, isRead: true })))
      return { previous }
    },
    onError: (_error, _variables, context) => queryClient.setQueryData(key, context?.previous),
    onSettled: () => queryClient.invalidateQueries({ queryKey: key }),
  })
}

export function useDeleteNotification() {
  const queryClient = useQueryClient()
  const key = queryKeys.notifications()
  return useMutation({
    mutationFn: (id: string) => notificationsApi.delete(id),
    onMutate: async (id) => {
      await queryClient.cancelQueries({ queryKey: key })
      const previous = queryClient.getQueryData<AppNotification[]>(key)
      queryClient.setQueryData<AppNotification[]>(key, (items) => items?.filter((n) => n.id !== id))
      return { previous }
    },
    onError: (_error, _id, context) => queryClient.setQueryData(key, context?.previous),
    onSettled: () => queryClient.invalidateQueries({ queryKey: key }),
  })
}

/** Where a notification leads: its card, board or team. */
export function useOpenNotification() {
  const navigate = useNavigate()
  const markAsRead = useMarkAsRead()

  return async (notification: AppNotification) => {
    if (!notification.isRead) {
      markAsRead.mutate(notification.id)
    }
    const id = notification.resourceId
    if (!id) {
      return
    }
    try {
      switch (notification.type) {
        case 'CardAssigned':
        case 'CommentAdded':
        case 'Mentioned': {
          // The notification only has the card id; the card says which board it is on.
          const card = await cardsApi.get(id)
          navigate(`/boards/${card.boardId}/cards/${card.id}`)
          break
        }
        case 'AddedToBoard':
          navigate(`/boards/${id}`)
          break
        case 'AddedToTeam':
          navigate(`/teams/${id}`)
          break
      }
    } catch (error) {
      toast.error(errorMessage(error))
    }
  }
}

interface NotificationMessage {
  notificationId: string
  message: string
}

/** Live notifications: refreshes the list and shows a toast. Mounted once, in the app shell. */
export function useNotificationStream() {
  const queryClient = useQueryClient()
  useHubEvent<NotificationMessage>('NotificationReceived', (message) => {
    void queryClient.invalidateQueries({ queryKey: queryKeys.notifications() })
    // Being added to a team or board changes what the sidebar lists.
    void queryClient.invalidateQueries({ queryKey: queryKeys.teams() })
    toast(notificationText(message.message), { icon: '🔔' })
  })
}
