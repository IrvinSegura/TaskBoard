import { MessagesSquare } from 'lucide-react'
import { useState } from 'react'
import { Outlet, useParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils/cn'
import { ConversationList } from './conversation-list'
import { NewConversationDialog } from './new-conversation-dialog'

/** Conversation list on the left and the open conversation on the right; one at a time on small screens. */
export function ChatPage() {
  const { conversationId } = useParams()
  const [creating, setCreating] = useState(false)

  return (
    <div className="flex h-full min-h-0">
      <aside
        className={cn(
          'w-full shrink-0 border-r border-line bg-surface md:w-80 lg:w-96',
          conversationId ? 'hidden md:block' : 'block',
        )}
      >
        <ConversationList onNewConversation={() => setCreating(true)} />
      </aside>

      <section className={cn('min-w-0 flex-1 bg-canvas', conversationId ? 'flex' : 'hidden md:flex')}>
        {conversationId ? (
          <Outlet />
        ) : (
          <div className="m-auto flex max-w-sm flex-col items-center px-6 text-center">
            <div className="mb-4 flex size-14 items-center justify-center rounded-2xl bg-primary-soft text-primary">
              <MessagesSquare className="size-7" />
            </div>
            <h2 className="text-lg font-semibold">Tus mensajes</h2>
            <p className="mt-1 text-sm text-muted">
              Escribe a una persona, a un grupo o al canal de un equipo o tablero. Puedes enviar enlaces, imágenes,
              audios, videos y documentos.
            </p>
            <Button className="mt-5" onClick={() => setCreating(true)}>
              Nueva conversación
            </Button>
          </div>
        )}
      </section>

      <NewConversationDialog open={creating} onOpenChange={setCreating} />
    </div>
  )
}
