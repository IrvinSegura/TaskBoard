import { api } from '@/lib/api/client'
import type { AccessTokenResponse } from '@/lib/api/session'

export interface CurrentUser {
  id: string
  email: string
  displayName: string
  createdAt: string
}

export interface RegisterRequest {
  email: string
  displayName: string
  password: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export const authApi = {
  login: (email: string, password: string) =>
    api.post<AccessTokenResponse>('/auth/login', { email, password }, { auth: false }),
  register: (request: RegisterRequest) => api.post<{ id: string }>('/auth/register', request, { auth: false }),
  logout: () => api.post('/auth/logout', undefined, { auth: false }),
  me: () => api.get<CurrentUser>('/me'),
  updateProfile: (displayName: string) => api.patch('/me', { displayName }),
  changePassword: (request: ChangePasswordRequest) => api.post('/me/password', request),
}
