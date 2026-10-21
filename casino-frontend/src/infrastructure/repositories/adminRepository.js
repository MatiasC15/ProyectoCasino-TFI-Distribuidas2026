import { request } from '../http/apiClient.js'
import { tokenStore } from '../auth/tokenStore.js'

const adminRequest = (path, options) => request(path, { ...options, token: tokenStore.read() })

export const adminRepository = {
  statistics: () => adminRequest('/api/admin/statistics'),
  rouletteSettings: () => adminRequest('/api/admin/roulette-settings'),
  updateRouletteSettings: settings => adminRequest('/api/admin/roulette-settings', { method: 'PUT', body: JSON.stringify(settings) }),
  admins: () => adminRequest('/api/admin/admins'),
  createAdmin: details => adminRequest('/api/admin/admins', { method: 'POST', body: JSON.stringify(details) }),
  auditLog: () => adminRequest('/api/admin/audit-log'),
}
