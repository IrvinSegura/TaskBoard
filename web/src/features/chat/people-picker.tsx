import { Check, Search } from 'lucide-react'
import { useState } from 'react'
import { Avatar } from '@/components/ui/avatar'
import { Skeleton } from '@/components/ui/feedback'
import { cn } from '@/lib/utils/cn'
import type { ChatUser } from './chat-api'

interface PeoplePickerProps {
  people: ChatUser[] | undefined
  loading: boolean
  /** Single choice picks on click; multiple choice toggles. */
  selected: string[]
  onToggle: (user: ChatUser) => void
  multiple?: boolean
  emptyText: string
}

export function PeoplePicker({ people, loading, selected, onToggle, multiple = false, emptyText }: PeoplePickerProps) {
  const [search, setSearch] = useState('')
  const needle = search.trim().toLowerCase()
  const visible = (people ?? []).filter(
    (p) => !needle || p.displayName.toLowerCase().includes(needle) || p.email.toLowerCase().includes(needle),
  )

  return (
    <div className="space-y-2">
      <label className="relative block">
        <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-faint" />
        <input
          type="search"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Buscar por nombre o email"
          className="h-9 w-full rounded-lg border border-line bg-surface pr-3 pl-9 text-sm placeholder:text-faint focus:border-primary focus:ring-3 focus:ring-ring focus:outline-none"
        />
      </label>
      <div
        className="scrollbar-thin max-h-64 overflow-y-auto rounded-lg border border-line"
        role="listbox"
        aria-multiselectable={multiple}
      >
        {loading ? (
          <div className="space-y-2 p-2">
            <Skeleton className="h-10" />
            <Skeleton className="h-10" />
          </div>
        ) : visible.length === 0 ? (
          <p className="px-3 py-6 text-center text-sm text-faint">{needle ? 'Nadie coincide con la búsqueda.' : emptyText}</p>
        ) : (
          visible.map((person) => {
            const isSelected = selected.includes(person.id)
            return (
              <button
                key={person.id}
                type="button"
                role="option"
                aria-selected={isSelected}
                onClick={() => onToggle(person)}
                className={cn(
                  'flex w-full cursor-pointer items-center gap-3 px-3 py-2 text-left transition-colors not-last:border-b not-last:border-line',
                  isSelected ? 'bg-primary-soft' : 'hover:bg-subtle',
                )}
              >
                <Avatar id={person.id} name={person.displayName} />
                <span className="min-w-0 flex-1">
                  <span className="block truncate text-sm font-medium">{person.displayName}</span>
                  <span className="block truncate text-xs text-muted">{person.email}</span>
                </span>
                {multiple && (
                  <span
                    className={cn(
                      'flex size-5 shrink-0 items-center justify-center rounded-md border',
                      isSelected ? 'border-primary bg-primary text-primary-fg' : 'border-line-strong',
                    )}
                  >
                    {isSelected && <Check className="size-3.5" />}
                  </span>
                )}
              </button>
            )
          })
        )}
      </div>
    </div>
  )
}
