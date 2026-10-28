import type { ChatMessage, Conversation, LastMessage } from './chat-api'
import { attachmentKind } from './chat-files'

const timeFormat = new Intl.DateTimeFormat('es', { hour: '2-digit', minute: '2-digit' })
const dayFormat = new Intl.DateTimeFormat('es', { weekday: 'long', day: 'numeric', month: 'long' })
const dayWithYearFormat = new Intl.DateTimeFormat('es', { day: 'numeric', month: 'long', year: 'numeric' })
const shortDayFormat = new Intl.DateTimeFormat('es', { day: 'numeric', month: 'short' })

function attachmentsLabel(count: number): string {
  return count === 1 ? '📎 Archivo' : `📎 ${count} archivos`
}

/** One line for a toast: the text, or what kind of file was sent. */
export function messagePreview(message: Pick<ChatMessage, 'body' | 'attachments' | 'isDeleted'>): string {
  if (message.isDeleted) {
    return 'Mensaje eliminado'
  }
  if (message.body) {
    return message.body
  }
  const [first] = message.attachments
  if (message.attachments.length === 1 && first) {
    switch (attachmentKind(first.contentType)) {
      case 'image':
        return '📷 Imagen'
      case 'audio':
        return '🎤 Audio'
      case 'video':
        return '🎬 Video'
    }
  }
  return attachmentsLabel(message.attachments.length)
}

/** The second line of a conversation in the list, with "Tú:" or the sender's name in group chats. */
export function lastMessageText(conversation: Conversation, myId: string): string {
  const last: LastMessage | null = conversation.lastMessage
  if (!last) {
    return conversation.kind === 'Direct' ? 'Empieza la conversación' : 'Todavía no hay mensajes'
  }
  const text = last.isDeleted ? 'Mensaje eliminado' : (last.body ?? attachmentsLabel(last.attachmentCount))
  if (last.senderId === myId) {
    return `Tú: ${text}`
  }
  return conversation.kind === 'Direct' ? text : `${last.senderName.split(' ')[0]}: ${text}`
}

export function formatMessageTime(value: string): string {
  return timeFormat.format(new Date(value))
}

/** "14:05" today, "ayer", or a short date, for the conversation list. */
export function formatListTime(value: string, now: Date = new Date()): string {
  const date = new Date(value)
  const days = dayDifference(date, now)
  if (days === 0) {
    return timeFormat.format(date)
  }
  if (days === 1) {
    return 'ayer'
  }
  return shortDayFormat.format(date)
}

/** "Hoy", "Ayer" or the full date, for the separators between days in a thread. */
export function formatDayLabel(value: string, now: Date = new Date()): string {
  const date = new Date(value)
  const days = dayDifference(date, now)
  if (days === 0) {
    return 'Hoy'
  }
  if (days === 1) {
    return 'Ayer'
  }
  const label = date.getFullYear() === now.getFullYear() ? dayFormat.format(date) : dayWithYearFormat.format(date)
  return label.charAt(0).toUpperCase() + label.slice(1)
}

export function isSameDay(a: string, b: string): boolean {
  return dayDifference(new Date(a), new Date(b)) === 0
}

function dayDifference(date: Date, now: Date): number {
  const start = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime()
  return Math.round((start(now) - start(date)) / (24 * 3600 * 1000))
}

/** What a conversation is, under its title: the team of a board, the people of a group... */
export function conversationSubtitle(conversation: Conversation, myId: string): string {
  switch (conversation.kind) {
    case 'Direct':
      return conversation.participants.find((p) => p.id !== myId)?.email ?? 'Mensaje directo'
    case 'Group':
      return `Grupo · ${conversation.participants.length} personas`
    case 'Team':
      return 'Canal del equipo'
    case 'Board':
      return conversation.teamName ? `Tablero de ${conversation.teamName}` : 'Canal del tablero'
  }
}
