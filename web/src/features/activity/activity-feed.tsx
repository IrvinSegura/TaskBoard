import { Activity } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { EmptyState, ErrorState, Skeleton } from '@/components/ui/feedback'
import { cn } from '@/lib/utils/cn'
import { useActivityFeed, type ActivityScope } from './activity-api'
import { ActivityItem } from './activity-item'

interface ActivityFeedProps {
  scope: ActivityScope
  showBoard?: boolean
  pageSize?: number
  emptyText?: string
  className?: string
}

export function ActivityFeed({ scope, showBoard, pageSize, emptyText, className }: ActivityFeedProps) {
  const feed = useActivityFeed(scope, pageSize)
  const entries = feed.data?.pages.flatMap((page) => page.items) ?? []

  if (feed.isPending) {
    return (
      <div className={cn('space-y-3', className)}>
        {Array.from({ length: 4 }, (_, i) => (
          <Skeleton key={i} className="h-12" />
        ))}
      </div>
    )
  }
  if (feed.isError) {
    return <ErrorState error={feed.error} onRetry={() => feed.refetch()} className={className} />
  }
  if (entries.length === 0) {
    return (
      <EmptyState
        icon={Activity}
        title="Sin actividad todavía"
        description={emptyText ?? 'Aquí verás quién crea, mueve y comenta tarjetas.'}
        className={className}
      />
    )
  }

  return (
    <div className={className}>
      <ol className="divide-y divide-line">
        {entries.map((entry) => (
          <ActivityItem key={entry.id} entry={entry} showBoard={showBoard} />
        ))}
      </ol>
      {feed.hasNextPage && (
        <Button
          variant="ghost"
          size="sm"
          className="mt-2"
          loading={feed.isFetchingNextPage}
          onClick={() => void feed.fetchNextPage()}
        >
          Ver más actividad
        </Button>
      )}
    </div>
  )
}
