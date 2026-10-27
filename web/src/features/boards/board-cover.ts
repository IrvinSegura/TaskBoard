// Board covers: a gradient picked from the id, so each board keeps its look everywhere it appears.
const covers = [
  'from-indigo-500 via-indigo-600 to-violet-600',
  'from-sky-500 via-sky-600 to-indigo-600',
  'from-emerald-500 via-teal-600 to-cyan-700',
  'from-amber-500 via-orange-500 to-rose-500',
  'from-rose-500 via-pink-600 to-fuchsia-600',
  'from-violet-500 via-purple-600 to-indigo-700',
  'from-slate-600 via-slate-700 to-slate-800',
]

export function boardCover(id: string): string {
  let hash = 0
  for (let i = 0; i < id.length; i++) {
    hash = (hash * 37 + id.charCodeAt(i)) | 0
  }
  return covers[Math.abs(hash) % covers.length]!
}
