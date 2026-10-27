// The API writes notification messages in English; these patterns turn the known ones into Spanish.
const translations: [RegExp, (name: string) => string][] = [
  [/^You were assigned to "(.*)"\.$/, (name) => `Te asignaron la tarjeta «${name}».`],
  [/^You were added to the board "(.*)"\.$/, (name) => `Te agregaron al tablero «${name}».`],
  [/^You were added to the team "(.*)"\.$/, (name) => `Te agregaron al equipo «${name}».`],
]

export function notificationText(message: string): string {
  for (const [pattern, format] of translations) {
    const match = pattern.exec(message)
    if (match) {
      return format(match[1]!)
    }
  }
  return message
}
