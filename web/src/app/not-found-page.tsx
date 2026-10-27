import { Compass } from 'lucide-react'
import { Link } from 'react-router'
import { PageContainer } from '@/components/layout/page-header'
import { buttonStyles } from '@/components/ui/button'
import { EmptyState } from '@/components/ui/feedback'

export function NotFoundPage() {
  return (
    <PageContainer className="max-w-xl py-20">
      <EmptyState
        icon={Compass}
        title="Esta página no existe"
        description="Puede que el enlace esté mal escrito o que la página se haya movido."
        action={
          <Link to="/" className={buttonStyles()}>
            Volver al inicio
          </Link>
        }
      />
    </PageContainer>
  )
}
