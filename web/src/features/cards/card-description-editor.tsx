import { AlignLeft } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/input'
import { errorMessage } from '@/lib/api/api-error'
import { useUpdateCard } from './card-queries'
import type { CardDetail } from './cards-api'
import { SectionTitle } from './section-title'

const maxLength = 5000

export function CardDescriptionEditor({ card, canEdit }: { card: CardDetail; canEdit: boolean }) {
  const update = useUpdateCard(card)
  const [editing, setEditing] = useState(false)
  const [draft, setDraft] = useState(card.description ?? '')

  const startEditing = () => {
    setDraft(card.description ?? '')
    setEditing(true)
  }

  const save = () => {
    const description = draft.trim() || null
    setEditing(false)
    if (description === card.description) {
      return
    }
    update.mutate({ description }, { onError: (error) => toast.error(errorMessage(error)) })
  }

  return (
    <section>
      <SectionTitle icon={AlignLeft}>Descripción</SectionTitle>
      {editing ? (
        <div className="space-y-2">
          <Textarea
            autoFocus
            rows={6}
            value={draft}
            maxLength={maxLength}
            onChange={(event) => setDraft(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Escape') {
                event.stopPropagation()
                setEditing(false)
              }
              if (event.key === 'Enter' && (event.metaKey || event.ctrlKey)) {
                save()
              }
            }}
            placeholder="Agrega contexto, pasos o enlaces…"
            aria-label="Descripción"
          />
          <div className="flex items-center gap-2">
            <Button size="sm" onClick={save}>
              Guardar
            </Button>
            <Button size="sm" variant="ghost" onClick={() => setEditing(false)}>
              Cancelar
            </Button>
            <span className="ml-auto text-xs text-faint tabular-nums">
              {draft.length}/{maxLength}
            </span>
          </div>
        </div>
      ) : card.description ? (
        <div
          className={canEdit ? '-mx-2 cursor-text rounded-lg px-2 py-1 hover:bg-muted-surface' : undefined}
          onClick={canEdit ? startEditing : undefined}
        >
          <p className="text-sm leading-relaxed whitespace-pre-wrap text-fg/90">{card.description}</p>
        </div>
      ) : canEdit ? (
        <button
          type="button"
          onClick={startEditing}
          className="w-full cursor-pointer rounded-lg border border-dashed border-line-strong px-3 py-4 text-left text-sm text-faint transition-colors hover:border-primary hover:text-primary"
        >
          Agrega una descripción más detallada…
        </button>
      ) : (
        <p className="text-sm text-faint">Sin descripción.</p>
      )}
    </section>
  )
}
