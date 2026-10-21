import { authRepository } from '../../infrastructure/repositories/authRepository.js'
export async function login(email, password) {
  const result = await authRepository.login({ email, password })
  if (!result.token) throw new Error('La API no devolvió una sesión válida.')
  return { token: result.token, userId: result.userId }
}
export async function register(data) { return authRepository.register(data) }
