import {
  closestCenter,
  closestCorners,
  DndContext,
  DragOverlay,
  KeyboardSensor,
  PointerSensor,
  pointerWithin,
  TouchSensor,
  useSensor,
  useSensors,
  type CollisionDetection,
  type DragEndEvent,
  type DragOverEvent,
  type DragStartEvent,
} from '@dnd-kit/core'
import { arrayMove, horizontalListSortingStrategy, SortableContext, sortableKeyboardCoordinates } from '@dnd-kit/sortable'
import { useCallback, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { toast } from 'sonner'
import { errorMessage } from '@/lib/api/api-error'
import { AddColumn } from './add-column'
import { BoardCardView } from './board-card'
import { BoardColumn } from './board-column'
import { neighborsOf } from './board-order'
import { useMoveCard, useMoveColumn } from './board-queries'
import type { BoardColumn as Column, BoardDetail } from './boards-api'

interface BoardCanvasProps {
  board: BoardDetail
  canEdit: boolean
  /** Card ids matching the active filters, or null when nothing is filtered. */
  visibleCardIds: Set<string> | null
  onOpenCard: (cardId: string) => void
}

type Active = { type: 'card' | 'column'; id: string }

/** Where a card sits: its column and the cards around it. */
function cardLocation(columns: Column[], cardId: string) {
  const column = columns.find((c) => c.cards.some((card) => card.id === cardId))
  if (!column) {
    return null
  }
  const ids = column.cards.map((card) => card.id)
  return { columnId: column.id, ...neighborsOf(ids, cardId) }
}

export function BoardCanvas({ board, canEdit, visibleCardIds, onOpenCard }: BoardCanvasProps) {
  const moveCard = useMoveCard(board.id)
  const moveColumn = useMoveColumn(board.id)
  const [active, setActive] = useState<Active | null>(null)
  // Local order while dragging. It also covers the moment between the drop and the optimistic cache update:
  // it only applies while `base` is still the board it was taken from.
  const [draft, setDraft] = useState<{ base: BoardDetail; columns: Column[] } | null>(null)
  const lastDragEnd = useRef(0)

  const columns = draft && (active || draft.base === board) ? draft.columns : board.columns
  // Neighbor ids would be wrong with hidden cards in between, so filtering turns dragging off.
  const dragEnabled = canEdit && visibleCardIds === null

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 6 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 200, tolerance: 6 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  )

  const collisionDetection: CollisionDetection = useCallback(
    (args) => {
      if (active?.type === 'column') {
        return closestCenter({
          ...args,
          droppableContainers: args.droppableContainers.filter((c) => c.data.current?.type === 'column'),
        })
      }
      // Prefer the card under the pointer, then the column under it (empty columns), then the nearest card.
      const pointer = pointerWithin(args)
      const cards = pointer.filter((c) => args.droppableContainers.find((d) => d.id === c.id)?.data.current?.type === 'card')
      if (cards.length > 0) {
        return cards
      }
      return pointer.length > 0 ? pointer : closestCorners(args)
    },
    [active],
  )

  const onDragStart = ({ active: dragged }: DragStartEvent) => {
    setActive({ type: dragged.data.current?.type === 'column' ? 'column' : 'card', id: String(dragged.id) })
    setDraft({ base: board, columns: board.columns })
  }

  // Moves the card into the column it is over, so the target column opens a gap while dragging.
  const onDragOver = ({ active: dragged, over }: DragOverEvent) => {
    if (!draft || !over || dragged.data.current?.type !== 'card') {
      return
    }
    const cardId = String(dragged.id)
    const overId = String(over.id)
    const from = draft.columns.find((c) => c.cards.some((card) => card.id === cardId))
    const to =
      over.data.current?.type === 'column'
        ? draft.columns.find((c) => c.id === overId)
        : draft.columns.find((c) => c.cards.some((card) => card.id === overId))
    if (!from || !to || from.id === to.id) {
      return
    }
    const card = from.cards.find((c) => c.id === cardId)!
    const overIndex = to.cards.findIndex((c) => c.id === overId)
    const insertAt = overIndex === -1 ? to.cards.length : overIndex
    setDraft({
      base: draft.base,
      columns: draft.columns.map((column) => {
        if (column.id === from.id) {
          return { ...column, cards: column.cards.filter((c) => c.id !== cardId) }
        }
        if (column.id === to.id) {
          const cards = [...column.cards]
          cards.splice(insertAt, 0, card)
          return { ...column, cards }
        }
        return column
      }),
    })
  }

  const onDragEnd = ({ active: dragged, over }: DragEndEvent) => {
    lastDragEnd.current = Date.now()
    setActive(null)
    if (!draft || !over) {
      setDraft(null)
      return
    }
    const draggedId = String(dragged.id)
    const overId = String(over.id)

    if (dragged.data.current?.type === 'column') {
      const ids = draft.columns.map((c) => c.id)
      const from = ids.indexOf(draggedId)
      const to = ids.indexOf(overId)
      if (from === -1 || to === -1 || from === to) {
        setDraft(null)
        return
      }
      const reordered = arrayMove(draft.columns, from, to)
      setDraft({ base: draft.base, columns: reordered })
      moveColumn.mutate(
        { columnId: draggedId, neighbors: neighborsOf(reordered.map((c) => c.id), draggedId) },
        { onError: (error) => toast.error(errorMessage(error)) },
      )
      return
    }

    // Reorder inside the column the card ended up in (onDragOver already moved it across columns).
    let finalColumns = draft.columns
    const column = finalColumns.find((c) => c.cards.some((card) => card.id === draggedId))
    if (column) {
      const from = column.cards.findIndex((c) => c.id === draggedId)
      const to = column.cards.findIndex((c) => c.id === overId)
      if (to !== -1 && from !== to) {
        finalColumns = finalColumns.map((c) =>
          c.id === column.id ? { ...c, cards: arrayMove(c.cards, from, to) } : c,
        )
      }
    }

    const before = cardLocation(draft.base.columns, draggedId)
    const after = cardLocation(finalColumns, draggedId)
    if (
      !after ||
      (before &&
        before.columnId === after.columnId &&
        before.previousId === after.previousId &&
        before.nextId === after.nextId)
    ) {
      setDraft(null)
      return
    }
    setDraft({ base: draft.base, columns: finalColumns })
    moveCard.mutate(
      {
        cardId: draggedId,
        targetColumnId: after.columnId,
        neighbors: { previousId: after.previousId, nextId: after.nextId },
      },
      { onError: (error) => toast.error(errorMessage(error)) },
    )
  }

  const onDragCancel = () => {
    setActive(null)
    setDraft(null)
  }

  // A drop ends with a click on the card under the pointer; do not open it.
  const openCard = (cardId: string) => {
    if (Date.now() - lastDragEnd.current > 250) {
      onOpenCard(cardId)
    }
  }

  const activeCard =
    active?.type === 'card' ? columns.flatMap((c) => c.cards).find((card) => card.id === active.id) : undefined
  const activeColumn = active?.type === 'column' ? columns.find((c) => c.id === active.id) : undefined

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={collisionDetection}
      onDragStart={onDragStart}
      onDragOver={onDragOver}
      onDragEnd={onDragEnd}
      onDragCancel={onDragCancel}
      accessibility={{
        screenReaderInstructions: {
          draggable: 'Pulsa espacio para tomar el elemento, usa las flechas para moverlo y espacio de nuevo para soltarlo.',
        },
      }}
    >
      <div className="scrollbar-thin flex h-full items-start gap-3 overflow-x-auto px-4 pb-4 sm:px-6">
        <SortableContext items={columns.map((c) => c.id)} strategy={horizontalListSortingStrategy}>
          {columns.map((column, index) => (
            <BoardColumn
              key={column.id}
              boardId={board.id}
              column={column}
              cards={visibleCardIds ? column.cards.filter((card) => visibleCardIds.has(card.id)) : column.cards}
              members={board.members}
              labels={board.labels}
              index={index}
              canEdit={canEdit}
              dragEnabled={dragEnabled}
              filtered={visibleCardIds !== null}
              onOpenCard={openCard}
            />
          ))}
        </SortableContext>
        {canEdit && <AddColumn boardId={board.id} emphasize={columns.length === 0} />}
      </div>

      {createPortal(
        <DragOverlay dropAnimation={{ duration: 180, easing: 'cubic-bezier(0.16, 1, 0.3, 1)' }}>
          {activeCard && <BoardCardView card={activeCard} members={board.members} labels={board.labels} overlay />}
          {activeColumn && (
            <div className="flex w-72 rotate-1 flex-col gap-2 rounded-xl border border-line bg-subtle p-2 shadow-overlay">
              <p className="px-1 pt-0.5 text-sm font-semibold">{activeColumn.name}</p>
              {activeColumn.cards.slice(0, 6).map((card) => (
                <BoardCardView key={card.id} card={card} members={board.members} labels={board.labels} />
              ))}
            </div>
          )}
        </DragOverlay>,
        document.body,
      )}
    </DndContext>
  )
}
