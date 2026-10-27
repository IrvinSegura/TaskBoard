import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router'
import { z } from 'zod'
import { Alert } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Field } from '@/components/ui/field'
import { authApi } from '@/features/auth/auth-api'
import { useAuth } from '@/features/auth/auth-provider'
import { PasswordInput } from '@/features/auth/password-input'
import { passwordSchema } from '@/features/auth/password-schema'
import { ApiError } from '@/lib/api/api-error'
import { applyServerErrors } from '@/lib/forms'

const schema = z
  .object({
    currentPassword: z.string().min(1, 'Escribe tu contraseña actual.'),
    newPassword: passwordSchema,
    confirmPassword: z.string(),
  })
  .refine((values) => values.newPassword !== values.currentPassword, {
    path: ['newPassword'],
    message: 'La nueva contraseña debe ser distinta de la actual.',
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    path: ['confirmPassword'],
    message: 'Las contraseñas no coinciden.',
  })

type ChangePasswordValues = z.infer<typeof schema>

export function ChangePasswordForm() {
  const { endSession } = useAuth()
  const navigate = useNavigate()
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ChangePasswordValues>({
    resolver: zodResolver(schema),
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
  })

  const onSubmit = handleSubmit(async ({ currentPassword, newPassword }) => {
    setFormError(null)
    try {
      await authApi.changePassword({ currentPassword, newPassword })
      // The API revoked every refresh token, this one included: sign in again with the new password.
      endSession()
      navigate('/login', {
        replace: true,
        state: { notice: 'Contraseña actualizada. Inicia sesión con tu nueva contraseña.' },
      })
    } catch (error) {
      if (error instanceof ApiError && error.code === 'Users.WrongPassword') {
        setError('currentPassword', { type: 'server', message: error.message })
        return
      }
      setFormError(applyServerErrors(error, setError, ['currentPassword', 'newPassword']))
    }
  })

  return (
    <form onSubmit={onSubmit} className="max-w-sm space-y-4" noValidate>
      {formError && <Alert>{formError}</Alert>}
      <Field label="Contraseña actual" error={errors.currentPassword?.message}>
        <PasswordInput autoComplete="current-password" {...register('currentPassword')} />
      </Field>
      <Field label="Nueva contraseña" error={errors.newPassword?.message} hint="Al menos 8 caracteres.">
        <PasswordInput autoComplete="new-password" {...register('newPassword')} />
      </Field>
      <Field label="Confirmar nueva contraseña" error={errors.confirmPassword?.message}>
        <PasswordInput autoComplete="new-password" {...register('confirmPassword')} />
      </Field>
      <Button type="submit" loading={isSubmitting}>
        Cambiar contraseña
      </Button>
    </form>
  )
}
