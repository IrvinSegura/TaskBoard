import * as RadixMenu from '@radix-ui/react-dropdown-menu'
import { useQueryClient } from '@tanstack/react-query'
import { Check, UserPlus } from 'lucide-react'
import type { ReactNode } from 'react'
import { toast } from 'sonner'
import { Avatar } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { DropdownMenu, MenuLabel } from '@/components/ui/dropdown-menu'
import { Input, Select } from '@/components/ui/input'
import { useMoveCard } from '@/features/boards/board-queries'
import { cardPriorities, type BoardDetail, type CardPriority } from '@/features/boards/boards-api'
import { priorityMeta } from '@/features/boards/card-meta'
import { LabelPicker } from '@/features/labels/label-picker'
import { errorMessage } from '@/lib/api/api-error'
import { queryKeys } from '@/lib/api/query-keys'
import { fromDateInputValue, toDateInputValue } from '@/lib/utils/format'
import { useToggleAssignee, useUpdateCard } from './card-queries'
import type { CardDetail } from './cards-api'

function FieldBlock({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div>
      <p className="mb-1.5 text-xs font-medium tracking-wide text-muted uppercase">{label}</p>
      {children}
    </div>
  )
}

interface CardFieldsProps {
  card: CardDetail
  board: BoardDetail
  canEdit: boolean
}

export function CardFields({ card, board, canEdit }: CardFieldsProps) {
  const queryClient = useQueryClient()
  const update = useUpdateCard(card)
  const moveCard = useMoveCard(board.id)
  const toggleAssignee = useToggleAssignee(card.id, board.id)
  const onError = (error: unknown) => toast.error(errorMessage(error))

  // Moving from here puts the card at the bottom of the chosen column.
  const moveTo = (columnId: string) => {
    const target = board.columns.find((c) => c.id === columnId)
    if (!target || columnId === card.columnId) {
      return
    }
    const last = target.cards.at(-1)
    moveCard.mutate(
      { cardId: card.id, targetColumnId: columnId, neighbors: { previousId: last?.id ?? null, nextId: null } },
      {
        onSuccess: () => toast.success(`Movida a ${target.name}`),
        onError,
        onSettled: () => queryClient.invalidateQueries({ queryKey: queryKeys.card(card.id), exact: true }),
      },
    )
  }

  const assignedIds = new Set(card.assignees.map((a) => a.userId))

  return (
    <div className="space-y-5">
      <FieldBlock label="Columna">
        <Select
          aria-label="Columna"
          value={card.columnId}
          disabled={!canEdit || moveCard.isPending}
          onChange={(event) => moveTo(event.target.value)}
          className="h-9"
        >
          {board.columns.map((column) => (
            <option key={column.id} value={column.id}>
              {column.name}
            </option>
          ))}
        </Select>
      </FieldBlock>

      <FieldBlock label="Prioridad">
        <Select
          aria-label="Prioridad"
          value={card.priority}
          disabled={!canEdit}
          onChange={(event) => update.mutate({ priority: event.target.value as CardPriority }, { onError })}
          className="h-9"
        >
          {cardPriorities.map((priority) => (
            <option key={priority} value={priority}>
              {priorityMeta[priority].label}
            </option>
          ))}
        </Select>
      </FieldBlock>

      <FieldBlock label="Fecha límite">
        <div className="flex items-center gap-1.5">
          <Input
            type="date"
            aria-label="Fecha límite"
            value={toDateInputValue(card.dueDate)}
            disabled={!canEdit}
            onChange={(event) => update.mutate({ dueDate: fromDateInputValue(event.target.value) }, { onError })}
            className="h-9"
          />
          {card.dueDate && canEdit && (
            <Button variant="ghost" size="sm" onClick={() => update.mutate({ dueDate: null }, { onError })}>
              Quitar
            </Button>
          )}
        </div>
      </FieldBlock>

      <FieldBlock label="Etiquetas">
        <LabelPicker
          cardId={card.id}
          boardId={board.id}
          labels={board.labels}
          labelIds={card.labelIds}
          canEdit={canEdit}
        />
      </FieldBlock>

      <FieldBlock label="Asignados">
        <div className="space-y-2">
          {card.assignees.length === 0 && <p className="text-sm text-faint">Nadie asignado.</p>}
          {card.assignees.map((assignee) => (
            <div key={assignee.userId} className="flex items-center gap-2">
              <Avatar id={assignee.userId} name={assignee.displayName} size="sm" />
              <span className="truncate text-sm">{assignee.displayName}</span>
            </div>
          ))}
          {canEdit && (
            <DropdownMenu
              align="start"
              trigger={
                <Button variant="secondary" size="sm" className="w-full">
                  <UserPlus />
                  Asignar
                </Button>
              }
            >
              <MenuLabel>Miembros del tablero</MenuLabel>
              {board.members.map((member) => {
                const assigned = assignedIds.has(member.userId)
                return (
                  <RadixMenu.CheckboxItem
                    key={member.userId}
                    checked={assigned}
                    // Keep the menu open to toggle several people.
                    onSelect={(event) => event.preventDefault()}
                    onCheckedChange={() =>
                      toggleAssignee.mutate({ userId: member.userId, assigned }, { onError })
                    }
                    className="flex cursor-pointer items-center gap-2.5 rounded-lg px-2.5 py-1.5 text-sm outline-none select-none data-[highlighted]:bg-muted-surface"
                  >
                    <Avatar id={member.userId} name={member.displayName} size="xs" />
                    <span className="flex-1 truncate">{member.displayName}</span>
                    {assigned && <Check className="size-4 text-primary" />}
                  </RadixMenu.CheckboxItem>
                )
              })}
            </DropdownMenu>
          )}
        </div>
      </FieldBlock>
    </div>
  )
}
