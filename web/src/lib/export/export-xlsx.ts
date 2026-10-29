import type { Borders, Cell, Fill, Worksheet } from 'exceljs'
import { generatedAt, sheetName, type ExportDocument, type ExportKind, type ExportTable } from './export-document'
import { argb, colors, statColors, toneColors, type Rgb } from './export-theme'

const xlsxType = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
const font = 'Calibri'

const formats: Record<ExportKind, string | undefined> = {
  text: undefined,
  number: '#,##0',
  percent: '0%',
  date: 'dd/mm/yyyy',
}

// Table sheets: title on row 1, description on row 2, header on row 4.
const headerRow = 4

/** A cover sheet with the title, figure cards and links to every table, then one formatted sheet per table. */
export async function buildXlsx(document: ExportDocument): Promise<Blob> {
  // Loaded on demand: exceljs is large and only needed when someone exports.
  const module = await import('exceljs')
  const ExcelJS = ('default' in module ? module.default : module) as typeof module
  const workbook = new ExcelJS.Workbook()
  workbook.creator = 'TaskBoard'
  workbook.title = document.title
  workbook.created = new Date()

  const names = uniqueSheetNames(['Resumen', ...document.tables.map((table) => table.name)])
  writeCover(workbook.addWorksheet(names[0]!, sheetOptions(argb(colors.brand))), document, names.slice(1))
  document.tables.forEach((table, i) => {
    writeTable(workbook.addWorksheet(names[i + 1]!, sheetOptions(argb(colors.brandTint))), table, document.title)
  })

  const buffer = await workbook.xlsx.writeBuffer()
  return new Blob([buffer], { type: xlsxType })
}

function sheetOptions(tabColor: string) {
  return { properties: { tabColor: { argb: tabColor } }, views: [{ showGridLines: false }] }
}

function uniqueSheetNames(names: string[]): string[] {
  const used = new Set<string>()
  return names.map((name) => {
    let candidate = sheetName(name)
    for (let i = 2; used.has(candidate.toLowerCase()); i++) {
      candidate = sheetName(`${name.slice(0, 26)} (${i})`)
    }
    used.add(candidate.toLowerCase())
    return candidate
  })
}

function writeCover(sheet: Worksheet, document: ExportDocument, tableSheets: string[]) {
  // Cards take two columns each, with a narrow gap column between them.
  sheet.columns = [2, 18, 18, 2, 18, 18, 2, 18, 18, 2].map((width) => ({ width }))
  const lastColumn = 9

  const band = (row: number, height: number) => {
    sheet.getRow(row).height = height
    for (let column = 1; column <= lastColumn + 1; column++) {
      sheet.getCell(row, column).fill = solid(colors.brand)
    }
  }
  band(1, 10)
  band(2, 22)
  band(3, 34)
  band(4, 20)
  band(5, 12)
  sheet.getCell('B2').value = 'TaskBoard'
  sheet.getCell('B2').font = { name: font, bold: true, size: 11, color: { argb: argb(colors.brandTint) } }
  sheet.getCell('I2').value = generatedAt()
  sheet.getCell('I2').font = { name: font, size: 9, color: { argb: argb(colors.brandTint) } }
  sheet.getCell('I2').alignment = { horizontal: 'right' }
  sheet.getCell('B3').value = document.title
  sheet.getCell('B3').font = { name: font, bold: true, size: 22, color: { argb: argb(colors.white) } }
  sheet.getCell('B3').alignment = { vertical: 'middle' }
  if (document.subtitle) {
    sheet.getCell('B4').value = document.subtitle
    sheet.getCell('B4').font = { name: font, size: 11, color: { argb: argb(colors.brandTint) } }
  }

  let row = 7
  if (document.summary?.length) {
    sectionTitle(sheet.getCell(row, 2), 'Resumen')
    row += 2
    document.summary.forEach((stat, i) => {
      const top = row + Math.floor(i / 3) * 4
      const left = 2 + (i % 3) * 3
      const accent = statColors[stat.tone ?? 'primary']
      const lines: [string | number, Partial<Cell['font']>, number][] = [
        [stat.label.toUpperCase(), { size: 8, bold: true, color: { argb: argb(colors.muted) } }, 18],
        [stat.value, { size: 20, bold: true, color: { argb: argb(colors.ink) } }, 30],
        [stat.hint ?? '', { size: 9, color: { argb: argb(colors.faint) } }, 18],
      ]
      lines.forEach(([value, style, height], line) => {
        const r = top + line
        sheet.getRow(r).height = Math.max(sheet.getRow(r).height ?? 0, height)
        sheet.mergeCells(r, left, r, left + 1)
        const cell = sheet.getCell(r, left)
        cell.value = value
        cell.font = { name: font, ...style }
        cell.alignment = { vertical: 'middle', indent: 1, horizontal: 'left' }
        for (const c of [left, left + 1]) {
          const target = sheet.getCell(r, c)
          target.fill = solid(colors.card)
          target.border = {
            left: c === left ? { style: 'thick', color: { argb: argb(accent) } } : undefined,
            right: c === left + 1 ? thin(colors.line) : undefined,
            top: line === 0 ? thin(colors.line) : undefined,
            bottom: line === 2 ? thin(colors.line) : undefined,
          }
        }
      })
    })
    row += Math.ceil(document.summary.length / 3) * 4 + 1
  }

  sectionTitle(sheet.getCell(row, 2), 'Contenido')
  row += 2
  document.tables.forEach((table, i) => {
    sheet.mergeCells(row, 2, row, 3)
    const link = sheet.getCell(row, 2)
    link.value = { text: table.name, hyperlink: `#'${tableSheets[i]!.replace(/'/g, "''")}'!A1` }
    link.font = { name: font, size: 11, underline: true, color: { argb: argb(colors.brand) } }
    sheet.mergeCells(row, 5, row, 9)
    const about = sheet.getCell(row, 5)
    about.value = `${table.rows.length} ${table.rows.length === 1 ? 'fila' : 'filas'}${table.description ? ` · ${table.description}` : ''}`
    about.font = { name: font, size: 10, color: { argb: argb(colors.muted) } }
    for (let c = 2; c <= lastColumn; c++) {
      sheet.getCell(row, c).border = { bottom: thin(colors.line) }
    }
    sheet.getRow(row).height = 20
    row++
  })

  sheet.pageSetup = { orientation: 'portrait', fitToPage: true, fitToWidth: 1, fitToHeight: 0, paperSize: 9 }
}

function writeTable(sheet: Worksheet, table: ExportTable, documentTitle: string) {
  const count = table.columns.length
  sheet.columns = table.columns.map((column) => ({ width: column.width + 2 }))
  sheet.views = [{ state: 'frozen', ySplit: headerRow, showGridLines: false }]

  sheet.getRow(1).height = 30
  sheet.mergeCells(1, 1, 1, count)
  const title = sheet.getCell(1, 1)
  title.value = table.name
  title.font = { name: font, bold: true, size: 16, color: { argb: argb(colors.ink) } }
  title.alignment = { vertical: 'middle' }
  sheet.mergeCells(2, 1, 2, count)
  const about = sheet.getCell(2, 1)
  about.value = [documentTitle, table.description].filter(Boolean).join(' · ')
  about.font = { name: font, size: 10, color: { argb: argb(colors.muted) } }
  sheet.getRow(3).height = 8

  const header = sheet.getRow(headerRow)
  header.height = 26
  table.columns.forEach((column, i) => {
    const cell = header.getCell(i + 1)
    cell.value = column.header
    cell.fill = solid(colors.brand)
    cell.font = { name: font, bold: true, size: 10, color: { argb: argb(colors.white) } }
    cell.alignment = { vertical: 'middle', horizontal: alignFor(column.kind), indent: 1 }
    cell.border = { bottom: { style: 'medium', color: { argb: argb(colors.brandDark) } } }
  })

  table.rows.forEach((cells, r) => {
    const row = sheet.getRow(headerRow + 1 + r)
    row.height = 20
    cells.forEach((value, i) => {
      const column = table.columns[i]!
      const cell = row.getCell(i + 1)
      const tone = table.tones[r]?.[i]
      cell.value =
        value === null
          ? '—'
          : value instanceof Date
            ? calendarDay(value)
            : column.kind === 'percent' && typeof value === 'number'
              ? value / 100
              : value
      cell.numFmt = formats[column.kind] ?? 'General'
      cell.font = {
        name: font,
        size: 10,
        bold: (tone !== null && tone !== 'muted') || (i === 0 && !tone),
        color: { argb: argb(value === null ? colors.faint : tone ? toneColors[tone] : i === 0 ? colors.ink : colors.body) },
      }
      cell.alignment = {
        vertical: 'middle',
        horizontal: value === null ? 'center' : alignFor(column.kind),
        indent: 1,
        wrapText: column.kind === 'text' && column.width >= 30,
      }
      if (r % 2 === 1) {
        cell.fill = solid(colors.stripe)
      }
      cell.border = { bottom: thin(colors.line) }
    })
  })

  if (table.rows.length === 0) {
    sheet.mergeCells(headerRow + 1, 1, headerRow + 1, count)
    const empty = sheet.getCell(headerRow + 1, 1)
    empty.value = 'Sin datos para mostrar'
    empty.font = { name: font, italic: true, color: { argb: argb(colors.muted) } }
    empty.alignment = { horizontal: 'center', vertical: 'middle' }
    sheet.getRow(headerRow + 1).height = 28
  } else {
    sheet.autoFilter = { from: { row: headerRow, column: 1 }, to: { row: headerRow, column: count } }
  }

  if (table.totals && table.rows.length > 0) {
    const first = headerRow + 1
    const last = headerRow + table.rows.length
    const row = sheet.getRow(last + 1)
    row.height = 22
    table.columns.forEach((column, i) => {
      const cell = row.getCell(i + 1)
      const total = table.totals![i]
      const letter = sheet.getColumn(i + 1).letter
      if (i === 0) {
        cell.value = 'Total'
      } else if (total !== null && total !== undefined) {
        cell.value = { formula: `SUM(${letter}${first}:${letter}${last})`, result: total }
        cell.numFmt = formats[column.kind] ?? 'General'
      }
      cell.fill = solid(colors.brandSoft)
      cell.font = { name: font, bold: true, size: 10, color: { argb: argb(colors.brandDark) } }
      cell.alignment = { vertical: 'middle', horizontal: i === 0 ? 'left' : alignFor(column.kind), indent: 1 }
      cell.border = { top: { style: 'thin', color: { argb: argb(colors.brand) } } }
    })
  }

  sheet.pageSetup = {
    orientation: count > 5 ? 'landscape' : 'portrait',
    paperSize: 9,
    fitToPage: true,
    fitToWidth: 1,
    fitToHeight: 0,
    printTitlesRow: `${headerRow}:${headerRow}`,
    margins: { left: 0.5, right: 0.5, top: 0.6, bottom: 0.6, header: 0.3, footer: 0.3 },
  }
  sheet.headerFooter = {
    oddFooter: `&L&8TaskBoard · ${documentTitle.replace(/&/g, '&&')}&R&8Página &P de &N`,
  }
}

function sectionTitle(cell: Cell, value: string) {
  cell.value = value
  cell.font = { name: font, bold: true, size: 13, color: { argb: argb(colors.brandDark) } }
}

function solid(color: Rgb): Fill {
  return { type: 'pattern', pattern: 'solid', fgColor: { argb: argb(color) } }
}

function thin(color: Rgb): Partial<Borders>['bottom'] {
  return { style: 'thin', color: { argb: argb(color) } }
}

/** Numbers line up on the right; text and dates read from the left. */
function alignFor(kind: ExportKind): 'left' | 'right' {
  return kind === 'number' || kind === 'percent' ? 'right' : 'left'
}

/** Excel dates have no time zone and exceljs writes them as UTC; keep the local day the person sees. */
function calendarDay(date: Date): Date {
  return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()))
}
