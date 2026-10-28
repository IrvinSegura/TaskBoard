import { Mic, Paperclip, SendHorizontal, Square, Trash2, X } from 'lucide-react'
import { useEffect, useRef, useState, type ClipboardEvent, type KeyboardEvent } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { errorMessage } from '@/lib/api/api-error'
import { cn } from '@/lib/utils/cn'
import { formatBytes } from '@/lib/utils/format'
import { allowedChatExtensions, attachmentKind, chatFilesProblem, maxChatBodyLength } from './chat-files'
import { useSendMessage } from './chat-queries'
import { FileTypeIcon } from './file-type-icon'
import type { PendingFiles } from './use-pending-files'
import { canRecordVoice, useVoiceRecorder } from './use-voice-recorder'

interface MessageComposerProps {
  conversationId: string
  pending: PendingFiles
  onSent: () => void
}

function formatDuration(seconds: number): string {
  return `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, '0')}`
}

export function MessageComposer({ conversationId, pending, onSent }: MessageComposerProps) {
  const send = useSendMessage(conversationId)
  const voice = useVoiceRecorder()
  const [body, setBody] = useState('')
  const { files, add: addFiles, set: setFiles } = pending
  const textRef = useRef<HTMLTextAreaElement>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  // Grows with the text up to a few lines, then scrolls.
  useEffect(() => {
    const textarea = textRef.current
    if (textarea) {
      textarea.style.height = 'auto'
      textarea.style.height = `${Math.min(textarea.scrollHeight, 160)}px`
    }
  }, [body])

  const submit = (text: string, attached: File[]) => {
    if (!text.trim() && attached.length === 0) {
      return
    }
    const problem = chatFilesProblem(attached)
    if (problem) {
      toast.error(problem)
      return
    }
    // Cleared straight away so the next message can be typed; put back if sending fails.
    setBody('')
    setFiles([])
    onSent()
    send.mutate(
      { body: text, files: attached },
      {
        onError: (error) => {
          toast.error(errorMessage(error))
          setBody((current) => current || text)
          setFiles((current) => (current.length > 0 ? current : attached))
        },
      },
    )
    textRef.current?.focus()
  }

  const onKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === 'Enter' && !event.shiftKey && !event.nativeEvent.isComposing) {
      event.preventDefault()
      submit(body, files)
    }
  }

  const onPaste = (event: ClipboardEvent<HTMLTextAreaElement>) => {
    const pasted = Array.from(event.clipboardData.files)
    if (pasted.length > 0) {
      event.preventDefault()
      // Pasted screenshots are all called "image.png"; give each its own name.
      addFiles(
        pasted.map((file, index) =>
          file.name === 'image.png'
            ? new File([file], `captura-${Date.now()}${index > 0 ? `-${index}` : ''}.png`, { type: file.type })
            : file,
        ),
      )
    }
  }

  const startRecording = async () => {
    try {
      await voice.start()
    } catch (error) {
      toast.error(
        error instanceof DOMException && error.name === 'NotAllowedError'
          ? 'Permite el acceso al micrófono para grabar notas de voz.'
          : errorMessage(error),
      )
    }
  }

  const sendRecording = async () => {
    const note = await voice.stop()
    if (note) {
      submit('', [note])
    }
  }

  if (voice.recording) {
    return (
      <div className="flex items-center gap-3 rounded-2xl border border-line bg-surface px-3 py-2.5 shadow-card">
        <Button variant="ghost" size="icon" onClick={voice.cancel} aria-label="Descartar la nota de voz">
          <Trash2 />
        </Button>
        <span className="relative flex size-3">
          <span className="absolute inline-flex size-full animate-ping rounded-full bg-danger opacity-60" />
          <span className="relative inline-flex size-3 rounded-full bg-danger" />
        </span>
        <span className="flex-1 text-sm font-medium tabular-nums" aria-live="polite">
          Grabando {formatDuration(voice.seconds)}
        </span>
        <Button onClick={() => void sendRecording()} aria-label="Enviar la nota de voz">
          <Square className="fill-current" />
          Enviar
        </Button>
      </div>
    )
  }

  const empty = !body.trim() && files.length === 0

  return (
    <div className="rounded-2xl border border-line bg-surface shadow-card focus-within:border-primary focus-within:ring-3 focus-within:ring-ring">
      {files.length > 0 && (
        <ul className="flex flex-wrap gap-2 border-b border-line p-2.5" aria-label="Archivos para enviar">
          {files.map((file, index) => (
            <PendingFile
              key={`${file.name}-${index}`}
              file={file}
              onRemove={() => setFiles((current) => current.filter((_, i) => i !== index))}
            />
          ))}
        </ul>
      )}

      <div className="flex items-end gap-1.5 p-1.5">
        <Button
          variant="ghost"
          size="icon"
          onClick={() => inputRef.current?.click()}
          aria-label="Adjuntar archivos"
          title="Imágenes, audio, video o documentos · hasta 25 MB cada uno"
        >
          <Paperclip />
        </Button>
        <input
          ref={inputRef}
          type="file"
          multiple
          hidden
          accept={allowedChatExtensions.join(',')}
          onChange={(event) => {
            addFiles(Array.from(event.target.files ?? []))
            event.target.value = ''
          }}
        />

        <textarea
          ref={textRef}
          value={body}
          onChange={(event) => setBody(event.target.value)}
          onKeyDown={onKeyDown}
          onPaste={onPaste}
          rows={1}
          maxLength={maxChatBodyLength}
          placeholder="Escribe un mensaje…"
          title="Enter envía · Shift+Enter salta de línea"
          aria-label="Mensaje"
          className="scrollbar-thin max-h-40 min-h-9 flex-1 resize-none bg-transparent px-1.5 py-2 text-sm leading-5 placeholder:text-faint focus:outline-none focus-visible:ring-0"
        />

        {empty && canRecordVoice() ? (
          <Button variant="ghost" size="icon" onClick={() => void startRecording()} aria-label="Grabar nota de voz">
            <Mic />
          </Button>
        ) : (
          <Button size="icon" onClick={() => submit(body, files)} disabled={empty} aria-label="Enviar">
            <SendHorizontal />
          </Button>
        )}
      </div>

      {body.length > maxChatBodyLength - 200 && (
        <p className={cn('px-3 pb-2 text-right text-xs', body.length >= maxChatBodyLength ? 'text-danger' : 'text-faint')}>
          {body.length}/{maxChatBodyLength}
        </p>
      )}
    </div>
  )
}

function PendingFile({ file, onRemove }: { file: File; onRemove: () => void }) {
  const isImage = attachmentKind(file.type) === 'image'
  const preview = useRef<HTMLImageElement>(null)

  // Set on the element directly: the URL lives exactly as long as this thumbnail.
  useEffect(() => {
    if (!isImage || !preview.current) {
      return
    }
    const url = URL.createObjectURL(file)
    preview.current.src = url
    return () => URL.revokeObjectURL(url)
  }, [file, isImage])

  return (
    <li className="group relative flex max-w-56 items-center gap-2 rounded-lg border border-line bg-subtle py-1.5 pr-7 pl-1.5">
      {isImage ? (
        <img ref={preview} alt="" className="size-9 shrink-0 rounded-md bg-muted-surface object-cover" />
      ) : (
        <span className="flex size-9 shrink-0 items-center justify-center rounded-md bg-primary-soft text-primary">
          <FileTypeIcon contentType={file.type} className="size-4" />
        </span>
      )}
      <span className="min-w-0">
        <span className="block truncate text-xs font-medium" title={file.name}>
          {file.name}
        </span>
        <span className="block text-[11px] text-muted">{formatBytes(file.size)}</span>
      </span>
      <button
        type="button"
        onClick={onRemove}
        className="absolute top-1 right-1 cursor-pointer rounded-md p-0.5 text-faint hover:bg-muted-surface hover:text-fg"
        aria-label={`Quitar ${file.name}`}
      >
        <X className="size-3.5" />
      </button>
    </li>
  )
}
