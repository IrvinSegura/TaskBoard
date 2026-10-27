import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { toast } from 'sonner'
import { z } from 'zod'
import { Alert } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { authApi } from '@/features/auth/auth-api'
import { useAuth, useCurrentUser } from '@/features/auth/auth-provider'
import { applyServerErrors } from '@/lib/forms'

const schema = z.object({
  displayName: z.string().trim().min(1, 'Escribe tu nombre.').max(100, 'Máximo 100 caracteres.'),
})

type ProfileValues = z.infer<typeof schema>

/** The name other people see on boards, cards and comments. */
export function ProfileForm() {
  const user = useCurrentUser()
  const { reloadUser } = useAuth()
  const [formError, setFormError] = useState<string | null>(null)
  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<ProfileValues>({
    resolver: zodResolver(schema),
    defaultValues: { displayName: user.displayName },
  })

  const onSubmit = handleSubmit(async ({ displayName }) => {
    setFormError(null)
    try {
      await authApi.updateProfile(displayName)
      await reloadUser()
      reset({ displayName })
      toast.success('Perfil actualizado')
    } catch (error) {
      setFormError(applyServerErrors(error, setError, ['displayName']))
    }
  })

  return (
    <form onSubmit={onSubmit} className="max-w-sm space-y-4" noValidate>
      {formError && <Alert>{formError}</Alert>}
      <Field label="Nombre visible" error={errors.displayName?.message}>
        <Input autoComplete="name" maxLength={100} {...register('displayName')} />
      </Field>
      <Field label="Email" hint="El email no se puede cambiar.">
        <Input value={user.email} disabled readOnly />
      </Field>
      <Button type="submit" loading={isSubmitting} disabled={!isDirty}>
        Guardar cambios
      </Button>
    </form>
  )
}
