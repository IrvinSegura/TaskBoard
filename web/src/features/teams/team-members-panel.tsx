import { LogOut, MoreHorizontal, UserMinus, UserPlus, Users } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { Avatar } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { ConfirmDialog } from '@/components/ui/confirm-dialog'
import { DropdownMenu, MenuItem } from '@/components/ui/dropdown-menu'
import { EmptyState, ErrorState, Skeleton } from '@/components/ui/feedback'
import { Select } from '@/components/ui/input'
import { useCurrentUser } from '@/features/auth/auth-provider'
import { errorMessage } from '@/lib/api/api-error'
import { AddTeamMemberDialog } from './add-team-member-dialog'
import { TeamRoleBadge } from './team-role-badge'
import { useChangeTeamRole, useRemoveTeamMember, useTeamMembers } from './team-queries'
import { canManageTeam, teamRoleLabels, type TeamMember, type TeamRole } from './teams-api'

interface TeamMembersPanelProps {
  teamId: string
  teamName: string
  myRole: TeamRole
}

export function TeamMembersPanel({ teamId, teamName, myRole }: TeamMembersPanelProps) {
  const me = useCurrentUser()
  const navigate = useNavigate()
  const members = useTeamMembers(teamId)
  const changeRole = useChangeTeamRole(teamId)
  const removeMember = useRemoveTeamMember(teamId)
  const [adding, setAdding] = useState(false)
  const [removing, setRemoving] = useState<TeamMember | null>(null)
  const canManage = canManageTeam(myRole)

  const onChangeRole = (member: TeamMember, role: TeamRole) => {
    changeRole.mutate(
      { userId: member.userId, role },
      {
        onSuccess: () => toast.success(`${member.displayName} ahora es ${teamRoleLabels[role].toLowerCase()}`),
        onError: (error) => toast.error(errorMessage(error)),
      },
    )
  }

  const onConfirmRemove = () => {
    if (!removing) {
      return
    }
    const leaving = removing.userId === me.id
    removeMember.mutate(removing.userId, {
      onSuccess: () => {
        setRemoving(null)
        if (leaving) {
          toast.success(`Saliste de ${teamName}`)
          navigate('/teams', { replace: true })
        } else {
          toast.success(`${removing.displayName} ya no es parte del equipo`)
        }
      },
      onError: (error) => toast.error(errorMessage(error)),
    })
  }

  if (members.isPending) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 4 }, (_, i) => (
          <Skeleton key={i} className="h-16 rounded-xl" />
        ))}
      </div>
    )
  }
  if (members.isError) {
    return <ErrorState error={members.error} onRetry={() => members.refetch()} />
  }

  const isLeaving = removing?.userId === me.id

  return (
    <section>
      <div className="mb-4 flex items-center justify-between gap-4">
        <p className="text-sm text-muted">
          {members.data.length} {members.data.length === 1 ? 'persona' : 'personas'} en el equipo
        </p>
        {canManage && (
          <Button onClick={() => setAdding(true)}>
            <UserPlus />
            Agregar miembro
          </Button>
        )}
      </div>

      {members.data.length === 0 ? (
        <EmptyState icon={Users} title="Sin miembros" />
      ) : (
        <ul className="divide-y divide-line overflow-hidden rounded-xl border border-line bg-surface shadow-card">
          {members.data.map((member) => {
            const isMe = member.userId === me.id
            const isOwner = member.role === 'Owner'
            return (
              <li key={member.userId} className="flex items-center gap-3 px-4 py-3 sm:px-5">
                <Avatar id={member.userId} name={member.displayName} />
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium">
                    {member.displayName}
                    {isMe && <span className="ml-1.5 text-faint">(tú)</span>}
                  </p>
                  <p className="truncate text-[13px] text-muted">{member.email}</p>
                </div>

                {canManage && !isOwner && !isMe ? (
                  <div className="w-40 max-sm:hidden">
                    <Select
                      aria-label={`Rol de ${member.displayName}`}
                      value={member.role}
                      disabled={changeRole.isPending}
                      onChange={(event) => onChangeRole(member, event.target.value as TeamRole)}
                      className="h-8 text-[13px]"
                    >
                      <option value="Member">{teamRoleLabels.Member}</option>
                      <option value="Admin">{teamRoleLabels.Admin}</option>
                    </Select>
                  </div>
                ) : (
                  <TeamRoleBadge role={member.role} />
                )}

                {!isOwner && (isMe || canManage) ? (
                  <DropdownMenu
                    trigger={
                      <Button variant="ghost" size="icon-sm" aria-label={`Acciones para ${member.displayName}`}>
                        <MoreHorizontal />
                      </Button>
                    }
                  >
                    {isMe ? (
                      <MenuItem destructive icon={<LogOut />} onSelect={() => setRemoving(member)}>
                        Salir del equipo
                      </MenuItem>
                    ) : (
                      <MenuItem destructive icon={<UserMinus />} onSelect={() => setRemoving(member)}>
                        Quitar del equipo
                      </MenuItem>
                    )}
                  </DropdownMenu>
                ) : (
                  <span className="size-7" />
                )}
              </li>
            )
          })}
        </ul>
      )}

      <AddTeamMemberDialog teamId={teamId} open={adding} onOpenChange={setAdding} />
      <ConfirmDialog
        open={removing !== null}
        onOpenChange={(open) => !open && setRemoving(null)}
        title={isLeaving ? `¿Salir de ${teamName}?` : `¿Quitar a ${removing?.displayName}?`}
        description={
          isLeaving
            ? 'Perderás el acceso a todos los tableros del equipo. Para volver, alguien tendrá que invitarte de nuevo.'
            : 'También se le quitará de todos los tableros del equipo.'
        }
        confirmLabel={isLeaving ? 'Salir del equipo' : 'Quitar'}
        loading={removeMember.isPending}
        onConfirm={onConfirmRemove}
      />
    </section>
  )
}
