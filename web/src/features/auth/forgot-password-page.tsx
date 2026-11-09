import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { z } from 'zod'
import { Alert } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { applyServerErrors } from '@/lib/forms'
import { authApi } from './auth-api'
import { AuthLayout } from './auth-layout'

const schema = z.object({
  email: z.email('Escribe un email válido.'),
})

type ForgotPasswordValues = z.infer<typeof schema>

export function ForgotPasswordPage() {
  const [sent, setSent] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ForgotPasswordValues>({ resolver: zodResolver(schema), defaultValues: { email: '' } })

  const onSubmit = handleSubmit(async ({ email }) => {
    setFormError(null)
    try {
      await authApi.forgotPassword(email)
      setSent(true)
    } catch (error) {
      setFormError(applyServerErrors(error, setError, ['email']))
    }
  })

  return (
    <AuthLayout
      title="Recupera tu contraseña"
      subtitle={
        <>
          ¿Ya la recordaste?{' '}
          <Link to="/login" className="font-medium text-primary hover:underline">
            Inicia sesión
          </Link>
        </>
      }
    >
      {sent ? (
        // Worded the same whether the account exists or not: the API does not reveal it, and neither does this page.
        <Alert tone="info">
          Si hay una cuenta con ese email, te enviamos un enlace para elegir una contraseña nueva. Revisa también la
          carpeta de spam.
        </Alert>
      ) : (
        <form onSubmit={onSubmit} className="space-y-5" noValidate>
          {formError && <Alert>{formError}</Alert>}
          <p className="text-sm text-muted">Escribe el email de tu cuenta y te enviaremos un enlace para restablecerla.</p>
          <Field label="Email" error={errors.email?.message}>
            <Input type="email" autoComplete="email" placeholder="tu@empresa.com" autoFocus {...register('email')} />
          </Field>
          <Button type="submit" size="lg" className="w-full" loading={isSubmitting}>
            Enviar enlace
          </Button>
        </form>
      )}
    </AuthLayout>
  )
}
