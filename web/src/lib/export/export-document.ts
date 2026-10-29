// What a page hands to the PDF and Excel exporters: plain tables, already turned into cells.

export type ExportCell = string | number | Date | null

/** How a column is formatted; percent values go from 0 to 100. */
export type ExportKind = 'text' | 'number' | 'percent' | 'date'

/** Emphasis for one cell, e.g. an overdue date. */
export type ExportTone = 'danger' | 'warning' | 'success' | 'muted'

export interface ExportColumn<T> {
  header: string
  value: (row: T) => ExportCell
  /** Width in characters; Excel uses it as is and the PDF as a proportion. */
  width?: number
  /** Guessed from the values when left out. */
  kind?: ExportKind
  tone?: (row: T) => ExportTone | null
  /** Adds the column up in a totals row. */
  total?: boolean
}

export interface ExportTableColumn {
  header: string
  width: number
  kind: ExportKind
  total: boolean
}

export interface ExportTable {
  name: string
  description?: string
  columns: ExportTableColumn[]
  rows: ExportCell[][]
  /** Same shape as rows. */
  tones: (ExportTone | null)[][]
  /** Sums of the columns marked `total` (null elsewhere), or null when no column is. */
  totals: (number | null)[] | null
}

export interface ExportStat {
  label: string
  value: string | number
  hint?: string
  tone?: 'primary' | 'success' | 'danger' | 'warning'
}

export interface ExportDocument {
  title: string
  subtitle?: string
  /** File name without extension or date, e.g. "reporte". */
  fileName: string
  /** Headline figures, shown as cards above the tables. */
  summary?: ExportStat[]
  tables: ExportTable[]
}

const defaultWidths: Record<ExportKind, number> = { text: 18, number: 12, percent: 16, date: 14 }

function guessKind(cells: ExportCell[]): ExportKind {
  const present = cells.filter((cell) => cell !== null)
  if (present.length > 0 && present.every((cell) => cell instanceof Date)) {
    return 'date'
  }
  return present.length > 0 && present.every((cell) => typeof cell === 'number') ? 'number' : 'text'
}

export function exportTable<T>(
  name: string,
  columns: ExportColumn<T>[],
  rows: T[],
  description?: string,
): ExportTable {
  const cells = rows.map((row) => columns.map((column) => column.value(row)))
  const tableColumns = columns.map((column, i) => {
    const kind = column.kind ?? guessKind(cells.map((row) => row[i]!))
    return {
      header: column.header,
      width: column.width ?? Math.max(defaultWidths[kind], column.header.length + 4),
      kind,
      total: column.total ?? false,
    }
  })
  const totals = tableColumns.some((column) => column.total)
    ? tableColumns.map((column, i) =>
        column.total ? cells.reduce((sum, row) => sum + (typeof row[i] === 'number' ? row[i] : 0), 0) : null,
      )
    : null
  return {
    name,
    description,
    columns: tableColumns,
    rows: cells,
    tones: rows.map((row) => columns.map((column) => column.tone?.(row) ?? null)),
    totals,
  }
}

/** "lanzamiento-app-movil-2026-10-29": no accents or spaces, so it is a safe file name everywhere. */
export function exportFileName(base: string, now: Date = new Date()): string {
  const slug = base
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${slug || 'export'}-${now.getFullYear()}-${month}-${day}`
}

/** Excel sheet names: at most 31 characters and none of : \ / ? * [ ]. */
export function sheetName(name: string): string {
  return name.replace(/[:\\/?*[\]]/g, ' ').trim().slice(0, 31) || 'Hoja'
}

const dateFormat = new Intl.DateTimeFormat('es', { day: '2-digit', month: '2-digit', year: 'numeric' })
const numberFormat = new Intl.NumberFormat('es')

/** A cell as text, for the PDF. */
export function cellText(cell: ExportCell, kind: ExportKind = 'text'): string {
  if (cell === null) {
    return '—'
  }
  if (cell instanceof Date) {
    return dateFormat.format(cell)
  }
  if (typeof cell === 'number') {
    return kind === 'percent' ? `${Math.round(cell)}%` : numberFormat.format(cell)
  }
  return cell
}

export function generatedAt(now: Date = new Date()): string {
  return `Generado el ${new Intl.DateTimeFormat('es', { dateStyle: 'long', timeStyle: 'short' }).format(now)}`
}

export function saveBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.append(link)
  link.click()
  link.remove()
  // Give the browser a moment to start the download before freeing the blob.
  setTimeout(() => URL.revokeObjectURL(url), 1000)
}
