// Fills a local TaskBoard with demo people, teams, boards and cards through the public API,
// so every rule, notification and realtime event runs as it would for real users.
//
//   node scripts/seed-demo.mjs                      (API on http://localhost:5080)
//   API_URL=http://localhost:5081 node scripts/seed-demo.mjs
//   node scripts/seed-demo.mjs --share-with you@example.com
//
// Every demo account uses DEMO_PASSWORD below; sign in as laura@demo.taskboard.test to see it all.
// Only for local development: it refuses to run when the demo data is already there.
// --share-with adds an account you already registered to the demo teams and boards, as an admin,
// and assigns it the same cards as Laura; run it as many times as you like.

const API = (process.env.API_URL ?? 'http://localhost:5080').replace(/\/$/, '')
const DEMO_PASSWORD = 'Demo-TaskBoard-2026'
const DOMAIN = 'demo.taskboard.test'

const people = {
  laura: 'Laura Méndez',
  carlos: 'Carlos Ruiz',
  sofia: 'Sofía Herrera',
  diego: 'Diego Torres',
  valentina: 'Valentina Cruz',
  andres: 'Andrés Gómez',
  mariana: 'Mariana López',
  javier: 'Javier Castillo',
}

const day = 24 * 60 * 60 * 1000
const inDays = (days) => new Date(Date.now() + days * day).toISOString()

// ---------------------------------------------------------------------------------------------
// Data. Cards: [title, { description, priority, due (days from today), assignees, labels,
// checklist: [[text, done]], comments: [[person, text]], archived, attachment }]
// ---------------------------------------------------------------------------------------------

const teams = [
  {
    name: 'Producto',
    owner: 'laura',
    members: { carlos: 'Admin', sofia: 'Member', diego: 'Member', valentina: 'Member', andres: 'Member' },
    boards: [
      {
        name: 'Lanzamiento app móvil',
        visibility: 'Team',
        members: { carlos: 'Admin', sofia: 'Editor', diego: 'Editor', valentina: 'Editor', andres: 'Editor' },
        labels: [
          ['Frontend', 'Blue'],
          ['Backend', 'Purple'],
          ['Diseño', 'Pink'],
          ['Bug', 'Red'],
          ['Investigación', 'Teal'],
        ],
        columns: {
          Backlog: [
            ['Modo sin conexión', { description: 'Guardar las tarjetas en el dispositivo y sincronizar al recuperar la red.', priority: 'Low', labels: ['Investigación'] }],
            ['Widgets para la pantalla de inicio', { priority: 'Low', labels: ['Frontend', 'Diseño'] }],
            ['Exportar tableros a PDF', { labels: ['Backend'] }],
          ],
          'Por hacer': [
            ['Pantalla de configuración', { priority: 'Medium', due: 9, assignees: ['valentina'], labels: ['Frontend'], checklist: [['Tema claro y oscuro', false], ['Idioma', false], ['Notificaciones push', false]] }],
            ['Onboarding de 3 pasos', { priority: 'Medium', due: 12, assignees: ['sofia'], labels: ['Diseño'], comments: [['laura', 'Usemos las ilustraciones nuevas del equipo de marca.']] }],
            ['Límite de intentos en el inicio de sesión', { priority: 'High', due: 6, assignees: ['diego'], labels: ['Backend'] }],
          ],
          'En curso': [
            ['Notificaciones push', { description: 'Firebase en Android y APNs en iOS, con preferencias por tipo de aviso.', priority: 'High', due: 3, assignees: ['diego', 'laura'], labels: ['Backend'], checklist: [['Registro del dispositivo', true], ['Envío desde el servidor', true], ['Preferencias por tipo', false], ['Pruebas en iOS', false]], comments: [['diego', 'El envío desde el servidor ya funciona en staging.'], ['carlos', '@Laura ¿lo probamos el jueves con el equipo de QA?']] }],
            ['Pantalla de detalle de tarea', { priority: 'Medium', due: 5, assignees: ['valentina'], labels: ['Frontend'], checklist: [['Cabecera y estado', true], ['Comentarios', true], ['Adjuntos', false]] }],
            ['Sistema de diseño en Figma', { priority: 'Medium', due: 1, assignees: ['sofia'], labels: ['Diseño'], checklist: [['Colores y tipografía', true], ['Botones y campos', true], ['Componentes de lista', true], ['Documentación', false]], attachment: ['guia-de-estilo.txt', 'Colores primarios: índigo 600 y pizarra 900.\nTipografía: Inter.\n'] }],
          ],
          'En revisión': [
            ['Inicio de sesión con Google', { priority: 'High', due: -1, assignees: ['diego'], labels: ['Backend', 'Frontend'], checklist: [['Botón y flujo OAuth', true], ['Vincular cuentas existentes', true]], comments: [['laura', 'Revisado el flujo en Android, falta iOS.']] }],
            ['La lista se corta en pantallas pequeñas', { priority: 'Urgent', due: -2, assignees: ['valentina'], labels: ['Bug', 'Frontend'], comments: [['andres', 'Pasa en el iPhone SE y en Android de 5 pulgadas.']] }],
          ],
          Hecho: [
            ['Configurar CI para las builds móviles', { assignees: ['carlos'], labels: ['Backend'], checklist: [['Android', true], ['iOS', true]] }],
            ['Investigación con 8 usuarios', { assignees: ['sofia'], labels: ['Investigación'], attachment: ['hallazgos-entrevistas.csv', 'usuario,hallazgo\n1,Quiere ver sus tareas del día al abrir la app\n2,No encuentra el filtro por persona\n'] }],
            ['Icono y pantalla de carga', { assignees: ['sofia'], labels: ['Diseño'] }],
            ['Cierre de sesión en todos los dispositivos', { labels: ['Backend'], archived: true }],
          ],
        },
      },
      {
        name: 'Rediseño del sitio web',
        visibility: 'Team',
        members: { sofia: 'Editor', valentina: 'Editor' },
        labels: [
          ['SEO', 'Green'],
          ['Contenido', 'Yellow'],
          ['Diseño', 'Pink'],
        ],
        columns: {
          Ideas: [
            ['Blog con casos de éxito', { labels: ['Contenido', 'SEO'] }],
            ['Calculadora de precios', { priority: 'Low' }],
          ],
          Diseño: [
            ['Nueva página de precios', { priority: 'High', due: 4, assignees: ['sofia'], labels: ['Diseño'], checklist: [['Tabla comparativa', true], ['Preguntas frecuentes', false]] }],
            ['Página de inicio', { priority: 'Medium', due: 8, assignees: ['sofia', 'valentina'], labels: ['Diseño', 'Contenido'] }],
          ],
          Desarrollo: [
            ['Migrar a Astro', { priority: 'Medium', due: 15, assignees: ['valentina'], checklist: [['Plantillas', true], ['Formularios', false], ['Redirecciones', false]] }],
          ],
          Publicado: [['Meta descripciones de todas las páginas', { labels: ['SEO'], assignees: ['laura'] }]],
        },
      },
      {
        name: 'Bugs y soporte',
        visibility: 'Private',
        members: { diego: 'Editor', andres: 'Editor' },
        labels: [
          ['iOS', 'Gray'],
          ['Android', 'Green'],
          ['Web', 'Blue'],
        ],
        columns: {
          Nuevos: [
            ['Se duplica la notificación al comentar', { priority: 'Medium', labels: ['Web'], comments: [['andres', 'Lo reportaron dos clientes esta semana.']] }],
            ['La app se cierra al adjuntar fotos grandes', { priority: 'Urgent', due: 0, assignees: ['diego'], labels: ['Android'] }],
          ],
          Investigando: [
            ['Fechas límite en la zona horaria equivocada', { priority: 'High', due: 2, assignees: ['andres'], labels: ['Web', 'iOS'], checklist: [['Reproducir', true], ['Encontrar la causa', false]] }],
          ],
          Resuelto: [['El buscador ignora las tildes', { labels: ['Web'], assignees: ['diego'] }]],
        },
      },
      {
        name: 'Sprint 12 (cerrado)',
        visibility: 'Team',
        archived: true,
        columns: { Hecho: [['Retrospectiva del sprint', {}], ['Demo con clientes', {}]] },
      },
    ],
  },
  {
    name: 'Marketing',
    owner: 'mariana',
    members: { laura: 'Member', valentina: 'Member', javier: 'Admin' },
    boards: [
      {
        name: 'Campaña Q4',
        visibility: 'Team',
        members: { laura: 'Editor', javier: 'Editor', valentina: 'Viewer' },
        labels: [
          ['Redes', 'Orange'],
          ['Email', 'Teal'],
          ['Anuncios', 'Red'],
        ],
        columns: {
          Planificación: [
            ['Definir mensajes clave', { priority: 'High', due: 2, assignees: ['mariana', 'laura'], checklist: [['Público objetivo', true], ['Propuesta de valor', false]], comments: [['mariana', 'Laura, ¿me ayudas con la propuesta de valor del producto?']] }],
            ['Presupuesto por canal', { priority: 'Medium', due: 5, assignees: ['javier'] }],
          ],
          Producción: [
            ['Video de lanzamiento', { priority: 'High', due: 10, assignees: ['mariana'], labels: ['Redes', 'Anuncios'], checklist: [['Guion', true], ['Grabación', false], ['Edición', false]] }],
            ['Secuencia de 3 emails', { priority: 'Medium', due: 7, assignees: ['laura'], labels: ['Email'] }],
          ],
          Programado: [['Publicaciones de la semana 1', { labels: ['Redes'], assignees: ['javier'], due: 1 }]],
          Publicado: [['Landing de preventa', { labels: ['Anuncios'] }]],
        },
      },
      {
        name: 'Calendario de contenidos',
        visibility: 'Team',
        labels: [['Blog', 'Yellow'], ['Newsletter', 'Teal']],
        columns: {
          'Este mes': [
            ['Guía: cómo organizar un sprint', { due: 6, labels: ['Blog'], assignees: ['mariana'] }],
            ['Newsletter de noviembre', { due: 4, labels: ['Newsletter'], assignees: ['mariana'] }],
          ],
          'Próximo mes': [['Resumen del año', { labels: ['Blog'] }]],
        },
      },
    ],
  },
  {
    name: 'Operaciones',
    owner: 'javier',
    members: { laura: 'Admin', carlos: 'Member' },
    boards: [
      {
        name: 'Onboarding de clientes',
        visibility: 'Team',
        members: { laura: 'Editor', carlos: 'Editor' },
        labels: [['Enterprise', 'Purple'], ['Pyme', 'Blue']],
        columns: {
          'Por empezar': [['Acme Corp', { priority: 'High', due: 3, labels: ['Enterprise'], assignees: ['laura'] }]],
          Configurando: [
            ['Globex', { priority: 'Medium', due: 1, labels: ['Pyme'], assignees: ['carlos'], checklist: [['Cuenta creada', true], ['Importar tableros', true], ['Capacitación', false]] }],
          ],
          Activos: [['Initech', { labels: ['Pyme'], assignees: ['javier'] }]],
        },
      },
    ],
  },
]

// ---------------------------------------------------------------------------------------------

async function call(method, path, { token, body, form } = {}) {
  for (let attempt = 1; ; attempt++) {
    const response = await fetch(API + path, {
      method,
      headers: {
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...(body ? { 'Content-Type': 'application/json' } : {}),
      },
      body: form ?? (body ? JSON.stringify(body) : undefined),
    })
    // Sign-in and sign-up are rate limited per IP; wait for the window instead of failing.
    if (response.status === 429 && attempt <= 5) {
      const wait = Number(response.headers.get('retry-after') ?? 15)
      console.log(`  límite de intentos, esperando ${wait} s…`)
      await new Promise((resolve) => setTimeout(resolve, wait * 1000))
      continue
    }
    const text = await response.text()
    if (!response.ok) {
      throw new Error(`${method} ${path} → ${response.status} ${text}`)
    }
    return text ? JSON.parse(text) : null
  }
}

const users = {}

async function signIn(key) {
  const email = `${key}@${DOMAIN}`
  const { accessToken } = await call('POST', '/auth/login', { body: { email, password: DEMO_PASSWORD } })
  const me = await call('GET', '/me', { token: accessToken })
  users[key] = { email, token: accessToken, id: me.id }
}

async function main() {
  console.log(`Sembrando datos de demostración en ${API}`)

  for (const [key, displayName] of Object.entries(people)) {
    await call('POST', '/auth/register', { body: { email: `${key}@${DOMAIN}`, displayName, password: DEMO_PASSWORD } }).catch(
      (error) => {
        if (String(error).includes('Users.EmailTaken')) {
          throw new Error('Los datos de demostración ya existen; inicia sesión con laura@demo.taskboard.test.')
        }
        throw error
      },
    )
  }
  for (const key of Object.keys(people)) {
    await signIn(key)
  }
  console.log(`  ${Object.keys(people).length} personas`)

  let boardCount = 0
  let cardCount = 0
  for (const team of teams) {
    const owner = users[team.owner]
    const { id: teamId } = await call('POST', '/teams', { token: owner.token, body: { name: team.name } })
    for (const [key, role] of Object.entries(team.members)) {
      await call('POST', `/teams/${teamId}/members`, { token: owner.token, body: { email: users[key].email, role } })
    }

    for (const board of team.boards) {
      const { id: boardId } = await call('POST', `/teams/${teamId}/boards`, {
        token: owner.token,
        body: { name: board.name, visibility: board.visibility },
      })
      boardCount++
      for (const [key, role] of Object.entries(board.members ?? {})) {
        await call('POST', `/boards/${boardId}/members`, { token: owner.token, body: { userId: users[key].id, role } })
      }

      const labelIds = {}
      for (const [name, color] of board.labels ?? []) {
        labelIds[name] = (await call('POST', `/boards/${boardId}/labels`, { token: owner.token, body: { name, color } })).id
      }

      for (const [columnName, cards] of Object.entries(board.columns)) {
        const { id: columnId } = await call('POST', `/boards/${boardId}/columns`, { token: owner.token, body: { name: columnName } })
        for (const [title, spec] of cards) {
          await createCard(owner, columnId, labelIds, title, spec)
          cardCount++
        }
      }

      if (board.archived) {
        await call('POST', `/boards/${boardId}/archive`, { token: owner.token })
      }
    }
    console.log(`  equipo ${team.name}: ${team.boards.length} ${team.boards.length === 1 ? 'tablero' : 'tableros'}`)
  }

  console.log(`Listo: ${boardCount} tableros y ${cardCount} tarjetas. Entra como laura@${DOMAIN}.`)
}

async function createCard(owner, columnId, labelIds, title, spec) {
  const token = owner.token
  const { id: cardId } = await call('POST', `/columns/${columnId}/cards`, {
    token,
    body: { title, description: spec.description ?? null },
  })

  if (spec.priority || spec.due !== undefined) {
    await call('PUT', `/cards/${cardId}`, {
      token,
      body: {
        title,
        description: spec.description ?? null,
        priority: spec.priority ?? 'None',
        dueDate: spec.due === undefined ? null : inDays(spec.due),
      },
    })
  }
  for (const key of spec.assignees ?? []) {
    await call('POST', `/cards/${cardId}/assignees`, { token, body: { userId: users[key].id } })
  }
  for (const name of spec.labels ?? []) {
    await call('POST', `/cards/${cardId}/labels`, { token, body: { labelId: labelIds[name] } })
  }
  for (const [text, done] of spec.checklist ?? []) {
    const { id: itemId } = await call('POST', `/cards/${cardId}/checklist`, { token, body: { text } })
    if (done) {
      await call('PATCH', `/cards/${cardId}/checklist/${itemId}`, { token, body: { isDone: true } })
    }
  }
  for (const [key, body] of spec.comments ?? []) {
    await call('POST', `/cards/${cardId}/comments`, { token: users[key].token, body: { body } })
  }
  if (spec.attachment) {
    const [fileName, content] = spec.attachment
    const form = new FormData()
    form.append('file', new Blob([content], { type: 'text/plain' }), fileName)
    await call('POST', `/cards/${cardId}/attachments`, { token, form })
  }
  if (spec.archived) {
    await call('POST', `/cards/${cardId}/archive`, { token })
  }
}

// Adds an existing account to every demo team and active board, and to Laura's cards.
async function share(email) {
  console.log(`Compartiendo los datos de demostración con ${email} en ${API}`)
  await signIn('laura')
  for (const team of teams) {
    if (!users[team.owner]) {
      await signIn(team.owner)
    }
  }

  // Already being a member is fine: the script can run again after more demo data appears.
  const ignoring = (code) => (error) => {
    if (!String(error).includes(code)) {
      throw error
    }
  }

  let boardCount = 0
  let cardCount = 0
  for (const team of teams) {
    const owner = users[team.owner]
    const teamId = (await call('GET', '/teams', { token: owner.token })).find((t) => t.name === team.name)?.id
    if (!teamId) {
      throw new Error(`No existe el equipo de demostración «${team.name}»; corre primero el script sin --share-with.`)
    }

    await call('POST', `/teams/${teamId}/members`, { token: owner.token, body: { email, role: 'Admin' } }).catch(
      ignoring('Teams.AlreadyMember'),
    )
    const member = (await call('GET', `/teams/${teamId}/members`, { token: owner.token })).find(
      (m) => m.email.toLowerCase() === email.trim().toLowerCase(),
    )

    // Archived boards cannot take new members; team members can still read them.
    for (const { id: boardId } of await call('GET', `/teams/${teamId}/boards`, { token: owner.token })) {
      await call('POST', `/boards/${boardId}/members`, {
        token: owner.token,
        body: { userId: member.userId, role: 'Admin' },
      }).catch(ignoring('Boards.AlreadyMember'))
      boardCount++

      const board = await call('GET', `/boards/${boardId}`, { token: owner.token })
      for (const card of board.columns.flatMap((column) => column.cards)) {
        if (card.assigneeIds.includes(users.laura.id)) {
          await call('POST', `/cards/${card.id}/assignees`, { token: owner.token, body: { userId: member.userId } }).catch(
            ignoring('Cards.AlreadyAssigned'),
          )
          cardCount++
        }
      }
    }
  }

  console.log(`Listo: ${email} está en ${teams.length} equipos y ${boardCount} tableros, con ${cardCount} tarjetas asignadas.`)
}

const shareFlag = process.argv.indexOf('--share-with')
const shareWith = shareFlag >= 0 ? process.argv[shareFlag + 1] : undefined
if (shareFlag >= 0 && !shareWith) {
  console.error('Uso: node scripts/seed-demo.mjs --share-with tu@email.com')
  process.exit(1)
}

;(shareWith ? share(shareWith) : main()).catch((error) => {
  console.error(error.message)
  process.exit(1)
})
