import { Link } from 'react-router'
import { cn } from '@/lib/utils/cn'

export function LogoMark({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 32 32" className={cn('size-7', className)} aria-hidden="true">
      <rect width="32" height="32" rx="8" className="fill-primary" />
      <rect x="7" y="8" width="5" height="16" rx="1.5" fill="#fff" />
      <rect x="13.5" y="8" width="5" height="11" rx="1.5" fill="#fff" fillOpacity=".8" />
      <rect x="20" y="8" width="5" height="7" rx="1.5" fill="#fff" fillOpacity=".6" />
    </svg>
  )
}

export function Logo({ className }: { className?: string }) {
  return (
    <Link to="/" className={cn('inline-flex items-center gap-2.5', className)}>
      <LogoMark />
      <span className="text-[17px] font-semibold tracking-tight">TaskBoard</span>
    </Link>
  )
}
