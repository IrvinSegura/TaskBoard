import type { BoardLabel } from '@/features/labels/labels-api'
import { api, type CreatedResponse } from '@/lib/api/client'

// Ordered like the API's BoardRole enum, so roles compare by privilege.
export const boardRoles = ['Viewer', 'Editor', 'Admin'] as const
export type BoardRole = (typeof boardRoles)[number]

export type BoardVisibility = 'Private' | 'Team'

export const cardPriorities = ['None', 'Low', 'Medium', 'High', 'Urgent'] as const
export type CardPriority = (typeof cardPriorities)[number]

export interface BoardSummary {
  id: string
  name: string
  visibility: BoardVisibility
}

export interface BoardMember {
  userId: string
  displayName: string
  role: BoardRole
}

export interface CardSummary {
  id: string
  title: string
  priority: CardPriority
  dueDate: string | null
  position: number
  assigneeIds: string[]
  labelIds: string[]
  /** Null when the card has no checklist. */
  checklist: { done: number; total: number } | null
}

export interface BoardColumn {
  id: string
  name: string
  position: number
  cards: CardSummary[]
}

export interface BoardDetail {
  id: string
  name: string
  visibility: BoardVisibility
  members: BoardMember[]
  labels: BoardLabel[]
  columns: BoardColumn[]
}

export interface CardMovedResponse {
  cardId: string
  columnId: string
  position: number
}

export const boardsApi = {
  listForTeam: (teamId: string, archived = false) =>
    api.get<BoardSummary[]>(`/teams/${teamId}/boards`, { query: { archived } }),
  create: (teamId: string, name: string, visibility: BoardVisibility) =>
    api.post<CreatedResponse>(`/teams/${teamId}/boards`, { name, visibility }),
  get: (boardId: string) => api.get<BoardDetail>(`/boards/${boardId}`),
  rename: (boardId: string, name: string) => api.patch(`/boards/${boardId}`, { name }),
  archive: (boardId: string) => api.post(`/boards/${boardId}/archive`),
  restore: (boardId: string) => api.post(`/boards/${boardId}/restore`),
  delete: (boardId: string) => api.delete(`/boards/${boardId}`),
  addMember: (boardId: string, userId: string, role: BoardRole) =>
    api.post(`/boards/${boardId}/members`, { userId, role }),
  removeMember: (boardId: string, userId: string) => api.delete(`/boards/${boardId}/members/${userId}`),

  createColumn: (boardId: string, name: string) => api.post<CreatedResponse>(`/boards/${boardId}/columns`, { name }),
  renameColumn: (columnId: string, name: string) => api.patch(`/columns/${columnId}`, { name }),
  moveColumn: (columnId: string, previousColumnId: string | null, nextColumnId: string | null) =>
    api.patch(`/columns/${columnId}/move`, { previousColumnId, nextColumnId }),
  deleteColumn: (columnId: string) => api.delete(`/columns/${columnId}`),

  createCard: (columnId: string, title: string) =>
    api.post<CreatedResponse>(`/columns/${columnId}/cards`, { title, description: null }),
  moveCard: (cardId: string, targetColumnId: string, previousCardId: string | null, nextCardId: string | null) =>
    api.patch<CardMovedResponse>(`/cards/${cardId}/move`, { targetColumnId, previousCardId, nextCardId }),
}
