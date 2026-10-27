import { useState, type KeyboardEvent } from 'react'
import { toast } from 'sonner'
import { errorMessage } from '@/lib/api/api-error'
import { cn } from '@/lib/utils/cn'
import { useUpdateCard } from './card-queries'
import type { CardDetail } from './cards-api'

export function CardTitleEditor({ card, canEdit }: { card: CardDetail; canEdit: boolean }) {
  const update = useUpdateCard(card)
  const [editing, setEditing] = useState(false)
  const [title, setTitle] = useState(card.title)

  const save = () => {
    setEditing(false)
    const trimmed = title.trim()
    if (!trimmed || trimmed === card.title) {
      setTitle(card.title)
      return
    }
    update.mutate({ title: trimmed }, { onError: (error) => toast.error(errorMessage(error)) })
  }

  const onKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === 'Enter') {
      event.preventDefault()
      save()
    } else if (event.key === 'Escape') {
      // Keep the dialog open: Escape here only cancels the edit.
      event.stopPropagation()
      setTitle(card.title)
      setEditing(false)
    }
  }

  if (editing) {
    return (
      <textarea
        autoFocus
        rows={2}
        value={title}
        maxLength={200}
        onChange={(event) => setTitle(event.target.value)}
        onBlur={save}
        onKeyDown={onKeyDown}
        aria-label="Título de la tarjeta"
        className="mt-1 w-full resize-none rounded-lg border border-primary bg-surface px-2 py-1 text-xl font-semibold tracking-tight ring-3 ring-ring outline-none"
      />
    )
  }

  return (
    <h2
      className={cn(
        'mt-1 rounded-lg px-2 py-1 -mx-2 text-xl leading-snug font-semibold tracking-tight break-words',
        canEdit && 'cursor-text hover:bg-muted-surface',
      )}
      onClick={() => {
        if (canEdit) {
          setTitle(card.title)
          setEditing(true)
        }
      }}
    >
      {card.title}
    </h2>
  )
}
