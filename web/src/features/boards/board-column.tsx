import { SortableContext, useSortable, verticalListSortingStrategy } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { GripVertical, MoreHorizontal, Pencil, Trash2 } from 'lucide-react'
import { useState, type KeyboardEvent } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { ConfirmDialog } from '@/components/ui/confirm-dialog'
import { DropdownMenu, MenuItem, MenuSeparator } from '@/components/ui/dropdown-menu'
import { useCurrentUser } from '@/features/auth/auth-provider'
import type { BoardLabel } from '@/features/labels/labels-api'
import { errorMessage } from '@/lib/api/api-error'
import { cn } from '@/lib/utils/cn'
import { AddCardForm } from './add-card-form'
import { BoardCard } from './board-card'
import { useDeleteColumn, useRenameColumn } from './board-queries'
import type { BoardColumn as Column, BoardMember, CardSummary } from './boards-api'

// From "not started" grey to "done" green as columns go left to right, then around again.
const columnAccents = ['bg-slate-400', 'bg-info', 'bg-warning', 'bg-primary', 'bg-success']

interface BoardColumnProps {
  boardId: string
  column: Column
  /** The cards to show, already filtered when a search is active. */
  cards: CardSummary[]
  members: BoardMember[]
  labels: BoardLabel[]
  /** Position of the column on the board, which picks its accent color. */
  index: number
  canEdit: boolean
  dragEnabled: boolean
  filtered: boolean
  onOpenCard: (cardId: string) => void
}

export function BoardColumn({
  boardId,
  column,
  cards,
  members,
  labels,
  index,
  canEdit,
  dragEnabled,
  filtered,
  onOpenCard,
}: BoardColumnProps) {
  const { attributes, listeners, setNodeRef, setActivatorNodeRef, transform, transition, isDragging } = useSortable({
    id: column.id,
    data: { type: 'column' },
    disabled: !dragEnabled,
  })

  return (
    <section
      ref={setNodeRef}
      style={{ transform: CSS.Translate.toString(transform), transition }}
      aria-label={column.name}
      className={cn(
        'flex max-h-full w-72 shrink-0 flex-col rounded-xl border border-line/70 bg-subtle',
        isDragging && 'opacity-40',
      )}
    >
      <header className="flex items-center gap-1 px-2 pt-2.5 pb-1.5">
        {dragEnabled && (
          <button
            ref={setActivatorNodeRef}
            {...attributes}
            {...listeners}
            type="button"
            className="cursor-grab rounded p-1 text-faint hover:bg-muted-surface hover:text-fg active:cursor-grabbing"
            aria-label={`Mover la columna ${column.name}`}
          >
            <GripVertical className="size-4" />
          </button>
        )}
        <span className={cn('ml-1 size-2 shrink-0 rounded-full', columnAccents[index % columnAccents.length])} aria-hidden="true" />
        <ColumnTitle boardId={boardId} column={column} members={members} canEdit={canEdit} />
        <span className="ml-1 rounded-full bg-muted-surface px-1.5 text-xs font-medium text-muted tabular-nums">
          {filtered ? `${cards.length}/${column.cards.length}` : column.cards.length}
        </span>
      </header>

      <SortableContext items={cards.map((card) => card.id)} strategy={verticalListSortingStrategy}>
        <ol className="scrollbar-thin flex min-h-10 flex-1 flex-col gap-2 overflow-y-auto px-2 pt-1 pb-2">
          {cards.map((card) => (
            <BoardCard
              key={card.id}
              card={card}
              columnId={column.id}
              members={members}
              labels={labels}
              draggable={dragEnabled}
              onOpen={onOpenCard}
            />
          ))}
          {cards.length === 0 && (
            <li className="list-none rounded-lg border border-dashed border-line-strong/70 px-3 py-5 text-center text-xs text-faint">
              {filtered ? 'Ninguna tarjeta coincide' : 'Sin tarjetas'}
            </li>
          )}
        </ol>
      </SortableContext>

      {canEdit && !filtered && (
        <div className="px-2 pb-2">
          <AddCardForm boardId={boardId} columnId={column.id} />
        </div>
      )}
    </section>
  )
}

interface ColumnTitleProps {
  boardId: string
  column: Column
  members: BoardMember[]
  canEdit: boolean
}

function ColumnTitle({ boardId, column, members, canEdit }: ColumnTitleProps) {
  const me = useCurrentUser()
  const [editing, setEditing] = useState(false)
  const [name, setName] = useState(column.name)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const rename = useRenameColumn(boardId)
  const remove = useDeleteColumn(boardId)
  // Deleting takes the column's cards with it, so the API keeps it for board admins.
  const canDelete = canEdit && members.some((m) => m.userId === me.id && m.role === 'Admin')

  // mutateAsync rather than mutate callbacks: the board refetch removes this column, and with it this component,
  // before per-call callbacks would run.
  const onDelete = async () => {
    try {
      await remove.mutateAsync(column.id)
      toast.success(`Se eliminó la columna «${column.name}»`)
    } catch (error) {
      toast.error(errorMessage(error))
    }
  }
  const cardCount = column.cards.length

  const save = () => {
    const trimmed = name.trim()
    setEditing(false)
    if (!trimmed || trimmed === column.name) {
      setName(column.name)
      return
    }
    rename.mutate(
      { columnId: column.id, name: trimmed },
      {
        onError: (error) => {
          setName(column.name)
          toast.error(errorMessage(error))
        },
      },
    )
  }

  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter') {
      save()
    }
    if (event.key === 'Escape') {
      setName(column.name)
      setEditing(false)
    }
  }

  if (editing) {
    return (
      <input
        autoFocus
        value={name}
        maxLength={100}
        onChange={(event) => setName(event.target.value)}
        onBlur={save}
        onKeyDown={onKeyDown}
        aria-label="Nombre de la columna"
        className="h-7 min-w-0 flex-1 rounded-md border border-primary bg-surface px-2 text-sm font-semibold ring-3 ring-ring outline-none"
      />
    )
  }

  return (
    <>
      <h2
        className={cn('min-w-0 flex-1 truncate px-1 text-sm font-semibold', canEdit && 'cursor-text')}
        onDoubleClick={() => canEdit && setEditing(true)}
        title={column.name}
      >
        {rename.isPending ? rename.variables?.name : column.name}
      </h2>
      {canEdit && (
        <DropdownMenu
          trigger={
            <Button variant="ghost" size="icon-sm" aria-label={`Opciones de ${column.name}`}>
              <MoreHorizontal />
            </Button>
          }
        >
          <MenuItem
            icon={<Pencil />}
            onSelect={() => {
              setName(column.name)
              setEditing(true)
            }}
          >
            Renombrar columna
          </MenuItem>
          {canDelete && <MenuSeparator />}
          {canDelete && (
            <MenuItem icon={<Trash2 />} destructive onSelect={() => setConfirmDelete(true)}>
              Eliminar columna
            </MenuItem>
          )}
        </DropdownMenu>
      )}
      <ConfirmDialog
        open={confirmDelete}
        onOpenChange={setConfirmDelete}
        title={`¿Eliminar la columna «${column.name}»?`}
        description={
          cardCount > 0
            ? `También se eliminarán ${cardCount === 1 ? 'su tarjeta' : `sus ${cardCount} tarjetas`} y las archivadas que tenga, con sus comentarios y adjuntos. No se puede deshacer.`
            : 'También se eliminarán las tarjetas archivadas que tenga. No se puede deshacer.'
        }
        confirmLabel="Eliminar columna"
        loading={remove.isPending}
        onConfirm={() => void onDelete()}
      />
    </>
  )
}
