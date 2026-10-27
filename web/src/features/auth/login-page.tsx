import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useLocation } from 'react-router'
import { z } from 'zod'
import { Alert } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { applyServerErrors } from '@/lib/forms'
import { AuthLayout } from './auth-layout'
import { useAuth } from './auth-provider'
import { PasswordInput } from './password-input'

const schema = z.object({
  email: z.email('Escribe un email válido.'),
  password: z.string().min(1, 'Escribe tu contraseña.'),
})

type LoginForm = z.infer<typeof schema>

export function LoginPage() {
  const { signIn } = useAuth()
  const location = useLocation()
  const notice = (location.state as { notice?: string } | null)?.notice
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({ resolver: zodResolver(schema), defaultValues: { email: '', password: '' } })

  const onSubmit = handleSubmit(async ({ email, password }) => {
    setFormError(null)
    try {
      // The route guard sends the user on once the session is set.
      await signIn(email, password)
    } catch (error) {
      setFormError(applyServerErrors(error, setError, ['email', 'password']))
    }
  })

  return (
    <AuthLayout
      title="Bienvenido de nuevo"
      subtitle={
        <>
          ¿No tienes cuenta?{' '}
          <Link to="/register" className="font-medium text-primary hover:underline">
            Crea una gratis
          </Link>
        </>
      }
    >
      <form onSubmit={onSubmit} className="space-y-5" noValidate>
        {notice && !formError && <Alert tone="info">{notice}</Alert>}
        {formError && <Alert>{formError}</Alert>}
        <Field label="Email" error={errors.email?.message}>
          <Input type="email" autoComplete="email" placeholder="tu@empresa.com" autoFocus {...register('email')} />
        </Field>
        <Field label="Contraseña" error={errors.password?.message}>
          <PasswordInput autoComplete="current-password" placeholder="••••••••" {...register('password')} />
        </Field>
        <Button type="submit" size="lg" className="w-full" loading={isSubmitting}>
          Iniciar sesión
        </Button>
      </form>
    </AuthLayout>
  )
}
