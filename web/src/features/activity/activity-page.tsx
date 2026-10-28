import { PageContainer, PageHeader } from '@/components/layout/page-header'
import { Card } from '@/components/ui/card'
import { ActivityFeed } from './activity-feed'

export function ActivityPage() {
  return (
    <PageContainer className="max-w-3xl">
      <PageHeader title="Actividad" description="Lo que pasa en tus equipos y tableros." />
      <Card className="mt-8 px-5 py-2">
        <ActivityFeed scope={{ kind: 'me' }} showBoard className="py-2" />
      </Card>
    </PageContainer>
  )
}
