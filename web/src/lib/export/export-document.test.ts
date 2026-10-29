import { cellText, exportFileName, exportTable, sheetName } from './export-document'

describe('exportTable', () => {
  const rows = [
    { title: 'Diseño', total: 3, due: new Date(2026, 9, 1) },
    { title: 'Pruebas', total: 2, due: null },
  ]
  const table = exportTable(
    'Tarjetas',
    [
      { header: 'Título', value: (row: (typeof rows)[number]) => row.title, width: 40 },
      { header: 'Total', value: (row) => row.total, total: true, tone: (row) => (row.total > 2 ? 'danger' : null) },
      { header: 'Fecha', value: (row) => row.due },
    ],
    rows,
  )

  it('turns rows into cells in column order', () => {
    expect(table.rows).toEqual([
      ['Diseño', 3, new Date(2026, 9, 1)],
      ['Pruebas', 2, null],
    ])
  })

  it('guesses the kind of a column from its values and sizes it', () => {
    expect(table.columns.map((column) => column.kind)).toEqual(['text', 'number', 'date'])
    expect(table.columns.map((column) => column.width)).toEqual([40, 12, 14])
  })

  it('keeps the tone of every cell and adds up the total columns', () => {
    expect(table.tones).toEqual([
      [null, 'danger', null],
      [null, null, null],
    ])
    expect(table.totals).toEqual([null, 5, null])
  })
})

describe('exportFileName', () => {
  it('drops accents and spaces and adds the date', () => {
    expect(exportFileName('Lanzamiento app móvil', new Date(2026, 9, 9))).toBe('lanzamiento-app-movil-2026-10-09')
  })

  it('falls back when nothing usable is left', () => {
    expect(exportFileName('¿?', new Date(2026, 0, 1))).toBe('export-2026-01-01')
  })
})

describe('sheetName', () => {
  it('removes characters Excel rejects and keeps 31 characters', () => {
    expect(sheetName('Q4: plan/ideas')).toBe('Q4  plan ideas')
    expect(sheetName('a'.repeat(40))).toHaveLength(31)
  })
})

describe('cellText', () => {
  it('formats dates, percents and empty cells', () => {
    expect(cellText(new Date(2026, 9, 29))).toBe('29/10/2026')
    expect(cellText(null)).toBe('—')
    expect(cellText(4)).toBe('4')
    expect(cellText(33.4, 'percent')).toBe('33%')
  })
})
