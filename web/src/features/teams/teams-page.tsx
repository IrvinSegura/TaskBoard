import { Plus, Users } from 'lucide-react'
import { useState } from 'react'
import { PageContainer, PageHeader } from '@/components/layout/page-header'
import { Button } from '@/components/ui/button'
import { EmptyState, ErrorState, Skeleton } from '@/components/ui/feedback'
import { CreateTeamDialog } from './create-team-dialog'
import { TeamCard } from './team-card'
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
              <TeamCard key={team.id} team={team} />
            ))}
          </div>
        )}
      </div>

      <CreateTeamDialog open={creating} onOpenChange={setCreating} />
    </PageContainer>
  )
}
