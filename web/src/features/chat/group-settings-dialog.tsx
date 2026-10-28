import { LogOut, UserPlus } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { Avatar } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { ConfirmDialog } from '@/components/ui/confirm-dialog'
import { Dialog } from '@/components/ui/dialog'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { useCurrentUser } from '@/features/auth/auth-provider'
import { errorMessage } from '@/lib/api/api-error'
import type { Conversation } from './chat-api'
import { useAddParticipants, useChatContacts, useLeaveGroup, useRenameGroup } from './chat-queries'
import { PeoplePicker } from './people-picker'

interface GroupSettingsDialogProps {
  conversation: Conversation
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function GroupSettingsDialog({ conversation, open, onOpenChange }: GroupSettingsDialogProps) {
  const me = useCurrentUser()
  const navigate = useNavigate()
  const rename = useRenameGroup(conversation.id)
  const addParticipants = useAddParticipants(conversation.id)
  const leave = useLeaveGroup(conversation.id)
  const [title, setTitle] = useState(conversation.title)
  const [adding, setAdding] = useState(false)
  const [selected, setSelected] = useState<string[]>([])
  const [confirmLeave, setConfirmLeave] = useState(false)
  const contacts = useChatContacts(open && adding)
  const inGroup = new Set(conversation.participants.map((p) => p.id))

  const submitTitle = (event: FormEvent) => {
    event.preventDefault()
    rename.mutate(title.trim(), {
      onSuccess: () => toast.success('Nombre actualizado'),
      onError: (error) => toast.error(errorMessage(error)),
    })
  }

  const addSelected = () =>
    addParticipants.mutate(selected, {
      onSuccess: () => {
        toast.success(selected.length === 1 ? 'Persona añadida' : `${selected.length} personas añadidas`)
        setSelected([])
        setAdding(false)
      },
      onError: (error) => toast.error(errorMessage(error)),
    })

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange} title="Ajustes del grupo">
        <div className="space-y-6">
          <form onSubmit={submitTitle} className="flex items-end gap-2">
            <Field label="Nombre" className="flex-1">
              <Input maxLength={100} value={title} onChange={(event) => setTitle(event.target.value)} />
            </Field>
            <Button
              type="submit"
              variant="secondary"
              className="h-10"
              loading={rename.isPending}
              disabled={!title.trim() || title.trim() === conversation.title}
            >
              Guardar
            </Button>
          </form>

          <section>
            <div className="mb-2 flex items-center justify-between">
              <h3 className="text-[13px] font-medium">{conversation.participants.length} personas</h3>
              {!adding && (
                <Button variant="ghost" size="sm" onClick={() => setAdding(true)}>
                  <UserPlus />
                  Añadir
                </Button>
              )}
            </div>

            {adding ? (
              <div className="space-y-3">
                <PeoplePicker
                  multiple
                  people={contacts.data?.filter((c) => !inGroup.has(c.id))}
                  loading={contacts.isPending}
                  selected={selected}
                  onToggle={(user) =>
                    setSelected((ids) => (ids.includes(user.id) ? ids.filter((id) => id !== user.id) : [...ids, user.id]))
                  }
                  emptyText="Todas las personas de tus equipos ya están en el grupo."
                />
                <div className="flex justify-end gap-2">
                  <Button variant="ghost" size="sm" onClick={() => setAdding(false)}>
                    Cancelar
                  </Button>
                  <Button size="sm" onClick={addSelected} loading={addParticipants.isPending} disabled={selected.length === 0}>
                    Añadir al grupo
                  </Button>
                </div>
              </div>
            ) : (
              <ul className="divide-y divide-line rounded-lg border border-line">
                {conversation.participants.map((person) => (
                  <li key={person.id} className="flex items-center gap-3 px-3 py-2">
                    <Avatar id={person.id} name={person.displayName} />
                    <span className="min-w-0 flex-1">
                      <span className="block truncate text-sm font-medium">
                        {person.displayName}
                        {person.id === me.id && <span className="ml-1 font-normal text-muted">(tú)</span>}
                      </span>
                      <span className="block truncate text-xs text-muted">{person.email}</span>
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </section>

          <Button variant="ghost" className="w-full text-danger hover:bg-danger-soft hover:text-danger" onClick={() => setConfirmLeave(true)}>
            <LogOut />
            Salir del grupo
          </Button>
        </div>
      </Dialog>

      <ConfirmDialog
        open={confirmLeave}
        onOpenChange={setConfirmLeave}
        title="¿Salir del grupo?"
        description="Dejarás de ver sus mensajes. Alguien del grupo puede volver a añadirte."
        confirmLabel="Salir"
        loading={leave.isPending}
        onConfirm={() =>
          leave.mutate(undefined, {
            onSuccess: () => {
              setConfirmLeave(false)
              onOpenChange(false)
              navigate('/chat')
            },
            onError: (error) => toast.error(errorMessage(error)),
          })
        }
      />
    </>
  )
}
