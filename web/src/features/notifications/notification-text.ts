// The API writes notification messages in English; these patterns turn the known ones into Spanish.
const translations: [RegExp, (name: string) => string][] = [
  [/^You were assigned to "(.*)"\.$/, (name) => `Te asignaron la tarjeta «${name}».`],
  [/^You were added to the board "(.*)"\.$/, (name) => `Te agregaron al tablero «${name}».`],
  [/^You were added to the team "(.*)"\.$/, (name) => `Te agregaron al equipo «${name}».`],
]

// Messages that start with a person's name: "<name> mentioned you in "<card>"."
const personTranslations: [RegExp, (person: string, card: string) => string][] = [
  [/^(.+) mentioned you in "(.*)"\.$/, (person, card) => `${person} te mencionó en «${card}».`],
  [/^(.+) commented on "(.*)"\.$/, (person, card) => `${person} comentó en «${card}».`],
]

export function notificationText(message: string): string {
  for (const [pattern, format] of translations) {
    const match = pattern.exec(message)
    if (match) {
      return format(match[1]!)
    }
  }
  for (const [pattern, format] of personTranslations) {
    const match = pattern.exec(message)
    if (match) {
      return format(match[1]!, match[2]!)
    }
  }
  return message
}
