import { describe, expect, it } from 'vitest'
import type { ChatAttachment, Conversation } from './chat-api'
import { formatDayLabel, formatListTime, lastMessageText, messagePreview } from './chat-text'

const photo: ChatAttachment = { id: '1', fileName: 'a.png', contentType: 'image/png', sizeInBytes: 1 }
const voice: ChatAttachment = { id: '2', fileName: 'nota.weba', contentType: 'audio/webm', sizeInBytes: 1 }

function conversation(overrides: Partial<Conversation>): Conversation {
  return {
    id: 'c',
    kind: 'Group',
    title: 'Lanzamiento',
    boardId: null,
    teamId: null,
    teamName: null,
    participants: [],
    lastMessage: null,
    unreadCount: 0,
    createdAt: '2026-10-27T10:00:00Z',
    ...overrides,
  }
}

describe('chat text', () => {
  it('describes messages that only carry files', () => {
    expect(messagePreview({ body: null, attachments: [photo], isDeleted: false })).toBe('📷 Imagen')
    expect(messagePreview({ body: null, attachments: [voice], isDeleted: false })).toBe('🎤 Audio')
    expect(messagePreview({ body: null, attachments: [photo, voice], isDeleted: false })).toBe('📎 2 archivos')
    expect(messagePreview({ body: 'Hola', attachments: [photo], isDeleted: false })).toBe('Hola')
    expect(messagePreview({ body: null, attachments: [], isDeleted: true })).toBe('Mensaje eliminado')
  })

  it('says who wrote the last message', () => {
    const last = {
      id: 'm',
      senderId: 'ana',
      senderName: 'Ana Ruiz',
      body: 'Listo',
      attachmentCount: 0,
      isDeleted: false,
      createdAt: '2026-10-27T10:00:00Z',
    }
    expect(lastMessageText(conversation({ lastMessage: last }), 'ana')).toBe('Tú: Listo')
    expect(lastMessageText(conversation({ lastMessage: last }), 'beto')).toBe('Ana: Listo')
    expect(lastMessageText(conversation({ kind: 'Direct', lastMessage: last }), 'beto')).toBe('Listo')
    expect(lastMessageText(conversation({ kind: 'Direct' }), 'beto')).toBe('Empieza la conversación')
  })

  it('formats times relative to today', () => {
    const now = new Date(2026, 9, 27, 18, 0)
    expect(formatListTime(new Date(2026, 9, 27, 9, 5).toISOString(), now)).toMatch(/09:05/)
    expect(formatListTime(new Date(2026, 9, 26, 23, 0).toISOString(), now)).toBe('ayer')
    expect(formatDayLabel(new Date(2026, 9, 27, 1, 0).toISOString(), now)).toBe('Hoy')
    expect(formatDayLabel(new Date(2026, 9, 26, 1, 0).toISOString(), now)).toBe('Ayer')
    expect(formatDayLabel(new Date(2025, 0, 3).toISOString(), now)).toMatch(/2025/)
  })
})
