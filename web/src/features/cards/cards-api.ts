import { api, requestRaw, type CreatedResponse, type PagedResponse } from '@/lib/api/client'
import type { CardPriority } from '@/features/boards/boards-api'

export interface CardAssignee {
  userId: string
  displayName: string
}

export interface CardAttachment {
  id: string
  fileName: string
  contentType: string
  sizeInBytes: number
  uploadedById: string
  createdAt: string
}

export interface CardDetail {
  id: string
  boardId: string
  columnId: string
  title: string
  description: string | null
  priority: CardPriority
  dueDate: string | null
  position: number
  isArchived: boolean
  createdById: string
  createdAt: string
  commentCount: number
  assignees: CardAssignee[]
  attachments: CardAttachment[]
}

export interface CardComment {
  id: string
  authorId: string
  authorName: string
  body: string
  isEdited: boolean
  createdAt: string
}

export interface UpdateCardRequest {
  title: string
  description: string | null
  priority: CardPriority
  dueDate: string | null
}

export const commentsPageSize = 20

export const cardsApi = {
  get: (cardId: string) => api.get<CardDetail>(`/cards/${cardId}`),
  update: (cardId: string, request: UpdateCardRequest) => api.put(`/cards/${cardId}`, request),
  archive: (cardId: string) => api.post(`/cards/${cardId}/archive`),
  restore: (cardId: string) => api.post(`/cards/${cardId}/restore`),
  assign: (cardId: string, userId: string) => api.post(`/cards/${cardId}/assignees`, { userId }),
  unassign: (cardId: string, userId: string) => api.delete(`/cards/${cardId}/assignees/${userId}`),

  comments: (cardId: string, page: number) =>
    api.get<PagedResponse<CardComment>>(`/cards/${cardId}/comments`, {
      query: { page, pageSize: commentsPageSize },
    }),
  addComment: (cardId: string, body: string) => api.post<CreatedResponse>(`/cards/${cardId}/comments`, { body }),
  editComment: (commentId: string, body: string) => api.patch(`/comments/${commentId}`, { body }),
  deleteComment: (commentId: string) => api.delete(`/comments/${commentId}`),

  uploadAttachment: (cardId: string, file: File) => {
    const form = new FormData()
    form.append('file', file)
    return api.post<CreatedResponse>(`/cards/${cardId}/attachments`, form)
  },
  deleteAttachment: (attachmentId: string) => api.delete(`/attachments/${attachmentId}`),
  /** Downloads go through the API (membership check, bearer token), so they cannot be plain links. */
  downloadAttachment: async (attachment: CardAttachment) => {
    const response = await requestRaw(`/attachments/${attachment.id}`)
    const url = URL.createObjectURL(await response.blob())
    const link = document.createElement('a')
    link.href = url
    link.download = attachment.fileName
    document.body.append(link)
    link.click()
    link.remove()
    setTimeout(() => URL.revokeObjectURL(url), 10_000)
  },
}

// Same list and limit as AttachmentRules in the API, to reject a file before uploading it.
export const allowedAttachmentExtensions = [
  '.png',
  '.jpg',
  '.jpeg',
  '.gif',
  '.pdf',
  '.txt',
  '.csv',
  '.zip',
  '.docx',
  '.xlsx',
]
export const maxAttachmentBytes = 10 * 1024 * 1024

export function attachmentProblem(file: File): string | null {
  const extension = file.name.includes('.') ? file.name.slice(file.name.lastIndexOf('.')).toLowerCase() : ''
  if (!allowedAttachmentExtensions.includes(extension)) {
    return `No se admiten archivos ${extension || 'sin extensión'}. Formatos permitidos: ${allowedAttachmentExtensions.join(', ')}.`
  }
  if (file.size > maxAttachmentBytes) {
    return 'El archivo supera el máximo de 10 MB.'
  }
  if (file.size === 0) {
    return 'El archivo está vacío.'
  }
  return null
}
