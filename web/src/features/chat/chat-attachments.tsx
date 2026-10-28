import { Download, ImageOff, Play } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Dialog } from '@/components/ui/dialog'
import { Skeleton } from '@/components/ui/feedback'
import { Spinner } from '@/components/ui/spinner'
import { errorMessage } from '@/lib/api/api-error'
import { cn } from '@/lib/utils/cn'
import { formatBytes } from '@/lib/utils/format'
import type { ChatAttachment } from './chat-api'
import { attachmentKind } from './chat-files'
import { FileTypeIcon } from './file-type-icon'
import { downloadAttachment, useAttachmentUrl } from './use-attachment-url'

/** Images first in a grid, then players and files, one per line. */
export function ChatAttachments({ attachments, mine }: { attachments: ChatAttachment[]; mine: boolean }) {
  const images = attachments.filter((a) => attachmentKind(a.contentType) === 'image')
  const others = attachments.filter((a) => attachmentKind(a.contentType) !== 'image')

  return (
    <div className={cn('flex flex-col gap-1.5', mine ? 'items-end' : 'items-start')}>
      {images.length > 0 && (
        <div className={cn('grid gap-1.5', images.length > 1 ? 'grid-cols-2' : 'grid-cols-1')}>
          {images.map((image) => (
            <ChatImage key={image.id} attachment={image} single={images.length === 1} />
          ))}
        </div>
      )}
      {others.map((attachment) => {
        switch (attachmentKind(attachment.contentType)) {
          case 'audio':
            return <ChatAudio key={attachment.id} attachment={attachment} />
          case 'video':
            return <ChatVideo key={attachment.id} attachment={attachment} />
          default:
            return <ChatFile key={attachment.id} attachment={attachment} />
        }
      })}
    </div>
  )
}

function ChatImage({ attachment, single }: { attachment: ChatAttachment; single: boolean }) {
  const { url, error } = useAttachmentUrl(attachment.id)
  const [open, setOpen] = useState(false)
  const size = single ? 'max-h-72 max-w-full sm:max-w-sm' : 'size-32 object-cover sm:size-40'

  if (error) {
    return (
      <div className="flex size-32 flex-col items-center justify-center gap-1 rounded-xl border border-line bg-subtle text-xs text-faint">
        <ImageOff className="size-5" />
        No se pudo cargar
      </div>
    )
  }
  if (!url) {
    return <Skeleton className={single ? 'h-48 w-64 rounded-xl' : 'size-32 rounded-xl sm:size-40'} />
  }
  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="cursor-zoom-in overflow-hidden rounded-xl border border-line bg-subtle"
        aria-label={`Ver ${attachment.fileName}`}
      >
        <img src={url} alt={attachment.fileName} className={cn('block', size)} />
      </button>
      <Dialog open={open} onOpenChange={setOpen} title={attachment.fileName} size="xl">
        <div className="space-y-4">
          <img src={url} alt={attachment.fileName} className="mx-auto max-h-[70dvh] rounded-lg" />
          <div className="flex justify-end">
            <DownloadButton attachment={attachment} withLabel />
          </div>
        </div>
      </Dialog>
    </>
  )
}

function ChatAudio({ attachment }: { attachment: ChatAttachment }) {
  const { url, error } = useAttachmentUrl(attachment.id)
  return (
    <div className="flex w-72 max-w-full items-center gap-2 rounded-xl border border-line bg-surface p-2 shadow-card">
      {url ? (
        <audio controls preload="metadata" src={url} className="h-9 min-w-0 flex-1" aria-label={attachment.fileName} />
      ) : (
        <span className="flex h-9 flex-1 items-center gap-2 px-2 text-xs text-faint">
          {error ? 'No se pudo cargar el audio' : <Spinner className="size-4" />}
        </span>
      )}
      <DownloadButton attachment={attachment} />
    </div>
  )
}

// Videos can weigh up to 25 MB, so they only download when someone presses play.
function ChatVideo({ attachment }: { attachment: ChatAttachment }) {
  const [requested, setRequested] = useState(false)
  const { url, error } = useAttachmentUrl(attachment.id, requested)

  if (url) {
    return (
      <video controls autoPlay src={url} className="max-h-80 max-w-full rounded-xl border border-line bg-black sm:max-w-sm">
        <track kind="captions" />
      </video>
    )
  }
  return (
    <button
      type="button"
      onClick={() => setRequested(true)}
      className="flex h-40 w-64 max-w-full cursor-pointer flex-col items-center justify-center gap-2 rounded-xl border border-line bg-fg/90 text-white"
    >
      <span className="flex size-11 items-center justify-center rounded-full bg-white/20">
        {requested && !error ? <Spinner className="size-5" /> : <Play className="size-5 fill-current" />}
      </span>
      <span className="max-w-[90%] truncate text-xs">
        {error ? 'No se pudo cargar el video' : `${attachment.fileName} · ${formatBytes(attachment.sizeInBytes)}`}
      </span>
    </button>
  )
}

function ChatFile({ attachment }: { attachment: ChatAttachment }) {
  return (
    <div className="flex w-72 max-w-full items-center gap-3 rounded-xl border border-line bg-surface px-3 py-2 shadow-card">
      <span className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-primary-soft text-primary">
        <FileTypeIcon contentType={attachment.contentType} className="size-4" />
      </span>
      <span className="min-w-0 flex-1">
        <span className="block truncate text-sm font-medium" title={attachment.fileName}>
          {attachment.fileName}
        </span>
        <span className="block text-xs text-muted">{formatBytes(attachment.sizeInBytes)}</span>
      </span>
      <DownloadButton attachment={attachment} />
    </div>
  )
}

function DownloadButton({ attachment, withLabel = false }: { attachment: ChatAttachment; withLabel?: boolean }) {
  const [busy, setBusy] = useState(false)
  const download = async () => {
    setBusy(true)
    try {
      await downloadAttachment(attachment.id, attachment.fileName)
    } catch (error) {
      toast.error(errorMessage(error))
    } finally {
      setBusy(false)
    }
  }
  return (
    <Button
      variant={withLabel ? 'secondary' : 'ghost'}
      size={withLabel ? 'sm' : 'icon-sm'}
      loading={busy}
      onClick={() => void download()}
      aria-label={`Descargar ${attachment.fileName}`}
    >
      {!busy && <Download />}
      {withLabel && 'Descargar'}
    </Button>
  )
}
