import { useMutation } from '@tanstack/react-query'
import { toast } from 'sonner'
import { Alert } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { useCurrentUser } from '@/features/auth/auth-provider'
import { authApi } from '@/features/auth/auth-api'
import { errorMessage } from '@/lib/api/api-error'

/** Shown until the user opens the link from their welcome email; nothing else waits on it. */
export function EmailVerificationNotice() {
  const user = useCurrentUser()
  const resend = useMutation({
    mutationFn: authApi.sendVerificationEmail,
    onSuccess: () => toast.success(`Te enviamos un enlace nuevo a ${user.email}.`),
    onError: (error) => toast.error(errorMessage(error)),
  })

  if (user.emailVerified) {
    return null
  }

  return (
    <Alert
      tone="warning"
      action={
        <Button variant="secondary" size="sm" loading={resend.isPending} onClick={() => resend.mutate()}>
          Enviar otro enlace
        </Button>
      }
    >
      Aún no confirmas tu correo. Abre el enlace que te enviamos al registrarte.
    </Alert>
  )
}
