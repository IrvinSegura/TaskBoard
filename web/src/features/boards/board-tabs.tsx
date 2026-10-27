import { Activity, CalendarDays, Kanban, List, Settings } from 'lucide-react'
import { NavLink } from 'react-router'
import { cn } from '@/lib/utils/cn'
import { useIsBoardView } from './board-view'

const views = [
  { to: 'list', label: 'Lista', icon: List },
  { to: 'calendar', label: 'Calendario', icon: CalendarDays },
  { to: 'activity', label: 'Actividad', icon: Activity },
  { to: 'settings', label: 'Configuración', icon: Settings },
]

const tabClass = (active: boolean) =>
  cn(
    'flex shrink-0 items-center gap-1.5 border-b-2 px-2 pt-1 pb-2.5 text-sm font-medium transition-colors sm:px-3 [&_svg]:size-4',
    active ? 'border-primary text-fg' : 'border-transparent text-muted hover:text-fg',
  )

/** Switches between the canvas and the other board views; sits on the bottom edge of the board header. */
export function BoardTabs({ boardId }: { boardId: string }) {
  // The canvas stays the active tab while a card or the archived cards are open on top of it.
  const onCanvas = !useIsBoardView()

  return (
    <nav
      className="scrollbar-thin -mb-3 mt-2 flex gap-0.5 overflow-x-auto overflow-y-hidden sm:gap-1"
      aria-label="Vistas del tablero"
    >
      <NavLink to={`/boards/${boardId}`} className={tabClass(onCanvas)} aria-current={onCanvas ? 'page' : undefined}>
        <Kanban />
        Tablero
      </NavLink>
      {views.map(({ to, label, icon: Icon }) => (
        <NavLink key={to} to={`/boards/${boardId}/${to}`} className={({ isActive }) => tabClass(isActive)}>
          <Icon />
          {label}
        </NavLink>
      ))}
    </nav>
  )
}
