import { MessageSquarePlus, Search } from 'lucide-react'
import { useMemo, useState } from 'react'
import { NavLink } from 'react-router'
import { Button } from '@/components/ui/button'
import { EmptyState, ErrorState, Skeleton } from '@/components/ui/feedback'
import { useCurrentUser } from '@/features/auth/auth-provider'
import { cn } from '@/lib/utils/cn'
import type { Conversation, ConversationKind } from './chat-api'
import { useConversations } from './chat-queries'
import { formatListTime, lastMessageText } from './chat-text'
import { ConversationAvatar } from './conversation-avatar'

type Filter = 'all' | 'unread' | 'people' | 'channels'

const filters: { value: Filter; label: string }[] = [
  { value: 'all', label: 'Todas' },
  { value: 'unread', label: 'No leídas' },
  { value: 'people', label: 'Personas' },
  { value: 'channels', label: 'Equipos y tableros' },
]

const peopleKinds: ConversationKind[] = ['Direct', 'Group']

function matches(conversation: Conversation, filter: Filter, search: string): boolean {
  if (filter === 'unread' && conversation.unreadCount === 0) {
    return false
  }
  if (filter === 'people' && !peopleKinds.includes(conversation.kind)) {
    return false
  }
  if (filter === 'channels' && peopleKinds.includes(conversation.kind)) {
    return false
  }
  const needle = search.trim().toLowerCase()
  return (
    !needle ||
    conversation.title.toLowerCase().includes(needle) ||
    (conversation.teamName?.toLowerCase().includes(needle) ?? false) ||
    conversation.participants.some((p) => p.displayName.toLowerCase().includes(needle))
  )
}

export function ConversationList({ onNewConversation }: { onNewConversation: () => void }) {
  const me = useCurrentUser()
  const conversations = useConversations()
  const [search, setSearch] = useState('')
  const [filter, setFilter] = useState<Filter>('all')

  const visible = useMemo(
    () => conversations.data?.filter((c) => matches(c, filter, search)) ?? [],
    [conversations.data, filter, search],
  )

  return (
    <div className="flex h-full flex-col">
      <div className="space-y-3 border-b border-line px-4 pt-5 pb-3">
        <div className="flex items-center justify-between">
          <h1 className="text-xl font-semibold tracking-tight">Mensajes</h1>
          <Button size="icon-sm" variant="ghost" onClick={onNewConversation} aria-label="Nueva conversación">
            <MessageSquarePlus />
          </Button>
        </div>
        <label className="relative block">
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-faint" />
          <input
            type="search"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Buscar conversaciones"
            className="h-9 w-full rounded-lg border border-line bg-subtle pr-3 pl-9 text-sm placeholder:text-faint focus:border-primary focus:bg-surface focus:ring-3 focus:ring-ring focus:outline-none"
          />
        </label>
        <div className="scrollbar-thin -mx-1 flex gap-1 overflow-x-auto px-1" role="tablist" aria-label="Filtrar">
          {filters.map((item) => (
            <button
              key={item.value}
              type="button"
              role="tab"
              aria-selected={filter === item.value}
              onClick={() => setFilter(item.value)}
              className={cn(
                'shrink-0 cursor-pointer rounded-full px-2.5 py-1 text-xs font-medium transition-colors',
                filter === item.value ? 'bg-primary text-primary-fg' : 'bg-muted-surface text-muted hover:text-fg',
              )}
            >
              {item.label}
            </button>
          ))}
        </div>
      </div>

      <div className="scrollbar-thin min-h-0 flex-1 overflow-y-auto p-2">
        {conversations.isPending ? (
          <div className="space-y-2 p-2">
            {[0, 1, 2, 3].map((i) => (
              <Skeleton key={i} className="h-14" />
            ))}
          </div>
        ) : conversations.isError ? (
          <ErrorState error={conversations.error} onRetry={() => conversations.refetch()} className="m-2 py-8" />
        ) : visible.length === 0 ? (
          <EmptyState
            icon={MessageSquarePlus}
            title={conversations.data.length === 0 ? 'Sin conversaciones' : 'Nada por aquí'}
            description={
              conversations.data.length === 0
                ? 'Escribe a alguien de tus equipos o crea un grupo.'
                : 'Ninguna conversación coincide con el filtro.'
            }
            action={
              conversations.data.length === 0 && (
                <Button size="sm" onClick={onNewConversation}>
                  Nueva conversación
                </Button>
              )
            }
            className="m-2 border-none py-10"
          />
        ) : (
          <ul className="space-y-0.5">
            {visible.map((conversation) => (
              <li key={conversation.id}>
                <ConversationRow conversation={conversation} myId={me.id} />
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}

function ConversationRow({ conversation, myId }: { conversation: Conversation; myId: string }) {
  const unread = conversation.unreadCount > 0
  const time = conversation.lastMessage?.createdAt ?? conversation.createdAt

  return (
    <NavLink
      to={`/chat/${conversation.id}`}
      className={({ isActive }) =>
        cn(
          'flex items-center gap-3 rounded-xl px-2.5 py-2 transition-colors',
          isActive ? 'bg-primary-soft' : 'hover:bg-muted-surface',
        )
      }
    >
      <ConversationAvatar conversation={conversation} myId={myId} />
      <span className="min-w-0 flex-1">
        <span className="flex items-baseline gap-2">
          <span className={cn('truncate text-sm', unread ? 'font-semibold text-fg' : 'font-medium')}>
            {conversation.title}
          </span>
          <span className={cn('ml-auto shrink-0 text-[11px] tabular-nums', unread ? 'text-primary' : 'text-faint')}>
            {formatListTime(time)}
          </span>
        </span>
        <span className="flex items-center gap-2">
          <span className={cn('truncate text-[13px]', unread ? 'text-fg' : 'text-muted')}>
            {lastMessageText(conversation, myId)}
          </span>
          {unread && (
            <span className="ml-auto shrink-0 rounded-full bg-primary px-1.5 text-[11px] leading-5 font-semibold text-primary-fg tabular-nums">
              {conversation.unreadCount > 99 ? '99+' : conversation.unreadCount}
            </span>
          )}
        </span>
      </span>
    </NavLink>
  )
}
