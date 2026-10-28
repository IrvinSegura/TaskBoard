import { ArrowRight, ChevronDown, Keyboard, LifeBuoy, Search, ShieldCheck, Sparkles, X } from 'lucide-react'
import { Fragment, useMemo, useState, type ReactNode } from 'react'
import { Link } from 'react-router'
import { PageContainer, PageHeader } from '@/components/layout/page-header'
import { boardRoles, faqs, quickStart, searchFaqs, shortcuts, teamRoles, type RoleRow } from './help-content'

export function HelpPage() {
  const [query, setQuery] = useState('')
  const results = useMemo(() => searchFaqs(faqs, query), [query])
  const topics = useMemo(() => [...new Set(results.map((faq) => faq.topic))], [results])
  const searching = query.trim().length > 0

  return (
    <PageContainer>
      <PageHeader title="Ayuda" description="Guía rápida, atajos de teclado y respuestas a las dudas más comunes." />

      <div className="relative mt-6 max-w-2xl">
        <Search className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-faint" />
        <input
          type="search"
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Busca una pregunta: archivar, menciones, favoritos…"
          aria-label="Buscar en la ayuda"
          className="h-11 w-full rounded-xl border border-line bg-surface pr-10 pl-10 text-sm shadow-card outline-none placeholder:text-faint focus:border-primary focus:ring-3 focus:ring-ring [&::-webkit-search-cancel-button]:hidden"
        />
        {searching && (
          <button
            type="button"
            onClick={() => setQuery('')}
            className="absolute top-1/2 right-2 flex size-7 -translate-y-1/2 cursor-pointer items-center justify-center rounded-md text-faint hover:bg-muted-surface hover:text-fg"
            aria-label="Borrar búsqueda"
          >
            <X className="size-4" />
          </button>
        )}
      </div>

      {!searching && (
        <>
          <Section icon={Sparkles} title="Primeros pasos">
            <ol className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              {quickStart.map((step, index) => (
                <li key={step.title} className="flex flex-col rounded-xl border border-line bg-surface p-4 shadow-card">
                  <span className="flex size-7 items-center justify-center rounded-full bg-primary-soft text-[13px] font-semibold text-primary">
                    {index + 1}
                  </span>
                  <h3 className="mt-3 text-sm font-semibold">{step.title}</h3>
                  <p className="mt-1 flex-1 text-[13px] text-muted">{step.description}</p>
                  <Link
                    to={step.to}
                    className="mt-3 inline-flex items-center gap-1 text-[13px] font-medium text-primary hover:underline"
                  >
                    {step.linkLabel}
                    <ArrowRight className="size-3.5" />
                  </Link>
                </li>
              ))}
            </ol>
          </Section>

          <Section icon={Keyboard} title="Atajos de teclado">
            <div className="overflow-hidden rounded-xl border border-line bg-surface shadow-card">
              <table className="w-full text-left text-sm">
                <thead className="bg-subtle text-[11px] font-semibold tracking-wider text-faint uppercase">
                  <tr>
                    <th scope="col" className="px-4 py-2.5">Teclas</th>
                    <th scope="col" className="px-4 py-2.5">Acción</th>
                    <th scope="col" className="hidden px-4 py-2.5 md:table-cell">Dónde</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-line">
                  {shortcuts.map((shortcut) => (
                    <tr key={shortcut.action}>
                      <td className="px-4 py-2.5 whitespace-nowrap">
                        <Keys combos={shortcut.keys} />
                      </td>
                      <td className="px-4 py-2.5">
                        {shortcut.action}
                        <span className="block text-xs text-muted md:hidden">{shortcut.where}</span>
                      </td>
                      <td className="hidden px-4 py-2.5 text-muted md:table-cell">{shortcut.where}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Section>

          <Section icon={ShieldCheck} title="Roles y permisos">
            <div className="grid gap-4 lg:grid-cols-2">
              <RoleTable title="En un equipo" rows={teamRoles} />
              <RoleTable title="En un tablero" rows={boardRoles} />
            </div>
          </Section>
        </>
      )}

      <Section icon={LifeBuoy} title={searching ? `Resultados (${results.length})` : 'Preguntas frecuentes'}>
        {results.length === 0 ? (
          <p className="rounded-xl border border-dashed border-line-strong px-4 py-8 text-center text-sm text-muted">
            No encontramos nada sobre «{query.trim()}». Prueba con otra palabra.
          </p>
        ) : (
          <div className="space-y-6">
            {topics.map((topic) => (
              <div key={topic}>
                <h3 className="mb-2 text-[11px] font-semibold tracking-wider text-faint uppercase">{topic}</h3>
                <div className="divide-y divide-line overflow-hidden rounded-xl border border-line bg-surface shadow-card">
                  {results
                    .filter((faq) => faq.topic === topic)
                    .map((faq) => (
                      // Open while searching, so the matching answers are visible at once.
                      <details key={`${faq.id}-${searching}`} open={searching} className="group">
                        <summary className="flex cursor-pointer list-none items-center gap-3 px-4 py-3 text-sm font-medium hover:bg-subtle [&::-webkit-details-marker]:hidden">
                          <span className="flex-1">{faq.question}</span>
                          <ChevronDown className="size-4 shrink-0 text-faint transition-transform group-open:rotate-180" />
                        </summary>
                        <p className="px-4 pb-4 text-sm leading-relaxed text-muted">{faq.answer}</p>
                      </details>
                    ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </Section>

      <div className="mt-10 flex flex-col items-start gap-3 rounded-xl border border-line bg-linear-to-br from-primary-soft to-surface p-5 sm:flex-row sm:items-center">
        <div className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-primary text-primary-fg">
          <LifeBuoy className="size-5" />
        </div>
        <div className="flex-1">
          <p className="text-sm font-semibold">¿Sigues con dudas?</p>
          <p className="text-[13px] text-muted">
            Pregunta a un administrador de tu equipo: puede revisar tus permisos y la actividad del tablero.
          </p>
        </div>
        <Link to="/activity" className="text-sm font-medium text-primary hover:underline">
          Ver la actividad reciente
        </Link>
      </div>
    </PageContainer>
  )
}

function Section({ icon: Icon, title, children }: { icon: typeof Sparkles; title: string; children: ReactNode }) {
  return (
    <section className="mt-10">
      <h2 className="mb-4 flex items-center gap-2 text-[15px] font-semibold">
        <Icon className="size-4 text-primary" />
        {title}
      </h2>
      {children}
    </section>
  )
}

/** Alternative key combinations, separated by "o"; keys of one combination joined by "+". */
function Keys({ combos }: { combos: string[][] }) {
  return (
    <span className="inline-flex flex-wrap items-center gap-1 text-xs text-faint">
      {combos.map((combo, i) => (
        <Fragment key={combo.join('+')}>
          {i > 0 && <span className="px-0.5">o</span>}
          {combo.map((key, j) => (
            <Fragment key={key}>
              {j > 0 && <span>+</span>}
              <kbd className="inline-flex h-6 min-w-6 items-center justify-center rounded-md border border-b-2 border-line bg-subtle px-1.5 font-sans text-xs font-medium text-fg">
                {key}
              </kbd>
            </Fragment>
          ))}
        </Fragment>
      ))}
    </span>
  )
}

function RoleTable({ title, rows }: { title: string; rows: RoleRow[] }) {
  return (
    <div className="overflow-hidden rounded-xl border border-line bg-surface shadow-card">
      <h3 className="border-b border-line bg-subtle px-4 py-2.5 text-[13px] font-semibold">{title}</h3>
      <dl className="divide-y divide-line">
        {rows.map((row) => (
          <div key={row.role} className="grid gap-1 px-4 py-3 sm:grid-cols-[8rem_1fr] sm:gap-4">
            <dt className="text-sm font-medium">{row.role}</dt>
            <dd className="text-[13px] text-muted">{row.can}</dd>
          </div>
        ))}
      </dl>
    </div>
  )
}
