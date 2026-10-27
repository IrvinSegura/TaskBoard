import { ListChecks, Plus, Trash2 } from 'lucide-react'
import { useState, type FormEvent, type KeyboardEvent } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { errorMessage } from '@/lib/api/api-error'
import { cn } from '@/lib/utils/cn'
import { useAddChecklistItem, useDeleteChecklistItem, useUpdateChecklistItem } from './card-queries'
import type { CardDetail, ChecklistItem } from './cards-api'
import { SectionTitle } from './section-title'

const onError = (error: unknown) => toast.error(errorMessage(error))

export function CardChecklist({ card, canEdit }: { card: CardDetail; canEdit: boolean }) {
  const add = useAddChecklistItem(card.id, card.boardId)
  const [text, setText] = useState('')
  const items = card.checklist
  const done = items.filter((item) => item.isDone).length
  const percent = items.length === 0 ? 0 : Math.round((done / items.length) * 100)

  if (items.length === 0 && !canEdit) {
    return null
  }

  const onSubmit = (event: FormEvent) => {
    event.preventDefault()
    const trimmed = text.trim()
    if (!trimmed) {
      return
    }
    add.mutate(trimmed, { onSuccess: () => setText(''), onError })
  }

  return (
    <section>
      <SectionTitle
        icon={ListChecks}
        action={items.length > 0 && <span className="text-xs text-muted tabular-nums">{done}/{items.length}</span>}
      >
        Checklist
      </SectionTitle>

      {items.length > 0 && (
        <div className="mb-3 h-1.5 overflow-hidden rounded-full bg-muted-surface" aria-hidden="true">
          <div
            className={cn('h-full rounded-full transition-[width]', percent === 100 ? 'bg-emerald-500' : 'bg-primary')}
            style={{ width: `${percent}%` }}
          />
        </div>
      )}

      <ul className="space-y-0.5">
        {items.map((item) => (
          <ChecklistRow key={item.id} card={card} item={item} canEdit={canEdit} />
        ))}
      </ul>

      {canEdit && (
        <form onSubmit={onSubmit} className="mt-2 flex gap-2">
          <Input
            value={text}
            maxLength={500}
            onChange={(event) => setText(event.target.value)}
            placeholder="Añadir un elemento…"
            aria-label="Nuevo elemento de la checklist"
            className="h-9"
          />
          <Button type="submit" variant="secondary" size="sm" className="h-9 shrink-0" loading={add.isPending} disabled={!text.trim()}>
            <Plus />
            Añadir
          </Button>
        </form>
      )}
    </section>
  )
}

function ChecklistRow({ card, item, canEdit }: { card: CardDetail; item: ChecklistItem; canEdit: boolean }) {
  const update = useUpdateChecklistItem(card.id, card.boardId)
  const remove = useDeleteChecklistItem(card.id, card.boardId)
  const [editing, setEditing] = useState(false)
  const [text, setText] = useState(item.text)

  const save = () => {
    setEditing(false)
    const trimmed = text.trim()
    if (!trimmed || trimmed === item.text) {
      setText(item.text)
      return
    }
    update.mutate({ itemId: item.id, text: trimmed }, { onError })
  }

  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter') {
      save()
    } else if (event.key === 'Escape') {
      setText(item.text)
      setEditing(false)
    }
  }

  return (
    <li className="group flex items-center gap-2.5 rounded-lg px-1.5 py-1 hover:bg-subtle">
      <input
        type="checkbox"
        checked={item.isDone}
        disabled={!canEdit}
        onChange={(event) => update.mutate({ itemId: item.id, isDone: event.target.checked }, { onError })}
        aria-label={item.text}
        className="size-4 shrink-0 cursor-pointer accent-primary disabled:cursor-default"
      />
      {editing ? (
        <input
          autoFocus
          value={text}
          maxLength={500}
          onChange={(event) => setText(event.target.value)}
          onBlur={save}
          onKeyDown={onKeyDown}
          aria-label="Texto del elemento"
          className="h-7 min-w-0 flex-1 rounded-md border border-primary bg-surface px-2 text-sm ring-3 ring-ring outline-none"
        />
      ) : (
        <span
          className={cn('min-w-0 flex-1 text-sm break-words', item.isDone && 'text-muted line-through', canEdit && 'cursor-text')}
          onDoubleClick={() => canEdit && setEditing(true)}
        >
          {item.text}
        </span>
      )}
      {canEdit && !editing && (
        <Button
          variant="ghost"
          size="icon-sm"
          className="shrink-0 text-faint opacity-100 hover:text-danger sm:opacity-0 sm:group-hover:opacity-100 sm:focus-visible:opacity-100"
          aria-label={`Eliminar «${item.text}»`}
          onClick={() => remove.mutate(item.id, { onError })}
        >
          <Trash2 />
        </Button>
      )}
    </li>
  )
}
