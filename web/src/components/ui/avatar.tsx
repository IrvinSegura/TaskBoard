import { cn } from '@/lib/utils/cn'
import { initials } from '@/lib/utils/format'

// Stable color per person, so the same user looks the same on every board.
const palette = [
  'bg-indigo-500',
  'bg-sky-500',
  'bg-emerald-500',
  'bg-amber-500',
  'bg-rose-500',
  'bg-violet-500',
  'bg-teal-500',
  'bg-orange-500',
  'bg-fuchsia-500',
  'bg-cyan-600',
]

function colorFor(seed: string): string {
  let hash = 0
  for (let i = 0; i < seed.length; i++) {
    hash = (hash * 31 + seed.charCodeAt(i)) | 0
  }
  return palette[Math.abs(hash) % palette.length]!
}

const sizes = {
  xs: 'size-5 text-[9px]',
  sm: 'size-6 text-[10px]',
  md: 'size-8 text-xs',
  lg: 'size-10 text-sm',
  xl: 'size-16 text-xl',
}

type AvatarSize = keyof typeof sizes

interface AvatarProps {
  name: string
  id?: string
  size?: AvatarSize
  className?: string
}

export function Avatar({ name, id, size = 'md', className }: AvatarProps) {
  return (
    <span
      title={name}
      className={cn(
        'inline-flex shrink-0 items-center justify-center rounded-full font-semibold text-white select-none',
        colorFor(id ?? name),
        sizes[size],
        className,
      )}
    >
      {initials(name)}
    </span>
  )
}

interface AvatarStackProps {
  people: { id: string; name: string }[]
  max?: number
  size?: AvatarSize
}

export function AvatarStack({ people, max = 4, size = 'sm' }: AvatarStackProps) {
  const visible = people.slice(0, max)
  const hidden = people.length - visible.length
  return (
    <div className="flex items-center -space-x-1.5">
      {visible.map((person) => (
        <Avatar key={person.id} id={person.id} name={person.name} size={size} className="ring-2 ring-surface" />
      ))}
      {hidden > 0 && (
        <span
          className={cn(
            'inline-flex items-center justify-center rounded-full bg-muted-surface font-semibold text-muted ring-2 ring-surface',
            sizes[size],
          )}
        >
          +{hidden}
        </span>
      )}
    </div>
  )
}
