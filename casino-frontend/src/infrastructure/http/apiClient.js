export const UNAUTHORIZED_EVENT = 'casino:unauthorized'

const apiBase = (import.meta.env.VITE_API_BASE_URL || '').replace(/\/$/, '')

export async function request(path, { token, ...options } = {}) {
  const headers = new Headers(options.headers || {})
  if (options.body) headers.set('Content-Type', 'application/json')
  if (token) headers.set('Authorization', `Bearer ${token}`)
  let response
  try {
    response = await fetch(`${apiBase}${path}`, { ...options, headers })
  } catch {
    throw new Error('No se pudo conectar con el servidor. Verificá que la API esté iniciada.')
  }
  if (!response.ok) {
    let message = `La solicitud falló (${response.status}).`
    try {
      const body = await response.json()
      message = body.error || body.message || message
    } catch { /* respuesta sin JSON */ }
    if (response.status === 401 && token) {
      message = 'Tu sesión venció. Ingresá nuevamente.'
      // Sin esto la app quedaba "logueada" con un token inválido y cada pantalla fallaba.
      window.dispatchEvent(new Event(UNAUTHORIZED_EVENT))
    }
    throw new Error(message)
  }
  if (response.status === 204) return null
  return response.json()
}
