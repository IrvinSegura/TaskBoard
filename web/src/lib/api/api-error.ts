/** Body of an RFC 7807 Problem Details response, as the API's ToProblem() writes it. */
export interface ProblemDetails {
  status?: number
  title?: string
  detail?: string
  errors?: { code: string; description: string }[]
}

// Spanish copy for the error codes users can actually run into; anything else falls back to the API text.
const friendlyMessages: Record<string, string> = {
  'Users.InvalidCredentials': 'Email o contraseña incorrectos.',
  'Users.EmailTaken': 'Ese email ya está registrado.',
  'Users.WrongPassword': 'La contraseña actual no es correcta.',
  'Users.NotFoundByEmail': 'No hay ningún usuario registrado con ese email.',
  'Users.InvalidRefreshToken': 'Tu sesión expiró. Inicia sesión de nuevo.',
  'Teams.NotAllowed': 'Solo los dueños y administradores del equipo pueden gestionar miembros.',
  'Teams.AlreadyMember': 'Esa persona ya es miembro del equipo.',
  'Teams.CannotAssignOwner': 'El rol de dueño no se puede asignar ni cambiar.',
  'Teams.CannotRemoveOwner': 'El dueño del equipo no se puede quitar.',
  'Boards.Archived': 'El tablero está archivado y es de solo lectura.',
  'Boards.AlreadyMember': 'Esa persona ya es miembro del tablero.',
  'Boards.UserNotInTeam': 'La persona tiene que ser miembro del equipo.',
  'Authorization.InsufficientRole': 'Tu rol en el tablero no permite esta acción.',
}

const statusMessages: Record<number, string> = {
  400: 'La solicitud no es válida.',
  401: 'Tu sesión expiró. Inicia sesión de nuevo.',
  403: 'No tienes permiso para hacer esto.',
  404: 'No encontramos lo que buscabas.',
  409: 'Hay un conflicto con el estado actual.',
  413: 'El archivo es demasiado grande.',
  429: 'Demasiados intentos. Espera un momento y vuelve a intentarlo.',
}

export class ApiError extends Error {
  readonly status: number
  readonly code: string | undefined
  /** Validation messages keyed by camelCased property name (`email`, `displayName`...). */
  readonly fieldErrors: Record<string, string>

  constructor(status: number, problem: ProblemDetails = {}) {
    super(ApiError.describe(status, problem))
    this.name = 'ApiError'
    this.status = status
    this.code = problem.title
    this.fieldErrors = {}
    for (const error of problem.errors ?? []) {
      const field = error.code.charAt(0).toLowerCase() + error.code.slice(1)
      this.fieldErrors[field] ??= error.description
    }
  }

  private static describe(status: number, problem: ProblemDetails): string {
    if (problem.title && friendlyMessages[problem.title]) {
      return friendlyMessages[problem.title]!
    }
    // Validation errors carry the useful text per field; the generic detail says nothing.
    const firstFieldError = problem.errors?.[0]?.description
    if (firstFieldError) {
      return firstFieldError
    }
    if (status >= 500) {
      return 'Algo salió mal en el servidor. Inténtalo de nuevo en unos segundos.'
    }
    return problem.detail ?? statusMessages[status] ?? 'Ocurrió un error inesperado.'
  }
}

export class NetworkError extends Error {
  constructor() {
    super('No se pudo conectar con el servidor. Revisa tu conexión.')
    this.name = 'NetworkError'
  }
}

export function errorMessage(error: unknown): string {
  if (error instanceof Error) {
    return error.message
  }
  return 'Ocurrió un error inesperado.'
}
