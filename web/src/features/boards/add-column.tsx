import { Plus, X } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { errorMessage } from '@/lib/api/api-error'
import { useCreateColumn } from './board-queries'

export function AddColumn({ boardId, emphasize }: { boardId: string; emphasize?: boolean }) {
  const [open, setOpen] = useState(emphasize ?? false)
  const [name, setName] = useState('')
  const createColumn = useCreateColumn(boardId)

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    const trimmed = name.trim()
    if (!trimmed) {
      return
    }
    try {
      await createColumn.mutateAsync(trimmed)
      setName('')
    } catch (error) {
      toast.error(errorMessage(error))
    }
  }

  if (!open) {
    return (
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="flex h-11 w-72 shrink-0 cursor-pointer items-center gap-2 rounded-xl border border-dashed border-line-strong px-3 text-sm font-medium text-muted transition-colors hover:border-primary hover:bg-primary-soft hover:text-primary"
      >
        <Plus className="size-4" />
        Añadir columna
      </button>
    )
  }

  return (
    <form onSubmit={submit} className="w-72 shrink-0 space-y-2 rounded-xl border border-line bg-subtle p-2">
      <Input
        autoFocus
        value={name}
        maxLength={100}
        onChange={(event) => setName(event.target.value)}
        onKeyDown={(event) => event.key === 'Escape' && setOpen(false)}
        placeholder="Nombre de la columna"
        aria-label="Nombre de la nueva columna"
        className="h-9"
      />
      <div className="flex items-center gap-1.5">
        <Button type="submit" size="sm" loading={createColumn.isPending} disabled={!name.trim()}>
          Añadir columna
        </Button>
        <Button variant="ghost" size="icon-sm" onClick={() => setOpen(false)} aria-label="Cancelar">
          <X />
        </Button>
      </div>
    </form>
  )
}
