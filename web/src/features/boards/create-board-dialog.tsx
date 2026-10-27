import { zodResolver } from '@hookform/resolvers/zod'
import { Lock, Users } from 'lucide-react'
import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { z } from 'zod'
import { Alert } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Dialog } from '@/components/ui/dialog'
import { Field } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { applyServerErrors } from '@/lib/forms'
import { cn } from '@/lib/utils/cn'
import { useCreateBoard } from './board-queries'

const schema = z.object({
  name: z.string().trim().min(1, 'Ponle un nombre al tablero.').max(100, 'Máximo 100 caracteres.'),
  visibility: z.enum(['Team', 'Private']),
})

type CreateBoardValues = z.infer<typeof schema>

const visibilityOptions = [
  {
    value: 'Team' as const,
    icon: Users,
    title: 'Equipo',
    description: 'Todo el equipo lo ve; los miembros que agregues pueden editar.',
  },
  {
    value: 'Private' as const,
    icon: Lock,
    title: 'Privado',
    description: 'Solo lo ven las personas que agregues al tablero.',
  },
]

interface CreateBoardDialogProps {
  teamId: string
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function CreateBoardDialog({ teamId, open, onOpenChange }: CreateBoardDialogProps) {
  const navigate = useNavigate()
  const createBoard = useCreateBoard(teamId)
  const [formError, setFormError] = useState<string | null>(null)
  const { register, handleSubmit, setError, reset, control, setValue, formState } = useForm<CreateBoardValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: '', visibility: 'Team' },
  })
  const visibility = useWatch({ control, name: 'visibility' })

  const close = (next: boolean) => {
    if (!next) {
      reset()
      setFormError(null)
    }
    onOpenChange(next)
  }

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null)
    try {
      const { id } = await createBoard.mutateAsync(values)
      toast.success(`Tablero «${values.name}» creado`)
      close(false)
      navigate(`/boards/${id}`)
    } catch (error) {
      setFormError(applyServerErrors(error, setError, ['name', 'visibility']))
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={close}
      title="Nuevo tablero"
      description="Serás administrador del tablero."
      size="lg"
      footer={
        <>
          <Button variant="secondary" onClick={() => close(false)}>
            Cancelar
          </Button>
          <Button type="submit" form="create-board-form" loading={formState.isSubmitting}>
            Crear tablero
          </Button>
        </>
      }
    >
      <form id="create-board-form" onSubmit={onSubmit} className="space-y-5" noValidate>
        {formError && <Alert>{formError}</Alert>}
        <Field label="Nombre" error={formState.errors.name?.message}>
          <Input placeholder="Ej. Lanzamiento v2, Sprint 14" autoFocus {...register('name')} />
        </Field>
        <fieldset>
          <legend className="mb-1.5 text-[13px] font-medium">Visibilidad</legend>
          <div className="grid gap-3 sm:grid-cols-2" role="radiogroup">
            {visibilityOptions.map(({ value, icon: Icon, title, description }) => (
              <button
                key={value}
                type="button"
                role="radio"
                aria-checked={visibility === value}
                onClick={() => setValue('visibility', value)}
                className={cn(
                  'flex cursor-pointer gap-3 rounded-xl border p-3.5 text-left transition-colors',
                  visibility === value
                    ? 'border-primary bg-primary-soft ring-1 ring-primary'
                    : 'border-line hover:border-line-strong',
                )}
              >
                <Icon className={cn('mt-0.5 size-4 shrink-0', visibility === value ? 'text-primary' : 'text-muted')} />
                <span>
                  <span className="block text-sm font-medium">{title}</span>
                  <span className="mt-0.5 block text-[13px] text-muted">{description}</span>
                </span>
              </button>
            ))}
          </div>
        </fieldset>
      </form>
    </Dialog>
  )
}
