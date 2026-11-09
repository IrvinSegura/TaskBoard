import { zodResolver } from '@hookform/resolvers/zod'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useLocation, useNavigate } from 'react-router'
import { z } from 'zod'
import { Alert } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Field } from '@/components/ui/field'
import { ApiError } from '@/lib/api/api-error'
import { applyServerErrors } from '@/lib/forms'
import { authApi } from './auth-api'
import { AuthLayout } from './auth-layout'
import { useAuth } from './auth-provider'
import { PasswordInput } from './password-input'
import { passwordSchema } from './password-schema'
import { readResetToken } from './reset-token'

const schema = z
  .object({
    newPassword: passwordSchema,
    confirmPassword: z.string(),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    path: ['confirmPassword'],
    message: 'Las contraseñas no coinciden.',
  })

type ResetPasswordValues = z.infer<typeof schema>

const backToForgotPassword = (
  <Link to="/forgot-password" className="font-medium text-primary hover:underline">
    Pide un enlace nuevo
  </Link>
)

export function ResetPasswordPage() {
  const { endSession } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()

  // Kept here because the effect below takes it out of the address.
  const [token] = useState(() => readResetToken(location.hash))
  const [linkFailed, setLinkFailed] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  // The token should not stay in the address bar or the browser's history once it has been read.
  useEffect(() => {
    if (location.hash) {
      void navigate({ hash: '' }, { replace: true })
    }
  }, [location.hash, navigate])

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ResetPasswordValues>({
    resolver: zodResolver(schema),
    defaultValues: { newPassword: '', confirmPassword: '' },
  })

  const onSubmit = handleSubmit(async ({ newPassword }) => {
    setFormError(null)
    try {
      await authApi.resetPassword(token!, newPassword)
      // The API revoked every refresh token, the one in this browser included.
      endSession()
      void navigate('/login', {
        replace: true,
        state: { notice: 'Contraseña actualizada. Inicia sesión con tu nueva contraseña.' },
      })
    } catch (error) {
      if (error instanceof ApiError && error.code === 'Users.InvalidPasswordResetToken') {
        setLinkFailed(true)
        return
      }
      setFormError(applyServerErrors(error, setError, ['newPassword']))
    }
  })

  if (!token || linkFailed) {
    return (
      <AuthLayout title="Este enlace ya no sirve" subtitle={backToForgotPassword}>
        <Alert>El enlace no es válido, ya venció o ya se usó.</Alert>
      </AuthLayout>
    )
  }

  return (
    <AuthLayout title="Elige una contraseña nueva" subtitle="Al guardarla se cerrarán todas tus sesiones.">
      <form onSubmit={onSubmit} className="space-y-5" noValidate>
        {formError && <Alert>{formError}</Alert>}
        <Field label="Nueva contraseña" error={errors.newPassword?.message} hint="Al menos 8 caracteres.">
          <PasswordInput autoComplete="new-password" autoFocus {...register('newPassword')} />
        </Field>
        <Field label="Confirmar nueva contraseña" error={errors.confirmPassword?.message}>
          <PasswordInput autoComplete="new-password" {...register('confirmPassword')} />
        </Field>
        <Button type="submit" size="lg" className="w-full" loading={isSubmitting}>
          Guardar contraseña
        </Button>
      </form>
    </AuthLayout>
  )
}
