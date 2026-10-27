import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { toast } from 'sonner'
import { z } from 'zod'
import { Alert } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Dialog } from '@/components/ui/dialog'
import { Field } from '@/components/ui/field'
import { Input, Select } from '@/components/ui/input'
import { applyServerErrors } from '@/lib/forms'
import { useAddTeamMember } from './team-queries'
import { teamRoleLabels } from './teams-api'

const schema = z.object({
  email: z.email('Escribe un email válido.'),
  // The owner role is never assignable.
  role: z.enum(['Member', 'Admin']),
})

type AddMemberValues = z.infer<typeof schema>

interface AddTeamMemberDialogProps {
  teamId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function AddTeamMemberDialog({ teamId, open, onOpenChange }: AddTeamMemberDialogProps) {
  const addMember = useAddTeamMember(teamId)
  const [formError, setFormError] = useState<string | null>(null)
  const { register, handleSubmit, setError, reset, formState } = useForm<AddMemberValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: '', role: 'Member' },
  })

  const close = (next: boolean) => {
    if (!next) {
      reset()
      setFormError(null)
    }
    onOpenChange(next)
  }

  const onSubmit = handleSubmit(async ({ email, role }) => {
    setFormError(null)
    try {
      await addMember.mutateAsync({ email, role })
      toast.success(`${email} ahora es parte del equipo`)
      close(false)
    } catch (error) {
      setFormError(applyServerErrors(error, setError, ['email', 'role']))
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={close}
      title="Agregar miembro"
      description="La persona tiene que tener una cuenta de TaskBoard con ese email."
      footer={
        <>
          <Button variant="secondary" onClick={() => close(false)}>
            Cancelar
          </Button>
          <Button type="submit" form="add-team-member-form" loading={formState.isSubmitting}>
            Agregar
          </Button>
        </>
      }
    >
      <form id="add-team-member-form" onSubmit={onSubmit} className="space-y-4" noValidate>
        {formError && <Alert>{formError}</Alert>}
        <Field label="Email" error={formState.errors.email?.message}>
          <Input type="email" placeholder="persona@empresa.com" autoFocus {...register('email')} />
        </Field>
        <Field
          label="Rol"
          error={formState.errors.role?.message}
          hint="Los administradores pueden agregar y quitar miembros."
        >
          <Select {...register('role')}>
            <option value="Member">{teamRoleLabels.Member}</option>
            <option value="Admin">{teamRoleLabels.Admin}</option>
          </Select>
        </Field>
      </form>
    </Dialog>
  )
}
