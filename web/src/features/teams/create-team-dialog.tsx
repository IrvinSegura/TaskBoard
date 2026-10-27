import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { z } from 'zod'
import { Alert } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Dialog } from '@/components/ui/dialog'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { applyServerErrors } from '@/lib/forms'
import { useCreateTeam } from './team-queries'

const schema = z.object({
  name: z.string().trim().min(1, 'Ponle un nombre al equipo.').max(100, 'Máximo 100 caracteres.'),
})

type CreateTeamValues = z.infer<typeof schema>

interface CreateTeamDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function CreateTeamDialog({ open, onOpenChange }: CreateTeamDialogProps) {
  const navigate = useNavigate()
  const createTeam = useCreateTeam()
  const [formError, setFormError] = useState<string | null>(null)
  const { register, handleSubmit, setError, reset, formState } = useForm<CreateTeamValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: '' },
  })

  const close = (next: boolean) => {
    if (!next) {
      reset()
      setFormError(null)
    }
    onOpenChange(next)
  }

  const onSubmit = handleSubmit(async ({ name }) => {
    setFormError(null)
    try {
      const { id } = await createTeam.mutateAsync(name)
      toast.success(`Equipo «${name}» creado`)
      close(false)
      navigate(`/teams/${id}`)
    } catch (error) {
      setFormError(applyServerErrors(error, setError, ['name']))
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={close}
      title="Nuevo equipo"
      description="Serás el dueño. Después podrás invitar a otras personas por su email."
      footer={
        <>
          <Button variant="secondary" onClick={() => close(false)}>
            Cancelar
          </Button>
          <Button type="submit" form="create-team-form" loading={formState.isSubmitting}>
            Crear equipo
          </Button>
        </>
      }
    >
      <form id="create-team-form" onSubmit={onSubmit} className="space-y-4" noValidate>
        {formError && <Alert>{formError}</Alert>}
        <Field label="Nombre del equipo" error={formState.errors.name?.message}>
          <Input placeholder="Ej. Producto, Marketing, Diseño" autoFocus {...register('name')} />
        </Field>
      </form>
    </Dialog>
  )
}
