// What the help page says. Keep it in step with the app: every shortcut and rule here exists in the code.

export interface Shortcut {
  keys: string[][]
  action: string
  where: string
}

export const shortcuts: Shortcut[] = [
  { keys: [['Ctrl', 'K'], ['⌘', 'K']], action: 'Abrir la búsqueda', where: 'En cualquier página' },
  { keys: [['Enter']], action: 'Crear la tarjeta y seguir escribiendo la siguiente', where: 'Al añadir una tarjeta' },
  { keys: [['Shift', 'Enter']], action: 'Salto de línea en el título', where: 'Al añadir una tarjeta' },
  { keys: [['Enter']], action: 'Abrir la tarjeta enfocada', where: 'En el tablero' },
  { keys: [['Espacio'], ['↑', '↓', '←', '→']], action: 'Tomar la tarjeta enfocada y moverla con las flechas', where: 'En el tablero' },
  { keys: [['Ctrl', 'Enter']], action: 'Guardar la descripción o publicar el comentario', where: 'En el detalle de una tarjeta' },
  { keys: [['@']], action: 'Mencionar a un miembro del tablero', where: 'Al escribir un comentario' },
  { keys: [['↑', '↓'], ['Tab']], action: 'Elegir a la persona mencionada', where: 'En la lista de menciones' },
  { keys: [['Esc']], action: 'Cancelar la edición en curso', where: 'Títulos, columnas, descripciones y comentarios' },
]

export interface GuideStep {
  title: string
  description: string
  to: string
  linkLabel: string
}

export const quickStart: GuideStep[] = [
  {
    title: 'Crea un equipo',
    description: 'Los equipos agrupan a las personas y sus tableros. Quien lo crea es su dueño.',
    to: '/teams',
    linkLabel: 'Ir a Equipos',
  },
  {
    title: 'Invita a tu gente',
    description: 'En la pestaña Miembros del equipo añade a cada persona por su correo y elige su rol.',
    to: '/teams',
    linkLabel: 'Elegir un equipo',
  },
  {
    title: 'Arma un tablero',
    description: 'Crea columnas para cada etapa del trabajo y tarjetas para cada tarea. Arrástralas al avanzar.',
    to: '/teams',
    linkLabel: 'Ver tableros',
  },
  {
    title: 'Sigue el trabajo',
    description: 'Mis tareas, Calendario y Notificaciones te dicen qué vence y qué cambió sin abrir cada tablero.',
    to: '/my-tasks',
    linkLabel: 'Ver mis tareas',
  },
]

export interface RoleRow {
  role: string
  can: string
}

export const teamRoles: RoleRow[] = [
  { role: 'Dueño', can: 'Todo lo de un administrador, y es el único que puede eliminar el equipo.' },
  { role: 'Administrador', can: 'Renombra el equipo, añade y quita miembros y cambia sus roles.' },
  { role: 'Miembro', can: 'Ve los tableros visibles para el equipo y puede crear tableros nuevos.' },
]

export const boardRoles: RoleRow[] = [
  { role: 'Administrador', can: 'Todo lo de un editor, y además gestiona miembros, renombra, archiva y restaura el tablero.' },
  { role: 'Editor', can: 'Crea y mueve columnas y tarjetas, gestiona etiquetas, comenta, adjunta archivos y usa checklists.' },
  { role: 'Lector', can: 'Solo puede ver el tablero. Así ve el equipo los tableros de equipo en los que no es miembro.' },
]

export interface Faq {
  id: string
  topic: string
  question: string
  answer: string
}

export const faqs: Faq[] = [
  {
    id: 'board-visibility',
    topic: 'Tableros',
    question: '¿Qué diferencia hay entre un tablero privado y uno de equipo?',
    answer:
      'Un tablero privado solo lo ven las personas que añadiste como miembros. Uno de equipo lo puede ver cualquier miembro del equipo como lector; para editar hace falta que lo añadas con rol de editor o administrador.',
  },
  {
    id: 'share-board',
    topic: 'Tableros',
    question: '¿Cómo comparto un tablero?',
    answer:
      'Abre el tablero y pulsa Compartir (o los avatares de la cabecera). Solo puedes añadir personas que ya sean miembros del equipo, así que invítalas primero desde la pestaña Miembros del equipo.',
  },
  {
    id: 'archive',
    topic: 'Tableros',
    question: '¿Qué pasa al archivar un tablero o una tarjeta?',
    answer:
      'Un tablero archivado queda de solo lectura y sale de la lista del equipo; lo restauras desde la pestaña Archivados del equipo. Una tarjeta archivada desaparece del tablero y vuelve desde «Tarjetas archivadas» en el menú del tablero. Nada se pierde hasta que lo eliminas desde ahí.',
  },
  {
    id: 'move-cards',
    topic: 'Tarjetas',
    question: '¿Cómo muevo una tarjeta a otra columna?',
    answer:
      'Arrástrala con el ratón o con el dedo. Con el teclado, enfócala con Tab, pulsa Espacio para tomarla, usa las flechas y vuelve a pulsar Espacio para soltarla. Los demás ven el cambio al instante.',
  },
  {
    id: 'due-dates',
    topic: 'Tarjetas',
    question: '¿Cómo funcionan las fechas límite?',
    answer:
      'Ponle una fecha a la tarjeta desde su detalle. Cuenta hasta el final de ese día; se marca en ámbar cuando faltan menos de 48 horas y en rojo cuando ya venció. Todas aparecen en el Calendario, y las que tienes asignadas también en Mis tareas.',
  },
  {
    id: 'labels-checklists',
    topic: 'Tarjetas',
    question: '¿Para qué sirven las etiquetas y las checklists?',
    answer:
      'Las etiquetas clasifican tarjetas (por ejemplo «Bug» o «Diseño») y permiten filtrar el tablero; editores y administradores las crean en la pestaña Configuración del tablero. La checklist divide una tarjeta en pasos y muestra el avance en la propia tarjeta.',
  },
  {
    id: 'mentions',
    topic: 'Colaboración',
    question: '¿Cómo menciono a alguien?',
    answer:
      'Escribe @ en un comentario y elige a la persona de la lista. Solo aparecen los miembros del tablero, y la persona mencionada recibe una notificación.',
  },
  {
    id: 'notifications',
    topic: 'Colaboración',
    question: '¿Cuándo recibo notificaciones?',
    answer:
      'Cuando te asignan una tarjeta, te añaden a un equipo o tablero, te mencionan, o alguien comenta en una tarjeta que tienes asignada. Llegan en vivo a la campana de la barra superior y puedes marcarlas como leídas o borrarlas.',
  },
  {
    id: 'favorites',
    topic: 'Organización',
    question: '¿Cómo agrego algo a favoritos?',
    answer:
      'Pulsa la estrella de un equipo o de un tablero, en su tarjeta o en su cabecera. Todo lo marcado aparece en Favoritos, en el menú lateral. Tus favoritos son solo tuyos: nadie más los ve.',
  },
  {
    id: 'calendar',
    topic: 'Organización',
    question: '¿Qué muestra el calendario?',
    answer:
      'Las fechas límite de las tarjetas de todos los tableros que puedes ver y la actividad de cada día. Filtra para ver solo las tarjetas asignadas a ti, oculta la actividad si prefieres, y pulsa un día para ver el detalle.',
  },
  {
    id: 'activity',
    topic: 'Organización',
    question: '¿Dónde veo quién cambió algo?',
    answer:
      'En Actividad: la general reúne todos tus tableros, y cada equipo, tablero y tarjeta tiene la suya. Ahí queda quién creó, movió, asignó, comentó o archivó cada cosa y cuándo.',
  },
  {
    id: 'account',
    topic: 'Cuenta',
    question: '¿Cómo cambio mi nombre o mi contraseña?',
    answer:
      'En Mi cuenta (menú de tu avatar). Al cambiar la contraseña se cierran tus sesiones en los demás dispositivos.',
  },
  {
    id: 'theme',
    topic: 'Cuenta',
    question: '¿Puedo usar el tema oscuro?',
    answer:
      'Sí. El botón del sol o la luna de la barra superior alterna entre tema claro, oscuro y el del sistema. La elección se guarda en este navegador.',
  },
  {
    id: 'leave-team',
    topic: 'Cuenta',
    question: '¿Qué pasa si me quitan de un equipo?',
    answer:
      'Dejas de ver sus tableros, su actividad y sus fechas en el calendario. Tus favoritos de ese equipo se conservan y vuelven a aparecer si te añaden de nuevo.',
  },
]

function normalize(text: string): string {
  return text.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase()
}

/** The questions that contain every word of the query, ignoring case and accents. */
export function searchFaqs(items: Faq[], query: string): Faq[] {
  const words = normalize(query).split(/\s+/).filter(Boolean)
  if (words.length === 0) {
    return items
  }
  return items.filter((faq) => {
    const text = normalize(`${faq.topic} ${faq.question} ${faq.answer}`)
    return words.every((word) => text.includes(word))
  })
}
