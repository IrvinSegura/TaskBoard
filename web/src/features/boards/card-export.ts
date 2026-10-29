import type { ExportTone } from '@/lib/export/export-document'
import { dueStatus } from '@/lib/utils/format'
import type { CardPriority } from './boards-api'

// The colors the board gives priorities and due dates, carried into the PDF and Excel exports.

export const priorityTone: Record<CardPriority, ExportTone | null> = {
  None: 'muted',
  Low: null,
  Medium: null,
  High: 'warning',
  Urgent: 'danger',
}

export function dueTone(dueDate: string | null): ExportTone | null {
  if (!dueDate) {
    return null
  }
  const status = dueStatus(dueDate)
  return status === 'overdue' ? 'danger' : status === 'soon' ? 'warning' : null
}

export function checklistTone(checklist: { done: number; total: number } | null): ExportTone | null {
  return checklist && checklist.total > 0 && checklist.done === checklist.total ? 'success' : null
}
