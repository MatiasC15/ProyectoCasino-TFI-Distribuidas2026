import { casinoRepository } from '../../infrastructure/repositories/casinoRepository.js'
export async function getBalance() { const result = await casinoRepository.balance(); return result.saldo }
export async function getTransactions() { return casinoRepository.transactions() }
export async function getGames() { return casinoRepository.games() }
export async function getRouletteState() { return casinoRepository.state() }
export async function getRouletteSettings() { return casinoRepository.rouletteSettings() }
export async function transact(type, amount) {
  if (!Number.isFinite(amount) || amount <= 0 || Math.round(amount * 100) !== amount * 100) throw new Error('Ingresá un importe positivo con hasta dos decimales.')
  return casinoRepository.transact(type, amount)
}
export async function placeBet(wager) {
  if (!Number.isFinite(wager.amount) || wager.amount <= 0 || Math.round(wager.amount * 100) !== wager.amount * 100) throw new Error('La apuesta debe ser positiva y tener hasta dos decimales.')
  return casinoRepository.bet(wager)
}
