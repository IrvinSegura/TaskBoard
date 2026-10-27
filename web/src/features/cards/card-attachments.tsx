import { Download, FileArchive, FileImage, FileSpreadsheet, FileText, Paperclip, Trash2, Upload } from 'lucide-react'
import { useRef, useState, type DragEvent } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { useCurrentUser } from '@/features/auth/auth-provider'
import type { BoardMember } from '@/features/boards/boards-api'
import { errorMessage } from '@/lib/api/api-error'
import { cn } from '@/lib/utils/cn'
import { formatBytes, formatRelative } from '@/lib/utils/format'
import { useDeleteAttachment, useUploadAttachment } from './card-queries'
import { allowedAttachmentExtensions, attachmentProblem, cardsApi, type CardAttachment, type CardDetail } from './cards-api'
import { SectionTitle } from './section-title'

function iconFor(contentType: string) {
  if (contentType.startsWith('image/')) {
    return FileImage
  }
  if (contentType.includes('zip')) {
    return FileArchive
  }
  if (contentType.includes('spreadsheet') || contentType === 'text/csv') {
    return FileSpreadsheet
  }
  return FileText
}

interface CardAttachmentsProps {
  card: CardDetail
  members: BoardMember[]
  canEdit: boolean
}

export function CardAttachments({ card, members, canEdit }: CardAttachmentsProps) {
  const me = useCurrentUser()
  const upload = useUploadAttachment(card.id, card.boardId)
  const remove = useDeleteAttachment(card.id, card.boardId)
  const inputRef = useRef<HTMLInputElement>(null)
  const [dragOver, setDragOver] = useState(false)
  const [downloading, setDownloading] = useState<string | null>(null)
  const isAdmin = members.find((m) => m.userId === me.id)?.role === 'Admin'

  const uploadFiles = (files: FileList | null) => {
    for (const file of Array.from(files ?? [])) {
      const problem = attachmentProblem(file)
      if (problem) {
        toast.error(`${file.name}: ${problem}`)
        continue
      }
      upload.mutate(file, {
        onSuccess: () => toast.success(`${file.name} adjuntado`),
        onError: (error) => toast.error(`${file.name}: ${errorMessage(error)}`),
      })
    }
  }

  const onDrop = (event: DragEvent) => {
    event.preventDefault()
    setDragOver(false)
    if (canEdit) {
      uploadFiles(event.dataTransfer.files)
    }
  }

  const download = async (attachment: CardAttachment) => {
    setDownloading(attachment.id)
    try {
      await cardsApi.downloadAttachment(attachment)
    } catch (error) {
      toast.error(errorMessage(error))
    } finally {
      setDownloading(null)
    }
  }

  return (
    <section
      onDragOver={(event) => {
        if (canEdit) {
          event.preventDefault()
          setDragOver(true)
        }
      }}
      onDragLeave={() => setDragOver(false)}
      onDrop={onDrop}
    >
      <SectionTitle
        icon={Paperclip}
        action={
          canEdit && (
            <Button variant="ghost" size="sm" onClick={() => inputRef.current?.click()} loading={upload.isPending}>
              <Upload />
              Subir
            </Button>
          )
        }
      >
        Adjuntos
        {card.attachments.length > 0 && <span className="ml-1.5 font-normal text-faint">{card.attachments.length}</span>}
      </SectionTitle>

      <input
        ref={inputRef}
        type="file"
        multiple
        hidden
        accept={allowedAttachmentExtensions.join(',')}
        onChange={(event) => {
          uploadFiles(event.target.files)
          event.target.value = ''
        }}
      />

      {card.attachments.length === 0 ? (
        canEdit ? (
          <button
            type="button"
            onClick={() => inputRef.current?.click()}
            className={cn(
              'w-full cursor-pointer rounded-lg border border-dashed px-3 py-5 text-center text-sm transition-colors',
              dragOver ? 'border-primary bg-primary-soft text-primary' : 'border-line-strong text-faint hover:text-fg',
            )}
          >
            Arrastra archivos aquí o haz clic para subirlos
            <span className="mt-1 block text-xs">PDF, imágenes, Office, CSV, TXT o ZIP · máximo 10 MB</span>
          </button>
        ) : (
          <p className="text-sm text-faint">Sin adjuntos.</p>
        )
      ) : (
        <ul className={cn('space-y-1.5 rounded-lg', dragOver && 'ring-2 ring-primary ring-offset-4 ring-offset-surface')}>
          {card.attachments.map((attachment) => {
            const Icon = iconFor(attachment.contentType)
            const uploader = members.find((m) => m.userId === attachment.uploadedById)
            const canDelete = canEdit && (attachment.uploadedById === me.id || isAdmin)
            return (
              <li
                key={attachment.id}
                className="group flex items-center gap-3 rounded-lg border border-line bg-surface px-3 py-2"
              >
                <span className="flex size-9 shrink-0 items-center justify-center rounded-md bg-primary-soft text-primary">
                  <Icon className="size-4" />
                </span>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium" title={attachment.fileName}>
                    {attachment.fileName}
                  </p>
                  <p className="truncate text-xs text-muted">
                    {formatBytes(attachment.sizeInBytes)} · {formatRelative(attachment.createdAt)}
                    {uploader && ` · ${uploader.displayName}`}
                  </p>
                </div>
                <Button
                  variant="ghost"
                  size="icon-sm"
                  aria-label={`Descargar ${attachment.fileName}`}
                  loading={downloading === attachment.id}
                  onClick={() => void download(attachment)}
                >
                  {downloading !== attachment.id && <Download />}
                </Button>
                {canDelete && (
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    aria-label={`Borrar ${attachment.fileName}`}
                    className="hover:text-danger"
                    loading={remove.isPending && remove.variables === attachment.id}
                    onClick={() =>
                      remove.mutate(attachment.id, {
                        onSuccess: () => toast.success(`${attachment.fileName} borrado`),
                        onError: (error) => toast.error(errorMessage(error)),
                      })
                    }
                  >
                    {!(remove.isPending && remove.variables === attachment.id) && <Trash2 />}
                  </Button>
                )}
              </li>
            )
          })}
        </ul>
      )}
    </section>
  )
}
