import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { tokenStore } from '../auth/tokenStore.js'

export async function connectRoulette(onEvent, onStatus) {
  const apiBase = (import.meta.env.VITE_API_BASE_URL || window.location.origin).replace(/\/$/, '')
  const connection = new HubConnectionBuilder()
    .withUrl(`${apiBase}/hubs/roulette`, { accessTokenFactory: () => tokenStore.read() || '' })
    .withAutomaticReconnect([0, 1500, 5000, 10000])
    .configureLogging(import.meta.env.DEV ? LogLevel.Warning : LogLevel.Error)
    .build()
  connection.on('roulette-event', onEvent)
  connection.onreconnecting(() => onStatus('connecting'))
  connection.onreconnected(() => onStatus('connected'))
  connection.onclose(() => onStatus('disconnected'))
  try { await connection.start(); onStatus('connected') } catch { onStatus('disconnected') }
  return connection
}
