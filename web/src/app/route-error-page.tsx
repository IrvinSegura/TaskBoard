import { AlertTriangle } from 'lucide-react'
import { useRouteError } from 'react-router'
import { Button } from '@/components/ui/button'

/** Last resort for a render error: the page broke, but the user can reload instead of seeing a blank screen. */
export function RouteErrorPage() {
  const error = useRouteError()
  if (import.meta.env.DEV) {
    console.error(error)
  }

  return (
    <div className="flex h-full items-center justify-center p-6">
      <div className="max-w-sm text-center">
        <div className="mx-auto mb-4 flex size-12 items-center justify-center rounded-xl bg-danger-soft text-danger">
          <AlertTriangle className="size-6" />
        </div>
        <h1 className="text-lg font-semibold">Algo salió mal</h1>
        <p className="mt-1 text-sm text-muted">Ocurrió un error inesperado al mostrar esta página.</p>
        <Button className="mt-5" onClick={() => window.location.reload()}>
          Recargar la página
        </Button>
      </div>
    </div>
  )
}
