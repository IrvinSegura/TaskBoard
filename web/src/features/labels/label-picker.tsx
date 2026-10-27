import * as RadixMenu from '@radix-ui/react-dropdown-menu'
import { Check, Settings, Tag } from 'lucide-react'
import { useNavigate } from 'react-router'
import { toast } from 'sonner'
import { Button } from '@/components/ui/button'
import { DropdownMenu, MenuItem, MenuLabel, MenuSeparator } from '@/components/ui/dropdown-menu'
import { errorMessage } from '@/lib/api/api-error'
import { cn } from '@/lib/utils/cn'
import { LabelChip } from './label-chip'
import { labelColorMeta, labelsByIds, type BoardLabel } from './labels-api'
import { useToggleCardLabel } from './label-queries'

interface LabelPickerProps {
  cardId: string
  boardId: string
  labels: BoardLabel[]
  labelIds: string[]
  canEdit: boolean
}

/** A card's labels, and the menu to add or remove board labels. */
export function LabelPicker({ cardId, boardId, labels, labelIds, canEdit }: LabelPickerProps) {
  const navigate = useNavigate()
  const toggle = useToggleCardLabel(cardId, boardId)
  const applied = labelsByIds(labels, labelIds)
  const appliedIds = new Set(labelIds)

  return (
    <div className="space-y-2">
      {applied.length === 0 ? (
        <p className="text-sm text-faint">Sin etiquetas.</p>
      ) : (
        <div className="flex flex-wrap gap-1.5">
          {applied.map((label) => (
            <LabelChip key={label.id} label={label} />
          ))}
        </div>
      )}
      {canEdit && (
        <DropdownMenu
          align="start"
          trigger={
            <Button variant="secondary" size="sm" className="w-full">
              <Tag />
              Etiquetas
            </Button>
          }
        >
          <MenuLabel>Etiquetas del tablero</MenuLabel>
          {labels.length === 0 && <p className="px-2.5 py-1.5 text-sm text-faint">El tablero aún no tiene etiquetas.</p>}
          {labels.map((label) => {
            const added = appliedIds.has(label.id)
            return (
              <RadixMenu.CheckboxItem
                key={label.id}
                checked={added}
                // Keep the menu open to toggle several labels.
                onSelect={(event) => event.preventDefault()}
                onCheckedChange={() =>
                  toggle.mutate({ labelId: label.id, added }, { onError: (error) => toast.error(errorMessage(error)) })
                }
                className="flex cursor-pointer items-center gap-2.5 rounded-lg px-2.5 py-1.5 text-sm outline-none select-none data-[highlighted]:bg-muted-surface"
              >
                <span className={cn('size-3 shrink-0 rounded-sm', labelColorMeta[label.color].swatch)} />
                <span className="flex-1 truncate">{label.name}</span>
                {added && <Check className="size-4 text-primary" />}
              </RadixMenu.CheckboxItem>
            )
          })}
          <MenuSeparator />
          <MenuItem icon={<Settings />} onSelect={() => navigate(`/boards/${boardId}/settings`)}>
            Gestionar etiquetas
          </MenuItem>
        </DropdownMenu>
      )}
    </div>
  )
}
