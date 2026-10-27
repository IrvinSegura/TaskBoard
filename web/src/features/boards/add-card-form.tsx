import { Plus, X } from 'lucide-react'
import { useRef, useState, type FormEvent, type KeyboardEvent } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { errorMessage } from '@/lib/api/api-error'
import { useCreateCard } from './board-queries'

const maxTitleLength = 200

export function AddCardForm({ boardId, columnId }: { boardId: string; columnId: string }) {
  const [open, setOpen] = useState(false)
  const [title, setTitle] = useState('')
  const createCard = useCreateCard(boardId)
  const inputRef = useRef<HTMLTextAreaElement>(null)

  const close = () => {
    setOpen(false)
    setTitle('')
  }

  const submit = async (event?: FormEvent) => {
    event?.preventDefault()
    const trimmed = title.trim()
    if (!trimmed) {
      return
    }
    try {
      await createCard.mutateAsync({ columnId, title: trimmed })
      // Stay open so several cards can be added in a row.
      setTitle('')
      inputRef.current?.focus()
    } catch (error) {
      toast.error(errorMessage(error))
    }
  }

  const onKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      void submit()
    }
    if (event.key === 'Escape') {
      close()
    }
  }

  if (!open) {
    return (
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="flex w-full cursor-pointer items-center gap-1.5 rounded-lg px-2 py-1.5 text-[13px] font-medium text-muted transition-colors hover:bg-muted-surface hover:text-fg"
      >
        <Plus className="size-4" />
        Añadir tarjeta
      </button>
    )
  }

  return (
    <form onSubmit={submit} className="space-y-2">
      <textarea
        ref={inputRef}
        autoFocus
        rows={2}
        value={title}
        maxLength={maxTitleLength}
        onChange={(event) => setTitle(event.target.value)}
        onKeyDown={onKeyDown}
        placeholder="Título de la tarjeta…"
        aria-label="Título de la nueva tarjeta"
        className="w-full resize-none rounded-lg border border-primary bg-surface px-3 py-2 text-sm shadow-card ring-3 ring-ring outline-none placeholder:text-faint"
      />
      <div className="flex items-center gap-1.5">
        <Button type="submit" size="sm" loading={createCard.isPending} disabled={!title.trim()}>
          Añadir
        </Button>
        <Button variant="ghost" size="icon-sm" onClick={close} aria-label="Cancelar">
          <X />
        </Button>
      </div>
    </form>
  )
}
