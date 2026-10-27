import { UserMinus, UserPlus } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'
import { Avatar } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Dialog } from '@/components/ui/dialog'
import { Select } from '@/components/ui/input'
import { useCurrentUser } from '@/features/auth/auth-provider'
import { useTeamMembers } from '@/features/teams/team-queries'
import { errorMessage } from '@/lib/api/api-error'
import { boardRoleDescriptions, boardRoleLabels } from './board-permissions'
import { useAddBoardMember, useRemoveBoardMember } from './board-queries'
import { boardRoles, type BoardDetail, type BoardRole } from './boards-api'

interface BoardMembersDialogProps {
  board: BoardDetail
  teamId: string
  canAdmin: boolean
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function BoardMembersDialog({ board, teamId, canAdmin, open, onOpenChange }: BoardMembersDialogProps) {
  const me = useCurrentUser()
  const teamMembers = useTeamMembers(teamId)
  const addMember = useAddBoardMember(board.id)
  const removeMember = useRemoveBoardMember(board.id)
  const [userId, setUserId] = useState('')
  const [role, setRole] = useState<BoardRole>('Editor')

  const candidates = (teamMembers.data ?? []).filter(
    (member) => !board.members.some((boardMember) => boardMember.userId === member.userId),
  )

  const add = () => {
    const person = candidates.find((c) => c.userId === userId)
    if (!person) {
      return
    }
    addMember.mutate(
      { userId, role },
      {
        onSuccess: () => {
          toast.success(`${person.displayName} se unió al tablero como ${boardRoleLabels[role].toLowerCase()}`)
          setUserId('')
        },
        onError: (error) => toast.error(errorMessage(error)),
      },
    )
  }

  const remove = (memberId: string, name: string) => {
    removeMember.mutate(memberId, {
      onSuccess: () => toast.success(`${name} ya no es miembro del tablero`),
      onError: (error) => toast.error(errorMessage(error)),
    })
  }

  return (
    <Dialog
      open={open}
      onOpenChange={onOpenChange}
      title="Miembros del tablero"
      description={
        board.visibility === 'Team'
          ? 'Todo el equipo puede ver este tablero. Los miembros que agregues aquí pueden editarlo o administrarlo.'
          : 'Este tablero es privado: solo sus miembros pueden verlo.'
      }
      size="lg"
    >
      {canAdmin && (
        <div className="mb-5 rounded-xl border border-line bg-subtle p-3">
          <p className="mb-2 text-[13px] font-medium">Agregar a alguien del equipo</p>
          {candidates.length === 0 && !teamMembers.isPending ? (
            <p className="text-[13px] text-muted">Todas las personas del equipo ya son miembros del tablero.</p>
          ) : (
            <div className="flex flex-col gap-2 sm:flex-row">
              <div className="flex-1">
                <Select aria-label="Persona" value={userId} onChange={(event) => setUserId(event.target.value)}>
                  <option value="">Elige una persona…</option>
                  {candidates.map((member) => (
                    <option key={member.userId} value={member.userId}>
                      {member.displayName} · {member.email}
                    </option>
                  ))}
                </Select>
              </div>
              <div className="sm:w-40">
                <Select aria-label="Rol" value={role} onChange={(event) => setRole(event.target.value as BoardRole)}>
                  {[...boardRoles].reverse().map((r) => (
                    <option key={r} value={r}>
                      {boardRoleLabels[r]}
                    </option>
                  ))}
                </Select>
              </div>
              <Button onClick={add} disabled={!userId} loading={addMember.isPending} className="h-10">
                <UserPlus />
                Agregar
              </Button>
            </div>
          )}
          <p className="mt-2 text-xs text-muted">{boardRoleDescriptions[role]}.</p>
        </div>
      )}

      <ul className="divide-y divide-line">
        {board.members.map((member) => (
          <li key={member.userId} className="flex items-center gap-3 py-2.5">
            <Avatar id={member.userId} name={member.displayName} />
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-medium">
                {member.displayName}
                {member.userId === me.id && <span className="ml-1.5 text-faint">(tú)</span>}
              </p>
              <p className="text-xs text-muted">{boardRoleDescriptions[member.role]}</p>
            </div>
            <Badge tone={member.role === 'Admin' ? 'primary' : 'neutral'}>{boardRoleLabels[member.role]}</Badge>
            {canAdmin && member.userId !== me.id && (
              <Button
                variant="ghost"
                size="icon-sm"
                aria-label={`Quitar a ${member.displayName}`}
                loading={removeMember.isPending && removeMember.variables === member.userId}
                onClick={() => remove(member.userId, member.displayName)}
              >
                <UserMinus />
              </Button>
            )}
          </li>
        ))}
      </ul>
    </Dialog>
  )
}
