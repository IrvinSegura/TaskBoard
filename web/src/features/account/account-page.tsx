import { Monitor, Moon, Sun } from 'lucide-react'
import { PageContainer, PageHeader } from '@/components/layout/page-header'
import { Avatar } from '@/components/ui/avatar'
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useCurrentUser } from '@/features/auth/auth-provider'
import { cn } from '@/lib/utils/cn'
import { formatLongDate } from '@/lib/utils/format'
import { useTheme, type ThemePreference } from '@/lib/theme/theme-provider'
import { ChangePasswordForm } from './change-password-form'

const themes: { value: ThemePreference; label: string; icon: typeof Sun }[] = [
  { value: 'light', label: 'Claro', icon: Sun },
  { value: 'dark', label: 'Oscuro', icon: Moon },
  { value: 'system', label: 'Sistema', icon: Monitor },
]

export function AccountPage() {
  const user = useCurrentUser()
  const { preference, setPreference } = useTheme()

  return (
    <PageContainer className="max-w-3xl">
      <PageHeader title="Mi cuenta" description="Tu perfil, la apariencia de la aplicación y tu contraseña." />

      <div className="mt-8 space-y-6">
        <Card>
          <div className="flex items-center gap-4 px-5 py-5">
            <Avatar id={user.id} name={user.displayName} size="xl" />
            <div className="min-w-0">
              <p className="truncate text-lg font-semibold">{user.displayName}</p>
              <p className="truncate text-sm text-muted">{user.email}</p>
              <p className="mt-1 text-xs text-faint">Miembro desde el {formatLongDate(user.createdAt)}</p>
            </div>
          </div>
        </Card>

        <Card>
          <CardHeader>
            <div>
              <CardTitle>Apariencia</CardTitle>
              <CardDescription>Elige cómo se ve TaskBoard en este navegador.</CardDescription>
            </div>
          </CardHeader>
          <div className="grid grid-cols-3 gap-3 p-5" role="radiogroup" aria-label="Tema">
            {themes.map(({ value, label, icon: Icon }) => (
              <button
                key={value}
                type="button"
                role="radio"
                aria-checked={preference === value}
                onClick={() => setPreference(value)}
                className={cn(
                  'flex cursor-pointer flex-col items-center gap-2 rounded-xl border px-3 py-4 text-sm font-medium transition-colors',
                  preference === value
                    ? 'border-primary bg-primary-soft text-primary'
                    : 'border-line text-muted hover:border-line-strong hover:text-fg',
                )}
              >
                <Icon className="size-5" />
                {label}
              </button>
            ))}
          </div>
        </Card>

        <Card>
          <CardHeader>
            <div>
              <CardTitle>Contraseña</CardTitle>
              <CardDescription>Al cambiarla se cierran todas tus sesiones, también esta.</CardDescription>
            </div>
          </CardHeader>
          <div className="p-5">
            <ChangePasswordForm />
          </div>
        </Card>
      </div>
    </PageContainer>
  )
}
