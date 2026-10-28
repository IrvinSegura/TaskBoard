import { ArrowLeft, ExternalLink, MessagesSquare, Settings2, Upload } from 'lucide-react'
import { useCallback, useEffect, useLayoutEffect, useRef, useState, type DragEvent, type ReactNode } from 'react'
import { Link, useParams } from 'react-router'
import { Button, buttonStyles } from '@/components/ui/button'
import { EmptyState, ErrorState, Skeleton } from '@/components/ui/feedback'
import { Spinner } from '@/components/ui/spinner'
import { useCurrentUser } from '@/features/auth/auth-provider'
import type { ChatMessage, Conversation } from './chat-api'
import { ChatMessageItem } from './chat-message'
import {
  flattenMessages,
  setOpenConversation,
  useChatMessages,
  useConversation,
  useMarkConversationRead,
} from './chat-queries'
import { conversationSubtitle, formatDayLabel, isSameDay } from './chat-text'
import { ConversationAvatar } from './conversation-avatar'
import { GroupSettingsDialog } from './group-settings-dialog'
import { MessageComposer } from './message-composer'
import { usePendingFiles } from './use-pending-files'

// Messages from one person this close together share one avatar and name.
const groupingWindowMs = 5 * 60 * 1000

function startsGroup(message: ChatMessage, previous: ChatMessage | undefined): boolean {
  return (
    !previous ||
    previous.senderId !== message.senderId ||
    !isSameDay(previous.createdAt, message.createdAt) ||
    Date.parse(message.createdAt) - Date.parse(previous.createdAt) > groupingWindowMs
  )
}

export function ChatThreadRoute() {
  const { conversationId } = useParams() as { conversationId: string }
  // A fresh thread per conversation: scroll position, drafts and pending files never leak between them.
  return <ChatThread key={conversationId} conversationId={conversationId} />
}

function ChatThread({ conversationId }: { conversationId: string }) {
  const { conversation, isPending, error, refetch } = useConversation(conversationId)

  useEffect(() => {
    setOpenConversation(conversationId)
    return () => setOpenConversation(null)
  }, [conversationId])

  if (isPending) {
    return (
      <div className="flex flex-1 flex-col">
        <div className="flex h-16 items-center gap-3 border-b border-line bg-surface px-4">
          <Skeleton className="size-10 rounded-lg" />
          <Skeleton className="h-4 w-40" />
        </div>
      </div>
    )
  }
  if (!conversation) {
    return (
      <div className="m-auto w-full max-w-md p-6">
        {error && !(error instanceof Error && 'status' in error && error.status === 404) ? (
          <ErrorState error={error} onRetry={refetch} />
        ) : (
          <EmptyState
            icon={MessagesSquare}
            title="Conversación no disponible"
            description="No existe o ya no tienes acceso a ella."
            action={
              <Link to="/chat" className={buttonStyles({ variant: 'secondary' })}>
                Volver a mensajes
              </Link>
            }
          />
        )}
      </div>
    )
  }
  return <Thread conversation={conversation} />
}

function Thread({ conversation }: { conversation: Conversation }) {
  const me = useCurrentUser()
  const messages = useChatMessages(conversation.id)
  const markRead = useMarkConversationRead(conversation.id)
  const pending = usePendingFiles()
  const [dragging, setDragging] = useState(false)
  const [settingsOpen, setSettingsOpen] = useState(false)

  const scroller = useRef<HTMLDivElement>(null)
  const content = useRef<HTMLOListElement>(null)
  const topSentinel = useRef<HTMLDivElement>(null)
  const stickToBottom = useRef(true)
  const prependAnchor = useRef<number | null>(null)

  const items = flattenMessages(messages.data)
  const oldestId = items[0]?.id
  const { hasNextPage, isFetchingNextPage, fetchNextPage } = messages

  // Read while it is on screen: when it opens, when new messages arrive and when the tab comes back.
  const unread = conversation.unreadCount
  const markReadNow = markRead.mutate
  useEffect(() => {
    const markIfVisible = () => {
      if (unread > 0 && document.visibilityState === 'visible') {
        markReadNow()
      }
    }
    markIfVisible()
    document.addEventListener('visibilitychange', markIfVisible)
    return () => document.removeEventListener('visibilitychange', markIfVisible)
  }, [unread, markReadNow])

  // Stay at the bottom while it is there: new messages, images that finish loading, a growing composer.
  useEffect(() => {
    const element = scroller.current
    const list = content.current
    if (!element || !list) {
      return
    }
    const observer = new ResizeObserver(() => {
      if (stickToBottom.current) {
        element.scrollTop = element.scrollHeight
      }
    })
    observer.observe(list)
    observer.observe(element)
    return () => observer.disconnect()
  }, [messages.isSuccess])

  // Older pages go on top; keep what was on screen where it was.
  useLayoutEffect(() => {
    const element = scroller.current
    if (element && prependAnchor.current !== null) {
      element.scrollTop = element.scrollHeight - prependAnchor.current
      prependAnchor.current = null
    }
  }, [oldestId])

  const loadOlder = useCallback(() => {
    const element = scroller.current
    if (element && hasNextPage && !isFetchingNextPage) {
      prependAnchor.current = element.scrollHeight - element.scrollTop
      void fetchNextPage()
    }
  }, [hasNextPage, isFetchingNextPage, fetchNextPage])

  useEffect(() => {
    const sentinel = topSentinel.current
    if (!sentinel) {
      return
    }
    const observer = new IntersectionObserver(([entry]) => entry?.isIntersecting && loadOlder(), {
      root: scroller.current,
      rootMargin: '200px 0px 0px 0px',
    })
    observer.observe(sentinel)
    return () => observer.disconnect()
  }, [loadOlder])

  const onScroll = () => {
    const element = scroller.current
    if (element) {
      stickToBottom.current = element.scrollHeight - element.scrollTop - element.clientHeight < 80
    }
  }

  const scrollToBottom = () => {
    stickToBottom.current = true
    const element = scroller.current
    if (element) {
      element.scrollTop = element.scrollHeight
    }
  }

  const onDrop = (event: DragEvent) => {
    event.preventDefault()
    setDragging(false)
    pending.add(Array.from(event.dataTransfer.files))
  }

  const showSenderNames = conversation.kind !== 'Direct'
  const link =
    conversation.kind === 'Board' && conversation.boardId
      ? { to: `/boards/${conversation.boardId}`, label: 'Abrir tablero' }
      : conversation.kind === 'Team' && conversation.teamId
        ? { to: `/teams/${conversation.teamId}`, label: 'Ver equipo' }
        : null

  return (
    <div
      className="relative flex min-w-0 flex-1 flex-col"
      onDragOver={(event) => {
        if (event.dataTransfer.types.includes('Files')) {
          event.preventDefault()
          setDragging(true)
        }
      }}
      onDragLeave={(event) => {
        if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
          setDragging(false)
        }
      }}
      onDrop={onDrop}
    >
      <header className="flex h-16 shrink-0 items-center gap-3 border-b border-line bg-surface px-3 sm:px-4">
        <Link to="/chat" className={buttonStyles({ variant: 'ghost', size: 'icon', className: 'md:hidden' })} aria-label="Volver a mensajes">
          <ArrowLeft />
        </Link>
        <ConversationAvatar conversation={conversation} myId={me.id} />
        <div className="min-w-0 flex-1">
          <h2 className="truncate text-[15px] font-semibold">{conversation.title}</h2>
          <p className="truncate text-xs text-muted">{conversationSubtitle(conversation, me.id)}</p>
        </div>
        {link && (
          <Link to={link.to} className={buttonStyles({ variant: 'secondary', size: 'sm' })}>
            <ExternalLink />
            <span className="hidden sm:inline">{link.label}</span>
          </Link>
        )}
        {conversation.kind === 'Group' && (
          <Button variant="ghost" size="icon" onClick={() => setSettingsOpen(true)} aria-label="Ajustes del grupo">
            <Settings2 />
          </Button>
        )}
      </header>

      <div ref={scroller} onScroll={onScroll} className="scrollbar-thin min-h-0 flex-1 overflow-y-auto px-3 py-4 sm:px-6">
        <div ref={topSentinel} />
        {messages.isPending ? (
          <div className="space-y-4">
            <Skeleton className="h-10 w-2/3" />
            <Skeleton className="ml-auto h-10 w-1/2" />
            <Skeleton className="h-16 w-3/5" />
          </div>
        ) : messages.isError ? (
          <ErrorState error={messages.error} onRetry={() => messages.refetch()} className="mx-auto max-w-md" />
        ) : (
          <>
            {isFetchingNextPage && (
              <div className="flex justify-center py-2">
                <Spinner className="size-5 text-faint" />
              </div>
            )}
            {!hasNextPage && (
              <div className="mx-auto mb-6 max-w-sm pt-6 text-center">
                <div className="mx-auto mb-3 w-fit">
                  <ConversationAvatar conversation={conversation} myId={me.id} />
                </div>
                <p className="text-sm font-medium">{conversation.title}</p>
                <p className="mt-1 text-xs text-muted">
                  {items.length === 0 ? 'Todavía no hay mensajes. ¡Escribe el primero!' : 'Este es el principio de la conversación.'}
                </p>
              </div>
            )}
            <ol ref={content} className="pb-1">
              {items.map((message, index) => {
                const previous = items[index - 1]
                const newDay = !previous || !isSameDay(previous.createdAt, message.createdAt)
                return (
                  <DayAware key={message.id} label={newDay ? formatDayLabel(message.createdAt) : null}>
                    <ChatMessageItem
                      message={message}
                      mine={message.senderId === me.id}
                      startsGroup={startsGroup(message, previous)}
                      showSenderName={showSenderNames}
                    />
                  </DayAware>
                )
              })}
            </ol>
          </>
        )}
      </div>

      <div className="shrink-0 px-3 pt-1 pb-3 sm:px-6 sm:pb-4">
        <MessageComposer conversationId={conversation.id} pending={pending} onSent={scrollToBottom} />
      </div>

      {dragging && (
        <div className="pointer-events-none absolute inset-2 z-10 flex flex-col items-center justify-center gap-2 rounded-2xl border-2 border-dashed border-primary bg-primary-soft/90 text-primary">
          <Upload className="size-7" />
          <p className="text-sm font-medium">Suelta los archivos para adjuntarlos</p>
        </div>
      )}

      {conversation.kind === 'Group' && (
        <GroupSettingsDialog conversation={conversation} open={settingsOpen} onOpenChange={setSettingsOpen} />
      )}
    </div>
  )
}

function DayAware({ label, children }: { label: string | null; children: ReactNode }) {
  return (
    <>
      {label && (
        <li className="my-4 flex items-center gap-3 text-[11px] font-medium text-faint" aria-label={label}>
          <span className="h-px flex-1 bg-line" />
          {label}
          <span className="h-px flex-1 bg-line" />
        </li>
      )}
      {children}
    </>
  )
}
