import { useEffect, useMemo, useRef, useState } from 'react'
import { useOutletContext } from 'react-router-dom'
import { getBalance, getRouletteSettings, getRouletteState, placeBet } from '../../application/useCases/casino.js'
import { money } from '../../domain/money.js'
import { connectRoulette } from '../../infrastructure/realtime/rouletteConnection.js'
import { RouletteSpin } from '../components/RouletteSpin.jsx'
import { getRouletteAnimationMs } from '../components/rouletteTiming.js'

const redNumbers = new Set([1,3,5,7,9,12,14,16,18,19,21,23,25,27,30,32,34,36])
const colorOf = number => number === 0 ? 'green' : redNumbers.has(number) ? 'red' : 'black'
const payoutMultiplier = { number: 36, color: 2, parity: 2, dozen: 3, split: 18, corner: 9, highLow: 2 }
const recentStorageKey = 'casino.roulette.recent.v1'
const defaultChipValues = [10, 25, 50, 100]
const HISTORY_SIZE = 10
const URGENT_SECONDS = 10
// Filas del paño de arriba hacia abajo: 3-6-9…, 2-5-8…, 1-4-7…
const gridRows = [0,1,2].map(row => Array.from({length:12},(_,column)=>column*3+(3-row)))
const outsideBets = [['highLow','low','1 a 18'],['parity','even','Par'],['color','red','Rojo'],['color','black','Negro'],['parity','odd','Impar'],['highLow','high','19 a 36']]
const dozens = [['1','1.ª docena'],['2','2.ª docena'],['3','3.ª docena']]

function readRecentResults() {
  try {
    const stored = JSON.parse(localStorage.getItem(recentStorageKey) || '[]')
    return Array.isArray(stored) ? stored.filter(item => Number.isInteger(item.number) && item.number >= 0 && item.number <= 36).slice(0, HISTORY_SIZE) : []
  } catch { return [] }
}

function isWinner(bet, number) {
  if (bet.type === 'number') return Number(bet.selection) === number
  if (bet.type === 'split' || bet.type === 'corner') return bet.selection.split(',').includes(String(number))
  if (number === 0) return false
  if (bet.type === 'color') return bet.selection === colorOf(number)
  if (bet.type === 'parity') return bet.selection === (number % 2 === 0 ? 'even' : 'odd')
  if (bet.type === 'dozen') return bet.selection === String(Math.floor((number - 1) / 12) + 1)
  if (bet.type === 'highLow') return bet.selection === (number <= 18 ? 'low' : 'high')
  return false
}

/**
 * Posición de un elemento en la grilla del paño. En escritorio el paño es horizontal (3 filas × 12 columnas);
 * en celular se rota a vertical (3 columnas × 12 filas) con las mismas celdas, solo cambiando estas variables.
 */
const track = (row, column) => ({ '--row': row, '--col': column, '--m-row': column, '--m-col': 6 - row })
const chipTone = index => `chip-tone-${index % 6}`
const compactAmount = amount => amount >= 1000 ? `${Number((amount / 1000).toFixed(1))}k` : amount

export function RoulettePage() {
  const { balance, setBalance } = useOutletContext()
  const [round,setRound] = useState(null), [connection,setConnection] = useState('connecting')
  const [rouletteSettings,setRouletteSettings] = useState({ chipValues: defaultChipValues, minimumBet: 1, maximumBet: 10000 })
  const [chipValue,setChipValue] = useState(25), [pending,setPending] = useState(null)
  const [notice,setNotice] = useState(''), [error,setError] = useState(''), [seconds,setSeconds] = useState(null)
  const pendingBets = useRef([]), inFlightBets = useRef(new Map()), lastSettledRound = useRef(null), spinTimer = useRef(null), busyRef = useRef(false)
  const [settlement,setSettlement] = useState(null), [spinning,setSpinning] = useState(false)
  const [recentResults,setRecentResults] = useState(readRecentResults), [placedBets,setPlacedBets] = useState([])
  const chipStacks = useMemo(() => placedBets.reduce((stacks, bet) => {
    stacks[bet.target] = { total: (stacks[bet.target]?.total || 0) + bet.amount, last: bet.amount }
    return stacks
  }, {}), [placedBets])

  useEffect(() => {
    getRouletteState().then(setRound).catch(() => {})
    getRouletteSettings().then(nextSettings => {
      setRouletteSettings(nextSettings)
      setChipValue(current => nextSettings.chipValues.includes(current) ? current : nextSettings.chipValues[0] ?? nextSettings.minimumBet)
    }).catch(() => {})
    let live = true
    const handle = connectRoulette(event => {
      if (!live || !event?.data) return
      if (event.name === 'roulette-settings-updated') {
        setRouletteSettings(event.data)
        setChipValue(current => event.data.chipValues.includes(current) ? current : event.data.chipValues[0] ?? event.data.minimumBet)
      }
      if (event.name === 'round-state' || event.name === 'round-opened' || event.name === 'round-settled') setRound(event.data)
      if (event.name === 'round-opened') setPlacedBets([])
      if (event.name === 'round-settled' && event.data.winningNumber != null && lastSettledRound.current !== event.data.id) {
        lastSettledRound.current = event.data.id
        const roundId = event.data.id
        const winningNumber = event.data.winningNumber
        const requests = [...inFlightBets.current.values()].filter(entry => entry.roundId === roundId).map(entry => entry.promise)
        Promise.allSettled(requests).then(() => {
          if (!live) return
          const bets = pendingBets.current.filter(bet => bet.roundId === roundId)
          const totalStake = bets.reduce((sum, bet) => sum + bet.amount, 0)
          const returned = bets.reduce((sum, bet) => sum + (isWinner(bet, winningNumber) ? bet.amount * payoutMultiplier[bet.type] : 0), 0)
          const net = returned - totalStake
          setSettlement({ roundId, number: winningNumber, color: colorOf(winningNumber), betCount: bets.length, returned, net, outcome: bets.length === 0 ? 'none' : net > 0 ? 'won' : net < 0 ? 'lost' : 'even' })
          pendingBets.current = pendingBets.current.filter(bet => bet.roundId !== roundId)
          setSpinning(true)
          if (spinTimer.current) clearTimeout(spinTimer.current)
          spinTimer.current = setTimeout(() => {
            setSpinning(false)
            setRecentResults(current => {
              const next = [{ roundId, number: winningNumber, color: colorOf(winningNumber), settledAtUtc: event.data.settledAtUtc ?? new Date().toISOString() }, ...current.filter(item => item.roundId !== roundId)].slice(0, HISTORY_SIZE)
              try { localStorage.setItem(recentStorageKey, JSON.stringify(next)) } catch { /* El historial visual sigue disponible durante esta sesión. */ }
              return next
            })
            getBalance().then(setBalance).catch(() => {})
          }, getRouletteAnimationMs())
        })
      }
    }, setConnection)
    return () => { live = false; if (spinTimer.current) clearTimeout(spinTimer.current); handle.then(connection => connection.stop()).catch(() => {}) }
  }, [setBalance])

  useEffect(() => {
    const tick = () => {
      const closeAt = round?.bettingClosesAtUtc ?? round?.BettingClosesAtUtc
      const deadline = closeAt ? Date.parse(closeAt) : NaN
      if (!Number.isFinite(deadline)) { setSeconds(null); return }
      setSeconds(Math.max(0, Math.ceil((deadline - Date.now()) / 1000)))
    }
    tick(); const timer = setInterval(tick, 500); return () => clearInterval(timer)
  }, [round])

  // Los avisos de confirmación se van solos; los errores quedan hasta la próxima acción.
  useEffect(() => {
    if (!notice) return undefined
    const timer = setTimeout(() => setNotice(''), 2400)
    return () => clearTimeout(timer)
  }, [notice])

  const open = (round?.state ?? round?.State) === 'BettingOpen' && Number.isFinite(seconds) && seconds > 0
  const urgent = open && seconds <= URGENT_SECONDS
  const countdown = Number.isFinite(seconds) ? `${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2,'0')}` : '—'
  const placedTotal = placedBets.reduce((sum, bet) => sum + bet.amount, 0)
  // El último número ganador queda marcado en el paño hasta que apostás en la ronda siguiente.
  const winner = !spinning && settlement && !placedBets.some(bet => bet.roundId !== settlement.roundId) ? settlement.number : null
  const wins = (type, selection) => winner !== null && isWinner({ type, selection }, winner)
  const connectionLabel = { connected: 'Conectado', connecting: 'Conectando', disconnected: 'Sin conexión' }[connection] ?? 'Conectando'

  async function placeOnFelt(type, selection, target) {
    if (busyRef.current) return
    if (!round?.id || !open) { setError('La ronda está cerrada. Esperá la próxima.'); return }
    busyRef.current = true
    setError(''); setNotice('')
    setPending({ target, amount: chipValue })
    const wager = { requestId: crypto.randomUUID(), roundId: round.id, amount: chipValue, type, selection }
    const promise = placeBet(wager)
    inFlightBets.current.set(wager.requestId, { roundId: wager.roundId, promise })
    try {
      const result = await promise
      pendingBets.current = [...pendingBets.current, wager]
      setPlacedBets(current => [...current, { ...wager, target }])
      setBalance(result.balance)
      setNotice(`Apuesta de ${money(chipValue)} aceptada.`)
    } catch (err) { setError(err.message) }
    finally { inFlightBets.current.delete(wager.requestId); busyRef.current = false; setPending(null) }
  }

  function stack(target) {
    const confirmed = chipStacks[target]?.total || 0
    const waiting = pending?.target === target ? pending.amount : 0
    if (!confirmed && !waiting) return null
    // El color de la pila es el de la última ficha apoyada, como en una mesa física.
    const tone = chipTone(Math.max(0, rouletteSettings.chipValues.indexOf(waiting || chipStacks[target].last)))
    return <span key={confirmed + waiting} className={`felt-chip ${tone} ${waiting ? 'felt-chip-pending' : ''}`} aria-label={`Fichas por ${money(confirmed + waiting)}`}>{compactAmount(confirmed + waiting)}</span>
  }
  function hotspot({ type, selection, target, row, column, title, className }) {
    return <button type="button" key={target} className={`bet-hotspot ${className}`} style={track(row, column)} disabled={!open} aria-label={title} title={title} onClick={() => placeOnFelt(type, selection, target)}>{stack(target)}</button>
  }
  const chipRack = <div className="chip-rack" role="radiogroup" aria-label="Valor de la ficha">
    {rouletteSettings.chipValues.map((value, index) => <button type="button" role="radio" key={value} className={`roulette-chip ${chipTone(index)}`} aria-checked={chipValue === value} aria-label={`Ficha de ${money(value)}`} onClick={() => setChipValue(value)}><span>{compactAmount(value)}</span></button>)}
  </div>
  const croupier = open ? 'Hagan sus apuestas' : spinning ? 'No va más' : 'Mesa en pausa'

  return <div className="table-page">
    <header className="table-head">
      <div className="table-title">
        <h1>Ruleta europea</h1>
        <p className={`connection connection-${connection}`}><i className="live-dot"/>{connectionLabel}</p>
      </div>
      <div className={`clock ${urgent ? 'clock-urgent' : ''} ${open ? '' : 'clock-closed'}`} role="timer" aria-label={open ? `Las apuestas cierran en ${countdown}` : 'Apuestas cerradas'}>
        <span className="clock-call">{croupier}</span>
        {open && <span className="clock-figure figure">{countdown}</span>}
      </div>
    </header>

    <div className="stage">
      <aside className="tote" aria-label="Últimos números">
        <h2>Últimos</h2>
        {recentResults.length ? <ol className="tote-list">{recentResults.map((item, index) => {
          const color = colorOf(item.number)
          return <li key={item.roundId ?? `${item.number}-${index}`} className={`tote-row tote-${color} ${index === 0 ? 'tote-latest' : ''}`}><span className="figure">{item.number}</span></li>
        })}</ol> : <p className="tote-empty">Los resultados de esta mesa van a aparecer acá.</p>}
      </aside>
      <RouletteSpin spinning={spinning} result={settlement}/>
    </div>

    <p className="felt-instruction">Tocá un número para apostar pleno, la línea entre dos para una división o el cruce entre cuatro para una esquina.</p>
    <div className={`felt ${open ? '' : 'felt-closed'}`} aria-busy={pending ? 'true' : 'false'}>
      <div className="felt-zero">
        <button type="button" className={`number-cell pocket-green zero-number ${wins('number','0') ? 'is-winner' : ''}`} disabled={!open} aria-label="Apostar al número 0" onClick={() => placeOnFelt('number','0','number:0')}><span className="figure">0</span>{wins('number','0') && <i className="dolly" aria-hidden="true"/>}{stack('number:0')}</button>
        {[1,2,3].map((number, index) => <button type="button" key={number} className="zero-split-hotspot" style={{ '--i': index }} disabled={!open} title={`División entre 0 y ${number}`} aria-label={`Apostar entre 0 y ${number}`} onClick={() => placeOnFelt('split',`0,${number}`,`split:0,${number}`)}>{stack(`split:0,${number}`)}</button>)}
      </div>
      <div className="inside-board">
        {gridRows.map((row,rowIndex)=>row.map((number,columnIndex)=>{
          const winning = wins('number', String(number))
          return <button type="button" key={`number-${number}`} className={`number-cell pocket-${colorOf(number)} ${winning ? 'is-winner' : ''}`} style={track(rowIndex*2+1, columnIndex*2+1)} disabled={!open} aria-label={`Apostar al número ${number}`} onClick={() => placeOnFelt('number',String(number),`number:${number}`)}><span className="figure">{number}</span>{winning && <i className="dolly" aria-hidden="true"/>}{stack(`number:${number}`)}</button>
        }))}
        {gridRows.map((row,rowIndex)=>row.slice(0,-1).map((number,columnIndex)=>{
          const next=number+3, selection=[number,next].sort((a,b)=>a-b).join(',')
          return hotspot({type:'split',selection,target:`split:${selection}`,row:rowIndex*2+1,column:columnIndex*2+2,title:`División entre ${number} y ${next}`,className:'split-hotspot'})
        }))}
        {[0,1].map(rowIndex=>gridRows[rowIndex].map((number,columnIndex)=>{
          const next=gridRows[rowIndex+1][columnIndex], selection=[number,next].sort((a,b)=>a-b).join(',')
          return hotspot({type:'split',selection,target:`split:${selection}`,row:rowIndex*2+2,column:columnIndex*2+1,title:`División entre ${number} y ${next}`,className:'split-hotspot'})
        }))}
        {[0,1].map(rowIndex=>gridRows[rowIndex].slice(0,-1).map((number,columnIndex)=>{
          const selection=[number,number+3,gridRows[rowIndex+1][columnIndex],gridRows[rowIndex+1][columnIndex]+3].sort((a,b)=>a-b).join(',')
          return hotspot({type:'corner',selection,target:`corner:${selection}`,row:rowIndex*2+2,column:columnIndex*2+2,title:`Esquina: ${selection.split(',').join(', ')}`,className:'corner-hotspot'})
        }))}
      </div>
      <div className="board-labels">{dozens.map(([value,label])=><button type="button" key={value} className={wins('dozen', value) ? 'is-winner' : ''} disabled={!open} onClick={() => placeOnFelt('dozen',value,`dozen:${value}`)}><span className="bet-label">{label}</span>{stack(`dozen:${value}`)}</button>)}</div>
      <div className="outside-bets">
        {outsideBets.map(([type,selection,label])=>{
          const target=`${type}:${selection}`
          return <button type="button" key={target} disabled={!open} aria-label={label} className={wins(type, selection) ? 'is-winner' : ''} onClick={() => placeOnFelt(type,selection,target)}>{type==='color' ? <i className={`diamond diamond-${selection}`} aria-hidden="true"/> : <span className="bet-label">{label}</span>}{stack(target)}</button>
        })}
      </div>
    </div>

    <dl className="payouts" aria-label="Pagos">
      <div><dt>Pleno</dt><dd>35 a 1</dd></div>
      <div><dt>División</dt><dd>17 a 1</dd></div>
      <div><dt>Esquina</dt><dd>8 a 1</dd></div>
      <div><dt>Docena</dt><dd>2 a 1</dd></div>
      <div><dt>Color, par o impar, mitades</dt><dd>1 a 1</dd></div>
    </dl>
    <p className="table-note">Con el cero pierden las apuestas exteriores. El servidor valida cada apuesta y anuncia el resultado a todos los jugadores al mismo tiempo.</p>

    <div className="tray">
      {chipRack}
      <div className="tray-figures">
        <div><span>En juego</span><b className="figure">{money(placedTotal)}</b></div>
        <div><span>Saldo</span><b className="figure">{balance === null ? '—' : money(balance)}</b></div>
      </div>
      <p className="tray-status" aria-live="polite">
        {error ? <span className="form-error" role="alert">{error}</span>
          : pending ? <span>Confirmando ficha…</span>
          : notice ? <span className="tray-ok">{notice}</span>
          : <span>Ficha de {money(chipValue)}. Mínimo {money(rouletteSettings.minimumBet)}, máximo {money(rouletteSettings.maximumBet)}.</span>}
      </p>
    </div>
  </div>
}
