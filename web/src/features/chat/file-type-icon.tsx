import { FileArchive, FileSpreadsheet, FileText, Mic, Presentation } from 'lucide-react'

export function FileTypeIcon({ contentType, className }: { contentType: string; className?: string }) {
  if (contentType.startsWith('audio/')) {
    return <Mic className={className} />
  }
  if (contentType.includes('zip')) {
    return <FileArchive className={className} />
  }
  if (contentType.includes('spreadsheet') || contentType === 'text/csv') {
    return <FileSpreadsheet className={className} />
  }
  if (contentType.includes('presentation')) {
    return <Presentation className={className} />
  }
  return <FileText className={className} />
}
