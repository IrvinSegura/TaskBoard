import { checklistTone, dueTone, priorityTone } from '@/features/boards/card-export'
import { priorityMeta } from '@/features/boards/card-meta'
import { exportTable, type ExportDocument, type ExportTone } from '@/lib/export/export-document'
import { dueGroup, dueGroupLabels, summarize, type DueGroup, type MyCard } from './my-tasks-api'

const groupTone: Record<DueGroup, ExportTone | null> = {
  overdue: 'danger',
  today: 'warning',
  week: null,
  later: null,
  none: 'muted',
}

/** The assigned cards in the order the page groups them, for the PDF and Excel exports. */
export function myTasksDocument(cards: MyCard[]): ExportDocument {
  const summary = summarize(cards)
  const boards = new Set(cards.map((card) => card.boardId)).size
  return {
    title: 'Mis tareas',
    subtitle: 'Las tarjetas que tienes asignadas en todos tus tableros',
    fileName: 'mis tareas',
    summary: [
      { label: 'Asignadas', value: summary.total, hint: `en ${boards} ${boards === 1 ? 'tablero' : 'tableros'}` },
      { label: 'Vencidas', value: summary.overdue, hint: 'con la fecha límite pasada', tone: 'danger' },
      { label: 'Para esta semana', value: summary.thisWeek, hint: 'hoy y los próximos 7 días', tone: 'warning' },
    ],
    tables: [
      exportTable(
        'Tarjetas asignadas',
        [
          { header: 'Tarjeta', value: (card) => card.title, width: 36 },
          {
            header: 'Vencimiento',
            value: (card) => dueGroupLabels[dueGroup(card.dueDate)],
            width: 16,
            tone: (card) => groupTone[dueGroup(card.dueDate)],
          },
          {
            header: 'Fecha límite',
            value: (card) => (card.dueDate ? new Date(card.dueDate) : null),
            kind: 'date',
            width: 14,
            tone: (card) => dueTone(card.dueDate),
          },
          {
            header: 'Prioridad',
            value: (card) => priorityMeta[card.priority].label,
            width: 14,
            tone: (card) => priorityTone[card.priority],
          },
          { header: 'Tablero', value: (card) => card.boardName, width: 22 },
          { header: 'Columna', value: (card) => card.columnName, width: 14 },
          { header: 'Equipo', value: (card) => card.teamName, width: 14 },
          { header: 'Etiquetas', value: (card) => card.labels.map((label) => label.name).join(', ') || null, width: 18 },
          {
            header: 'Checklist',
            value: (card) => (card.checklist ? `${card.checklist.done}/${card.checklist.total}` : null),
            width: 11,
            tone: (card) => checklistTone(card.checklist),
          },
        ],
        cards,
        'Agrupadas por vencimiento, de lo más urgente a lo que no tiene fecha.',
      ),
    ],
  }
}
