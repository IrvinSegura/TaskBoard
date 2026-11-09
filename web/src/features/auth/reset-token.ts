/** The token in the emailed link. It travels in the fragment, which browsers never send to any server. */
export function readResetToken(hash: string): string | null {
  return new URLSearchParams(hash.replace(/^#/, '')).get('token') || null
}
