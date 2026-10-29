import { useQueries } from '@tanstack/react-query'
import { AlarmClock, BarChart3, CircleCheckBig, Layers, UserX } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { Link } from 'react-router'
import { PageContainer, PageHeader } from '@/components/layout/page-header'
import { Avatar } from '@/components/ui/avatar'
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { ExportMenu } from '@/components/ui/export-menu'
import { EmptyState, ErrorState, Skeleton } from '@/components/ui/feedback'
import { boardCover } from '@/features/boards/board-cover'
import { boardsApi } from '@/features/boards/boards-api'
import { priorityMeta } from '@/features/boards/card-meta'
import { useTeams } from '@/features/teams/team-queries'
import { queryKeys } from '@/lib/api/query-keys'
import { cn } from '@/lib/utils/cn'
import { buildReport, percent, upcomingDays, type Report, type ReportBoard } from './report-data'
import { reportDocument } from './report-export'

const allTeams = 'all'
const dayFormat = new Intl.DateTimeFormat('es', { weekday: 'short', day: 'numeric' })
const longDayFormat = new Intl.DateTimeFormat('es', { weekday: 'long', day: 'numeric', month: 'long' })

export function ReportsPage() {
  const teams = useTeams()
  const [teamId, setTeamId] = useState(allTeams)

  // Same queries (and cache) as the sidebar's board lists and the board page.
  const boardLists = useQueries({
    queries: (teams.data ?? []).map((team) => ({
      queryKey: queryKeys.teamBoards(team.id, false),
      queryFn: () => boardsApi.listForTeam(team.id, false),
    })),
  })
  const summaries = (teams.data ?? []).flatMap((team, i) =>
    (boardLists[i]?.data ?? []).map((board) => ({ boardId: board.id, teamId: team.id, teamName: team.name })),
  )
  const details = useQueries({
    queries: summaries.map(({ boardId }) => ({
      queryKey: queryKeys.board(boardId),
      queryFn: () => boardsApi.get(boardId),
    })),
  })

  const pending = teams.isPending || boardLists.some((q) => q.isPending) || details.some((q) => q.isPending)
  const error = teams.error ?? boardLists.find((q) => q.error)?.error ?? null

  const boards: ReportBoard[] = summaries.flatMap((summary, i) => {
    const board = details[i]?.data
    return board && (teamId === allTeams || summary.teamId === teamId) ? [{ board, teamName: summary.teamName }] : []
  })
  const report = buildReport(boards)

  return (
    <PageContainer className="max-w-7xl">
      <PageHeader
        title="Reportes"
        description="Métricas de avance de tus tableros y equipos."
        actions={
          <>
            {(teams.data?.length ?? 0) > 1 && (
              <select
                aria-label="Equipo"
                value={teamId}
                onChange={(event) => setTeamId(event.target.value)}
                className="h-9 cursor-pointer rounded-lg border border-line bg-surface px-3 text-sm shadow-card hover:border-line-strong focus:border-primary focus:ring-3 focus:ring-ring focus:outline-none"
              >
                <option value={allTeams}>Todos los equipos</option>
                {teams.data!.map((team) => (
                  <option key={team.id} value={team.id}>
                    {team.name}
                  </option>
                ))}
              </select>
            )}
            <ExportMenu
              disabled={pending || !!error || report.total === 0}
              getDocument={() =>
                reportDocument(report, teams.data?.find((team) => team.id === teamId)?.name ?? 'Todos los equipos')
              }
            />
          </>
        }
      />

      {error ? (
        <ErrorState error={error} onRetry={() => void teams.refetch()} className="mt-6" />
      ) : pending ? (
        <ReportSkeleton />
      ) : report.total === 0 ? (
        <EmptyState
          icon={BarChart3}
          title="Todavía no hay nada que medir"
          description="Cuando tus tableros tengan tarjetas, aquí verás su avance, lo que está vencido y la carga de cada persona."
          className="mt-6"
        />
      ) : (
        <ReportBody report={report} />
      )}
    </PageContainer>
  )
}

function ReportBody({ report }: { report: Report }) {
  return (
    <>
      <div className="mt-6 grid grid-cols-2 gap-3 sm:gap-4 lg:grid-cols-4">
        <Stat tone="primary" icon={<Layers />} label="Tarjetas abiertas" value={report.open} hint={`de ${report.total} en total`} />
        <Stat
          tone="success"
          icon={<CircleCheckBig />}
          label="Completadas"
          value={report.done}
          hint={`${percent(report.done, report.total)}% del total`}
        />
        <Stat
          tone="danger"
          icon={<AlarmClock />}
          label="Vencidas"
          value={report.overdue}
          hint={`${percent(report.overdue, report.open)}% de las abiertas`}
        />
        <Stat
          tone="warning"
          icon={<UserX />}
          label="Sin asignar"
          value={report.unassigned}
          hint={`${percent(report.unassigned, report.open)}% de las abiertas`}
        />
      </div>

      <p className="mt-3 text-xs text-faint">
        Una tarjeta cuenta como completada cuando está en la última columna de su tablero.
        {report.checklist.total > 0 &&
          ` Checklists: ${report.checklist.done} de ${report.checklist.total} elementos hechos (${percent(report.checklist.done, report.checklist.total)}%).`}
      </p>

      {/* minmax(0, 1fr) so the wide upcoming chart scrolls inside its card instead of widening the page. */}
      <div className="mt-6 grid grid-cols-[minmax(0,1fr)] gap-6 lg:grid-cols-[repeat(2,minmax(0,1fr))]">
        <Card className="overflow-hidden lg:col-span-2">
          <CardHeader>
            <div>
              <CardTitle>Avance por tablero</CardTitle>
              <CardDescription>Tarjetas completadas sobre el total de cada tablero.</CardDescription>
            </div>
          </CardHeader>
          <ul className="divide-y divide-line">
            {report.boards.map((board) => {
              const done = percent(board.done, board.total)
              return (
                <li key={board.id}>
                  <Link
                    to={`/boards/${board.id}`}
                    className="group grid grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-x-3 gap-y-1.5 px-5 py-3 transition-colors hover:bg-subtle sm:grid-cols-[auto_minmax(0,14rem)_minmax(0,1fr)_auto]"
                  >
                    <span className={cn('size-8 shrink-0 rounded-lg bg-linear-to-br', boardCover(board.id))} aria-hidden="true" />
                    <span className="min-w-0">
                      <span className="block truncate text-sm font-medium group-hover:text-primary">{board.name}</span>
                      <span className="block truncate text-xs text-muted">{board.teamName}</span>
                    </span>
                    <span
                      className="col-span-3 h-2 overflow-hidden rounded-full bg-muted-surface sm:col-span-1"
                      role="img"
                      aria-label={`${done}% completado`}
                      title={`${board.done} de ${board.total} completadas`}
                    >
                      <span className="block h-full rounded-full bg-success" style={{ width: `${done}%` }} />
                    </span>
                    <span className="col-start-3 row-start-1 flex items-center gap-2 text-right text-xs whitespace-nowrap tabular-nums sm:col-start-4">
                      {board.overdue > 0 && (
                        <span className="rounded-md bg-danger-soft px-1.5 py-0.5 font-medium text-danger">
                          {board.overdue} {board.overdue === 1 ? 'vencida' : 'vencidas'}
                        </span>
                      )}
                      <span className="text-muted">
                        {board.done}/{board.total}
                      </span>
                      <span className="w-9 font-semibold text-fg">{done}%</span>
                    </span>
                  </Link>
                </li>
              )
            })}
          </ul>
        </Card>

        <Card>
          <CardHeader>
            <div>
              <CardTitle>Abiertas por prioridad</CardTitle>
              <CardDescription>Cómo se reparte el trabajo pendiente.</CardDescription>
            </div>
          </CardHeader>
          <div className="space-y-3 px-5 py-4">
            {report.byPriority.map(({ priority, count }) => {
              const meta = priorityMeta[priority]
              return (
                <BarRow
                  key={priority}
                  label={
                    <span className="inline-flex items-center gap-1.5">
                      <meta.icon className="size-3.5 text-muted" />
                      {meta.label}
                    </span>
                  }
                  value={count}
                  max={Math.max(...report.byPriority.map((p) => p.count))}
                  title={`${meta.label}: ${count} (${percent(count, report.open)}%)`}
                />
              )
            })}
          </div>
        </Card>

        <Card>
          <CardHeader>
            <div>
              <CardTitle>Carga por persona</CardTitle>
              <CardDescription>Tarjetas abiertas asignadas a cada quien.</CardDescription>
            </div>
            <Legend />
          </CardHeader>
          {report.people.length === 0 ? (
            <p className="px-5 py-8 text-center text-sm text-muted">Nadie tiene tarjetas abiertas asignadas.</p>
          ) : (
            <div className="space-y-3 px-5 py-4">
              {report.people.slice(0, 8).map((person) => (
                <BarRow
                  key={person.userId}
                  label={
                    <span className="inline-flex min-w-0 items-center gap-2">
                      <Avatar id={person.userId} name={person.name} size="xs" />
                      <span className="truncate">{person.name}</span>
                    </span>
                  }
                  value={person.open}
                  overdue={person.overdue}
                  max={report.people[0]!.open}
                  title={`${person.name}: ${person.open} abiertas, ${person.overdue} vencidas`}
                />
              ))}
              {report.people.length > 8 && (
                <p className="text-xs text-muted">y {report.people.length - 8} personas más</p>
              )}
            </div>
          )}
        </Card>

        <Card className="lg:col-span-2">
          <CardHeader>
            <div>
              <CardTitle>Vencimientos de los próximos {upcomingDays} días</CardTitle>
              <CardDescription>Tarjetas abiertas que vencen cada día, desde hoy.</CardDescription>
            </div>
          </CardHeader>
          <UpcomingChart days={report.upcoming} />
        </Card>
      </div>
    </>
  )
}

interface BarRowProps {
  label: ReactNode
  value: number
  /** Part of the value drawn as overdue, at the start of the bar. */
  overdue?: number
  max: number
  title: string
}

function BarRow({ label, value, overdue = 0, max, title }: BarRowProps) {
  const width = max === 0 ? 0 : (value / max) * 100
  return (
    <div className="grid grid-cols-[minmax(0,9rem)_minmax(0,1fr)_2rem] items-center gap-3 text-[13px]" title={title}>
      <span className="min-w-0 truncate">{label}</span>
      <span className="flex h-2.5 overflow-hidden rounded-full bg-muted-surface">
        <span className="flex h-full gap-0.5 overflow-hidden rounded-full" style={{ width: `${width}%` }}>
          {overdue > 0 && <span className="h-full bg-danger" style={{ width: `${(overdue / value) * 100}%` }} />}
          {value > overdue && <span className="h-full flex-1 bg-primary" />}
        </span>
      </span>
      <span className="text-right font-semibold tabular-nums">{value}</span>
    </div>
  )
}

function Legend() {
  return (
    <div className="flex shrink-0 flex-col gap-1 text-xs text-muted sm:flex-row sm:gap-3">
      <span className="inline-flex items-center gap-1.5">
        <span className="size-2 rounded-full bg-primary" />
        Abiertas
      </span>
      <span className="inline-flex items-center gap-1.5">
        <span className="size-2 rounded-full bg-danger" />
        Vencidas
      </span>
    </div>
  )
}

function UpcomingChart({ days }: { days: Report['upcoming'] }) {
  const max = Math.max(1, ...days.map((d) => d.count))
  const total = days.reduce((sum, d) => sum + d.count, 0)
  if (total === 0) {
    return <p className="px-5 py-8 text-center text-sm text-muted">No hay tarjetas abiertas que venzan en estos días.</p>
  }
  return (
    <div className="scrollbar-thin overflow-x-auto px-5 pt-5 pb-4">
      <div className="flex min-w-[36rem] items-end gap-1.5" role="list" aria-label="Vencimientos por día">
        {days.map(({ day, count }, i) => {
          const weekend = day.getDay() === 0 || day.getDay() === 6
          return (
            <div
              key={day.toISOString()}
              role="listitem"
              aria-label={`${longDayFormat.format(day)}: ${count}`}
              title={`${longDayFormat.format(day)}: ${count} ${count === 1 ? 'tarjeta' : 'tarjetas'}`}
              className="group flex flex-1 flex-col items-center gap-1.5"
            >
              <span className={cn('text-xs font-semibold tabular-nums', count === 0 ? 'text-faint' : 'text-fg')}>
                {count}
              </span>
              <span className="flex h-32 w-full items-end rounded-md transition-colors group-hover:bg-subtle">
                <span
                  className="w-full rounded-t-[4px] bg-primary transition-opacity group-hover:opacity-80"
                  style={{ height: count === 0 ? 0 : `${Math.max(4, (count / max) * 100)}%` }}
                />
              </span>
              <span
                className={cn(
                  'text-[11px] whitespace-nowrap capitalize',
                  i === 0 ? 'font-semibold text-primary' : weekend ? 'text-faint' : 'text-muted',
                )}
              >
                {i === 0 ? 'Hoy' : dayFormat.format(day)}
              </span>
            </div>
          )
        })}
      </div>
    </div>
  )
}

type StatTone = 'primary' | 'success' | 'danger' | 'warning'

const toneStyles: Record<StatTone, { icon: string; bar: string }> = {
  primary: { icon: 'bg-primary-soft text-primary', bar: 'bg-primary' },
  success: { icon: 'bg-success-soft text-success', bar: 'bg-success' },
  danger: { icon: 'bg-danger-soft text-danger', bar: 'bg-danger' },
  warning: { icon: 'bg-warning-soft text-warning', bar: 'bg-warning' },
}

function Stat({ tone, icon, label, value, hint }: { tone: StatTone; icon: ReactNode; label: string; value: number; hint: string }) {
  const styles = toneStyles[tone]
  return (
    <Card className="relative flex items-center gap-3 overflow-hidden px-4 py-4 sm:gap-4 sm:px-5">
      <span className={cn('absolute inset-x-0 top-0 h-0.5 opacity-70', styles.bar)} aria-hidden="true" />
      <span className={cn('flex size-10 shrink-0 items-center justify-center rounded-xl [&_svg]:size-5', styles.icon)}>{icon}</span>
      <div className="min-w-0">
        <p className="text-2xl font-semibold tracking-tight tabular-nums">{value}</p>
        <p className="truncate text-[13px] text-muted">{label}</p>
        <p className="truncate text-xs text-faint">{hint}</p>
      </div>
    </Card>
  )
}

function ReportSkeleton() {
  return (
    <>
      <div className="mt-6 grid grid-cols-2 gap-3 sm:gap-4 lg:grid-cols-4">
        {Array.from({ length: 4 }, (_, i) => (
          <Skeleton key={i} className="h-24 rounded-xl" />
        ))}
      </div>
      <div className="mt-6 grid gap-6 lg:grid-cols-2">
        <Skeleton className="h-56 rounded-xl lg:col-span-2" />
        <Skeleton className="h-56 rounded-xl" />
        <Skeleton className="h-56 rounded-xl" />
      </div>
    </>
  )
}
