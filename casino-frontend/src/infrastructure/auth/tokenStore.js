const KEY = 'casino.session.token'
export function roleFromToken(token) {
  try {
    const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    const decoded = JSON.parse(atob(payload.padEnd(Math.ceil(payload.length / 4) * 4, '=')))
    return decoded.rol || decoded.role || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || 'user'
  } catch { return 'user' }
}

export const tokenStore = {
  read: () => sessionStorage.getItem(KEY),
  write: token => sessionStorage.setItem(KEY, token),
  clear: () => sessionStorage.removeItem(KEY),
}
