import { CalendarCheck, CalendarOff, ChevronLeft, ChevronRight } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link, Outlet } from 'react-router'
import { AvatarStack } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { Segmented } from '@/components/ui/segmented'
import { useCurrentUser } from '@/features/auth/auth-provider'
import {
  addMonths,
  dayKey,
  groupByDay,
  monthGrid,
  monthLabel,
  sameMonth,
  startOfMonth,
  weekdayLabels,
} from '@/features/calendar/calendar-grid'
import { cn } from '@/lib/utils/cn'
import { dueStatus, formatLongDate } from '@/lib/utils/format'
import { boardRows, type CardRow } from './board-list-rows'
import { useBoardOutlet } from './board-page'
import type { BoardMember, CardPriority } from './boards-api'
import { DueDateChip, PriorityBadge } from './card-meta'

type CardFilter = 'all' | 'mine'

// Cells list a few cards; the day panel has the rest.
const cardsPerCell = 3

const priorityDot: Record<CardPriority, string> = {
  None: 'bg-faint',
  Low: 'bg-sky-500',
  Medium: 'bg-amber-500',
  High: 'bg-orange-500',
  Urgent: 'bg-red-500',
}

const weekdayFormat = new Intl.DateTimeFormat('es', { weekday: 'long' })

/** The board's cards on a month grid by due date; a click on a card opens it over the calendar. */
export function BoardCalendarView() {
  const outlet = useBoardOutlet()
  const { board } = outlet
  const me = useCurrentUser()
  const today = new Date()
  const [month, setMonth] = useState(() => startOfMonth(today))
  const [selected, setSelected] = useState(() => dayKey(today))
  const [filter, setFilter] = useState<CardFilter>('all')

  const grid = useMemo(() => monthGrid(month), [month])
  const rows = useMemo(
    () => boardRows(board).filter((row) => filter === 'all' || row.card.assigneeIds.includes(me.id)),
    [board, filter, me.id],
  )
  const { rowsByDay, undated } = useMemo(
    () => ({
      rowsByDay: groupByDay(
        rows.filter((row) => row.card.dueDate),
        (row) => row.card.dueDate!,
      ),
      undated: rows.filter((row) => !row.card.dueDate),
    }),
    [rows],
  )
  const inMonth = rows.filter((row) => row.card.dueDate && sameMonth(new Date(row.card.dueDate), month)).length

  const goTo = (target: Date) => {
    setMonth(startOfMonth(target))
    setSelected(dayKey(sameMonth(target, today) ? today : startOfMonth(target)))
  }

  const todayKey = dayKey(today)
  const selectedDay = new Date(`${selected}T00:00:00`)
  const selectedRows = rowsByDay.get(selected) ?? []

  return (
    <div className="px-4 py-6 sm:px-6">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h2 className="text-[15px] font-semibold">Calendario</h2>
          <p className="mt-1 text-sm text-muted">Las tarjetas del tablero según su fecha límite.</p>
        </div>
        <Segmented
          value={filter}
          onChange={setFilter}
          options={[
            { value: 'all', label: 'Todas' },
            { value: 'mine', label: 'Asignadas a mí' },
          ]}
          label="Tarjetas"
        />
      </div>

      <div className="mt-5 flex flex-wrap items-center gap-2">
        <div className="mr-auto flex flex-col sm:flex-row sm:items-baseline sm:gap-2">
          <h3 className="text-lg font-semibold tracking-tight">{monthLabel(month)}</h3>
          <span className="text-xs text-muted tabular-nums">
            {inMonth} {inMonth === 1 ? 'vencimiento' : 'vencimientos'}
          </span>
        </div>
        <Button variant="secondary" size="sm" onClick={() => goTo(today)}>
          Hoy
        </Button>
        <Button variant="ghost" size="icon" onClick={() => goTo(addMonths(month, -1))} aria-label="Mes anterior">
          <ChevronLeft />
        </Button>
        <Button variant="ghost" size="icon" onClick={() => goTo(addMonths(month, 1))} aria-label="Mes siguiente">
          <ChevronRight />
        </Button>
      </div>

      <div className="mt-4 grid gap-6 xl:grid-cols-[minmax(0,1fr)_320px]">
        <div className="overflow-hidden rounded-xl border border-line bg-surface shadow-card">
          <div className="grid grid-cols-7 border-b border-line bg-subtle">
            {weekdayLabels.map((label) => (
              <div key={label} className="px-2 py-2 text-center text-[11px] font-semibold tracking-wider text-faint uppercase">
                {label}
              </div>
            ))}
          </div>
          <div className="grid grid-cols-7">
            {grid.days.map((day, index) => {
              const key = dayKey(day)
              return (
                <DayCell
                  key={key}
                  day={day}
                  inMonth={sameMonth(day, month)}
                  isToday={key === todayKey}
                  isSelected={key === selected}
                  rows={rowsByDay.get(key) ?? []}
                  lastColumn={index % 7 === 6}
                  lastRow={index >= 35}
                  onSelect={() => setSelected(key)}
                />
              )
            })}
          </div>
        </div>

        <aside className="flex flex-col self-start rounded-xl border border-line bg-surface shadow-card xl:sticky xl:top-4">
          <header className="border-b border-line px-4 py-3">
            <p className="text-xs font-medium text-muted capitalize">{weekdayFormat.format(selectedDay)}</p>
            <h3 className="text-[15px] font-semibold">{formatLongDate(selectedDay)}</h3>
          </header>
          <section className="px-4 py-3">
            <h4 className="mb-2 flex items-center gap-1.5 text-[13px] font-semibold">
              <CalendarCheck className="size-4 text-muted" />
              Vencen este día
              <Count value={selectedRows.length} />
            </h4>
            {selectedRows.length === 0 ? (
              <p className="text-[13px] text-muted">Ninguna tarjeta vence este día.</p>
            ) : (
              <CardLinks rows={selectedRows} members={board.members} />
            )}
          </section>
          {undated.length > 0 && (
            <section className="border-t border-line px-4 py-3">
              <h4 className="mb-2 flex items-center gap-1.5 text-[13px] font-semibold">
                <CalendarOff className="size-4 text-muted" />
                Sin fecha límite
                <Count value={undated.length} />
              </h4>
              <div className="scrollbar-thin -mx-2 max-h-72 overflow-y-auto px-2">
                <CardLinks rows={undated} members={board.members} />
              </div>
            </section>
          )}
        </aside>
      </div>

      <Outlet context={outlet} />
    </div>
  )
}

interface DayCellProps {
  day: Date
  inMonth: boolean
  isToday: boolean
  isSelected: boolean
  rows: CardRow[]
  lastColumn: boolean
  lastRow: boolean
  onSelect: () => void
}

function DayCell({ day, inMonth, isToday, isSelected, rows, lastColumn, lastRow, onSelect }: DayCellProps) {
  const overdue = rows.some((row) => dueStatus(row.card.dueDate!) === 'overdue')
  const label = day.toLocaleDateString('es', { weekday: 'long', day: 'numeric', month: 'long' })

  return (
    // A click anywhere in the cell selects its day (the day number is the keyboard target); card links also open the card.
    <div
      onClick={onSelect}
      className={cn(
        'relative flex min-h-16 cursor-pointer flex-col gap-1 border-line p-1 transition-colors sm:min-h-28 sm:p-1.5',
        !lastColumn && 'border-r',
        !lastRow && 'border-b',
        inMonth ? 'bg-surface hover:bg-subtle' : 'bg-subtle/60 text-faint hover:bg-subtle',
        isSelected && 'z-10 ring-2 ring-primary ring-inset',
      )}
    >
      <button
        type="button"
        aria-pressed={isSelected}
        aria-label={`${label}${rows.length ? `: ${rows.length} ${rows.length === 1 ? 'vencimiento' : 'vencimientos'}` : ''}`}
        className={cn(
          'flex size-6 cursor-pointer items-center justify-center rounded-full text-xs font-medium tabular-nums focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none',
          isToday ? 'bg-primary text-primary-fg' : inMonth ? 'text-fg' : 'text-faint',
        )}
      >
        {day.getDate()}
      </button>

      {/* Phones get a dot; wider screens get the card titles. */}
      {rows.length > 0 && (
        <span className={cn('ml-1 size-1.5 rounded-full sm:hidden', overdue ? 'bg-danger' : 'bg-primary')} aria-hidden="true" />
      )}
      <ul className="hidden space-y-0.5 sm:block">
        {rows.slice(0, cardsPerCell).map(({ card }) => (
          <li key={card.id}>
            <Link
              to={`cards/${card.id}`}
              title={card.title}
              className={cn(
                'flex items-center gap-1 truncate rounded px-1 py-0.5 text-[11px] leading-tight transition-colors',
                dueStatus(card.dueDate!) === 'overdue'
                  ? 'bg-danger-soft text-danger hover:brightness-95'
                  : 'bg-muted-surface text-fg hover:bg-primary-soft hover:text-primary',
              )}
            >
              <span className={cn('size-1.5 shrink-0 rounded-full', priorityDot[card.priority])} />
              <span className="truncate">{card.title}</span>
            </Link>
          </li>
        ))}
        {rows.length > cardsPerCell && (
          <li className="px-1 text-[11px] font-medium text-muted">+{rows.length - cardsPerCell} más</li>
        )}
      </ul>
    </div>
  )
}

function CardLinks({ rows, members }: { rows: CardRow[]; members: BoardMember[] }) {
  return (
    <ul className="-mx-2 space-y-0.5">
      {rows.map(({ card, columnName }) => (
        <li key={card.id}>
          <Link
            to={`cards/${card.id}`}
            className="group flex items-start gap-2.5 rounded-lg px-2 py-2 transition-colors hover:bg-subtle"
          >
            <span className={cn('mt-1.5 size-2 shrink-0 rounded-full', priorityDot[card.priority])} />
            <span className="min-w-0 flex-1">
              <span className="block truncate text-sm font-medium group-hover:text-primary">{card.title}</span>
              <span className="block truncate text-xs text-muted">{columnName}</span>
              {(card.dueDate || card.priority !== 'None') && (
                <span className="mt-1 flex flex-wrap items-center gap-1">
                  {card.dueDate && <DueDateChip dueDate={card.dueDate} />}
                  <PriorityBadge priority={card.priority} />
                </span>
              )}
            </span>
            {card.assigneeIds.length > 0 && (
              <AvatarStack
                size="xs"
                max={2}
                people={card.assigneeIds.map((id) => ({
                  id,
                  name: members.find((m) => m.userId === id)?.displayName ?? 'Sin acceso',
                }))}
              />
            )}
          </Link>
        </li>
      ))}
    </ul>
  )
}

function Count({ value }: { value: number }) {
  return <span className="rounded-full bg-muted-surface px-1.5 text-xs font-medium text-muted tabular-nums">{value}</span>
}
