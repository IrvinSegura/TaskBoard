import type { BoardLabel } from '@/features/labels/labels-api'
import { exportTable, type ExportDocument } from '@/lib/export/export-document'
import type { CardRow } from './board-list-rows'
import { checklistTone, dueTone, priorityTone } from './card-export'
import { priorityMeta } from './card-meta'

/** The rows on screen, filtered and sorted as they are, for the PDF and Excel exports. */
export function boardListDocument(
  boardName: string,
  rows: CardRow[],
  totalCards: number,
  membersById: Map<string, string>,
  labelsById: Map<string, BoardLabel>,
): ExportDocument {
  const overdue = rows.filter((row) => dueTone(row.card.dueDate) === 'danger').length
  const unassigned = rows.filter((row) => row.card.assigneeIds.length === 0).length
  const urgent = rows.filter((row) => row.card.priority === 'Urgent' || row.card.priority === 'High').length

  return {
    title: boardName,
    subtitle:
      rows.length === totalCards
        ? 'Lista de tarjetas del tablero'
        : `Lista de tarjetas del tablero, filtrada (${rows.length} de ${totalCards})`,
    fileName: boardName,
    summary: [
      { label: 'Tarjetas', value: rows.length, hint: rows.length === totalCards ? 'todo el tablero' : `de ${totalCards}` },
      { label: 'Vencidas', value: overdue, hint: 'con la fecha límite pasada', tone: 'danger' },
      { label: 'Prioridad alta o urgente', value: urgent, hint: 'para atender primero', tone: 'warning' },
      { label: 'Sin asignar', value: unassigned, hint: 'sin persona responsable', tone: 'warning' },
    ],
    tables: [
      exportTable(
        'Tarjetas',
        [
          { header: 'Tarjeta', value: (row) => row.card.title, width: 38 },
          { header: 'Columna', value: (row) => row.columnName, width: 16 },
          {
            header: 'Prioridad',
            value: (row) => priorityMeta[row.card.priority].label,
            width: 14,
            tone: (row) => priorityTone[row.card.priority],
          },
          {
            header: 'Asignadas',
            value: (row) => row.card.assigneeIds.map((id) => membersById.get(id) ?? 'Sin acceso').join(', ') || null,
            width: 24,
          },
          {
            header: 'Etiquetas',
            value: (row) =>
              row.card.labelIds
                .map((id) => labelsById.get(id)?.name)
                .filter(Boolean)
                .join(', ') || null,
            width: 20,
          },
          {
            header: 'Fecha límite',
            value: (row) => (row.card.dueDate ? new Date(row.card.dueDate) : null),
            kind: 'date',
            width: 14,
            tone: (row) => dueTone(row.card.dueDate),
          },
          {
            header: 'Checklist',
            value: (row) => (row.card.checklist ? `${row.card.checklist.done}/${row.card.checklist.total}` : null),
            width: 11,
            tone: (row) => checklistTone(row.card.checklist),
          },
          {
            header: 'Comentarios',
            value: (row) => row.card.commentCount,
            kind: 'number',
            width: 13,
            total: true,
            tone: (row) => (row.card.commentCount === 0 ? 'muted' : null),
          },
          {
            header: 'Adjuntos',
            value: (row) => row.card.attachmentCount,
            kind: 'number',
            width: 11,
            total: true,
            tone: (row) => (row.card.attachmentCount === 0 ? 'muted' : null),
          },
        ],
        rows,
        'En el orden y con los filtros de la vista de lista.',
      ),
    ],
  }
}
