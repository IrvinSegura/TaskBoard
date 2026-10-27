import { Archive, ArchiveRestore, ChevronRight, Layers, MoreHorizontal, Pencil, Radio, Users } from 'lucide-react'
import { useState, type KeyboardEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { toast } from 'sonner'
import { HubConnectionState } from '@microsoft/signalr'
import { AvatarStack } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { ConfirmDialog } from '@/components/ui/confirm-dialog'
import { DropdownMenu, MenuItem, MenuSeparator } from '@/components/ui/dropdown-menu'
import { errorMessage } from '@/lib/api/api-error'
import { useRealtime } from '@/lib/realtime/realtime-provider'
import { cn } from '@/lib/utils/cn'
import { BoardMembersDialog } from './board-members-dialog'
import { BoardTabs } from './board-tabs'
import { boardRoleLabels, type BoardPermissions } from './board-permissions'
import { useArchiveBoard, useRenameBoard, useRestoreBoard, type BoardLocation } from './board-queries'
import { VisibilityLabel } from './board-tile'
import type { BoardDetail } from './boards-api'

interface BoardHeaderProps {
  board: BoardDetail
  location: BoardLocation | null
  permissions: BoardPermissions
}

export function BoardHeader({ board, location, permissions }: BoardHeaderProps) {
  const navigate = useNavigate()
  const teamId = location?.teamId
  const archived = location?.archived ?? false
  const rename = useRenameBoard(board.id, teamId)
  const archive = useArchiveBoard(board.id, teamId)
  const restore = useRestoreBoard(board.id, teamId)
  const [editing, setEditing] = useState(false)
  const [name, setName] = useState(board.name)
  const [confirmArchive, setConfirmArchive] = useState(false)
  const [showMembers, setShowMembers] = useState(false)
  const canRename = permissions.canAdmin && !archived

  const save = () => {
    setEditing(false)
    const trimmed = name.trim()
    if (!trimmed || trimmed === board.name) {
      setName(board.name)
      return
    }
    rename.mutate(trimmed, {
      onError: (error) => {
        setName(board.name)
        toast.error(errorMessage(error))
      },
    })
  }

  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter') {
      save()
    } else if (event.key === 'Escape') {
      setName(board.name)
      setEditing(false)
    }
  }

  const onArchive = () =>
    archive.mutate(undefined, {
      onSuccess: () => {
        setConfirmArchive(false)
        toast.success('Tablero archivado. Ahora es de solo lectura.')
      },
      onError: (error) => toast.error(errorMessage(error)),
    })

  const onRestore = () =>
    restore.mutate(undefined, {
      onSuccess: () => toast.success('Tablero restaurado'),
      onError: (error) => toast.error(errorMessage(error)),
    })

  return (
    <header className="border-b border-line bg-surface/70 px-4 py-3 backdrop-blur sm:px-6">
      <nav className="flex items-center gap-1 text-[13px] text-muted" aria-label="Ruta">
        {location ? (
          <>
            <Link to={`/teams/${location.teamId}`} className="truncate hover:text-fg">
              {location.teamName}
            </Link>
            <ChevronRight className="size-3.5 shrink-0" />
          </>
        ) : null}
        <span className="truncate text-fg">{board.name}</span>
      </nav>

      <div className="mt-1.5 flex flex-wrap items-center gap-x-3 gap-y-2">
        {editing ? (
          <input
            autoFocus
            value={name}
            maxLength={100}
            onChange={(event) => setName(event.target.value)}
            onBlur={save}
            onKeyDown={onKeyDown}
            aria-label="Nombre del tablero"
            className="h-9 min-w-0 rounded-lg border border-primary bg-surface px-2 text-xl font-semibold tracking-tight ring-3 ring-ring outline-none"
          />
        ) : (
          <h1
            className={cn('truncate text-xl font-semibold tracking-tight', canRename && 'cursor-text')}
            onDoubleClick={() => canRename && setEditing(true)}
          >
            {rename.isPending ? rename.variables : board.name}
          </h1>
        )}
        <div className="flex items-center gap-2 text-xs text-muted">
          <VisibilityLabel visibility={board.visibility} />
          {permissions.role && <Badge>{boardRoleLabels[permissions.role]}</Badge>}
          {archived && (
            <Badge tone="warning">
              <Archive />
              Archivado
            </Badge>
          )}
          <LiveIndicator />
        </div>

        <div className="ml-auto flex items-center gap-2">
          <button
            type="button"
            onClick={() => setShowMembers(true)}
            className="flex cursor-pointer items-center gap-2 rounded-lg px-1.5 py-1 hover:bg-muted-surface"
            aria-label="Ver miembros del tablero"
          >
            <AvatarStack people={board.members.map((m) => ({ id: m.userId, name: m.displayName }))} max={5} />
          </button>
          {location && permissions.canAdmin && (
            <Button variant="secondary" size="sm" onClick={() => setShowMembers(true)}>
              <Users />
              Compartir
            </Button>
          )}
          <DropdownMenu
            trigger={
              <Button variant="ghost" size="icon" aria-label="Opciones del tablero">
                <MoreHorizontal />
              </Button>
            }
          >
            {canRename && (
              <MenuItem
                icon={<Pencil />}
                onSelect={() => {
                  setName(board.name)
                  setEditing(true)
                }}
              >
                Renombrar
              </MenuItem>
            )}
            <MenuItem icon={<Users />} onSelect={() => setShowMembers(true)}>
              Miembros
            </MenuItem>
            <MenuItem icon={<Layers />} onSelect={() => navigate('archived')}>
              Tarjetas archivadas
            </MenuItem>
            {permissions.canAdmin && <MenuSeparator />}
            {permissions.canAdmin &&
              (archived ? (
                <MenuItem icon={<ArchiveRestore />} onSelect={onRestore}>
                  Restaurar tablero
                </MenuItem>
              ) : (
                <MenuItem icon={<Archive />} destructive onSelect={() => setConfirmArchive(true)}>
                  Archivar tablero
                </MenuItem>
              ))}
          </DropdownMenu>
        </div>
      </div>

      <BoardTabs boardId={board.id} />

      {location && (
        <BoardMembersDialog
          board={board}
          teamId={location.teamId}
          canAdmin={permissions.canAdmin && !archived}
          open={showMembers}
          onOpenChange={setShowMembers}
        />
      )}
      <ConfirmDialog
        open={confirmArchive}
        onOpenChange={setConfirmArchive}
        title="¿Archivar este tablero?"
        description="Quedará de solo lectura para todos y saldrá de la lista del equipo. Podrás restaurarlo desde la pestaña Archivados."
        confirmLabel="Archivar"
        loading={archive.isPending}
        onConfirm={onArchive}
      />
    </header>
  )
}

function LiveIndicator() {
  const { state } = useRealtime()
  const live = state === HubConnectionState.Connected
  return (
    <span
      className={cn('inline-flex items-center gap-1', live ? 'text-emerald-600 dark:text-emerald-400' : 'text-faint')}
      title={live ? 'Los cambios de otras personas aparecen al instante' : 'Reconectando…'}
    >
      <Radio className="size-3.5" />
      {live ? 'En vivo' : 'Sin conexión en vivo'}
    </span>
  )
}
