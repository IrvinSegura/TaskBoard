import { Download, FileSpreadsheet, FileText } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import { exportFileName, saveBlob, type ExportDocument } from '@/lib/export/export-document'
import { Button } from './button'
import { DropdownMenu, MenuItem } from './dropdown-menu'

type Format = 'pdf' | 'xlsx'

interface ExportMenuProps {
  /** Built when the person picks a format, so it reflects the filters and order on screen at that moment. */
  getDocument: () => ExportDocument
  disabled?: boolean
}

/** "Exportar" button with PDF and Excel; the exporters load only when used. */
export function ExportMenu({ getDocument, disabled }: ExportMenuProps) {
  const [exporting, setExporting] = useState<Format | null>(null)

  const run = async (format: Format) => {
    setExporting(format)
    try {
      const document = getDocument()
      const blob =
        format === 'pdf'
          ? await (await import('@/lib/export/export-pdf')).buildPdf(document)
          : await (await import('@/lib/export/export-xlsx')).buildXlsx(document)
      saveBlob(blob, `${exportFileName(document.fileName)}.${format}`)
    } catch {
      toast.error('No se pudo generar el archivo. Inténtalo de nuevo.')
    } finally {
      setExporting(null)
    }
  }

  return (
    <DropdownMenu
      trigger={
        <Button variant="secondary" size="sm" disabled={disabled} loading={exporting !== null}>
          {exporting === null && <Download />}
          Exportar
        </Button>
      }
    >
      <MenuItem icon={<FileText />} onSelect={() => void run('pdf')}>
        PDF
      </MenuItem>
      <MenuItem icon={<FileSpreadsheet />} onSelect={() => void run('xlsx')}>
        Excel (.xlsx)
      </MenuItem>
    </DropdownMenu>
  )
}
