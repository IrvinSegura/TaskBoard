import { useRef, useState, type KeyboardEvent } from 'react'
import { Avatar } from '@/components/ui/avatar'
import { Textarea } from '@/components/ui/input'
import type { BoardMember } from '@/features/boards/boards-api'
import { cn } from '@/lib/utils/cn'
import { mentionQuery, normalize, type Mention } from './mentions'

interface MentionTextareaProps {
  value: string
  onChange: (value: string) => void
  mentions: Mention[]
  onMentionsChange: (mentions: Mention[]) => void
  members: BoardMember[]
  /** Keys the list does not use (Ctrl+Enter, Escape...). */
  onKeyDown?: (event: KeyboardEvent<HTMLTextAreaElement>) => void
  placeholder?: string
  autoFocus?: boolean
  maxLength?: number
  'aria-label': string
}

/** A textarea that suggests board members after "@" and remembers the ones picked. */
export function MentionTextarea({
  value,
  onChange,
  mentions,
  onMentionsChange,
  members,
  onKeyDown,
  ...props
}: MentionTextareaProps) {
  const ref = useRef<HTMLTextAreaElement>(null)
  const [query, setQuery] = useState<{ start: number; query: string } | null>(null)
  const [active, setActive] = useState(0)

  const matches = query
    ? members.filter((member) => normalize(member.displayName).includes(normalize(query.query))).slice(0, 6)
    : []
  const open = matches.length > 0

  const updateQuery = (text: string, caret: number) => {
    setQuery(mentionQuery(text, caret))
    setActive(0)
  }

  const pick = (member: BoardMember) => {
    if (!query) {
      return
    }
    const caret = query.start + query.query.length + 1
    const inserted = `@${member.displayName} `
    const next = value.slice(0, query.start) + inserted + value.slice(caret)
    onChange(next)
    if (!mentions.some((m) => m.id === member.userId)) {
      onMentionsChange([...mentions, { id: member.userId, name: member.displayName }])
    }
    setQuery(null)
    const position = query.start + inserted.length
    requestAnimationFrame(() => {
      ref.current?.focus()
      ref.current?.setSelectionRange(position, position)
    })
  }

  const handleKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (open) {
      if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
        event.preventDefault()
        setActive((index) => (index + (event.key === 'ArrowDown' ? 1 : matches.length - 1)) % matches.length)
        return
      }
      if (event.key === 'Enter' || event.key === 'Tab') {
        event.preventDefault()
        pick(matches[active]!)
        return
      }
      if (event.key === 'Escape') {
        event.preventDefault()
        event.stopPropagation()
        setQuery(null)
        return
      }
    }
    onKeyDown?.(event)
  }

  return (
    <div className="relative">
      <Textarea
        ref={ref}
        rows={2}
        value={value}
        onChange={(event) => {
          onChange(event.target.value)
          updateQuery(event.target.value, event.target.selectionStart)
        }}
        onKeyDown={handleKeyDown}
        onClick={(event) => updateQuery(value, event.currentTarget.selectionStart)}
        onBlur={() => setTimeout(() => setQuery(null), 150)}
        className="min-h-16"
        role="combobox"
        aria-expanded={open}
        aria-autocomplete="list"
        {...props}
      />
      {open && (
        <ul
          role="listbox"
          aria-label="Mencionar a"
          className="absolute top-full left-0 z-20 mt-1 w-64 overflow-hidden rounded-xl border border-line bg-surface p-1 shadow-overlay"
        >
          {matches.map((member, index) => (
            <li
              key={member.userId}
              role="option"
              aria-selected={index === active}
              // mousedown, not click: it runs before the textarea's blur closes the list.
              onMouseDown={(event) => {
                event.preventDefault()
                pick(member)
              }}
              onMouseEnter={() => setActive(index)}
              className={cn(
                'flex cursor-pointer items-center gap-2.5 rounded-lg px-2.5 py-1.5 text-sm',
                index === active && 'bg-muted-surface',
              )}
            >
              <Avatar id={member.userId} name={member.displayName} size="xs" />
              <span className="truncate">{member.displayName}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
