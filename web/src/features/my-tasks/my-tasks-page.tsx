import { CircleCheckBig, ListChecks } from 'lucide-react'
import { PageContainer, PageHeader } from '@/components/layout/page-header'
import { Card } from '@/components/ui/card'
import { ExportMenu } from '@/components/ui/export-menu'
import { EmptyState, ErrorState, Skeleton } from '@/components/ui/feedback'
import { cn } from '@/lib/utils/cn'
import { myTasksDocument } from './my-tasks-export'
import { MyTaskRow } from './my-task-row'
import { dueGroup, dueGroupLabels, useMyCards, type DueGroup, type MyCard } from './my-tasks-api'

const order: DueGroup[] = ['overdue', 'today', 'week', 'later', 'none']

const groupAccent: Record<DueGroup, string> = {
  overdue: 'bg-danger',
  today: 'bg-warning',
  week: 'bg-info',
  later: 'bg-primary',
  none: 'bg-line-strong',
}

export function MyTasksPage() {
  const cards = useMyCards()

  const groups = new Map<DueGroup, MyCard[]>()
  for (const card of cards.data ?? []) {
    const group = dueGroup(card.dueDate)
    groups.set(group, [...(groups.get(group) ?? []), card])
  }

  return (
    <PageContainer className="max-w-4xl">
      <PageHeader
        title="Mis tareas"
        description="Las tarjetas que tienes asignadas en todos tus tableros."
        actions={
          <ExportMenu
            disabled={!cards.data?.length}
            getDocument={() => myTasksDocument(order.flatMap((group) => groups.get(group) ?? []))}
          />
        }
      />

      <div className="mt-8 space-y-6">
        {cards.isPending ? (
          <Skeleton className="h-64 rounded-xl" />
        ) : cards.isError ? (
          <ErrorState error={cards.error} onRetry={() => cards.refetch()} />
        ) : cards.data.length === 0 ? (
          <EmptyState
            icon={CircleCheckBig}
            title="No tienes tareas asignadas"
            description="Cuando alguien te asigne una tarjeta, aparecerá aquí ordenada por fecha límite."
          />
        ) : (
          order
            .filter((group) => groups.has(group))
            .map((group) => (
              <section key={group}>
                <h2 className="mb-2.5 flex items-center gap-2 text-sm font-semibold">
                  <span className={cn('size-2 rounded-full', groupAccent[group])} aria-hidden="true" />
                  {dueGroupLabels[group]}
                  <span className="rounded-full bg-muted-surface px-1.5 text-xs font-medium text-muted tabular-nums">
                    {groups.get(group)!.length}
                  </span>
                </h2>
                <Card className="divide-y divide-line overflow-hidden">
                  {groups.get(group)!.map((card) => (
                    <MyTaskRow key={card.id} card={card} />
                  ))}
                </Card>
              </section>
            ))
        )}
        {cards.data && cards.data.length > 0 && (
          <p className="flex items-center justify-center gap-1.5 text-xs text-faint">
            <ListChecks className="size-3.5" />
            Las tarjetas archivadas y las de tableros archivados no aparecen aquí.
          </p>
        )}
      </div>
    </PageContainer>
  )
}
