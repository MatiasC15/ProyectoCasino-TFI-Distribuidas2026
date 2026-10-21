import { useCallback, useEffect, useState } from 'react'
import { Activity, CircleDollarSign, Clock3, RefreshCw, Save, Settings2, ShieldCheck, Users } from 'lucide-react'
import { useAuth } from '../auth/useAuth.js'
import { money } from '../../domain/money.js'
import { PageHeading } from '../components/PageHeading.jsx'
import {
  createAdmin, getAdminAuditLog, getAdminStatistics, getAdminUsers,
  getRouletteAdminSettings, saveRouletteAdminSettings,
} from '../../application/useCases/admin.js'
import './Admin.css'

const sections = [
  ['overview', 'Resumen'],
  ['roulette', 'Ruleta y apuestas'],
  ['admins', 'Administradores'],
  ['audit', 'Registro de cambios'],
]
const today = new Date().toISOString().slice(0, 10)

export function AdminPage() {
  const { role } = useAuth()
  const [section, setSection] = useState('overview')
  const [statistics, setStatistics] = useState(null)
  const [settings, setSettings] = useState(null)
  const [admins, setAdmins] = useState([])
  const [audit, setAudit] = useState([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [chipText, setChipText] = useState('')
  const [adminForm, setAdminForm] = useState({ dni: '', nombre: '', apellido: '', fechaNacimiento: '', email: '', password: '' })

  const refresh = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const [nextStats, nextSettings, nextAdmins, nextAudit] = await Promise.all([
        getAdminStatistics(), getRouletteAdminSettings(), getAdminUsers(), getAdminAuditLog(),
      ])
      setStatistics(nextStats)
      setSettings(nextSettings)
      setChipText(nextSettings.chipValues.join(', '))
      setAdmins(nextAdmins)
      setAudit(nextAudit)
    } catch (requestError) { setError(requestError.message) }
    finally { setLoading(false) }
  }, [])

  useEffect(() => {
    let active = true
    Promise.all([getAdminStatistics(), getRouletteAdminSettings(), getAdminUsers(), getAdminAuditLog()])
      .then(([nextStats, nextSettings, nextAdmins, nextAudit]) => {
        if (!active) return
        setStatistics(nextStats); setSettings(nextSettings); setChipText(nextSettings.chipValues.join(', '))
        setAdmins(nextAdmins); setAudit(nextAudit)
      })
      .catch(requestError => { if (active) setError(requestError.message) })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [])

  async function saveSettings(event) {
    event.preventDefault()
    setSaving(true); setError(''); setSuccess('')
    const chipValues = chipText.split(',').map(value => Number(value.trim()))
    try {
      const saved = await saveRouletteAdminSettings({
        bettingDurationSeconds: Number(settings.bettingDurationSeconds),
        minimumBet: Number(settings.minimumBet),
        maximumBet: Number(settings.maximumBet),
        chipValues,
      })
      setSettings(saved); setChipText(saved.chipValues.join(', '))
      setAudit(await getAdminAuditLog())
      setSuccess('La configuración quedó guardada. El intervalo nuevo se aplica en la próxima ronda.')
    } catch (requestError) { setError(requestError.message) }
    finally { setSaving(false) }
  }

  async function submitAdmin(event) {
    event.preventDefault()
    setSaving(true); setError(''); setSuccess('')
    try {
      await createAdmin({ ...adminForm, fechaNacimiento: `${adminForm.fechaNacimiento}T00:00:00` })
      setAdminForm({ dni: '', nombre: '', apellido: '', fechaNacimiento: '', email: '', password: '' })
      setSuccess('Se creó la cuenta de administrador. Debe iniciar sesión para recibir sus permisos.')
      const [nextAdmins, nextAudit, nextStats] = await Promise.all([getAdminUsers(), getAdminAuditLog(), getAdminStatistics()])
      setAdmins(nextAdmins); setAudit(nextAudit); setStatistics(nextStats)
    } catch (requestError) { setError(requestError.message) }
    finally { setSaving(false) }
  }

  const canCreateAdmins = role === 'superadmin'

  return <>
    <PageHeading eyebrow="CENTRO DE CONTROL" title="Administración" description="Configurá la mesa y revisá la actividad de la plataforma.">
      <button className="admin-refresh" type="button" onClick={refresh} disabled={loading}><RefreshCw size={14} className={loading ? 'admin-refreshing' : ''}/> Actualizar</button>
    </PageHeading>
    <nav className="admin-tabs" aria-label="Secciones de administración">
      {sections.map(([key, label]) => <button type="button" key={key} className={section === key ? 'admin-tab admin-tab-active' : 'admin-tab'} onClick={() => { setSection(key); setError(''); setSuccess('') }}>{label}</button>)}
    </nav>
    {error && <p className="inline-error admin-alert" role="alert">{error}</p>}
    {success && <p className="inline-success admin-alert" role="status">{success}</p>}
    {loading && !statistics ? <div className="admin-loading">Cargando datos del panel…</div> : <>
      {section === 'overview' && <Overview statistics={statistics} onOpenSettings={() => setSection('roulette')}/>}
      {section === 'roulette' && settings && <section className="admin-panel">
        <div className="admin-panel-heading"><span className="admin-icon"><Settings2 size={17}/></span><div><h2>Reglas de la mesa</h2><p>Los límites se validan en el servidor para cada apuesta.</p></div></div>
        <form className="admin-settings-form" onSubmit={saveSettings}>
          <label>Tiempo de apuestas por ronda <span className="admin-unit">segundos</span><input type="number" min="5" max="300" step="1" required value={settings.bettingDurationSeconds} onChange={event => setSettings({ ...settings, bettingDurationSeconds: event.target.value })}/><small>Entre 5 y 300 segundos. Se aplicará al iniciar la próxima ronda.</small></label>
          <div className="admin-form-pair"><label>Apuesta mínima<input type="number" min="0.01" max="1000000" step="0.01" required value={settings.minimumBet} onChange={event => setSettings({ ...settings, minimumBet: event.target.value })}/></label><label>Apuesta máxima<input type="number" min="0.01" max="1000000" step="0.01" required value={settings.maximumBet} onChange={event => setSettings({ ...settings, maximumBet: event.target.value })}/></label></div>
          <label>Valores de ficha <span className="admin-unit">créditos virtuales, separados por coma</span><input type="text" inputMode="decimal" required value={chipText} onChange={event => setChipText(event.target.value)} placeholder="10, 25, 50, 100"/><small>Entre 1 y 12 fichas únicas, cada una dentro de los límites de apuesta.</small></label>
          <div className="admin-preview-chips" aria-label="Vista previa de fichas">{chipText.split(',').map((value, index) => value.trim() && <span key={`${value}-${index}`}>{value.trim()}</span>)}</div>
          <div className="admin-form-footer"><span>{settings.updatedByEmail ? `Último cambio por ${settings.updatedByEmail}` : 'Configuración inicial'} · {new Date(settings.updatedAtUtc).toLocaleString()}</span><button className="button button-primary" disabled={saving}><Save size={15}/>{saving ? 'Guardando…' : 'Guardar cambios'}</button></div>
        </form>
      </section>}
      {section === 'roulette' && !settings && <section className="admin-panel admin-note"><Clock3 size={18}/><span>La configuración de la ruleta todavía no está disponible. Esperá unos segundos y actualizá el panel.</span></section>}
      {section === 'admins' && <AdminsSection admins={admins} canCreate={canCreateAdmins} form={adminForm} setForm={setAdminForm} onSubmit={submitAdmin} saving={saving} today={today}/>}
      {section === 'audit' && <AuditSection entries={audit}/>}
    </>}
  </>
}

function Overview({ statistics, onOpenSettings }) {
  if (!statistics) return <p className="admin-note">No se pudieron cargar las estadísticas.</p>
  const cards = [
    ['Usuarios registrados', statistics.registeredUsers, Users],
    ['Administradores', statistics.adminUsers, ShieldCheck],
    ['Rondas completadas', statistics.totalRounds, Activity],
    ['Apuestas de ruleta', statistics.totalBets, CircleDollarSign],
    ['Créditos apostados', money(statistics.totalWagered), CircleDollarSign],
    ['Balance de la mesa', money(statistics.houseNet), CircleDollarSign],
  ]
  return <div className="admin-overview">
    <div className="admin-stat-grid">{cards.map(([label, value, Icon]) => <article className="admin-stat-card" key={label}><span className="admin-stat-icon"><Icon size={16}/></span><small>{label}</small><strong>{value}</strong></article>)}</div>
    <section className="admin-panel admin-today"><div><span className="eyebrow muted">ACTIVIDAD DE HOY</span><h2>{statistics.roundsToday} rondas <span>·</span> {statistics.betsToday} apuestas</h2><p>Apostado {money(statistics.todayWagered)} <span>·</span> Premios pagados {money(statistics.todayPaid)}</p></div><button type="button" className="admin-secondary" onClick={onOpenSettings}><Settings2 size={15}/> Ajustar mesa</button></section>
    <section className="admin-panel admin-rounds"><div className="admin-panel-heading"><span className="admin-icon"><Activity size={17}/></span><div><h2>Rondas recientes</h2><p>Números ganadores y créditos procesados.</p></div></div>
      {statistics.recentRounds.length ? <div className="admin-table-wrap"><table className="admin-table"><thead><tr><th>Inicio</th><th>Resultado</th><th>Apuestas</th><th>Volumen</th><th>Premios</th></tr></thead><tbody>{statistics.recentRounds.map(round => <tr key={round.id}><td>{new Date(round.openedAtUtc).toLocaleString()}</td><td>{round.winningNumber == null ? <span className="admin-pending">En curso</span> : <span className={`admin-result ${round.winningNumber === 0 ? 'result-green' : 'result-number'}`}>{round.winningNumber}</span>}</td><td>{round.betCount}</td><td>{money(round.totalWagered)}</td><td>{money(round.totalPaid)}</td></tr>)}</tbody></table></div> : <p className="admin-empty">Todavía no hay rondas registradas.</p>}
    </section>
    <div className="admin-stat-footnote">El balance de la mesa se calcula como apuestas menos premios sobre créditos virtuales. No representa dinero real.</div>
  </div>
}

function AdminsSection({ admins, canCreate, form, setForm, onSubmit, saving, today }) {
  const update = event => setForm(current => ({ ...current, [event.target.name]: event.target.value }))
  return <div className="admin-two-column">
    <section className="admin-panel"><div className="admin-panel-heading"><span className="admin-icon"><Users size={17}/></span><div><h2>Equipo administrador</h2><p>Las cuentas de usuario no reciben permisos automáticamente.</p></div></div>
      {admins.length ? <div className="admin-table-wrap"><table className="admin-table"><thead><tr><th>Nombre</th><th>Correo</th><th>Rol</th><th>Créditos</th></tr></thead><tbody>{admins.map(admin => <tr key={admin.id}><td>{admin.name}</td><td>{admin.email}</td><td><span className={`admin-role ${admin.role === 'superadmin' ? 'role-super' : ''}`}>{admin.role === 'superadmin' ? 'Superadmin' : 'Admin'}</span></td><td>{money(admin.balance)}</td></tr>)}</tbody></table></div> : <p className="admin-empty">No hay administradores.</p>}
    </section>
    {canCreate ? <section className="admin-panel"><div className="admin-panel-heading"><span className="admin-icon"><ShieldCheck size={17}/></span><div><h2>Crear administrador</h2><p>Solo el superadmin puede dar este permiso.</p></div></div>
      <form className="admin-create-form" onSubmit={onSubmit}>
        <label>DNI<input name="dni" inputMode="numeric" pattern="[0-9]{7,8}" maxLength="8" required value={form.dni} onChange={update}/></label>
        <div className="admin-form-pair"><label>Nombre<input name="nombre" minLength="2" maxLength="100" required value={form.nombre} onChange={update}/></label><label>Apellido<input name="apellido" minLength="2" maxLength="100" required value={form.apellido} onChange={update}/></label></div>
        <label>Fecha de nacimiento<input name="fechaNacimiento" type="date" max={today} required value={form.fechaNacimiento} onChange={update}/></label>
        <label>Correo electrónico<input name="email" type="email" autoComplete="off" required value={form.email} onChange={update}/></label>
        <label>Contraseña temporal<input name="password" type="password" minLength="12" maxLength="128" autoComplete="new-password" required value={form.password} onChange={update}/><small>Al menos 12 caracteres. Compartila por un canal seguro.</small></label>
        <button className="button button-primary" disabled={saving}>{saving ? 'Creando…' : 'Crear cuenta admin'}<span>→</span></button>
      </form>
    </section> : <section className="admin-panel admin-note"><ShieldCheck size={18}/><span>La creación de nuevas cuentas administrador está reservada al superadmin.</span></section>}
  </div>
}

function AuditSection({ entries }) {
  return <section className="admin-panel"><div className="admin-panel-heading"><span className="admin-icon"><Activity size={17}/></span><div><h2>Registro de cambios</h2><p>Últimas 30 acciones administrativas.</p></div></div>
    {entries.length ? <div className="admin-table-wrap"><table className="admin-table"><thead><tr><th>Fecha</th><th>Acción</th><th>Realizado por</th><th>Detalle</th></tr></thead><tbody>{entries.map(entry => <tr key={entry.id}><td>{new Date(entry.createdAtUtc).toLocaleString()}</td><td>{entry.action === 'admin.created' ? 'Creó administrador' : entry.action === 'roulette.settings.updated' ? 'Actualizó reglas de ruleta' : entry.action}</td><td>{entry.actorEmail}</td><td>{auditSummary(entry)}</td></tr>)}</tbody></table></div> : <p className="admin-empty">Los cambios administrativos aparecerán acá.</p>}
  </section>
}

function auditSummary(entry) {
  try {
    const details = JSON.parse(entry.detailsJson)
    if (entry.action === 'roulette.settings.updated') return `${details.after.bettingDurationSeconds}s por ronda · ${money(details.after.minimumBet)}–${money(details.after.maximumBet)} · fichas ${details.after.chipValues.join(', ')}`
    if (entry.action === 'admin.created') return `ID ${details.adminId}`
  } catch { return 'Detalle no disponible' }
  return '—'
}
