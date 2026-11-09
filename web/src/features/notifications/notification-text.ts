// The API writes notification messages in English; these patterns turn the known ones into Spanish.
const translations: [RegExp, (name: string) => string][] = [
  [/^You were assigned to "(.*)"\.$/, (name) => `Te asignaron la tarjeta «${name}».`],
  [/^You were added to the board "(.*)"\.$/, (name) => `Te agregaron al tablero «${name}».`],
  [/^You were added to the team "(.*)"\.$/, (name) => `Te agregaron al equipo «${name}».`],
  [/^You were removed from the board "(.*)"\.$/, (name) => `Te quitaron del tablero «${name}».`],
  [/^You were removed from the team "(.*)"\.$/, (name) => `Te quitaron del equipo «${name}».`],
  [/^"(.*)" is due soon\.$/, (name) => `La tarjeta «${name}» vence pronto.`],
]

const roles: Record<string, string> = { admin: 'administrador', member: 'miembro' }

// Messages that start with a person's name: "<name> mentioned you in "<card>"."
const personTranslations: [RegExp, (person: string, card: string) => string][] = [
  [/^(.+) mentioned you in "(.*)"\.$/, (person, card) => `${person} te mencionó en «${card}».`],
  [/^(.+) commented on "(.*)"\.$/, (person, card) => `${person} comentó en «${card}».`],
]

// "Your role in the team "<team>" is now admin."
const rolePattern = /^Your role in the team "(.*)" is now (admin|member)\.$/

export function notificationText(message: string): string {
  for (const [pattern, format] of translations) {
    const match = pattern.exec(message)
    if (match) {
      return format(match[1]!)
    }
  }
  const role = rolePattern.exec(message)
  if (role) {
    return `Ahora eres ${roles[role[2]!]} del equipo «${role[1]}».`
  }
  for (const [pattern, format] of personTranslations) {
    const match = pattern.exec(message)
    if (match) {
      return format(match[1]!, match[2]!)
    }
  }
  return message
}
