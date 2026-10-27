import type { ActivityEntry } from './activity-api'

/** The sentence after the actor's name, in Spanish, with the names the entry kept. */
export function activityText(entry: ActivityEntry): { action: string; subject?: string; detail?: string } {
  const subject = entry.subject ?? undefined
  const detail = entry.detail ?? undefined
  switch (entry.type) {
    case 'CardCreated':
      return { action: 'creó la tarjeta', subject, detail: detail && `en ${detail}` }
    case 'CardUpdated':
      return { action: 'editó la tarjeta', subject }
    case 'CardMoved':
      return { action: 'movió', subject, detail: detail && `a ${detail}` }
    case 'CardArchived':
      return { action: 'archivó la tarjeta', subject }
    case 'CardRestored':
      return { action: 'restauró la tarjeta', subject }
    case 'CardDeleted':
      return { action: 'eliminó la tarjeta', subject }
    case 'CardAssigned':
      return { action: `asignó a ${detail ?? 'alguien'} a`, subject }
    case 'CardUnassigned':
      return { action: `quitó a ${detail ?? 'alguien'} de`, subject }
    case 'CommentAdded':
      return { action: 'comentó en', subject, detail }
    case 'AttachmentAdded':
      return { action: 'adjuntó un archivo a', subject, detail }
    case 'ColumnCreated':
      return { action: 'creó la columna', subject }
    case 'ColumnRenamed':
      return { action: 'renombró una columna a', subject }
    case 'ColumnDeleted':
      return { action: 'eliminó una columna' }
    case 'BoardRenamed':
      return { action: 'renombró el tablero a', subject }
    case 'BoardArchived':
      return { action: 'archivó el tablero' }
    case 'BoardRestored':
      return { action: 'restauró el tablero' }
    case 'MemberAdded':
      return { action: 'agregó al tablero a', subject }
    case 'MemberRemoved':
      return { action: 'quitó del tablero a', subject }
  }
}
