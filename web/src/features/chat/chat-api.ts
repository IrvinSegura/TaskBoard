import { api, requestRaw, type CreatedResponse } from '@/lib/api/client'

export type ConversationKind = 'Direct' | 'Group' | 'Board' | 'Team'

export interface ChatUser {
  id: string
  displayName: string
  email: string
}

export interface ChatAttachment {
  id: string
  fileName: string
  contentType: string
  sizeInBytes: number
}

export interface ChatMessage {
  id: string
  conversationId: string
  senderId: string
  senderName: string
  body: string | null
  isEdited: boolean
  isDeleted: boolean
  createdAt: string
  attachments: ChatAttachment[]
}

export interface LastMessage {
  id: string
  senderId: string
  senderName: string
  body: string | null
  attachmentCount: number
  isDeleted: boolean
  createdAt: string
}

/**
 * A conversation as the signed-in user sees it: the title is the group name, the board or team name,
 * or the other person's name. Board conversations carry their team in teamId and teamName.
 */
export interface Conversation {
  id: string
  kind: ConversationKind
  title: string
  boardId: string | null
  teamId: string | null
  teamName: string | null
  participants: ChatUser[]
  lastMessage: LastMessage | null
  unreadCount: number
  createdAt: string
}

/** One page of history, oldest first. hasMore says whether older messages remain. */
export interface ChatMessagesPage {
  items: ChatMessage[]
  hasMore: boolean
}

export const messagesPageSize = 40

export const chatApi = {
  conversations: () => api.get<Conversation[]>('/chat/conversations'),
  conversation: (id: string) => api.get<Conversation>(`/chat/conversations/${id}`),
  contacts: () => api.get<ChatUser[]>('/chat/contacts'),

  startDirect: (target: { userId: string } | { email: string }) =>
    api.post<CreatedResponse>('/chat/conversations/direct', target),
  createGroup: (title: string, participantIds: string[]) =>
    api.post<CreatedResponse>('/chat/conversations/group', { title, participantIds }),
  rename: (id: string, title: string) => api.patch(`/chat/conversations/${id}`, { title }),
  addParticipants: (id: string, userIds: string[]) =>
    api.post(`/chat/conversations/${id}/participants`, { userIds }),
  leave: (id: string) => api.delete(`/chat/conversations/${id}/participants/me`),
  markRead: (id: string) => api.post(`/chat/conversations/${id}/read`),

  messages: (id: string, before?: string) =>
    api.get<ChatMessagesPage>(`/chat/conversations/${id}/messages`, {
      query: { before, limit: messagesPageSize },
    }),
  send: (id: string, body: string, files: File[]) => {
    const form = new FormData()
    if (body.trim()) {
      form.append('body', body)
    }
    for (const file of files) {
      form.append('files', file)
    }
    return api.post<ChatMessage>(`/chat/conversations/${id}/messages`, form)
  },
  edit: (messageId: string, body: string) => api.patch(`/chat/messages/${messageId}`, { body }),
  delete: (messageId: string) => api.delete(`/chat/messages/${messageId}`),

  /** Files go through the API (access check, bearer token), so they cannot be plain links or <img> sources. */
  attachmentBlob: async (attachmentId: string) => (await requestRaw(`/chat/attachments/${attachmentId}`)).blob(),
}
