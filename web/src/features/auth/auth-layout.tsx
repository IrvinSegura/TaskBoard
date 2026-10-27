import { CheckCircle2, Radio, Users } from 'lucide-react'
import type { ReactNode } from 'react'
import { Logo } from '@/components/brand/logo'

interface AuthLayoutProps {
  title: string
  subtitle: ReactNode
  children: ReactNode
}

const highlights = [
  { icon: Radio, text: 'Cambios en tiempo real para todo el equipo' },
  { icon: Users, text: 'Equipos, roles y tableros privados o compartidos' },
  { icon: CheckCircle2, text: 'Tarjetas con prioridades, fechas, comentarios y adjuntos' },
]

export function AuthLayout({ title, subtitle, children }: AuthLayoutProps) {
  return (
    <div className="grid min-h-full lg:grid-cols-[minmax(0,1fr)_minmax(0,1.1fr)]">
      <main className="flex flex-col px-6 py-8 sm:px-12">
        <Logo />
        <div className="mx-auto flex w-full max-w-sm flex-1 flex-col justify-center py-12">
          <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>
          <p className="mt-2 text-sm text-muted">{subtitle}</p>
          <div className="mt-8">{children}</div>
        </div>
        <p className="text-xs text-faint">© {new Date().getFullYear()} TaskBoard</p>
      </main>

      <aside className="relative hidden overflow-hidden bg-linear-to-br from-indigo-600 via-indigo-700 to-violet-800 lg:flex lg:flex-col lg:justify-center lg:px-14">
        <div className="pointer-events-none absolute -top-32 -right-32 size-96 rounded-full bg-white/10 blur-3xl" />
        <div className="pointer-events-none absolute -bottom-40 -left-24 size-96 rounded-full bg-violet-400/20 blur-3xl" />
        <div className="relative max-w-lg">
          <h2 className="text-3xl leading-tight font-semibold tracking-tight text-white">
            Organiza el trabajo de tu equipo en un solo lugar.
          </h2>
          <ul className="mt-8 space-y-3">
            {highlights.map(({ icon: Icon, text }) => (
              <li key={text} className="flex items-center gap-3 text-[15px] text-indigo-100">
                <Icon className="size-5 text-indigo-200" />
                {text}
              </li>
            ))}
          </ul>
          <BoardPreview />
        </div>
      </aside>
    </div>
  )
}

const previewColumns = [
  { name: 'Por hacer', cards: [{ w: 'w-3/4', tag: 'bg-amber-300' }, { w: 'w-1/2', tag: 'bg-sky-300' }] },
  { name: 'En curso', cards: [{ w: 'w-2/3', tag: 'bg-rose-300' }, { w: 'w-4/5', tag: 'bg-emerald-300' }] },
  { name: 'Hecho', cards: [{ w: 'w-1/2', tag: 'bg-emerald-300' }] },
]

// A decorative miniature of a board, so the sign-in screen shows what the product looks like.
function BoardPreview() {
  return (
    <div className="mt-12 rounded-2xl border border-white/15 bg-white/10 p-4 shadow-2xl backdrop-blur-sm" aria-hidden="true">
      <div className="mb-3 flex items-center gap-2">
        <span className="size-2.5 rounded-full bg-white/40" />
        <span className="h-2.5 w-24 rounded-full bg-white/40" />
      </div>
      <div className="grid grid-cols-3 gap-3">
        {previewColumns.map((column) => (
          <div key={column.name} className="rounded-xl bg-white/10 p-2.5">
            <p className="mb-2 text-[11px] font-medium text-indigo-100">{column.name}</p>
            <div className="space-y-2">
              {column.cards.map((card, index) => (
                <div key={index} className="rounded-lg bg-white p-2.5 shadow-sm">
                  <span className={`block h-1.5 w-8 rounded-full ${card.tag}`} />
                  <span className={`mt-2 block h-2 rounded-full bg-slate-200 ${card.w}`} />
                  <span className="mt-1.5 block h-2 w-1/3 rounded-full bg-slate-100" />
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
