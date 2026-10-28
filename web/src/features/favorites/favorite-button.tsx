import { Star } from 'lucide-react'
import type { MouseEvent } from 'react'
import { cn } from '@/lib/utils/cn'
import { useFavorite, type FavoriteKind } from './favorites-api'

interface FavoriteButtonProps {
  kind: FavoriteKind
  id: string
  name: string
  /** "cover" sits on a colored cover (white star on a frosted chip); "plain" sits on the page surface. */
  tone?: 'cover' | 'plain'
  className?: string
}

const tones = {
  cover: {
    base: 'bg-black/20 text-white ring-1 ring-white/25 backdrop-blur-sm ring-inset hover:bg-black/35',
    on: 'text-amber-300',
  },
  plain: {
    base: 'text-faint hover:bg-muted-surface hover:text-fg',
    on: 'text-amber-500 hover:text-amber-500 dark:text-amber-400',
  },
}

/** Star toggle for a board or a team. Never place it inside a link: it is a button of its own. */
export function FavoriteButton({ kind, id, name, tone = 'plain', className }: FavoriteButtonProps) {
  const { isFavorite, ready, toggle } = useFavorite(kind, id)
  const label = isFavorite ? `Quitar ${name} de favoritos` : `Añadir ${name} a favoritos`

  const onClick = (event: MouseEvent<HTMLButtonElement>) => {
    event.preventDefault()
    event.stopPropagation()
    toggle()
  }

  return (
    <button
      type="button"
      onClick={onClick}
      disabled={!ready}
      aria-pressed={isFavorite}
      aria-label={label}
      title={isFavorite ? 'Quitar de favoritos' : 'Añadir a favoritos'}
      className={cn(
        'inline-flex size-8 shrink-0 cursor-pointer items-center justify-center rounded-lg transition-colors disabled:cursor-default disabled:opacity-60',
        tones[tone].base,
        isFavorite && tones[tone].on,
        className,
      )}
    >
      <Star className={cn('size-4 transition-transform active:scale-90', isFavorite && 'fill-current')} />
    </button>
  )
}
