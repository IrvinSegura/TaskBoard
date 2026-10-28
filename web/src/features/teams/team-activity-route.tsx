import { Card } from '@/components/ui/card'
import { ActivityFeed } from '@/features/activity/activity-feed'
import { useTeamOutlet } from './team-layout'

export function TeamActivityRoute() {
  const team = useTeamOutlet()

  return (
    <div className="max-w-3xl">
      <h2 className="text-[15px] font-semibold">Actividad del equipo</h2>
      <p className="mt-1 text-sm text-muted">Los cambios recientes en los tableros de este equipo.</p>
      <Card className="mt-4 px-5 py-2">
        <ActivityFeed scope={{ kind: 'team', id: team.id }} showBoard className="py-2" />
      </Card>
    </div>
  )
}
