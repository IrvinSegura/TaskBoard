import { Pencil, Plus, Tag, Trash2 } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { ConfirmDialog } from '@/components/ui/confirm-dialog'
import { EmptyState } from '@/components/ui/feedback'
import { Input } from '@/components/ui/input'
import { errorMessage } from '@/lib/api/api-error'
import { LabelChip, LabelColorPicker } from './label-chip'
import { labelColors, type BoardLabel, type LabelColor } from './labels-api'
import { useCreateLabel, useDeleteLabel, useUpdateLabel } from './label-queries'

interface BoardLabelsPanelProps {
  boardId: string
  labels: BoardLabel[]
  canEdit: boolean
}

/** Create, edit and delete a board's labels. */
export function BoardLabelsPanel({ boardId, labels, canEdit }: BoardLabelsPanelProps) {
  const remove = useDeleteLabel(boardId)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [deleting, setDeleting] = useState<BoardLabel | null>(null)

  const onDelete = (label: BoardLabel) =>
    remove.mutate(label.id, {
      onSuccess: () => {
        setDeleting(null)
        toast.success(`Se eliminó la etiqueta «${label.name}»`)
      },
      onError: (error) => toast.error(errorMessage(error)),
    })

  return (
    <Card>
      <CardHeader>
        <div>
          <CardTitle>Etiquetas</CardTitle>
          <CardDescription>
            {canEdit
              ? 'Sirven para clasificar las tarjetas y filtrarlas en el tablero.'
              : 'Solo los editores y administradores pueden cambiarlas.'}
          </CardDescription>
        </div>
      </CardHeader>

      <div className="space-y-4 p-5">
        {labels.length === 0 ? (
          <EmptyState icon={Tag} title="Este tablero no tiene etiquetas" className="py-8" />
        ) : (
          <ul className="divide-y divide-line rounded-lg border border-line">
            {labels.map((label) =>
              editingId === label.id ? (
                <li key={label.id} className="p-3">
                  <LabelForm boardId={boardId} label={label} onDone={() => setEditingId(null)} />
                </li>
              ) : (
                <li key={label.id} className="flex items-center gap-2 px-3 py-2">
                  <LabelChip label={label} className="text-[13px]" />
                  {canEdit && (
                    <div className="ml-auto flex items-center gap-1">
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        aria-label={`Editar ${label.name}`}
                        onClick={() => setEditingId(label.id)}
                      >
                        <Pencil />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        className="text-faint hover:text-danger"
                        aria-label={`Eliminar ${label.name}`}
                        onClick={() => setDeleting(label)}
                      >
                        <Trash2 />
                      </Button>
                    </div>
                  )}
                </li>
              ),
            )}
          </ul>
        )}

        {canEdit && <LabelForm boardId={boardId} />}
      </div>

      <ConfirmDialog
        open={deleting !== null}
        onOpenChange={(open) => !open && setDeleting(null)}
        title={`¿Eliminar la etiqueta «${deleting?.name ?? ''}»?`}
        description="Se quitará de todas las tarjetas que la tengan."
        confirmLabel="Eliminar etiqueta"
        loading={remove.isPending}
        onConfirm={() => deleting && onDelete(deleting)}
      />
    </Card>
  )
}

interface LabelFormProps {
  boardId: string
  /** Edits this label; without it the form creates a new one. */
  label?: BoardLabel
  onDone?: () => void
}

function LabelForm({ boardId, label, onDone }: LabelFormProps) {
  const create = useCreateLabel(boardId)
  const update = useUpdateLabel(boardId)
  const [name, setName] = useState(label?.name ?? '')
  const [color, setColor] = useState<LabelColor>(label?.color ?? 'Blue')
  const pending = create.isPending || update.isPending
  const trimmed = name.trim()

  const onSubmit = (event: FormEvent) => {
    event.preventDefault()
    if (!trimmed) {
      return
    }
    const onError = (error: unknown) => toast.error(errorMessage(error))
    if (label) {
      update.mutate({ id: label.id, name: trimmed, color }, { onSuccess: () => onDone?.(), onError })
    } else {
      create.mutate(
        { name: trimmed, color },
        {
          onSuccess: () => {
            setName('')
            toast.success(`Etiqueta «${trimmed}» creada`)
          },
          onError,
        },
      )
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-3">
      <div className="flex gap-2">
        <Input
          value={name}
          maxLength={50}
          autoFocus={label !== undefined}
          onChange={(event) => setName(event.target.value)}
          placeholder={label ? 'Nombre de la etiqueta' : 'Nueva etiqueta'}
          aria-label="Nombre de la etiqueta"
          className="h-9"
        />
        <Button type="submit" size="sm" className="h-9 shrink-0" loading={pending} disabled={!trimmed}>
          {!label && <Plus />}
          {label ? 'Guardar' : 'Crear'}
        </Button>
        {label && (
          <Button variant="ghost" size="sm" className="h-9 shrink-0" onClick={onDone}>
            Cancelar
          </Button>
        )}
      </div>
      <LabelColorPicker value={color} onChange={setColor} colors={labelColors} />
    </form>
  )
}
