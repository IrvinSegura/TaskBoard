import { ArrowRight, Plus, Users } from 'lucide-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { PageContainer, PageHeader } from '@/components/layout/page-header'
import { Button } from '@/components/ui/button'
import { EmptyState, ErrorState, Skeleton } from '@/components/ui/feedback'
import { boardCover } from '@/features/boards/board-cover'
import { cn } from '@/lib/utils/cn'
import { CreateTeamDialog } from './create-team-dialog'
import { TeamAvatar } from './team-avatar'
import { TeamRoleBadge } from './team-role-badge'
import { useTeams } from './team-queries'

export function TeamsPage() {
  const teams = useTeams()
  const [creating, setCreating] = useState(false)

  return (
    <PageContainer>
      <PageHeader
        title="Equipos"
        description="Los equipos agrupan a las personas y sus tableros."
        actions={
          <Button onClick={() => setCreating(true)}>
            <Plus />
            Nuevo equipo
          </Button>
        }
      />

      <div className="mt-8">
        {teams.isPending ? (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 3 }, (_, i) => (
              <Skeleton key={i} className="h-36 rounded-xl" />
            ))}
          </div>
        ) : teams.isError ? (
          <ErrorState error={teams.error} onRetry={() => teams.refetch()} />
        ) : teams.data.length === 0 ? (
          <EmptyState
            icon={Users}
            title="Todavía no tienes equipos"
            description="Crea tu primer equipo para empezar a organizar tableros, o pide a alguien que te invite al suyo."
            action={
              <Button onClick={() => setCreating(true)}>
                <Plus />
                Crear equipo
              </Button>
            }
          />
        ) : (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {teams.data.map((team) => (
              <Link
                key={team.id}
                to={`/teams/${team.id}`}
                className="group flex flex-col overflow-hidden rounded-xl border border-line bg-surface shadow-card transition-all hover:-translate-y-0.5 hover:border-line-strong hover:shadow-raised"
              >
                <div className={cn('relative h-16 bg-linear-to-br', boardCover(team.id))}>
                  <div className="bg-dots absolute inset-0 opacity-60" aria-hidden="true" />
                </div>
                <div className="flex items-end justify-between gap-3 px-5">
                  <TeamAvatar name={team.name} id={team.id} size="lg" className="-mt-6 ring-4 ring-surface" />
                  <TeamRoleBadge role={team.myRole} />
                </div>
                <h2 className="mt-3 truncate px-5 text-[15px] font-semibold group-hover:text-primary">{team.name}</h2>
                <div className="mt-auto flex items-center justify-between px-5 pt-3 pb-5 text-sm text-muted">
                  <span className="inline-flex items-center gap-1.5">
                    <Users className="size-4" />
                    {team.memberCount} {team.memberCount === 1 ? 'miembro' : 'miembros'}
                  </span>
                  <ArrowRight className="size-4 -translate-x-1 opacity-0 transition-all group-hover:translate-x-0 group-hover:opacity-100" />
                </div>
              </Link>
            ))}
          </div>
        )}
      </div>

      <CreateTeamDialog open={creating} onOpenChange={setCreating} />
    </PageContainer>
  )
}
