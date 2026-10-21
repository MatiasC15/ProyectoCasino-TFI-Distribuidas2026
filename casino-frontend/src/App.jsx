import { lazy, Suspense, useEffect, useState } from 'react'
import { BrowserRouter, Link, Navigate, NavLink, Outlet, Route, Routes, useLocation, useNavigate } from 'react-router-dom'
import { Coins, LayoutGrid, LogOut, Settings } from 'lucide-react'
import { AuthProvider } from './presentation/auth/AuthProvider.jsx'
import { useAuth } from './presentation/auth/useAuth.js'
import { login } from './application/useCases/auth.js'
import { RegisterPage } from './presentation/pages/RegisterPage.jsx'
import { getBalance } from './application/useCases/casino.js'
import { money } from './domain/money.js'
import { GamesPage } from './presentation/pages/GamesPage.jsx'
import { WalletPage } from './presentation/pages/WalletPage.jsx'
import { Wordmark, WheelGlyph, EntryWheel } from './presentation/components/Brand.jsx'
import './App.css'
import './Roulette.css'

const RoulettePage = lazy(() => import('./presentation/pages/RoulettePage.jsx').then(module => ({ default: module.RoulettePage })))
const AdminPage = lazy(() => import('./presentation/pages/AdminPage.jsx').then(module => ({ default: module.AdminPage })))

export default function App() {
  return <BrowserRouter><AuthProvider><Routes>
    <Route path="/login" element={<LoginPage/>}/>
    <Route path="/register" element={<RegisterPage/>}/>
    <Route element={<ProtectedRoute/>}><Route element={<Shell/>}>
      <Route index element={<Navigate to="/games" replace/>}/>
      <Route path="games" element={<GamesPage/>}/>
      <Route path="wallet" element={<WalletPage/>}/>
      <Route path="roulette" element={<Suspense fallback={<div className="page-loading">Preparando la mesa…</div>}><RoulettePage/></Suspense>}/>
      <Route path="admin" element={<AdminRoute/>}/>
    </Route></Route>
    <Route path="*" element={<Navigate to="/games" replace/>}/>
  </Routes></AuthProvider></BrowserRouter>
}

function ProtectedRoute(){return useAuth().token?<Outlet/>:<Navigate to="/login" replace/>}

function AdminRoute(){
  const {role}=useAuth()
  const isAdmin=role==='admin'||role==='superadmin'
  const lastPath=sessionStorage.getItem('casino.last.user.path')
  const safeReturn=lastPath&&!lastPath.startsWith('/admin')?lastPath:'/games'
  return isAdmin?<Suspense fallback={<div className="page-loading">Cargando administración…</div>}><AdminPage/></Suspense>:<Navigate to={safeReturn} replace/>
}

function LoginPage(){
  const {token,setSession}=useAuth(),navigate=useNavigate()
  const [email,setEmail]=useState(''),[password,setPassword]=useState(''),[error,setError]=useState(''),[busy,setBusy]=useState(false)
  if(token)return <Navigate to="/games" replace/>
  async function submit(e){e.preventDefault();setBusy(true);setError('');try{setSession(await login(email.trim(),password));navigate('/games')}catch(err){setError(err.message)}finally{setBusy(false)}}
  return <main className="entry">
    <EntryWheel/>
    <header className="entry-head"><Wordmark/></header>
    <section className="entry-copy">
      <h1>Hagan sus apuestas.</h1>
      <p>Ruleta europea en vivo. Una sola rueda, compartida con todos los que están en la mesa.</p>
    </section>
    <form onSubmit={submit} className="entry-form">
      <h2>Ingresá</h2>
      <label className="field">Correo electrónico<input autoComplete="username" type="email" value={email} onChange={e=>setEmail(e.target.value)} placeholder="nombre@correo.com" required/></label>
      <label className="field">Contraseña<input autoComplete="current-password" type="password" value={password} onChange={e=>setPassword(e.target.value)} required/></label>
      {error&&<p className="form-error" role="alert">{error}</p>}
      <button className="button button-primary" disabled={busy}>{busy?'Ingresando…':'Ingresar'}</button>
      <p className="entry-switch">¿Primera vez? <Link to="/register">Creá tu cuenta</Link></p>
    </form>
    <p className="entry-legal">Proyecto académico. Se juega solo con créditos de práctica.</p>
  </main>
}

function Shell(){
  const {role,clearSession}=useAuth(),[balance,setBalance]=useState(null),navigate=useNavigate(),location=useLocation()
  useEffect(()=>{getBalance().then(setBalance).catch(()=>setBalance(null))},[])
  useEffect(()=>{if(!location.pathname.startsWith('/admin'))sessionStorage.setItem('casino.last.user.path',location.pathname+location.search)},[location.pathname,location.search])
  const isAdmin=role==='admin'||role==='superadmin'
  const links=[['/games','Sala',LayoutGrid],['/roulette','Ruleta',WheelGlyph],['/wallet','Caja',Coins],...(isAdmin?[['/admin','Admin',Settings]]:[])]
  function logout(){clearSession();navigate('/login')}
  return <div className="app">
    <header className="rail">
      <Link to="/games" className="rail-brand" aria-label="Casino Club, ir a la sala"><Wordmark/></Link>
      <nav className="rail-nav" aria-label="Principal">{links.map(([to,label])=><NavLink key={to} to={to} className="rail-link">{label}{to==='/roulette'&&<i className="live-dot" aria-label="en vivo"/>}</NavLink>)}</nav>
      <div className="rail-end">
        <Link to="/wallet" className="rail-balance" title="Ir a la caja"><small>Saldo</small><span className="figure">{balance===null?'—':money(balance)}</span></Link>
        <button type="button" className="rail-exit" onClick={logout}><LogOut size={16} aria-hidden="true"/><span>Salir</span></button>
      </div>
    </header>
    <main className="page"><Outlet context={{balance,setBalance}}/></main>
    <footer className="fineprint">Proyecto académico. Los saldos son créditos de práctica y no representan dinero real.</footer>
    <nav className="tabbar" aria-label="Principal">{links.map(([to,label,Icon])=><NavLink key={to} to={to} className="tab"><Icon size={20} aria-hidden="true"/><span>{label}</span></NavLink>)}</nav>
  </div>
}
