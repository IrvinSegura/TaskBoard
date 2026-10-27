import { z } from 'zod'

// Same length rules as the API's register and change-password validators.
export const passwordSchema = z
  .string()
  .min(8, 'La contraseña debe tener al menos 8 caracteres.')
  .max(128, 'Máximo 128 caracteres.')
