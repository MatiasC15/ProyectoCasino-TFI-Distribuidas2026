import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getGames } from '../../application/useCases/casino.js'
import { money } from '../../domain/money.js'
import { WheelArt } from '../components/Brand.jsx'

const upcoming = ['Blackjack', 'Punto banco', 'Póker de mesa']

export function GamesPage() {
  const [games, setGames] = useState([])
  useEffect(() => { getGames().then(setGames).catch(() => setGames([])) }, [])
  const roulette = games.find(game => (game.nombre || '').toLowerCase().includes('ruleta'))
  const minimum = roulette ? money(roulette.apuestaMinima) : null

  return <div className="lobby">
    <section className="lobby-hero">
      <div className="lobby-hero-copy">
        <p className="lobby-status"><i className="live-dot"/>Mesa abierta ahora</p>
        <h1>Ruleta europea</h1>
        <p className="lobby-lede">Treinta y siete casilleros, un solo cero y una rueda que comparten todos los jugadores. Apostás sobre el paño y el resultado llega en vivo.</p>
        <Link to="/roulette" className="button button-primary">Sentarse a la mesa</Link>
      </div>
      <WheelArt className="lobby-wheel"/>
    </section>

    <section className="tables" aria-labelledby="tables-title">
      <h2 id="tables-title">Mesas</h2>
      <ul className="table-list">
        <li>
          <Link to="/roulette" className="table-row">
            <span className="table-name">Ruleta europea</span>
            <span className="table-detail">Mesa compartida, en vivo</span>
            <span className="table-detail">{minimum ? `Desde ${minimum}` : 'Límites de la mesa al entrar'}</span>
            <span className="table-state"><i className="live-dot"/>Abierta</span>
          </Link>
        </li>
        {upcoming.map(name => <li key={name} className="table-row table-row-soon" aria-disabled="true">
          <span className="table-name">{name}</span>
          <span className="table-detail">Próximamente</span>
        </li>)}
      </ul>
    </section>
  </div>
}
