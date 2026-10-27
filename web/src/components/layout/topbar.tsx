import { LogOut, Menu, Monitor, Moon, Sun, UserRound } from 'lucide-react'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { Avatar } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { DropdownMenu, MenuItem, MenuSeparator } from '@/components/ui/dropdown-menu'
import { useCurrentUser, useAuth } from '@/features/auth/auth-provider'
import { NotificationsMenu } from '@/features/notifications/notifications-menu'
import { useTheme, type ThemePreference } from '@/lib/theme/theme-provider'

const nextTheme: Record<ThemePreference, ThemePreference> = { light: 'dark', dark: 'system', system: 'light' }
const themeIcons = { light: Sun, dark: Moon, system: Monitor }
const themeLabels = { light: 'Tema claro', dark: 'Tema oscuro', system: 'Tema del sistema' }

export function Topbar({ onOpenSidebar }: { onOpenSidebar: () => void }) {
  const user = useCurrentUser()
  const { signOut } = useAuth()
  const navigate = useNavigate()
  const { preference, setPreference } = useTheme()
  const ThemeIcon = themeIcons[preference]

  const onSignOut = async () => {
    try {
      await signOut()
    } catch {
      // The local session is gone either way; the cookie expires on its own.
    }
    toast.success('Sesión cerrada')
  }

  return (
    <header className="flex h-14 shrink-0 items-center gap-2 border-b border-line bg-surface px-3 sm:px-4">
      <Button variant="ghost" size="icon" className="lg:hidden" onClick={onOpenSidebar} aria-label="Abrir menú">
        <Menu />
      </Button>

      <div className="ml-auto flex items-center gap-1">
        <Button
          variant="ghost"
          size="icon"
          onClick={() => setPreference(nextTheme[preference])}
          aria-label={`${themeLabels[preference]}. Cambiar tema`}
          title={themeLabels[preference]}
        >
          <ThemeIcon />
        </Button>
        <NotificationsMenu />
        <DropdownMenu
          trigger={
            <button
              type="button"
              className="ml-1 flex cursor-pointer items-center gap-2 rounded-lg py-1 pr-2 pl-1 hover:bg-muted-surface"
              aria-label="Menú de usuario"
            >
              <Avatar id={user.id} name={user.displayName} />
              <span className="hidden max-w-36 truncate text-sm font-medium sm:block">{user.displayName}</span>
            </button>
          }
        >
          <div className="px-2.5 py-2">
            <p className="truncate text-sm font-medium">{user.displayName}</p>
            <p className="truncate text-xs text-muted">{user.email}</p>
          </div>
          <MenuSeparator />
          <MenuItem icon={<UserRound />} onSelect={() => navigate('/account')}>
            Mi cuenta
          </MenuItem>
          <MenuItem icon={<LogOut />} onSelect={() => void onSignOut()}>
            Cerrar sesión
          </MenuItem>
        </DropdownMenu>
      </div>
    </header>
  )
}
