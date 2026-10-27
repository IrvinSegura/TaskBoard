import { zodResolver } from '@hookform/resolvers/zod'
import { Trash2 } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { z } from 'zod'
import { Alert } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { ConfirmDialog } from '@/components/ui/confirm-dialog'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { errorMessage } from '@/lib/api/api-error'
import { applyServerErrors } from '@/lib/forms'
import { useTeamOutlet } from './team-layout'
import { useDeleteTeam, useRenameTeam } from './team-queries'
import { canManageTeam, type TeamSummary } from './teams-api'

export function TeamSettingsRoute() {
  const team = useTeamOutlet()

  return (
    <div className="max-w-2xl space-y-6">
      <div>
        <h2 className="text-[15px] font-semibold">Configuración del equipo</h2>
        <p className="mt-1 text-sm text-muted">El nombre del equipo y las acciones que no se pueden deshacer.</p>
      </div>

      <Card>
        <CardHeader>
          <div>
            <CardTitle>Nombre</CardTitle>
            <CardDescription>
              {canManageTeam(team.myRole)
                ? 'Lo ven todos los miembros en la barra lateral y en sus tableros.'
                : 'Solo el dueño y los administradores pueden cambiarlo.'}
            </CardDescription>
          </div>
        </CardHeader>
        <div className="p-5">
          {/* Keyed by name so the form starts over when someone else renames the team. */}
          <RenameTeamForm key={team.name} team={team} />
        </div>
      </Card>

      {team.myRole === 'Owner' && <DeleteTeamCard team={team} />}
    </div>
  )
}

const schema = z.object({
  name: z.string().trim().min(1, 'Ponle un nombre al equipo.').max(100, 'Máximo 100 caracteres.'),
})

type RenameTeamValues = z.infer<typeof schema>

function RenameTeamForm({ team }: { team: TeamSummary }) {
  const rename = useRenameTeam(team.id)
  const canEdit = canManageTeam(team.myRole)
  const [formError, setFormError] = useState<string | null>(null)
  const { register, handleSubmit, setError, formState } = useForm<RenameTeamValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: team.name },
  })

  const onSubmit = handleSubmit(async ({ name }) => {
    setFormError(null)
    try {
      await rename.mutateAsync(name)
      toast.success('Nombre del equipo actualizado')
    } catch (error) {
      setFormError(applyServerErrors(error, setError, ['name']))
    }
  })

  return (
    <form onSubmit={onSubmit} className="max-w-sm space-y-4" noValidate>
      {formError && <Alert>{formError}</Alert>}
      <Field label="Nombre del equipo" error={formState.errors.name?.message}>
        <Input maxLength={100} disabled={!canEdit} {...register('name')} />
      </Field>
      {canEdit && (
        <Button type="submit" loading={formState.isSubmitting} disabled={!formState.isDirty}>
          Guardar
        </Button>
      )}
    </form>
  )
}

function DeleteTeamCard({ team }: { team: TeamSummary }) {
  const navigate = useNavigate()
  const remove = useDeleteTeam(team.id)
  const [confirming, setConfirming] = useState(false)

  // mutateAsync rather than mutate callbacks: dropping the team from the cache unmounts this page first.
  const onDelete = async () => {
    try {
      await remove.mutateAsync()
    } catch (error) {
      toast.error(errorMessage(error))
      return
    }
    navigate('/teams', { replace: true })
    toast.success(`Se eliminó el equipo «${team.name}»`)
  }

  return (
    <Card className="border-danger/40">
      <CardHeader>
        <div>
          <CardTitle className="text-danger">Eliminar el equipo</CardTitle>
          <CardDescription>
            Se borran el equipo, sus tableros (también los archivados) y todas sus tarjetas, comentarios y adjuntos.
          </CardDescription>
        </div>
      </CardHeader>
      <div className="p-5">
        <Button variant="danger" onClick={() => setConfirming(true)}>
          <Trash2 />
          Eliminar equipo
        </Button>
      </div>
      <ConfirmDialog
        open={confirming}
        onOpenChange={setConfirming}
        title={`¿Eliminar «${team.name}»?`}
        description="Todos los miembros perderán acceso a sus tableros al instante. No se puede deshacer."
        confirmLabel="Eliminar equipo"
        loading={remove.isPending}
        onConfirm={() => void onDelete()}
      />
    </Card>
  )
}
