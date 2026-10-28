import { ArrowRight, Users } from 'lucide-react'
import { Link } from 'react-router'
import { boardCover } from '@/features/boards/board-cover'
import { FavoriteButton } from '@/features/favorites/favorite-button'
import { cn } from '@/lib/utils/cn'
import { TeamAvatar } from './team-avatar'
import { TeamRoleBadge } from './team-role-badge'
import type { TeamSummary } from './teams-api'

/** A team on its cover, with a star to add it to favorites. */
export function TeamCard({ team }: { team: TeamSummary }) {
  return (
    <div className="group relative flex">
      <Link
        to={`/teams/${team.id}`}
        className="flex flex-1 flex-col overflow-hidden rounded-xl border border-line bg-surface shadow-card transition-all group-hover:-translate-y-0.5 group-hover:border-line-strong group-hover:shadow-raised"
      >
        <div className={cn('relative h-16 bg-linear-to-br', boardCover(team.id))}>
          <div className="bg-dots absolute inset-0 opacity-60" aria-hidden="true" />
        </div>
        {/* Relative so the avatar paints above the cover, which is positioned and would cover it. */}
        <div className="relative flex items-end justify-between gap-3 px-5">
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
      {/* Outside the link, but moves with the card when it lifts on hover. */}
      <FavoriteButton
        kind="team"
        id={team.id}
        name={team.name}
        tone="cover"
        className="absolute top-2.5 right-2.5 transition-transform group-hover:-translate-y-0.5"
      />
    </div>
  )
}
