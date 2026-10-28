import { AtSign, UserRound, UsersRound } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { Dialog } from '@/components/ui/dialog'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Tabs } from '@/components/ui/tabs'
import { errorMessage } from '@/lib/api/api-error'
import { useChatContacts, useCreateGroup, useStartDirect } from './chat-queries'
import { PeoplePicker } from './people-picker'

type Mode = 'direct' | 'group'

interface NewConversationDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function NewConversationDialog({ open, onOpenChange }: NewConversationDialogProps) {
  const navigate = useNavigate()
  const contacts = useChatContacts(open)
  const startDirect = useStartDirect()
  const createGroup = useCreateGroup()
  const [mode, setMode] = useState<Mode>('direct')
  const [email, setEmail] = useState('')
  const [title, setTitle] = useState('')
  const [selected, setSelected] = useState<string[]>([])
  const pending = startDirect.isPending ? startDirect.variables : undefined
  const pendingUserId = pending && 'userId' in pending ? pending.userId : null

  const close = (next: boolean) => {
    onOpenChange(next)
    if (!next) {
      setEmail('')
      setTitle('')
      setSelected([])
      setMode('direct')
    }
  }

  const openConversation = (id: string) => {
    close(false)
    navigate(`/chat/${id}`)
  }

  const startWith = (target: { userId: string } | { email: string }) =>
    startDirect.mutate(target, {
      onSuccess: ({ id }) => openConversation(id),
      onError: (error) => toast.error(errorMessage(error)),
    })

  const submitEmail = (event: FormEvent) => {
    event.preventDefault()
    if (email.trim()) {
      startWith({ email: email.trim() })
    }
  }

  const submitGroup = (event: FormEvent) => {
    event.preventDefault()
    createGroup.mutate(
      { title: title.trim(), participantIds: selected },
      {
        onSuccess: ({ id }) => openConversation(id),
        onError: (error) => toast.error(errorMessage(error)),
      },
    )
  }

  return (
    <Dialog
      open={open}
      onOpenChange={close}
      title="Nueva conversación"
      description="Las conversaciones de tus equipos y tableros ya están en la lista."
      footer={
        mode === 'group' && (
          <>
            <Button variant="secondary" onClick={() => close(false)}>
              Cancelar
            </Button>
            <Button
              type="submit"
              form="new-group"
              loading={createGroup.isPending}
              disabled={!title.trim() || selected.length === 0}
            >
              Crear grupo{selected.length > 0 && ` (${selected.length + 1})`}
            </Button>
          </>
        )
      }
    >
      <Tabs
        className="mb-4"
        value={mode}
        onChange={setMode}
        items={[
          { value: 'direct', label: 'Mensaje directo', icon: <UserRound /> },
          { value: 'group', label: 'Grupo', icon: <UsersRound /> },
        ]}
      />

      {mode === 'direct' ? (
        <div className="space-y-4">
          <PeoplePicker
            people={contacts.data}
            loading={contacts.isPending}
            selected={pendingUserId ? [pendingUserId] : []}
            onToggle={(user) => startWith({ userId: user.id })}
            emptyText="Todavía no compartes equipos ni tableros con nadie."
          />
          <form onSubmit={submitEmail} className="flex items-end gap-2">
            <Field label="O escribe a cualquier usuario por email" className="flex-1">
              <Input
                type="email"
                placeholder="nombre@empresa.com"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
              />
            </Field>
            <Button
              type="submit"
              variant="secondary"
              className="h-10"
              loading={startDirect.isPending && !pendingUserId}
              disabled={!email.trim()}
            >
              <AtSign />
              Escribir
            </Button>
          </form>
        </div>
      ) : (
        <form id="new-group" onSubmit={submitGroup} className="space-y-4">
          <Field label="Nombre del grupo">
            <Input
              maxLength={100}
              placeholder="Lanzamiento de octubre"
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              autoFocus
            />
          </Field>
          <PeoplePicker
            multiple
            people={contacts.data}
            loading={contacts.isPending}
            selected={selected}
            onToggle={(user) =>
              setSelected((ids) => (ids.includes(user.id) ? ids.filter((id) => id !== user.id) : [...ids, user.id]))
            }
            emptyText="Todavía no compartes equipos ni tableros con nadie."
          />
        </form>
      )}
    </Dialog>
  )
}
