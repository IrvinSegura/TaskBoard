import { priorityTone } from '@/features/boards/card-export'
import { priorityMeta } from '@/features/boards/card-meta'
import { exportTable, type ExportDocument } from '@/lib/export/export-document'
import { percent, upcomingDays, type Report } from './report-data'

/** The report as it is on screen, for the PDF and Excel exports. */
export function reportDocument(report: Report, scope: string): ExportDocument {
  return {
    title: 'Reporte de avance',
    subtitle: scope,
    fileName: `reporte ${scope}`,
    summary: [
      { label: 'Tarjetas abiertas', value: report.open, hint: `de ${report.total} en total`, tone: 'primary' },
      {
        label: 'Completadas',
        value: report.done,
        hint: `${percent(report.done, report.total)}% del total`,
        tone: 'success',
      },
      {
        label: 'Vencidas',
        value: report.overdue,
        hint: `${percent(report.overdue, report.open)}% de las abiertas`,
        tone: 'danger',
      },
      {
        label: 'Sin asignar',
        value: report.unassigned,
        hint: `${percent(report.unassigned, report.open)}% de las abiertas`,
        tone: 'warning',
      },
      {
        label: 'Checklists',
        value: `${percent(report.checklist.done, report.checklist.total)}%`,
        hint: `${report.checklist.done} de ${report.checklist.total} elementos`,
        tone: 'success',
      },
      { label: 'Tableros', value: report.boards.length, hint: `${report.people.length} personas con tareas` },
    ],
    tables: [
      exportTable(
        'Avance por tablero',
        [
          { header: 'Tablero', value: (board) => board.name, width: 30 },
          { header: 'Equipo', value: (board) => board.teamName, width: 18 },
          { header: 'Tarjetas', value: (board) => board.total, total: true },
          { header: 'Completadas', value: (board) => board.done, total: true },
          {
            header: '% completado',
            value: (board) => percent(board.done, board.total),
            kind: 'percent',
            width: 22,
            tone: (board) => (board.total > 0 && board.done === board.total ? 'success' : null),
          },
          {
            header: 'Vencidas',
            value: (board) => board.overdue,
            total: true,
            tone: (board) => (board.overdue > 0 ? 'danger' : 'muted'),
          },
        ],
        report.boards,
        'Tarjetas en la última columna de cada tablero sobre su total.',
      ),
      exportTable(
        'Abiertas por prioridad',
        [
          {
            header: 'Prioridad',
            value: (row) => priorityMeta[row.priority].label,
            width: 20,
            tone: (row) => priorityTone[row.priority],
          },
          { header: 'Tarjetas abiertas', value: (row) => row.count, total: true },
          { header: '% de las abiertas', value: (row) => percent(row.count, report.open), kind: 'percent', width: 24 },
        ],
        report.byPriority,
        'Cómo se reparte el trabajo pendiente.',
      ),
      exportTable(
        'Carga por persona',
        [
          { header: 'Persona', value: (person) => person.name, width: 28 },
          { header: 'Abiertas', value: (person) => person.open, total: true },
          {
            header: 'Vencidas',
            value: (person) => person.overdue,
            total: true,
            tone: (person) => (person.overdue > 0 ? 'danger' : 'muted'),
          },
          {
            header: '% vencidas',
            value: (person) => percent(person.overdue, person.open),
            kind: 'percent',
            width: 22,
          },
        ],
        report.people,
        'Tarjetas abiertas asignadas a cada quien.',
      ),
      exportTable(
        'Próximos vencimientos',
        [
          { header: 'Fecha', value: (day) => day.day, width: 16 },
          {
            header: 'Día',
            value: (day) => day.day.toLocaleDateString('es', { weekday: 'long' }),
            width: 16,
          },
          {
            header: 'Tarjetas que vencen',
            value: (day) => day.count,
            total: true,
            tone: (day) => (day.count === 0 ? 'muted' : null),
          },
        ],
        report.upcoming,
        `Tarjetas abiertas que vencen en los próximos ${upcomingDays} días, desde hoy.`,
      ),
    ],
  }
}
