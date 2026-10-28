import { LayoutGrid, Star, Users, type LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { PageContainer, PageHeader } from '@/components/layout/page-header'
import { EmptyState, ErrorState, Skeleton } from '@/components/ui/feedback'
import { BoardTile } from '@/features/boards/board-tile'
import { TeamCard } from '@/features/teams/team-card'
import { useFavorites } from './favorites-api'

export function FavoritesPage() {
  const favorites = useFavorites()

  return (
    <PageContainer>
      <PageHeader title="Favoritos" description="Los equipos y tableros que marcaste con estrella, a un clic." />

      <div className="mt-8">
        {favorites.isPending ? (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 3 }, (_, i) => (
              <Skeleton key={i} className="h-36 rounded-xl" />
            ))}
          </div>
        ) : favorites.isError ? (
          <ErrorState error={favorites.error} onRetry={() => favorites.refetch()} />
        ) : favorites.data.teams.length === 0 && favorites.data.boards.length === 0 ? (
          <EmptyState
            icon={Star}
            title="Aún no tienes favoritos"
            description="Pulsa la estrella de un equipo o de un tablero para tenerlo siempre a mano aquí."
            action={
              <Link to="/teams" className="text-sm font-medium text-primary hover:underline">
                Ver mis equipos
              </Link>
            }
          />
        ) : (
          <div className="space-y-10">
            <Section icon={LayoutGrid} title="Tableros" count={favorites.data.boards.length} empty="Ningún tablero marcado.">
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                {favorites.data.boards.map((board) => (
                  <BoardTile key={board.id} board={board} subtitle={board.teamName} />
                ))}
              </div>
            </Section>
            <Section icon={Users} title="Equipos" count={favorites.data.teams.length} empty="Ningún equipo marcado.">
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
                {favorites.data.teams.map((team) => (
                  <TeamCard key={team.id} team={team} />
                ))}
              </div>
            </Section>
          </div>
        )}
      </div>
    </PageContainer>
  )
}

interface SectionProps {
  icon: LucideIcon
  title: string
  count: number
  empty: string
  children: ReactNode
}

function Section({ icon: Icon, title, count, empty, children }: SectionProps) {
  return (
    <section>
      <h2 className="mb-4 flex items-center gap-2 text-[15px] font-semibold">
        <Icon className="size-4 text-muted" />
        {title}
        <span className="rounded-full bg-muted-surface px-1.5 text-xs font-medium text-muted tabular-nums">{count}</span>
      </h2>
      {count === 0 ? <p className="text-sm text-muted">{empty}</p> : children}
    </section>
  )
}
