import { TeamMembersPanel } from './team-members-panel'
import { useTeamOutlet } from './team-layout'

export function TeamMembersRoute() {
  const team = useTeamOutlet()
  return <TeamMembersPanel teamId={team.id} teamName={team.name} myRole={team.myRole} />
}
