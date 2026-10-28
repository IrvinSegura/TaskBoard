import { Card } from '@/components/ui/card'
import { ActivityFeed } from '@/features/activity/activity-feed'
import { useBoardOutlet } from './board-page'

export function BoardActivityView() {
  const { board } = useBoardOutlet()

  return (
    <div className="max-w-3xl px-4 py-6 sm:px-6">
      <h2 className="text-[15px] font-semibold">Actividad</h2>
      <p className="mt-1 text-sm text-muted">Quién cambió qué en este tablero.</p>
      <Card className="mt-4 px-5 py-2">
        <ActivityFeed scope={{ kind: 'board', id: board.id }} className="py-2" />
      </Card>
    </div>
  )
}
