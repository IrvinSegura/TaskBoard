import { useQueries } from '@tanstack/react-query'
import { ArrowRight, Bell, LayoutGrid, Plus, Users } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import { Link } from 'react-router'
import { PageContainer } from '@/components/layout/page-header'
import { Button, buttonStyles } from '@/components/ui/button'
import { Card, CardHeader, CardTitle } from '@/components/ui/card'
import { EmptyState, ErrorState, Skeleton } from '@/components/ui/feedback'
import { useCurrentUser } from '@/features/auth/auth-provider'
import { BoardTile } from '@/features/boards/board-tile'
import { boardsApi } from '@/features/boards/boards-api'
import { NotificationItem } from '@/features/notifications/notification-item'
import { useNotifications, useOpenNotification } from '@/features/notifications/notification-queries'
import { CreateTeamDialog } from '@/features/teams/create-team-dialog'
import { useTeams } from '@/features/teams/team-queries'
import { queryKeys } from '@/lib/api/query-keys'

function greeting(now = new Date()): string {
  const hour = now.getHours()
  if (hour < 12) {
    return 'Buenos días'
  }
  return hour < 20 ? 'Buenas tardes' : 'Buenas noches'
}

export function DashboardPage() {
  const user = useCurrentUser()
  const teams = useTeams()
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
  const firstName = user.displayName.split(' ')[0]

  return (
    <PageContainer>
      <div>
        <p className="text-sm text-muted">
          {new Intl.DateTimeFormat('es', { weekday: 'long', day: 'numeric', month: 'long' }).format(new Date())}
        </p>
        <h1 className="mt-1 text-2xl font-semibold tracking-tight">
          {greeting()}, {firstName}
        </h1>
      </div>

      <div className="mt-8 grid gap-4 sm:grid-cols-3">
        <Stat icon={<Users />} label="Equipos" value={teams.data?.length} to="/teams" />
        <Stat icon={<LayoutGrid />} label="Tableros activos" value={boardsPending ? undefined : boards.length} />
        <Stat icon={<Bell />} label="Sin leer" value={notifications.data ? unread.length : undefined} to="/notifications" />
      </div>

      <div className="mt-10 grid gap-8 lg:grid-cols-[minmax(0,1fr)_22rem]">
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
                <Skeleton key={i} className="h-36 rounded-xl" />
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

        <Card className="h-fit overflow-hidden">
          <CardHeader className="items-center py-3">
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
              {notifications.data!.slice(0, 5).map((notification) => (
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

interface StatProps {
  icon: ReactNode
  label: string
  value: number | undefined
  to?: string
}

function Stat({ icon, label, value, to }: StatProps) {
  const content = (
    <Card className="flex items-center gap-4 px-5 py-4 transition-colors hover:border-line-strong">
      <span className="flex size-10 items-center justify-center rounded-xl bg-primary-soft text-primary [&_svg]:size-5">
        {icon}
      </span>
      <div>
        {value === undefined ? (
          <Skeleton className="h-7 w-10" />
        ) : (
          <p className="text-2xl font-semibold tracking-tight tabular-nums">{value}</p>
        )}
        <p className="text-[13px] text-muted">{label}</p>
      </div>
    </Card>
  )
  return to ? <Link to={to}>{content}</Link> : content
}
