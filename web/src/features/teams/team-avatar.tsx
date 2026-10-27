import { cn } from '@/lib/utils/cn'

const gradients = [
  'from-indigo-500 to-violet-600',
  'from-sky-500 to-indigo-600',
  'from-emerald-500 to-teal-600',
  'from-amber-500 to-orange-600',
  'from-rose-500 to-pink-600',
  'from-fuchsia-500 to-purple-600',
]

function gradientFor(id: string): string {
  let hash = 0
  for (let i = 0; i < id.length; i++) {
    hash = (hash * 33 + id.charCodeAt(i)) | 0
  }
  return gradients[Math.abs(hash) % gradients.length]!
}

interface TeamAvatarProps {
  name: string
  id: string
  size?: 'sm' | 'md' | 'lg' | 'xl'
  className?: string
}

const sizes = {
  sm: 'size-6 rounded-md text-[11px]',
  md: 'size-10 rounded-lg text-base',
  lg: 'size-12 rounded-xl text-lg',
  xl: 'size-16 rounded-2xl text-2xl',
}

export function TeamAvatar({ name, id, size = 'md', className }: TeamAvatarProps) {
  return (
    <span
      className={cn(
        'inline-flex shrink-0 items-center justify-center bg-linear-to-br font-semibold text-white shadow-card',
        gradientFor(id),
        sizes[size],
        className,
      )}
      aria-hidden="true"
    >
      {name.trim().charAt(0).toUpperCase() || '?'}
    </span>
  )
}
