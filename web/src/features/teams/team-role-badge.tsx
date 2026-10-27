import { Crown, Shield } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { teamRoleLabels, type TeamRole } from './teams-api'

export function TeamRoleBadge({ role }: { role: TeamRole }) {
  if (role === 'Owner') {
    return (
      <Badge tone="warning">
        <Crown />
        {teamRoleLabels.Owner}
      </Badge>
    )
  }
  if (role === 'Admin') {
    return (
      <Badge tone="primary">
        <Shield />
        {teamRoleLabels.Admin}
      </Badge>
    )
  }
  return <Badge>{teamRoleLabels.Member}</Badge>
}
