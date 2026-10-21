import { money } from '../../domain/money.js'
import { Roulette3D } from './Roulette3D.jsx'

const colorNames = { red: 'Rojo', black: 'Negro', green: 'Cero' }

function outcomeText(result) {
  if (result.betCount === 0) return 'No apostaste en esta ronda.'
  const bets = `${result.betCount} ${result.betCount === 1 ? 'apuesta' : 'apuestas'}`
  if (result.outcome === 'won') return `Ganaste ${money(result.net)} con ${bets}. Cobrás ${money(result.returned)}.`
  if (result.outcome === 'lost') return `Perdiste ${money(-result.net)} en ${bets}.`
  return `Recuperaste lo apostado en ${bets}.`
}

export function RouletteSpin({ spinning, result }) {
  const revealed = !spinning && result
  return <section className="spin" aria-live="polite">
    <div className="spin-wheel"><Roulette3D spinning={spinning} winningNumber={result?.number}/></div>
    <div className="spin-result">
      {revealed
        ? <div className={`result result-${result.color} result-${result.outcome}`} key={result.roundId}>
            <span className="result-figure figure">{result.number}</span>
            <span className="result-color">{colorNames[result.color]}</span>
            <p className="result-outcome">{outcomeText(result)}</p>
          </div>
        : <div className="result result-waiting">
            <span className="result-figure figure" aria-hidden="true">{spinning ? '·' : '—'}</span>
            <p className="result-outcome">{spinning ? 'La bola está girando. El resultado aparece cuando se detiene.' : 'Todavía no hubo giros desde que llegaste.'}</p>
          </div>}
    </div>
  </section>
}
