import { useEffect, useMemo, useState } from 'react'
import { roleFromToken, tokenStore } from '../../infrastructure/auth/tokenStore.js'
import { UNAUTHORIZED_EVENT } from '../../infrastructure/http/apiClient.js'
import { AuthContext } from './authContext.js'

const signedOut = { token: null, userId: null, role: 'user' }

export function AuthProvider({ children }) {
  const [session, setSessionState] = useState(() => {
    const token = tokenStore.read()
    return { token, userId: null, role: roleFromToken(token || '') }
  })

  // Si la API responde 401 a una llamada autenticada, cerramos la sesión y las rutas protegidas redirigen al ingreso.
  useEffect(() => {
    const expire = () => { tokenStore.clear(); setSessionState(signedOut) }
    window.addEventListener(UNAUTHORIZED_EVENT, expire)
    return () => window.removeEventListener(UNAUTHORIZED_EVENT, expire)
  }, [])

  const value = useMemo(() => ({
    ...session,
    setSession(next) { tokenStore.write(next.token); setSessionState({ ...next, role: roleFromToken(next.token) }) },
    clearSession() { tokenStore.clear(); setSessionState(signedOut) },
  }), [session])
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
