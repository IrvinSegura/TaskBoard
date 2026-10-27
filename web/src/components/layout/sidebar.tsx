import { Bell, ChevronRight, Home, Plus, Users } from 'lucide-react'
import { useState } from 'react'
import { NavLink, useLocation } from 'react-router'
import { Logo } from '@/components/brand/logo'
import { Skeleton } from '@/components/ui/feedback'
import { useTeamBoards } from '@/features/boards/board-queries'
import { boardCover } from '@/features/boards/board-cover'
import { useUnreadCount } from '@/features/notifications/notification-queries'
import { CreateTeamDialog } from '@/features/teams/create-team-dialog'
import { TeamAvatar } from '@/features/teams/team-avatar'
import { useTeams } from '@/features/teams/team-queries'
import type { TeamSummary } from '@/features/teams/teams-api'
import { cn } from '@/lib/utils/cn'

const navLinkClass = ({ isActive }: { isActive: boolean }) =>
  cn(
    'flex items-center gap-2.5 rounded-lg px-2.5 py-1.5 text-sm font-medium transition-colors [&_svg]:size-4',
    isActive ? 'bg-primary-soft text-primary' : 'text-muted hover:bg-muted-surface hover:text-fg',
  )

export function Sidebar({ onNavigate }: { onNavigate?: () => void }) {
  const teams = useTeams()
  const unread = useUnreadCount()
  const [creating, setCreating] = useState(false)

  return (
    <div className="flex h-full flex-col">
      <div className="flex h-14 shrink-0 items-center px-4">
        <Logo />
      </div>

      <nav className="space-y-0.5 px-3 pt-2" aria-label="Principal">
        <NavLink to="/" end className={navLinkClass} onClick={onNavigate}>
          <Home />
          Inicio
        </NavLink>
        <NavLink to="/notifications" className={navLinkClass} onClick={onNavigate}>
          <Bell />
          Notificaciones
          {unread > 0 && (
            <span className="ml-auto rounded-full bg-primary px-1.5 text-[11px] leading-5 font-semibold text-primary-fg tabular-nums">
              {unread}
            </span>
          )}
        </NavLink>
        <NavLink to="/teams" end className={navLinkClass} onClick={onNavigate}>
          <Users />
          Equipos
        </NavLink>
      </nav>

      <div className="mt-6 flex items-center justify-between px-5">
        <p className="text-xs font-semibold tracking-wide text-faint uppercase">Tus equipos</p>
        <button
          type="button"
          onClick={() => setCreating(true)}
          className="cursor-pointer rounded-md p-1 text-faint hover:bg-muted-surface hover:text-fg"
          aria-label="Nuevo equipo"
        >
          <Plus className="size-4" />
        </button>
      </div>

      <div className="scrollbar-thin mt-2 flex-1 overflow-y-auto px-3 pb-4">
        {teams.isPending ? (
          <div className="space-y-2 px-2">
            <Skeleton className="h-6" />
            <Skeleton className="h-6" />
          </div>
        ) : teams.data?.length === 0 ? (
          <p className="px-2.5 text-[13px] text-faint">Aún no perteneces a ningún equipo.</p>
        ) : (
          <ul className="space-y-0.5">
            {teams.data?.map((team) => (
              <TeamNavItem key={team.id} team={team} onNavigate={onNavigate} />
            ))}
          </ul>
        )}
      </div>

      <CreateTeamDialog open={creating} onOpenChange={setCreating} />
    </div>
  )
}

function TeamNavItem({ team, onNavigate }: { team: TeamSummary; onNavigate?: () => void }) {
  const location = useLocation()
  const inTeam = location.pathname.startsWith(`/teams/${team.id}`)
  const [expanded, setExpanded] = useState(inTeam)
  const boards = useTeamBoards(team.id)
  // Keep the team open while one of its boards is on screen.
  const showingBoard = boards.data?.some((board) => location.pathname.startsWith(`/boards/${board.id}`)) ?? false
  const open = expanded || showingBoard

  return (
    <li>
      <div className="group flex items-center">
        <button
          type="button"
          onClick={() => setExpanded(!open)}
          className="cursor-pointer rounded-md p-1 text-faint hover:text-fg"
          aria-label={open ? `Contraer ${team.name}` : `Expandir ${team.name}`}
          aria-expanded={open}
        >
          <ChevronRight className={cn('size-3.5 transition-transform', open && 'rotate-90')} />
        </button>
        <NavLink
          to={`/teams/${team.id}`}
          onClick={onNavigate}
          className={({ isActive }) =>
            cn(
              'flex min-w-0 flex-1 items-center gap-2 rounded-lg px-1.5 py-1.5 text-sm transition-colors',
              isActive ? 'bg-muted-surface font-medium text-fg' : 'text-muted hover:bg-muted-surface hover:text-fg',
            )
          }
        >
          <TeamAvatar id={team.id} name={team.name} size="sm" />
          <span className="truncate">{team.name}</span>
        </NavLink>
      </div>
      {open && (
        <ul className="mt-0.5 mb-1.5 ml-[18px] space-y-0.5 border-l border-line pl-2.5">
          {boards.isPending && <Skeleton className="my-1 h-5 w-32" />}
          {boards.data?.length === 0 && <li className="px-2 py-1 text-[13px] text-faint">Sin tableros</li>}
          {boards.data?.map((board) => (
            <li key={board.id}>
              <NavLink
                to={`/boards/${board.id}`}
                onClick={onNavigate}
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-2 rounded-md px-2 py-1 text-[13px] transition-colors',
                    isActive ? 'bg-primary-soft font-medium text-primary' : 'text-muted hover:bg-muted-surface hover:text-fg',
                  )
                }
              >
                <span className={cn('size-2.5 shrink-0 rounded-[3px] bg-linear-to-br', boardCover(board.id))} />
                <span className="truncate">{board.name}</span>
              </NavLink>
            </li>
          ))}
        </ul>
      )}
    </li>
  )
}
