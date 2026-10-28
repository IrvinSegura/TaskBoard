import { Activity, CalendarCheck } from 'lucide-react'
import { Link } from 'react-router'
import { Skeleton } from '@/components/ui/feedback'
import { ActivityItem } from '@/features/activity/activity-item'
import type { ActivityEntry } from '@/features/activity/activity-api'
import { boardCover } from '@/features/boards/board-cover'
import { DueDateChip, PriorityBadge } from '@/features/boards/card-meta'
import { cn } from '@/lib/utils/cn'
import { formatLongDate } from '@/lib/utils/format'
import type { CalendarCard } from './calendar-api'

const weekdayFormat = new Intl.DateTimeFormat('es', { weekday: 'long' })

interface CalendarDayPanelProps {
  day: Date
  cards: CalendarCard[]
  /** Null when activity is hidden. */
  activity: ActivityEntry[] | null
  loading: boolean
}

/** Everything about the selected day: the cards due and what happened on the boards. */
export function CalendarDayPanel({ day, cards, activity, loading }: CalendarDayPanelProps) {
  const weekday = weekdayFormat.format(day)

  return (
    <aside className="flex flex-col self-start rounded-xl border border-line bg-surface shadow-card xl:sticky xl:top-4 xl:max-h-[calc(100vh-7rem)]">
      <header className="border-b border-line px-4 py-3">
        <p className="text-xs font-medium text-muted capitalize">{weekday}</p>
        <h2 className="text-[15px] font-semibold">{formatLongDate(day)}</h2>
      </header>

      <div className="scrollbar-thin min-h-0 flex-1 overflow-y-auto">
        <section className="px-4 py-3">
          <h3 className="mb-2 flex items-center gap-1.5 text-[13px] font-semibold">
            <CalendarCheck className="size-4 text-muted" />
            Vencen este día
            {!loading && <Count value={cards.length} />}
          </h3>
          {loading ? (
            <Skeleton className="h-12" />
          ) : cards.length === 0 ? (
            <p className="text-[13px] text-muted">Ninguna tarjeta vence este día.</p>
          ) : (
            <ul className="-mx-2 space-y-0.5">
              {cards.map((card) => (
                <li key={card.id}>
                  <Link
                    to={`/boards/${card.boardId}/cards/${card.id}`}
                    className="group flex items-start gap-2.5 rounded-lg px-2 py-2 transition-colors hover:bg-subtle"
                  >
                    <span className={cn('mt-0.5 h-8 w-1 shrink-0 rounded-full bg-linear-to-b', boardCover(card.boardId))} />
                    <span className="min-w-0 flex-1">
                      <span className="block truncate text-sm font-medium group-hover:text-primary">{card.title}</span>
                      <span className="block truncate text-xs text-muted">
                        {card.boardName} · {card.columnName}
                      </span>
                      <span className="mt-1 flex flex-wrap items-center gap-1">
                        <DueDateChip dueDate={card.dueDate} />
                        <PriorityBadge priority={card.priority} />
                        {card.isAssignedToMe && (
                          <span className="rounded-md bg-primary-soft px-1.5 py-0.5 text-xs font-medium text-primary">Tuya</span>
                        )}
                      </span>
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </section>

        {activity && (
          <section className="border-t border-line px-4 py-3">
            <h3 className="flex items-center gap-1.5 text-[13px] font-semibold">
              <Activity className="size-4 text-muted" />
              Actividad
              {!loading && <Count value={activity.length} />}
            </h3>
            {loading ? (
              <Skeleton className="mt-2 h-12" />
            ) : activity.length === 0 ? (
              <p className="mt-2 text-[13px] text-muted">No hubo movimientos este día.</p>
            ) : (
              <ol className="divide-y divide-line">
                {activity.map((entry) => (
                  <ActivityItem key={entry.id} entry={entry} showBoard />
                ))}
              </ol>
            )}
          </section>
        )}
      </div>
    </aside>
  )
}

function Count({ value }: { value: number }) {
  return <span className="rounded-full bg-muted-surface px-1.5 text-xs font-medium text-muted tabular-nums">{value}</span>
}
