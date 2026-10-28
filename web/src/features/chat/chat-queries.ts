import {
  useInfiniteQuery,
  useMutation,
  useQuery,
  useQueryClient,
  type InfiniteData,
  type QueryClient,
} from '@tanstack/react-query'
import { HubConnectionState } from '@microsoft/signalr'
import { useEffect, useRef } from 'react'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { useCurrentUser } from '@/features/auth/auth-provider'
import { queryKeys } from '@/lib/api/query-keys'
import { useHubEvents, useRealtime } from '@/lib/realtime/realtime-provider'
import { chatApi, type ChatMessage, type ChatMessagesPage, type Conversation } from './chat-api'
import { messagePreview } from './chat-text'

type MessagesData = InfiniteData<ChatMessagesPage, string | undefined>

// The conversation on screen, so a pushed message there marks it read instead of showing a toast.
let openConversationId: string | null = null

export function setOpenConversation(conversationId: string | null) {
  openConversationId = conversationId
}

function isViewing(conversationId: string): boolean {
  return openConversationId === conversationId && document.visibilityState === 'visible'
}

export function useConversations() {
  return useQuery({ queryKey: queryKeys.conversations(), queryFn: chatApi.conversations })
}

/**
 * Taken from the list, which stays current with every push (unread count, title). Only conversations the list
 * leaves out, such as the chat of an archived board, are fetched on their own.
 */
export function useConversation(conversationId: string) {
  const list = useConversations()
  const fromList = list.data?.find((c) => c.id === conversationId)
  const single = useQuery({
    queryKey: queryKeys.conversation(conversationId),
    queryFn: () => chatApi.conversation(conversationId),
    enabled: list.isSuccess && !fromList,
    retry: false,
  })
  return {
    conversation: fromList ?? single.data,
    isPending: list.isPending || (!fromList && single.isPending && single.fetchStatus !== 'idle'),
    error: list.error ?? single.error,
    refetch: () => void (list.isError ? list.refetch() : single.refetch()),
  }
}

export function useChatUnreadCount(): number {
  const { data } = useConversations()
  return data?.reduce((total, conversation) => total + conversation.unreadCount, 0) ?? 0
}

export function useChatContacts(enabled = true) {
  return useQuery({ queryKey: queryKeys.chatContacts(), queryFn: chatApi.contacts, enabled })
}

/** Pages go from newest to oldest; each page is oldest first. */
export function useChatMessages(conversationId: string) {
  return useInfiniteQuery({
    queryKey: queryKeys.chatMessages(conversationId),
    queryFn: ({ pageParam }) => chatApi.messages(conversationId, pageParam),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (page) => (page.hasMore ? page.items[0]?.id : undefined),
  })
}

/** Oldest first, ready to render top to bottom. */
export function flattenMessages(data: InfiniteData<ChatMessagesPage, unknown> | undefined): ChatMessage[] {
  return data ? [...data.pages].reverse().flatMap((page) => page.items) : []
}

/** Adds a message to the newest page unless it is already there (the sender gets it twice: response and push). */
function addMessage(queryClient: QueryClient, message: ChatMessage) {
  queryClient.setQueryData<MessagesData>(queryKeys.chatMessages(message.conversationId), (data) => {
    if (!data || data.pages.some((page) => page.items.some((m) => m.id === message.id))) {
      return data
    }
    const [newest, ...older] = data.pages
    return { ...data, pages: [{ ...newest!, items: [...newest!.items, message] }, ...older] }
  })
}

function patchMessage(
  queryClient: QueryClient,
  conversationId: string,
  messageId: string,
  patch: Partial<ChatMessage>,
) {
  queryClient.setQueryData<MessagesData>(queryKeys.chatMessages(conversationId), (data) =>
    data
      ? {
          ...data,
          pages: data.pages.map((page) => ({
            ...page,
            items: page.items.map((m) => (m.id === messageId ? { ...m, ...patch } : m)),
          })),
        }
      : data,
  )
}

function refreshConversations(queryClient: QueryClient) {
  void queryClient.invalidateQueries({ queryKey: queryKeys.conversations() })
}

export function useSendMessage(conversationId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ body, files }: { body: string; files: File[] }) => chatApi.send(conversationId, body, files),
    onSuccess: (message) => {
      addMessage(queryClient, message)
      refreshConversations(queryClient)
    },
  })
}

export function useEditMessage(conversationId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ messageId, body }: { messageId: string; body: string }) => chatApi.edit(messageId, body),
    onSuccess: (_result, { messageId, body }) => {
      patchMessage(queryClient, conversationId, messageId, { body: body.trim() || null, isEdited: true })
      refreshConversations(queryClient)
    },
  })
}

export function useDeleteMessage(conversationId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (messageId: string) => chatApi.delete(messageId),
    onSuccess: (_result, messageId) => {
      patchMessage(queryClient, conversationId, messageId, { body: null, isDeleted: true, attachments: [] })
      refreshConversations(queryClient)
    },
  })
}

export function useMarkConversationRead(conversationId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => chatApi.markRead(conversationId),
    // Clear the badge straight away; the server confirms with ConversationRead.
    onMutate: () => {
      queryClient.setQueryData<Conversation[]>(queryKeys.conversations(), (items) =>
        items?.map((c) => (c.id === conversationId ? { ...c, unreadCount: 0 } : c)),
      )
    },
  })
}

export function useStartDirect() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: chatApi.startDirect,
    onSuccess: () => refreshConversations(queryClient),
  })
}

export function useCreateGroup() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ title, participantIds }: { title: string; participantIds: string[] }) =>
      chatApi.createGroup(title, participantIds),
    onSuccess: () => refreshConversations(queryClient),
  })
}

function useGroupMutation<T>(conversationId: string, mutationFn: (variables: T) => Promise<void>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn,
    onSuccess: () => {
      refreshConversations(queryClient)
      void queryClient.invalidateQueries({ queryKey: queryKeys.conversation(conversationId) })
    },
  })
}

export function useRenameGroup(conversationId: string) {
  return useGroupMutation(conversationId, (title: string) => chatApi.rename(conversationId, title))
}

export function useAddParticipants(conversationId: string) {
  return useGroupMutation(conversationId, (userIds: string[]) => chatApi.addParticipants(conversationId, userIds))
}

export function useLeaveGroup(conversationId: string) {
  return useGroupMutation(conversationId, () => chatApi.leave(conversationId))
}

interface ChatEvent {
  message?: ChatMessage
  messageId?: string
  conversationId?: string
  body?: string | null
}

const chatEvents = [
  'ChatMessageReceived',
  'ChatMessageUpdated',
  'ChatMessageDeleted',
  'ConversationUpdated',
  'ConversationRead',
] as const

/** Live chat: keeps the open threads and the unread counts current and shows a toast. Mounted once, in the app shell. */
export function useChatStream() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const me = useCurrentUser()
  const { state } = useRealtime()
  const wasConnected = useRef(false)

  useHubEvents<ChatEvent>(chatEvents, (event, name) => {
    switch (name) {
      case 'ChatMessageReceived': {
        const message = event.message!
        addMessage(queryClient, message)
        refreshConversations(queryClient)
        if (message.senderId !== me.id && !isViewing(message.conversationId)) {
          const title =
            queryClient
              .getQueryData<Conversation[]>(queryKeys.conversations())
              ?.find((c) => c.id === message.conversationId)?.title ?? message.senderName
          toast(message.senderName === title ? title : `${message.senderName} · ${title}`, {
            description: messagePreview(message),
            icon: '💬',
            action: { label: 'Abrir', onClick: () => navigate(`/chat/${message.conversationId}`) },
          })
        }
        break
      }
      case 'ChatMessageUpdated':
        patchMessage(queryClient, event.conversationId!, event.messageId!, { body: event.body ?? null, isEdited: true })
        refreshConversations(queryClient)
        break
      case 'ChatMessageDeleted':
        patchMessage(queryClient, event.conversationId!, event.messageId!, {
          body: null,
          isDeleted: true,
          attachments: [],
        })
        refreshConversations(queryClient)
        break
      case 'ConversationUpdated':
        refreshConversations(queryClient)
        void queryClient.invalidateQueries({ queryKey: queryKeys.conversation(event.conversationId!) })
        break
      case 'ConversationRead':
        refreshConversations(queryClient)
        break
    }
  })

  // Messages pushed while the connection was down are lost; reload what is on screen once it is back.
  useEffect(() => {
    if (state === HubConnectionState.Connected) {
      if (wasConnected.current) {
        void queryClient.invalidateQueries({ queryKey: queryKeys.allChat() })
      }
      wasConnected.current = true
    }
  }, [state, queryClient])
}
