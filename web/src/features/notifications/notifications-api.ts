import { api } from '@/lib/api/client'

export type NotificationType = 'CardAssigned' | 'CommentAdded' | 'AddedToBoard' | 'AddedToTeam'

export interface AppNotification {
  id: string
  type: NotificationType
  message: string
  resourceId: string | null
  isRead: boolean
  createdAt: string
}

export const notificationsApi = {
  list: () => api.get<AppNotification[]>('/notifications'),
  markAsRead: (notificationId: string) => api.patch(`/notifications/${notificationId}/read`),
}
