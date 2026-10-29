import { Activity, CalendarClock, ChevronLeft, ChevronRight, TriangleAlert } from 'lucide-react'
import { useMemo, useState } from 'react'
import { PageContainer, PageHeader } from '@/components/layout/page-header'
import { Button } from '@/components/ui/button'
import { ErrorState, Skeleton } from '@/components/ui/feedback'
import { Segmented } from '@/components/ui/segmented'
import { boardCover } from '@/features/boards/board-cover'
import { cn } from '@/lib/utils/cn'
import { dueStatus } from '@/lib/utils/format'
import { useCalendar, type CalendarCard } from './calendar-api'
import { CalendarDayPanel } from './calendar-day-panel'
import { addMonths, dayKey, groupByDay, monthGrid, monthLabel, sameMonth, startOfMonth, weekdayLabels } from './calendar-grid'

type CardFilter = 'all' | 'mine'

// Cells list a few cards; the day panel has the rest.
const cardsPerCell = 3

export function CalendarPage() {
  const today = new Date()
  const [month, setMonth] = useState(() => startOfMonth(today))
  const [selected, setSelected] = useState(() => dayKey(today))
  const [filter, setFilter] = useState<CardFilter>('all')
  const [showActivity, setShowActivity] = useState(true)

  const grid = useMemo(() => monthGrid(month), [month])
  const calendar = useCalendar(grid.from, grid.to)

  const cards = useMemo(
    () => (calendar.data?.cards ?? []).filter((card) => filter === 'all' || card.isAssignedToMe),
    [calendar.data, filter],
  )
  const cardsByDay = useMemo(() => groupByDay(cards, (card) => card.dueDate), [cards])
  const activityByDay = useMemo(
    () => groupByDay(calendar.data?.activity ?? [], (entry) => entry.occurredAt),
    [calendar.data],
  )

  const goTo = (target: Date) => {
    setMonth(startOfMonth(target))
    // Keep the selection inside the month on screen: today when it is there, else the first day.
    setSelected(dayKey(sameMonth(target, today) ? today : startOfMonth(target)))
  }

  const todayKey = dayKey(today)

  return (
    <PageContainer className="max-w-7xl">
      <PageHeader
        title="Calendario"
        description="Las fechas límite de las tarjetas y toda la actividad de tus tableros, día por día."
        actions={
          <>
            <Segmented
              value={filter}
              onChange={setFilter}
              options={[
                { value: 'all', label: 'Todas' },
                { value: 'mine', label: 'Asignadas a mí' },
              ]}
              label="Tarjetas"
            />
            <Button
              variant={showActivity ? 'subtle' : 'ghost'}
              size="sm"
              aria-pressed={showActivity}
              onClick={() => setShowActivity(!showActivity)}
            >
              <Activity />
              Actividad
            </Button>
          </>
        }
      />

      <div className="mt-6 flex flex-wrap items-center gap-2">
        <h2 className="mr-auto text-lg font-semibold tracking-tight">{monthLabel(month)}</h2>
        {calendar.isFetching && !calendar.isPending && <span className="text-xs text-faint">Actualizando…</span>}
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

      <Legend showActivity={showActivity} />

      {calendar.data?.activityTruncated && showActivity && (
        <p className="mt-3 flex items-center gap-2 rounded-lg bg-warning-soft px-3 py-2 text-[13px] text-warning">
          <TriangleAlert className="size-4 shrink-0" />
          Este mes tiene mucha actividad: se muestra la más reciente.
        </p>
      )}

      {calendar.isError ? (
        <ErrorState error={calendar.error} onRetry={() => calendar.refetch()} className="mt-4" />
      ) : (
        <div className="mt-4 grid gap-6 xl:grid-cols-[minmax(0,1fr)_340px]">
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
                    cards={cardsByDay.get(key) ?? []}
                    activityCount={showActivity ? (activityByDay.get(key)?.length ?? 0) : 0}
                    loading={calendar.isPending}
                    lastColumn={index % 7 === 6}
                    lastRow={index >= 35}
                    onSelect={() => setSelected(key)}
                  />
                )
              })}
            </div>
          </div>

          <CalendarDayPanel
            day={new Date(`${selected}T00:00:00`)}
            cards={cardsByDay.get(selected) ?? []}
            activity={showActivity ? (activityByDay.get(selected) ?? []) : null}
            loading={calendar.isPending}
          />
        </div>
      )}
    </PageContainer>
  )
}

interface DayCellProps {
  day: Date
  inMonth: boolean
  isToday: boolean
  isSelected: boolean
  cards: CalendarCard[]
  activityCount: number
  loading: boolean
  lastColumn: boolean
  lastRow: boolean
  onSelect: () => void
}

function DayCell({ day, inMonth, isToday, isSelected, cards, activityCount, loading, lastColumn, lastRow, onSelect }: DayCellProps) {
  const overdue = cards.some((card) => dueStatus(card.dueDate) === 'overdue')
  const parts = [
    cards.length > 0 && `${cards.length} ${cards.length === 1 ? 'vencimiento' : 'vencimientos'}`,
    activityCount > 0 && `${activityCount} ${activityCount === 1 ? 'movimiento' : 'movimientos'}`,
  ].filter(Boolean)

  return (
    <button
      type="button"
      onClick={onSelect}
      aria-pressed={isSelected}
      aria-label={`${day.toLocaleDateString('es', { weekday: 'long', day: 'numeric', month: 'long' })}${parts.length ? `: ${parts.join(', ')}` : ''}`}
      className={cn(
        'relative flex min-h-16 cursor-pointer flex-col items-stretch gap-1 border-line p-1 text-left transition-colors sm:min-h-28 sm:p-1.5',
        !lastColumn && 'border-r',
        !lastRow && 'border-b',
        inMonth ? 'bg-surface hover:bg-subtle' : 'bg-subtle/60 text-faint hover:bg-subtle',
        isSelected && 'z-10 ring-2 ring-primary ring-inset',
      )}
    >
      <div className="flex items-center justify-between gap-1">
        <span
          className={cn(
            'flex size-6 items-center justify-center rounded-full text-xs font-medium tabular-nums',
            isToday ? 'bg-primary text-primary-fg' : inMonth ? 'text-fg' : 'text-faint',
          )}
        >
          {day.getDate()}
        </span>
        {activityCount > 0 && (
          <span
            className="hidden items-center gap-0.5 rounded-full bg-info-soft px-1.5 text-[10px] leading-4 font-semibold text-info tabular-nums sm:inline-flex"
            aria-hidden="true"
          >
            <Activity className="size-2.5" />
            {activityCount}
          </span>
        )}
      </div>

      {loading ? (
        inMonth && <Skeleton className="hidden h-4 sm:block" />
      ) : (
        <>
          {/* Phones get dots; wider screens get the card titles. */}
          <div className="flex flex-wrap gap-0.5 px-0.5 sm:hidden" aria-hidden="true">
            {cards.length > 0 && <span className={cn('size-1.5 rounded-full', overdue ? 'bg-danger' : 'bg-primary')} />}
            {activityCount > 0 && <span className="size-1.5 rounded-full bg-info" />}
          </div>
          <ul className="hidden space-y-0.5 sm:block" aria-hidden="true">
            {cards.slice(0, cardsPerCell).map((card) => (
              <li
                key={card.id}
                className={cn(
                  'flex items-center gap-1 truncate rounded px-1 py-0.5 text-[11px] leading-tight',
                  dueStatus(card.dueDate) === 'overdue' ? 'bg-danger-soft text-danger' : 'bg-muted-surface text-fg',
                )}
                title={`${card.title} · ${card.boardName}`}
              >
                <span className={cn('size-1.5 shrink-0 rounded-full bg-linear-to-br', boardCover(card.boardId))} />
                <span className={cn('truncate', card.isAssignedToMe && 'font-semibold')}>{card.title}</span>
              </li>
            ))}
            {cards.length > cardsPerCell && (
              <li className="px-1 text-[11px] font-medium text-muted">+{cards.length - cardsPerCell} más</li>
            )}
          </ul>
        </>
      )}
    </button>
  )
}

function Legend({ showActivity }: { showActivity: boolean }) {
  return (
    <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-muted">
      <span className="inline-flex items-center gap-1.5">
        <CalendarClock className="size-3.5" />
        Fecha límite (en negrita, las tuyas)
      </span>
      <span className="inline-flex items-center gap-1.5">
        <span className="size-2 rounded-full bg-danger" />
        Vencida
      </span>
      {showActivity && (
        <span className="inline-flex items-center gap-1.5">
          <span className="size-2 rounded-full bg-info" />
          Actividad del día
        </span>
      )}
    </div>
  )
}
