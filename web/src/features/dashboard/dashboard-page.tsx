import { useQueries } from '@tanstack/react-query'
import { AlarmClock, ArrowRight, Bell, CalendarClock, CircleCheckBig, LayoutGrid, ListChecks, Plus, Users } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { Link } from 'react-router'
import { PageContainer } from '@/components/layout/page-header'
import { Button, buttonStyles } from '@/components/ui/button'
import { Card, CardHeader, CardTitle } from '@/components/ui/card'
import { EmptyState, ErrorState, Skeleton } from '@/components/ui/feedback'
import { useCurrentUser } from '@/features/auth/auth-provider'
import { BoardTile } from '@/features/boards/board-tile'
import { boardsApi } from '@/features/boards/boards-api'
import { MyTaskRow } from '@/features/my-tasks/my-task-row'
import { summarize, useMyCards } from '@/features/my-tasks/my-tasks-api'
import { NotificationItem } from '@/features/notifications/notification-item'
import { useNotifications, useOpenNotification } from '@/features/notifications/notification-queries'
import { CreateTeamDialog } from '@/features/teams/create-team-dialog'
import { useTeams } from '@/features/teams/team-queries'
import { queryKeys } from '@/lib/api/query-keys'
import { cn } from '@/lib/utils/cn'

function greeting(now = new Date()): string {
  const hour = now.getHours()
  if (hour < 12) {
    return 'Buenos días'
  }
  return hour < 20 ? 'Buenas tardes' : 'Buenas noches'
}

function plural(count: number, one: string, many: string): string {
  return `${count} ${count === 1 ? one : many}`
}

export function DashboardPage() {
  const user = useCurrentUser()
  const teams = useTeams()
  const myCards = useMyCards()
  const notifications = useNotifications()
  const openNotification = useOpenNotification()
  const [creatingTeam, setCreatingTeam] = useState(false)

  // Same queries (and cache) as the sidebar's team board lists.
  const boardLists = useQueries({
    queries: (teams.data ?? []).map((team) => ({
      queryKey: queryKeys.teamBoards(team.id, false),
      queryFn: () => boardsApi.listForTeam(team.id, false),
    })),
  })
  const boards = (teams.data ?? []).flatMap((team, i) =>
    (boardLists[i]?.data ?? []).map((board) => ({ board, teamName: team.name })),
  )
  const boardsPending = teams.isPending || boardLists.some((list) => list.isPending)
  const unread = notifications.data?.filter((n) => !n.isRead) ?? []
  const summary = myCards.data ? summarize(myCards.data) : undefined
  const firstName = user.displayName.split(' ')[0]

  return (
    <PageContainer className="max-w-7xl">
      <section className="relative overflow-hidden rounded-2xl bg-brand px-6 py-7 text-white shadow-raised sm:px-8 sm:py-8">
        <div className="bg-dots absolute inset-0 opacity-60" aria-hidden="true" />
        <div className="relative flex flex-col gap-6 md:flex-row md:items-end md:justify-between">
          <div>
            <p className="text-xs font-medium tracking-wider text-white/70 uppercase">
              {new Intl.DateTimeFormat('es', { weekday: 'long', day: 'numeric', month: 'long' }).format(new Date())}
            </p>
            <h1 className="mt-2 text-2xl font-semibold tracking-tight sm:text-3xl">
              {greeting()}, {firstName}
            </h1>
            <p className="mt-2 max-w-xl text-sm text-white/80">
              {!summary
                ? 'Preparando tu resumen…'
                : summary.total === 0
                  ? 'No tienes tareas asignadas. Buen momento para planear lo que sigue.'
                  : `Tienes ${plural(summary.total, 'tarea asignada', 'tareas asignadas')}` +
                    (summary.overdue > 0 ? `, ${plural(summary.overdue, 'vencida', 'vencidas')}` : '') +
                    (summary.thisWeek > 0 ? ` y ${summary.thisWeek} para esta semana.` : '.')}
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <Link
              to="/my-tasks"
              className="inline-flex h-9 items-center gap-2 rounded-lg bg-white px-3.5 text-sm font-medium text-indigo-700 shadow-card transition-colors hover:bg-white/90"
            >
              <ListChecks className="size-4" />
              Ver mis tareas
            </Link>
            <button
              type="button"
              onClick={() => setCreatingTeam(true)}
              className="inline-flex h-9 cursor-pointer items-center gap-2 rounded-lg bg-white/15 px-3.5 text-sm font-medium text-white ring-1 ring-white/25 backdrop-blur transition-colors ring-inset hover:bg-white/25"
            >
              <Plus className="size-4" />
              Nuevo equipo
            </button>
          </div>
        </div>
      </section>

      <div className="mt-6 grid grid-cols-2 gap-3 sm:gap-4 lg:grid-cols-4">
        <Stat
          tone="primary"
          icon={<ListChecks />}
          label="Asignadas a ti"
          value={summary?.total}
          to="/my-tasks"
        />
        <Stat tone="danger" icon={<AlarmClock />} label="Vencidas" value={summary?.overdue} to="/my-tasks" />
        <Stat tone="warning" icon={<CalendarClock />} label="Para esta semana" value={summary?.thisWeek} to="/my-tasks" />
        <Stat
          tone="info"
          icon={<Bell />}
          label="Notificaciones sin leer"
          value={notifications.data ? unread.length : undefined}
          to="/notifications"
        />
      </div>

      <div className="mt-8 grid gap-6 lg:grid-cols-[minmax(0,1fr)_22rem]">
        <div className="min-w-0 space-y-8">
          <Card className="overflow-hidden">
            <CardHeader className="items-center py-3.5">
              <CardTitle>Próximas tareas</CardTitle>
              <Link to="/my-tasks" className="inline-flex items-center gap-1 text-[13px] font-medium text-primary hover:underline">
                Ver todas
                <ArrowRight className="size-3.5" />
              </Link>
            </CardHeader>
            {myCards.isPending ? (
              <div className="space-y-2 p-4">
                <Skeleton className="h-12" />
                <Skeleton className="h-12" />
                <Skeleton className="h-12" />
              </div>
            ) : myCards.isError ? (
              <ErrorState error={myCards.error} onRetry={() => myCards.refetch()} className="m-4 border-0" />
            ) : myCards.data.length === 0 ? (
              <div className="flex flex-col items-center gap-2 px-6 py-10 text-center">
                <CircleCheckBig className="size-8 text-success" />
                <p className="text-sm font-medium">Todo al día</p>
                <p className="text-sm text-muted">No tienes tarjetas asignadas.</p>
              </div>
            ) : (
              <div className="divide-y divide-line">
                {myCards.data.slice(0, 6).map((card) => (
                  <MyTaskRow key={card.id} card={card} compact />
                ))}
              </div>
            )}
          </Card>

          <section>
            <div className="mb-4 flex items-center justify-between">
              <h2 className="text-[15px] font-semibold">Tus tableros</h2>
              {(teams.data?.length ?? 0) > 0 && (
                <Link to="/teams" className="inline-flex items-center gap-1 text-[13px] font-medium text-primary hover:underline">
                  Ver equipos
                  <ArrowRight className="size-3.5" />
                </Link>
              )}
            </div>
            {teams.isError ? (
              <ErrorState error={teams.error} onRetry={() => teams.refetch()} />
            ) : boardsPending ? (
              <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
                {Array.from({ length: 3 }, (_, i) => (
                  <Skeleton key={i} className="h-40 rounded-xl" />
                ))}
              </div>
            ) : teams.data?.length === 0 ? (
              <EmptyState
                icon={Users}
                title="Empieza creando un equipo"
                description="Los tableros viven dentro de un equipo. Crea el tuyo o pide que te inviten a uno existente."
                action={
                  <Button onClick={() => setCreatingTeam(true)}>
                    <Plus />
                    Crear equipo
                  </Button>
                }
              />
            ) : boards.length === 0 ? (
              <EmptyState
                icon={LayoutGrid}
                title="Todavía no hay tableros"
                description="Entra a uno de tus equipos y crea el primer tablero."
                action={
                  <Link to={`/teams/${teams.data![0]!.id}`} className={buttonStyles()}>
                    Ir a {teams.data![0]!.name}
                  </Link>
                }
              />
            ) : (
              <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
                {boards.map(({ board, teamName }) => (
                  <BoardTile key={board.id} board={board} subtitle={teamName} />
                ))}
              </div>
            )}
          </section>
        </div>

        <Card className="h-fit overflow-hidden lg:sticky lg:top-6">
          <CardHeader className="items-center py-3.5">
            <CardTitle>Actividad reciente</CardTitle>
            <Link to="/notifications" className="text-[13px] font-medium text-primary hover:underline">
              Ver todo
            </Link>
          </CardHeader>
          {notifications.isPending ? (
            <div className="space-y-2 p-4">
              <Skeleton className="h-12" />
              <Skeleton className="h-12" />
            </div>
          ) : (notifications.data?.length ?? 0) === 0 ? (
            <p className="px-5 py-8 text-center text-sm text-muted">Sin novedades por ahora.</p>
          ) : (
            <ul className="divide-y divide-line">
              {notifications.data!.slice(0, 6).map((notification) => (
                <li key={notification.id}>
                  <NotificationItem notification={notification} onOpen={(n) => void openNotification(n)} />
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>

      <CreateTeamDialog open={creatingTeam} onOpenChange={setCreatingTeam} />
    </PageContainer>
  )
}

type StatTone = 'primary' | 'danger' | 'warning' | 'info'

const toneStyles: Record<StatTone, { icon: string; bar: string }> = {
  primary: { icon: 'bg-primary-soft text-primary', bar: 'bg-primary' },
  danger: { icon: 'bg-danger-soft text-danger', bar: 'bg-danger' },
  warning: { icon: 'bg-warning-soft text-warning', bar: 'bg-warning' },
  info: { icon: 'bg-info-soft text-info', bar: 'bg-info' },
}

interface StatProps {
  tone: StatTone
  icon: ReactNode
  label: string
  value: number | undefined
  to?: string
}

function Stat({ tone, icon, label, value, to }: StatProps) {
  const styles = toneStyles[tone]
  const content = (
    <Card className="group relative flex h-full items-center gap-3 overflow-hidden px-4 py-4 transition-all hover:-translate-y-0.5 hover:shadow-raised sm:gap-4 sm:px-5">
      <span className={cn('absolute inset-x-0 top-0 h-0.5 opacity-70', styles.bar)} aria-hidden="true" />
      <span className={cn('flex size-10 shrink-0 items-center justify-center rounded-xl [&_svg]:size-5', styles.icon)}>{icon}</span>
      <div className="min-w-0">
        {value === undefined ? (
          <Skeleton className="h-7 w-10" />
        ) : (
          <p className="text-2xl font-semibold tracking-tight tabular-nums">{value}</p>
        )}
        <p className="truncate text-[13px] text-muted">{label}</p>
      </div>
    </Card>
  )
  return to ? <Link to={to}>{content}</Link> : content
}
