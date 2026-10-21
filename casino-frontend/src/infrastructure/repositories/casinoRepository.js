import { request } from '../http/apiClient.js'
import { tokenStore } from '../auth/tokenStore.js'
const protectedRequest = (path, options) => request(path, { ...options, token: tokenStore.read() })
export const casinoRepository = {
  balance: () => protectedRequest('/api/wallet/balance'),
  transactions: () => protectedRequest('/api/wallet/transactions'),
  games: () => request('/api/games'),
  state: () => protectedRequest('/api/roulette/state'),
  rouletteSettings: () => protectedRequest('/api/roulette/settings'),
  transact: (type, amount) => protectedRequest(`/api/wallet/${type}`, { method: 'POST', body: JSON.stringify({ amount }) }),
  bet: wager => protectedRequest('/api/roulette/bets', { method: 'POST', body: JSON.stringify(wager) }),
}
