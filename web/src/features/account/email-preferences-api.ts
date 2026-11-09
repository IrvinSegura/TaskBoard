import { api } from '@/lib/api/client'
import type { NotificationType } from '@/features/notifications/notifications-api'

export interface EmailPreferences {
  /** The notifications the user does not want by email; everything else is sent. */
  mutedTypes: NotificationType[]
}

export const emailPreferencesApi = {
  get: () => api.get<EmailPreferences>('/me/email-preferences'),
  update: (mutedTypes: NotificationType[]) => api.put('/me/email-preferences', { mutedTypes }),
}
