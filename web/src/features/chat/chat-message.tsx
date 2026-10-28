import { Copy, MoreHorizontal, Pencil, Trash2 } from 'lucide-react'
import { useState, type KeyboardEvent } from 'react'
import { toast } from 'sonner'
import { Avatar } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { ConfirmDialog } from '@/components/ui/confirm-dialog'
import { DropdownMenu, MenuItem, MenuSeparator } from '@/components/ui/dropdown-menu'
import { errorMessage } from '@/lib/api/api-error'
import { cn } from '@/lib/utils/cn'
import type { ChatMessage } from './chat-api'
import { ChatAttachments } from './chat-attachments'
import { maxChatBodyLength } from './chat-files'
import { useDeleteMessage, useEditMessage } from './chat-queries'
import { formatMessageTime } from './chat-text'
import { linkify } from './linkify'

interface ChatMessageItemProps {
  message: ChatMessage
  mine: boolean
  /** First of a run of messages from the same person: shows their avatar and name. */
  startsGroup: boolean
  showSenderName: boolean
}

export function ChatMessageItem({ message, mine, startsGroup, showSenderName }: ChatMessageItemProps) {
  const [editing, setEditing] = useState(false)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const remove = useDeleteMessage(message.conversationId)

  const copy = () =>
    void navigator.clipboard
      .writeText(message.body ?? '')
      .then(() => toast.success('Texto copiado'))
      .catch(() => toast.error('No se pudo copiar'))

  return (
    <li className={cn('group flex gap-2.5', mine ? 'flex-row-reverse' : 'flex-row', startsGroup ? 'mt-3' : 'mt-0.5')}>
      <div className="w-8 shrink-0">
        {!mine && startsGroup && <Avatar id={message.senderId} name={message.senderName} />}
      </div>

      <div className={cn('flex max-w-[min(36rem,80%)] min-w-0 flex-col gap-1', mine ? 'items-end' : 'items-start')}>
        {startsGroup && (showSenderName || mine) && (
          <span className="px-1 text-xs text-muted">
            {!mine && <span className="mr-1.5 font-medium text-fg">{message.senderName}</span>}
            {formatMessageTime(message.createdAt)}
          </span>
        )}

        {message.isDeleted ? (
          <p className="rounded-2xl border border-dashed border-line-strong px-3.5 py-2 text-sm text-faint italic">
            Mensaje eliminado
          </p>
        ) : editing ? (
          <EditBox message={message} onDone={() => setEditing(false)} />
        ) : (
          <>
            {message.body && (
              <p
                className={cn(
                  'rounded-2xl px-3.5 py-2 text-sm leading-relaxed break-words whitespace-pre-wrap',
                  mine
                    ? 'rounded-tr-md bg-primary text-primary-fg [&_a]:text-primary-fg'
                    : 'rounded-tl-md border border-line bg-surface text-fg shadow-card [&_a]:text-primary',
                  !startsGroup && (mine ? 'rounded-tr-2xl' : 'rounded-tl-2xl'),
                )}
              >
                <MessageText text={message.body} />
              </p>
            )}
            {message.attachments.length > 0 && <ChatAttachments attachments={message.attachments} mine={mine} />}
          </>
        )}

        {!message.isDeleted && (message.isEdited || !startsGroup) && (
          <span className="px-1 text-[11px] text-faint opacity-0 transition-opacity group-hover:opacity-100 group-focus-within:opacity-100">
            {formatMessageTime(message.createdAt)}
            {message.isEdited && ' · editado'}
          </span>
        )}
      </div>

      {!message.isDeleted && !editing && (mine || message.body) && (
        <div className="self-center opacity-0 transition-opacity group-hover:opacity-100 focus-within:opacity-100 has-[[data-state=open]]:opacity-100">
          <DropdownMenu
            align={mine ? 'end' : 'start'}
            trigger={
              <Button variant="ghost" size="icon-sm" aria-label="Opciones del mensaje">
                <MoreHorizontal />
              </Button>
            }
          >
            {message.body && (
              <MenuItem icon={<Copy />} onSelect={copy}>
                Copiar texto
              </MenuItem>
            )}
            {mine && (
              <>
                <MenuItem icon={<Pencil />} onSelect={() => setEditing(true)}>
                  Editar
                </MenuItem>
                <MenuSeparator />
                <MenuItem icon={<Trash2 />} destructive onSelect={() => setConfirmDelete(true)}>
                  Eliminar
                </MenuItem>
              </>
            )}
          </DropdownMenu>
        </div>
      )}

      <ConfirmDialog
        open={confirmDelete}
        onOpenChange={setConfirmDelete}
        title="¿Eliminar el mensaje?"
        description="Se borrará para todos, junto con sus archivos. En la conversación quedará como “Mensaje eliminado”."
        confirmLabel="Eliminar"
        loading={remove.isPending}
        onConfirm={() =>
          remove.mutate(message.id, {
            onSuccess: () => setConfirmDelete(false),
            onError: (error) => toast.error(errorMessage(error)),
          })
        }
      />
    </li>
  )
}

function MessageText({ text }: { text: string }) {
  return (
    <>
      {linkify(text).map((segment, index) =>
        segment.type === 'link' ? (
          <a
            key={index}
            href={segment.href}
            target="_blank"
            rel="noopener noreferrer nofollow"
            className="font-medium underline underline-offset-2"
          >
            {segment.text}
          </a>
        ) : (
          <span key={index}>{segment.text}</span>
        ),
      )}
    </>
  )
}

function EditBox({ message, onDone }: { message: ChatMessage; onDone: () => void }) {
  const edit = useEditMessage(message.conversationId)
  const [body, setBody] = useState(message.body ?? '')
  // A message that only had text cannot be emptied; one with files can.
  const canSave = body.trim().length > 0 || message.attachments.length > 0

  const save = () => {
    if (!canSave) {
      return
    }
    if (body.trim() === (message.body ?? '')) {
      onDone()
      return
    }
    edit.mutate(
      { messageId: message.id, body },
      { onSuccess: onDone, onError: (error) => toast.error(errorMessage(error)) },
    )
  }

  const onKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      save()
    } else if (event.key === 'Escape') {
      onDone()
    }
  }

  return (
    <div className="w-80 max-w-full space-y-2">
      <textarea
        value={body}
        onChange={(event) => setBody(event.target.value)}
        onKeyDown={onKeyDown}
        maxLength={maxChatBodyLength}
        autoFocus
        rows={3}
        aria-label="Editar mensaje"
        className="w-full resize-none rounded-xl border border-primary bg-surface px-3 py-2 text-sm ring-3 ring-ring focus:outline-none"
      />
      <div className="flex justify-end gap-2">
        <Button size="sm" variant="ghost" onClick={onDone}>
          Cancelar
        </Button>
        <Button size="sm" onClick={save} loading={edit.isPending} disabled={!canSave}>
          Guardar
        </Button>
      </div>
    </div>
  )
}
