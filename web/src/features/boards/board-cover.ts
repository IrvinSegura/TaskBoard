// Board covers: a gradient picked from the id, so each board keeps its look everywhere it appears.
const covers = [
  'from-indigo-500 via-indigo-600 to-violet-600',
  'from-sky-500 via-sky-600 to-indigo-600',
  'from-emerald-500 via-teal-600 to-cyan-700',
  'from-amber-500 via-orange-500 to-rose-500',
  'from-rose-500 via-pink-600 to-fuchsia-600',
  'from-violet-500 via-purple-600 to-indigo-700',
  'from-cyan-500 via-sky-600 to-blue-700',
]

// FNV-1a: ids are UUID v7, whose shared time prefix made a plain multiply-and-add hash pick the same few covers.
export function boardCover(id: string): string {
  let hash = 0x811c9dc5
  for (let i = 0; i < id.length; i++) {
    hash ^= id.charCodeAt(i)
    hash = Math.imul(hash, 0x01000193)
  }
  return covers[(hash >>> 0) % covers.length]!
}
