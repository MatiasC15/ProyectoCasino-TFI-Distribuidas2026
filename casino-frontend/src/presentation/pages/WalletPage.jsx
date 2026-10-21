import { useEffect, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import { getTransactions, transact } from '../../application/useCases/casino.js'
import { money } from '../../domain/money.js'
import { PageHeading } from '../components/PageHeading.jsx'

const quickAmounts = [1000, 5000, 10000]
const names = { deposito: 'Carga de créditos', retiro: 'Retiro', apuesta: 'Apuesta', premio: 'Premio', reintegro: 'Reintegro' }

export function WalletPage() {
  const { balance, setBalance } = useOutletContext()
  const [transactions, setTransactions] = useState([]), [historyError, setHistoryError] = useState('')
  const [amount, setAmount] = useState(''), [mode, setMode] = useState('deposit'), [busy, setBusy] = useState(false), [message, setMessage] = useState(''), [error, setError] = useState('')
  async function refreshTransactions() { try { setTransactions(await getTransactions()); setHistoryError('') } catch (err) { setHistoryError(err.message) } }
  useEffect(() => {
    let active = true
    getTransactions().then(items => { if (active) { setTransactions(items); setHistoryError('') } }).catch(err => { if (active) setHistoryError(err.message) })
    return () => { active = false }
  }, [])
  async function submit(event) {
    event.preventDefault(); setBusy(true); setMessage(''); setError('')
    try { const result = await transact(mode, Number(amount)); setBalance(result.nuevoSaldo); setMessage(mode === 'deposit' ? `Cargaste ${money(amount)}.` : `Retiraste ${money(amount)}.`); setAmount(''); await refreshTransactions() }
    catch (err) { setError(err.message) } finally { setBusy(false) }
  }
  function changeMode(next) { setMode(next); setError(''); setMessage('') }
  const deposit = mode === 'deposit'

  return <>
    <PageHeading title="Caja" description="Cargá o retirá créditos de práctica."/>
    <div className="cashier">
      <section className="cashier-balance" aria-label="Saldo">
        <span className="cashier-figure figure">{balance === null ? '—' : money(balance)}</span>
        <p>Saldo disponible para jugar</p>
      </section>

      <section className="cashier-form">
        <div className="segmented" role="tablist" aria-label="Tipo de movimiento">
          <button type="button" role="tab" aria-selected={deposit} onClick={() => changeMode('deposit')}>Cargar</button>
          <button type="button" role="tab" aria-selected={!deposit} onClick={() => changeMode('withdraw')}>Retirar</button>
        </div>
        <form onSubmit={submit}>
          <label className="field field-amount">{deposit ? 'Importe a cargar' : 'Importe a retirar'}
            <input type="number" inputMode="decimal" min="0.01" step="0.01" max={!deposit && balance !== null ? balance : undefined} value={amount} onChange={e => setAmount(e.target.value)} placeholder="0" required/>
          </label>
          <div className="quick-amounts" aria-label="Importes rápidos">{quickAmounts.map(value => <button type="button" key={value} onClick={() => setAmount(String(value))}>{money(value).replace(',00', '')}</button>)}</div>
          {!deposit && <p className="field-hint">Podés retirar hasta tu saldo disponible.</p>}
          {error && <p className="form-error" role="alert">{error}</p>}
          {message && <p className="form-success" role="status">{message}</p>}
          <button className="button button-primary" disabled={busy}>{busy ? 'Procesando…' : deposit ? 'Cargar créditos' : 'Retirar créditos'}</button>
        </form>
      </section>
    </div>

    <section className="ledger" aria-labelledby="ledger-title">
      <div className="ledger-head"><h2 id="ledger-title">Movimientos</h2><span>{transactions.length} {transactions.length === 1 ? 'movimiento' : 'movimientos'}</span></div>
      {historyError && <p className="form-error" role="alert">{historyError}</p>}
      {!historyError && transactions.length === 0 ? <p className="ledger-empty">Todavía no hay movimientos. Cargá créditos para empezar a jugar.</p> : <ol className="ledger-list">{transactions.map(item => {
        const type = String(item.tipo ?? item.Tipo ?? '').toLowerCase()
        const credit = ['deposito', 'premio', 'reintegro'].includes(type)
        const date = item.fecha ?? item.Fecha
        return <li className="ledger-row" key={item.id ?? item.Id}>
          <span className="ledger-name">{names[type] ?? 'Movimiento'}</span>
          <time>{date ? new Date(date).toLocaleString('es-AR', { dateStyle: 'medium', timeStyle: 'short' }) : 'Sin fecha'}</time>
          <strong className={`figure ${credit ? 'is-credit' : 'is-debit'}`}>{credit ? '+' : '−'}{money(item.monto ?? item.Monto)}</strong>
        </li>
      })}</ol>}
    </section>
  </>
}
