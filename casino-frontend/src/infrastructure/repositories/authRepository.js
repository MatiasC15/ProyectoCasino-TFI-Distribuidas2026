import { request } from '../http/apiClient.js'
export const authRepository = {
  login: credentials => request('/api/auth/login', { method: 'POST', body: JSON.stringify(credentials) }),
  register: data => request('/api/auth/register', { method: 'POST', body: JSON.stringify(data) }),
}
