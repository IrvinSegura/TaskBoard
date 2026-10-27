import { Activity, Archive, ChevronRight, LayoutGrid, Settings, Users } from 'lucide-react'
import { Link, NavLink, Outlet, useOutletContext, useParams } from 'react-router'
import { PageContainer, PageHeader } from '@/components/layout/page-header'
import { EmptyState, ErrorState, Skeleton } from '@/components/ui/feedback'
import { cn } from '@/lib/utils/cn'
import { TeamAvatar } from './team-avatar'
import { TeamRoleBadge } from './team-role-badge'
import { useTeam } from './team-queries'
import type { TeamSummary } from './teams-api'

const tabs = [
  { to: '', label: 'Tableros', icon: LayoutGrid, end: true },
  { to: 'archived', label: 'Archivados', icon: Archive, end: false },
  { to: 'members', label: 'Miembros', icon: Users, end: false },
  { to: 'activity', label: 'Actividad', icon: Activity, end: false },
  { to: 'settings', label: 'Configuración', icon: Settings, end: false },
]

/** Header and tabs of a team; the tab content comes from the child routes. */
export function TeamLayout() {
  const { teamId } = useParams<{ teamId: string }>()
  const { team, isPending, isError, error, refetch } = useTeam(teamId)

  if (isPending) {
    return (
      <PageContainer>
        <Skeleton className="h-4 w-32" />
        <Skeleton className="mt-4 h-9 w-72" />
        <Skeleton className="mt-10 h-48 rounded-xl" />
      </PageContainer>
    )
  }
  if (isError) {
    return (
      <PageContainer>
        <ErrorState error={error} onRetry={() => refetch()} />
      </PageContainer>
    )
  }
  if (!team) {
    return (
      <PageContainer>
        <EmptyState
          icon={Users}
          title="Equipo no encontrado"
          description="Puede que lo hayan eliminado o que ya no seas miembro."
          action={
            <Link to="/teams" className="text-sm font-medium text-primary hover:underline">
              Ver mis equipos
            </Link>
          }
        />
      </PageContainer>
    )
  }

  return (
    <PageContainer>
      <PageHeader
        eyebrow={
          <nav className="flex items-center gap-1" aria-label="Ruta">
            <Link to="/teams" className="hover:text-fg">
              Equipos
            </Link>
            <ChevronRight className="size-3.5" />
            <span className="truncate text-fg">{team.name}</span>
          </nav>
        }
        title={
          <span className="flex items-center gap-3">
            <TeamAvatar id={team.id} name={team.name} size="lg" />
            <span className="truncate">{team.name}</span>
          </span>
        }
        actions={<TeamRoleBadge role={team.myRole} />}
      />

      {/* The nav overlaps the border by 1px so the active tab's underline sits on it while the tabs scroll. */}
      <div className="mt-8 border-b border-line">
        <nav
          className="scrollbar-thin -mb-px flex gap-0.5 overflow-x-auto overflow-y-hidden sm:gap-1"
          aria-label="Secciones del equipo"
        >
          {tabs.map(({ to, label, icon: Icon, end }) => (
            <NavLink
              key={label}
              to={to}
              end={end}
              className={({ isActive }) =>
                cn(
                  'flex shrink-0 items-center gap-1.5 border-b-2 px-2 pt-1 pb-2.5 text-sm font-medium transition-colors sm:gap-2 sm:px-3',
                  isActive ? 'border-primary text-fg' : 'border-transparent text-muted hover:text-fg',
                )
              }
            >
              <Icon className="size-4" />
              {label}
              {label === 'Miembros' && (
                <span className="rounded-full bg-muted-surface px-1.5 text-xs text-muted tabular-nums">
                  {team.memberCount}
                </span>
              )}
            </NavLink>
          ))}
        </nav>
      </div>

      <div className="mt-6">
        <Outlet context={team satisfies TeamSummary} />
      </div>
    </PageContainer>
  )
}

/** The team the TeamLayout above is showing, for its child routes. */
export function useTeamOutlet(): TeamSummary {
  return useOutletContext<TeamSummary>()
}
