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
import { AuthLayout } from './auth-layout'
import { useAuth } from './auth-provider'
import { PasswordInput } from './password-input'
import { passwordSchema } from './password-schema'

// Same limits as RegisterCommandValidator.
const schema = z.object({
  displayName: z.string().trim().min(1, 'Escribe tu nombre.').max(100, 'Máximo 100 caracteres.'),
  email: z.email('Escribe un email válido.').max(256, 'Máximo 256 caracteres.'),
  password: passwordSchema,
})

type RegisterForm = z.infer<typeof schema>

export function RegisterPage() {
  const { signUp } = useAuth()
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegisterForm>({
    resolver: zodResolver(schema),
    defaultValues: { displayName: '', email: '', password: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null)
    try {
      await signUp(values)
    } catch (error) {
      setFormError(applyServerErrors(error, setError, ['displayName', 'email', 'password']))
    }
  })

  return (
    <AuthLayout
      title="Crea tu cuenta"
      subtitle={
        <>
          ¿Ya tienes cuenta?{' '}
          <Link to="/login" className="font-medium text-primary hover:underline">
            Inicia sesión
          </Link>
        </>
      }
    >
      <form onSubmit={onSubmit} className="space-y-5" noValidate>
        {formError && <Alert>{formError}</Alert>}
        <Field label="Nombre" error={errors.displayName?.message}>
          <Input autoComplete="name" placeholder="Ana López" autoFocus {...register('displayName')} />
        </Field>
        <Field label="Email" error={errors.email?.message}>
          <Input type="email" autoComplete="email" placeholder="tu@empresa.com" {...register('email')} />
        </Field>
        <Field label="Contraseña" error={errors.password?.message} hint="Al menos 8 caracteres.">
          <PasswordInput autoComplete="new-password" placeholder="••••••••" {...register('password')} />
        </Field>
        <Button type="submit" size="lg" className="w-full" loading={isSubmitting}>
          Crear cuenta
        </Button>
      </form>
    </AuthLayout>
  )
}
