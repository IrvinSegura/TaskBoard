import { ArrowDown, ArrowUp, ArrowUpDown, List, ListChecks, MessageSquare, Paperclip, SearchX } from 'lucide-react'
import { useMemo, useState, type ReactNode } from 'react'
import { Link, Outlet, useNavigate } from 'react-router'
import { AvatarStack } from '@/components/ui/avatar'
import { ExportMenu } from '@/components/ui/export-menu'
import { EmptyState } from '@/components/ui/feedback'
import { useCurrentUser } from '@/features/auth/auth-provider'
import { LabelChip } from '@/features/labels/label-chip'
import { cn } from '@/lib/utils/cn'
import { BoardFilters } from './board-filters'
import { boardListDocument } from './board-list-export'
import {
  boardRows,
  defaultListSort,
  nextSort,
  sortRows,
  type ListSort,
  type ListSortKey,
} from './board-list-rows'
import { useBoardOutlet } from './board-page'
import { DueDateChip, PriorityBadge } from './card-meta'
import { emptyFilters, useFilteredCardIds, type CardFilters } from './card-search'

const headers: { key: ListSortKey | null; label: string; className?: string }[] = [
  { key: 'title', label: 'Tarjeta', className: 'min-w-64' },
  { key: 'column', label: 'Columna' },
  { key: 'priority', label: 'Prioridad' },
  { key: null, label: 'Asignadas' },
  { key: null, label: 'Etiquetas' },
  { key: 'dueDate', label: 'Fecha límite' },
  { key: 'checklist', label: 'Checklist' },
]

/** Every card of the board in one table, to sort and compare them; a click opens the card over the list. */
export function BoardListView() {
  const outlet = useBoardOutlet()
  const { board } = outlet
  const me = useCurrentUser()
  const navigate = useNavigate()
  const [filters, setFilters] = useState<CardFilters>(emptyFilters)
  const [sort, setSort] = useState<ListSort>(defaultListSort)
  const filtered = useFilteredCardIds(board.id, filters)

  const allRows = useMemo(() => boardRows(board), [board])
  const rows = useMemo(() => {
    const visible = filtered.ids ? allRows.filter((row) => filtered.ids!.has(row.card.id)) : allRows
    return sortRows(visible, sort)
  }, [allRows, filtered.ids, sort])

  const membersById = useMemo(() => new Map(board.members.map((m) => [m.userId, m.displayName])), [board.members])
  const labelsById = useMemo(() => new Map(board.labels.map((label) => [label.id, label])), [board.labels])

  return (
    <div className="px-4 py-6 sm:px-6">
      <div className="flex flex-wrap items-end justify-between gap-2">
        <div>
          <h2 className="text-[15px] font-semibold">Lista</h2>
          <p className="mt-1 text-sm text-muted">Las tarjetas del tablero en una tabla, para ordenarlas y compararlas.</p>
        </div>
        <div className="flex items-center gap-3">
          <p className="text-xs text-muted tabular-nums">
            {rows.length === allRows.length ? `${allRows.length} tarjetas` : `${rows.length} de ${allRows.length} tarjetas`}
          </p>
          <ExportMenu
            disabled={rows.length === 0}
            getDocument={() =>
              boardListDocument(board.name, rows, allRows.length, membersById, labelsById)
            }
          />
        </div>
      </div>

      <div className="mt-4">
        <BoardFilters
          filters={filters}
          onChange={setFilters}
          members={board.members}
          labels={board.labels}
          meId={me.id}
          searching={filtered.isFetching}
          canEdit={false}
        />
      </div>

      {allRows.length === 0 ? (
        <EmptyState
          icon={List}
          title="Este tablero no tiene tarjetas todavía"
          description="Las tarjetas que agregues en el tablero aparecerán aquí."
          className="mt-4"
        />
      ) : (
        <div className="scrollbar-thin mt-4 overflow-x-auto rounded-xl border border-line bg-surface shadow-card">
          <table className="w-full min-w-[56rem] text-left text-sm">
            <thead className="border-b border-line bg-subtle">
              <tr>
                {headers.map((header) => (
                  <th
                    key={header.label}
                    scope="col"
                    aria-sort={
                      header.key && sort.key === header.key
                        ? sort.direction === 'asc'
                          ? 'ascending'
                          : 'descending'
                        : undefined
                    }
                    className={cn('px-3 py-2 text-[11px] font-semibold tracking-wider text-faint uppercase', header.className)}
                  >
                    {header.key ? (
                      <SortButton
                        active={sort.key === header.key}
                        direction={sort.direction}
                        onClick={() => setSort(nextSort(sort, header.key!))}
                      >
                        {header.label}
                      </SortButton>
                    ) : (
                      header.label
                    )}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-line">
              {rows.length === 0 ? (
                <tr>
                  <td colSpan={headers.length} className="px-3 py-10 text-center text-sm text-muted">
                    <SearchX className="mx-auto mb-2 size-6 text-faint" />
                    Ninguna tarjeta coincide con los filtros.
                  </td>
                </tr>
              ) : (
                rows.map(({ card, columnName }) => (
                  <tr
                    key={card.id}
                    onClick={() => navigate(`cards/${card.id}`)}
                    className="group cursor-pointer transition-colors hover:bg-subtle"
                  >
                    <td className="px-3 py-2.5">
                      <Link
                        to={`cards/${card.id}`}
                        onClick={(event) => event.stopPropagation()}
                        className="font-medium group-hover:text-primary focus-visible:underline focus-visible:outline-none"
                      >
                        {card.title}
                      </Link>
                      {(card.commentCount > 0 || card.attachmentCount > 0) && (
                        <span className="ml-2 inline-flex items-center gap-2 align-middle text-xs text-faint">
                          {card.commentCount > 0 && (
                            <span className="inline-flex items-center gap-0.5" title="Comentarios">
                              <MessageSquare className="size-3" />
                              {card.commentCount}
                            </span>
                          )}
                          {card.attachmentCount > 0 && (
                            <span className="inline-flex items-center gap-0.5" title="Adjuntos">
                              <Paperclip className="size-3" />
                              {card.attachmentCount}
                            </span>
                          )}
                        </span>
                      )}
                    </td>
                    <td className="px-3 py-2.5 whitespace-nowrap">
                      <span className="rounded-md bg-muted-surface px-1.5 py-0.5 text-xs font-medium text-muted">
                        {columnName}
                      </span>
                    </td>
                    <td className="px-3 py-2.5">
                      {card.priority === 'None' ? <Empty /> : <PriorityBadge priority={card.priority} />}
                    </td>
                    <td className="px-3 py-2.5">
                      {card.assigneeIds.length === 0 ? (
                        <Empty />
                      ) : (
                        <AvatarStack
                          size="xs"
                          max={3}
                          people={card.assigneeIds.map((id) => ({ id, name: membersById.get(id) ?? 'Sin acceso' }))}
                        />
                      )}
                    </td>
                    <td className="max-w-56 px-3 py-2.5">
                      {card.labelIds.length === 0 ? (
                        <Empty />
                      ) : (
                        <div className="flex flex-wrap gap-1">
                          {card.labelIds.map((id) => {
                            const label = labelsById.get(id)
                            return label && <LabelChip key={id} label={label} className="px-1.5 py-0 text-[11px]" />
                          })}
                        </div>
                      )}
                    </td>
                    <td className="px-3 py-2.5 whitespace-nowrap">
                      {card.dueDate ? <DueDateChip dueDate={card.dueDate} /> : <Empty />}
                    </td>
                    <td className="px-3 py-2.5">
                      {card.checklist && card.checklist.total > 0 ? (
                        <ChecklistProgress done={card.checklist.done} total={card.checklist.total} />
                      ) : (
                        <Empty />
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}

      <Outlet context={outlet} />
    </div>
  )
}

interface SortButtonProps {
  active: boolean
  direction: ListSort['direction']
  onClick: () => void
  children: ReactNode
}

function SortButton({ active, direction, onClick, children }: SortButtonProps) {
  const Icon = !active ? ArrowUpDown : direction === 'asc' ? ArrowUp : ArrowDown
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        'inline-flex cursor-pointer items-center gap-1 uppercase transition-colors hover:text-fg',
        active && 'text-fg',
      )}
    >
      {children}
      <Icon className={cn('size-3', !active && 'opacity-50')} />
    </button>
  )
}

function ChecklistProgress({ done, total }: { done: number; total: number }) {
  const complete = done === total
  return (
    <span className="flex items-center gap-2" title={`${done} de ${total} completados`}>
      <span className="h-1.5 w-16 overflow-hidden rounded-full bg-muted-surface">
        <span
          className={cn('block h-full rounded-full', complete ? 'bg-success' : 'bg-primary')}
          style={{ width: `${(done / total) * 100}%` }}
        />
      </span>
      <span className={cn('inline-flex items-center gap-1 text-xs tabular-nums', complete ? 'text-success' : 'text-muted')}>
        <ListChecks className="size-3.5" />
        {done}/{total}
      </span>
    </span>
  )
}

function Empty() {
  return <span className="text-faint">—</span>
}
