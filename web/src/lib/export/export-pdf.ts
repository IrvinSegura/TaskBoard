import type { jsPDF as JsPdf } from 'jspdf'
import { cellText, generatedAt, type ExportDocument, type ExportTable } from './export-document'
import { colors, statColors, toneColors, type Rgb } from './export-theme'

const margin = 36
const bandHeight = 104
const cardHeight = 62
const cardGap = 10
// About what one table row takes with the cell padding below; used to plan page breaks.
const rowHeight = 23
// Points per character of column width: narrow tables stay compact instead of stretching across the page.
const pointsPerChar = 7

/** A branded, printable A4 document: cover band, figure cards, one section per table and page numbers. */
export async function buildPdf(document: ExportDocument): Promise<Blob> {
  // Loaded on demand: only needed when someone exports.
  const [{ jsPDF }, { autoTable }] = await Promise.all([import('jspdf'), import('jspdf-autotable')])
  const wide = document.tables.some((table) => table.columns.length > 5)
  const pdf = new jsPDF({ orientation: wide ? 'landscape' : 'portrait', unit: 'pt', format: 'a4' })
  pdf.setProperties({ title: document.title, subject: document.subtitle ?? '', creator: 'TaskBoard' })
  const pageWidth = pdf.internal.pageSize.getWidth()
  const pageHeight = pdf.internal.pageSize.getHeight()
  const contentWidth = pageWidth - margin * 2
  const lastTableEnd = () => (pdf as unknown as { lastAutoTable: { finalY: number } }).lastAutoTable.finalY

  drawCover(pdf, document, pageWidth)
  let y = bandHeight + 24

  if (document.summary?.length) {
    const perRow = Math.min(document.summary.length, wide ? 6 : 3)
    const cardWidth = (contentWidth - cardGap * (perRow - 1)) / perRow
    document.summary.forEach((stat, i) => {
      const x = margin + (i % perRow) * (cardWidth + cardGap)
      const top = y + Math.floor(i / perRow) * (cardHeight + cardGap)
      fill(pdf, colors.card)
      stroke(pdf, colors.line)
      pdf.setLineWidth(0.6)
      pdf.roundedRect(x, top, cardWidth, cardHeight, 6, 6, 'FD')
      fill(pdf, statColors[stat.tone ?? 'primary'])
      pdf.roundedRect(x, top, 3.5, cardHeight, 1.5, 1.5, 'F')
      text(pdf, stat.label.toUpperCase(), x + 14, top + 17, 7.5, colors.muted, 'bold')
      text(pdf, String(stat.value), x + 14, top + 39, 18, colors.ink, 'bold')
      if (stat.hint) {
        text(pdf, fit(pdf, stat.hint, cardWidth - 24, 8), x + 14, top + 53, 8, colors.faint)
      }
    })
    y += Math.ceil(document.summary.length / perRow) * (cardHeight + cardGap) + 14
  }

  const pageBottom = pageHeight - margin - 18
  for (const table of document.tables) {
    // Move a section to a new page only when fewer than five of its rows (or all of them, if fewer) fit here.
    const rowsHere = Math.floor((pageBottom - y - 60) / rowHeight)
    if (rowsHere < Math.min(5, Math.max(1, table.rows.length) + (table.totals ? 1 : 0))) {
      pdf.addPage()
      y = margin + 28
    }
    y = drawSectionHeading(pdf, table, y)
    drawTable(pdf, autoTable, table, y, contentWidth)
    y = lastTableEnd() + 28
  }

  drawPageChrome(pdf, document.title, pageWidth, pageHeight)
  return pdf.output('blob')
}

function drawCover(pdf: JsPdf, document: ExportDocument, pageWidth: number) {
  fill(pdf, colors.brand)
  pdf.rect(0, 0, pageWidth, bandHeight, 'F')
  // A darker strip along the bottom edge gives the band some depth.
  fill(pdf, colors.brandDark)
  pdf.rect(0, bandHeight - 4, pageWidth, 4, 'F')

  drawLogo(pdf, margin, 22, 18)
  text(pdf, 'TaskBoard', margin + 25, 35, 11, colors.white, 'bold')
  text(pdf, generatedAt(), pageWidth - margin, 35, 8.5, colors.brandTint, 'normal', 'right')

  text(pdf, document.title, margin, 70, 22, colors.white, 'bold')
  if (document.subtitle) {
    text(pdf, document.subtitle, margin, 88, 10, colors.brandTint)
  }
}

/** The app's mark: a rounded square with three bars of falling height. */
function drawLogo(pdf: JsPdf, x: number, y: number, size: number) {
  const unit = size / 32
  fill(pdf, colors.brandDark)
  pdf.roundedRect(x, y, size, size, 8 * unit, 8 * unit, 'F')
  fill(pdf, colors.white)
  pdf.roundedRect(x + 7 * unit, y + 8 * unit, 5 * unit, 16 * unit, 1.5 * unit, 1.5 * unit, 'F')
  fill(pdf, [226, 229, 255])
  pdf.roundedRect(x + 13.5 * unit, y + 8 * unit, 5 * unit, 11 * unit, 1.5 * unit, 1.5 * unit, 'F')
  fill(pdf, [190, 196, 250])
  pdf.roundedRect(x + 20 * unit, y + 8 * unit, 5 * unit, 7 * unit, 1.5 * unit, 1.5 * unit, 'F')
}

function drawSectionHeading(pdf: JsPdf, table: ExportTable, y: number): number {
  fill(pdf, colors.brand)
  pdf.roundedRect(margin, y - 11, 3.5, 15, 1.5, 1.5, 'F')
  text(pdf, table.name, margin + 11, y, 12.5, colors.ink, 'bold')
  const count = `${table.rows.length} ${table.rows.length === 1 ? 'fila' : 'filas'}`
  text(pdf, count, pdf.internal.pageSize.getWidth() - margin, y, 8.5, colors.faint, 'normal', 'right')
  if (table.description) {
    text(pdf, table.description, margin + 11, y + 14, 9, colors.muted)
    return y + 24
  }
  return y + 10
}

type AutoTable = typeof import('jspdf-autotable').autoTable

function drawTable(pdf: JsPdf, autoTable: AutoTable, table: ExportTable, startY: number, contentWidth: number) {
  const totalWidth = table.columns.reduce((sum, column) => sum + column.width, 0)
  const tableWidth = Math.min(contentWidth, totalWidth * pointsPerChar)
  const align = (i: number) => {
    const kind = table.columns[i]!.kind
    return kind === 'number' || kind === 'percent' ? 'right' : 'left'
  }

  autoTable(pdf, {
    startY,
    margin: { left: margin, right: margin, top: margin + 28, bottom: margin + 18 },
    tableWidth,
    head: [table.columns.map((column) => column.header)],
    body: table.rows.length
      ? table.rows.map((row) => row.map((cell, i) => cellText(cell, table.columns[i]!.kind)))
      : [[{ content: 'Sin datos para mostrar', colSpan: table.columns.length, styles: { halign: 'center' } }]],
    foot: table.totals
      ? [table.totals.map((total, i) => (i === 0 ? 'Total' : total === null ? '' : cellText(total, table.columns[i]!.kind)))]
      : undefined,
    showFoot: 'lastPage',
    theme: 'plain',
    styles: {
      font: 'helvetica',
      fontSize: 8.5,
      textColor: colors.body,
      cellPadding: { top: 6, bottom: 6, left: 7, right: 7 },
      lineColor: colors.line,
      lineWidth: { bottom: 0.5 },
      overflow: 'linebreak',
      valign: 'middle',
    },
    headStyles: { fillColor: colors.brand, textColor: colors.white, fontStyle: 'bold', fontSize: 8, lineWidth: 0 },
    footStyles: { fillColor: colors.brandSoft, textColor: colors.brandDark, fontStyle: 'bold', lineWidth: 0 },
    alternateRowStyles: { fillColor: colors.stripe },
    columnStyles: Object.fromEntries(
      table.columns.map((column, i) => [i, { cellWidth: (column.width / totalWidth) * tableWidth, halign: align(i) }]),
    ),
    didParseCell: (data) => {
      if (data.section === 'head' || data.section === 'foot') {
        data.cell.styles.halign = align(data.column.index)
        return
      }
      const tone = table.tones[data.row.index]?.[data.column.index]
      if (tone) {
        data.cell.styles.textColor = toneColors[tone]
        data.cell.styles.fontStyle = tone === 'muted' ? 'normal' : 'bold'
      }
      if (table.rows[data.row.index]?.[data.column.index] === null) {
        data.cell.styles.textColor = colors.faint
      }
      if (data.column.index === 0 && table.rows.length > 0) {
        data.cell.styles.textColor = tone ? toneColors[tone] : colors.ink
      }
    },
    // Percent columns get a small progress bar to the left of the number, in the cell's tone if it has one.
    didDrawCell: (data) => {
      const column = table.columns[data.column.index]
      const value = table.rows[data.row.index]?.[data.column.index]
      if (data.section !== 'body' || column?.kind !== 'percent' || typeof value !== 'number') {
        return
      }
      const barWidth = data.cell.width - 14 - 30
      if (barWidth < 16) {
        return
      }
      const x = data.cell.x + 7
      const y = data.cell.y + data.cell.height / 2 - 2
      fill(pdf, colors.line)
      pdf.roundedRect(x, y, barWidth, 4, 2, 2, 'F')
      if (value > 0) {
        const tone = table.tones[data.row.index]?.[data.column.index]
        fill(pdf, tone && tone !== 'muted' ? toneColors[tone] : colors.brand)
        pdf.roundedRect(x, y, Math.max(4, (barWidth * Math.min(value, 100)) / 100), 4, 2, 2, 'F')
      }
    },
  })
}

/** Running header from the second page on, and a footer with page numbers on every page. */
function drawPageChrome(pdf: JsPdf, title: string, pageWidth: number, pageHeight: number) {
  const pages = pdf.getNumberOfPages()
  for (let page = 1; page <= pages; page++) {
    pdf.setPage(page)
    if (page > 1) {
      drawLogo(pdf, margin, margin - 8, 12)
      text(pdf, 'TaskBoard', margin + 17, margin + 1.5, 8.5, colors.ink, 'bold')
      text(pdf, title, pageWidth - margin, margin + 1.5, 8.5, colors.muted, 'normal', 'right')
      stroke(pdf, colors.line)
      pdf.setLineWidth(0.5)
      pdf.line(margin, margin + 10, pageWidth - margin, margin + 10)
    }
    stroke(pdf, colors.line)
    pdf.setLineWidth(0.5)
    pdf.line(margin, pageHeight - margin + 4, pageWidth - margin, pageHeight - margin + 4)
    text(pdf, `TaskBoard · ${title}`, margin, pageHeight - margin + 18, 8, colors.faint)
    text(pdf, `Página ${page} de ${pages}`, pageWidth - margin, pageHeight - margin + 18, 8, colors.muted, 'normal', 'right')
  }
}

function text(
  pdf: JsPdf,
  value: string,
  x: number,
  y: number,
  size: number,
  color: Rgb,
  style: 'normal' | 'bold' = 'normal',
  align: 'left' | 'right' = 'left',
) {
  pdf.setFont('helvetica', style).setFontSize(size).setTextColor(...color)
  pdf.text(value, x, y, { align })
}

/** Cuts text with an ellipsis so it stays inside `width`. */
function fit(pdf: JsPdf, value: string, width: number, size: number): string {
  pdf.setFontSize(size)
  if (pdf.getTextWidth(value) <= width) {
    return value
  }
  let cut = value
  while (cut.length > 1 && pdf.getTextWidth(`${cut}…`) > width) {
    cut = cut.slice(0, -1)
  }
  return `${cut.trimEnd()}…`
}

function fill(pdf: JsPdf, color: Rgb) {
  pdf.setFillColor(...color)
}

function stroke(pdf: JsPdf, color: Rgb) {
  pdf.setDrawColor(...color)
}
