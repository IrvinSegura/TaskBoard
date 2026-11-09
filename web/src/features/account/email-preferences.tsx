import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { ErrorState, Skeleton } from '@/components/ui/feedback'
import type { NotificationType } from '@/features/notifications/notifications-api'
import { errorMessage } from '@/lib/api/api-error'
import { queryKeys } from '@/lib/api/query-keys'
import { cn } from '@/lib/utils/cn'
import { emailPreferencesApi, type EmailPreferences as Preferences } from './email-preferences-api'

const notices: { type: NotificationType; label: string; description: string }[] = [
  { type: 'CardAssigned', label: 'Tarjetas asignadas', description: 'Cuando alguien te asigna una tarjeta.' },
  { type: 'Mentioned', label: 'Menciones', description: 'Cuando alguien te menciona con @ en un comentario.' },
  { type: 'CommentAdded', label: 'Comentarios', description: 'Cuando alguien comenta en una tarjeta que tienes asignada.' },
  { type: 'AddedToBoard', label: 'Tableros nuevos', description: 'Cuando te agregan a un tablero.' },
  { type: 'AddedToTeam', label: 'Equipos nuevos', description: 'Cuando te agregan a un equipo.' },
  { type: 'TeamRoleChanged', label: 'Cambios de rol', description: 'Cuando cambian tu rol en un equipo.' },
  { type: 'RemovedFromBoard', label: 'Salidas de tableros', description: 'Cuando alguien te quita de un tablero.' },
  { type: 'RemovedFromTeam', label: 'Salidas de equipos', description: 'Cuando alguien te quita de un equipo.' },
  { type: 'CardDueSoon', label: 'Vencimientos', description: 'Un día antes de que venza una tarjeta que tienes asignada.' },
]

/** One switch per notification that also arrives by email. The notifications in the app never change. */
export function EmailPreferences() {
  const queryClient = useQueryClient()
  const key = queryKeys.emailPreferences()

  const preferences = useQuery({ queryKey: key, queryFn: emailPreferencesApi.get })

  const update = useMutation({
    // In order: each request carries the whole list, so a late answer must not undo a newer switch.
    scope: { id: 'email-preferences' },
    mutationFn: (mutedTypes: NotificationType[]) => emailPreferencesApi.update(mutedTypes),
    onMutate: async (mutedTypes) => {
      await queryClient.cancelQueries({ queryKey: key })
      const previous = queryClient.getQueryData<Preferences>(key)
      queryClient.setQueryData<Preferences>(key, { mutedTypes })
      return { previous }
    },
    onError: (error, _mutedTypes, context) => {
      queryClient.setQueryData(key, context?.previous)
      toast.error(errorMessage(error))
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: key }),
  })

  if (preferences.isPending) {
    return (
      <div className="space-y-3" role="status" aria-label="Cargando tus preferencias">
        {notices.map(({ type }) => (
          <Skeleton key={type} className="h-10 w-full" />
        ))}
      </div>
    )
  }

  if (preferences.isError) {
    return <ErrorState error={preferences.error} onRetry={() => void preferences.refetch()} />
  }

  const muted = preferences.data.mutedTypes

  return (
    <ul className="divide-y divide-line">
      {notices.map(({ type, label, description }) => {
        const enabled = !muted.includes(type)
        return (
          <li key={type} className="flex items-center justify-between gap-4 py-3 first:pt-0 last:pb-0">
            <div className="min-w-0">
              <p className="text-sm font-medium">{label}</p>
              <p className="text-[13px] text-muted">{description}</p>
            </div>
            <button
              type="button"
              role="switch"
              aria-checked={enabled}
              aria-label={label}
              onClick={() => update.mutate(enabled ? [...muted, type] : muted.filter((t) => t !== type))}
              className={cn(
                'relative h-6 w-10 shrink-0 cursor-pointer rounded-full transition-colors focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none',
                enabled ? 'bg-primary' : 'bg-line-strong',
              )}
            >
              <span
                className={cn(
                  'absolute top-0.5 left-0.5 size-5 rounded-full bg-white shadow-sm transition-transform',
                  enabled && 'translate-x-4',
                )}
              />
            </button>
          </li>
        )
      })}
    </ul>
  )
}
