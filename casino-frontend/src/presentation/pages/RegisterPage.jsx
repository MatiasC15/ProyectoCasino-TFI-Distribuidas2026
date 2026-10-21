import { useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { login, register } from '../../application/useCases/auth.js'
import { useAuth } from '../auth/useAuth.js'
import { EntryWheel, Wordmark } from '../components/Brand.jsx'

// Último día de nacimiento permitido: 18 años cumplidos hoy.
function adultCutoff() {
  const date = new Date()
  date.setFullYear(date.getFullYear() - 18)
  return date.toISOString().slice(0, 10)
}

export function RegisterPage() {
  const { token, setSession } = useAuth()
  const navigate = useNavigate()
  const [form, setForm] = useState({ dni: '', nombre: '', apellido: '', fechaNacimiento: '', email: '', password: '' })
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const maxBirthDate = adultCutoff()
  if (token) return <Navigate to="/games" replace />

  function update(event) { setForm(current => ({ ...current, [event.target.name]: event.target.value })) }
  async function submit(event) {
    event.preventDefault()
    if (form.fechaNacimiento > maxBirthDate) { setError('Tenés que ser mayor de 18 años para crear una cuenta.'); return }
    setBusy(true); setError('')
    try {
      await register({ ...form, dni: form.dni.trim(), nombre: form.nombre.trim(), apellido: form.apellido.trim(), email: form.email.trim(), fechaNacimiento: `${form.fechaNacimiento}T00:00:00` })
      setSession(await login(form.email.trim(), form.password))
      navigate('/games')
    } catch (err) { setError(err.message) } finally { setBusy(false) }
  }

  return <main className="entry entry-register">
    <EntryWheel/>
    <header className="entry-head"><Wordmark/></header>
    <section className="entry-copy">
      <h1>Una silla libre en la mesa.</h1>
      <p>Creá tu cuenta y empezá con créditos de práctica. Necesitás ser mayor de 18 años.</p>
    </section>
    <form onSubmit={submit} className="entry-form">
      <h2>Creá tu cuenta</h2>
      <div className="field-grid">
        <label className="field">DNI<input name="dni" inputMode="numeric" pattern="[0-9]{7,8}" maxLength="8" autoComplete="off" value={form.dni} onChange={update} placeholder="12345678" required/></label>
        <label className="field">Fecha de nacimiento<input name="fechaNacimiento" type="date" max={maxBirthDate} value={form.fechaNacimiento} onChange={update} required/></label>
        <label className="field">Nombre<input name="nombre" autoComplete="given-name" value={form.nombre} onChange={update} required/></label>
        <label className="field">Apellido<input name="apellido" autoComplete="family-name" value={form.apellido} onChange={update} required/></label>
        <label className="field field-wide">Correo electrónico<input name="email" type="email" autoComplete="email" value={form.email} onChange={update} placeholder="nombre@correo.com" required/></label>
        <label className="field field-wide">Contraseña<input name="password" type="password" minLength="6" autoComplete="new-password" value={form.password} onChange={update} required/><span className="field-hint">Al menos 6 caracteres.</span></label>
      </div>
      {error && <p className="form-error" role="alert">{error}</p>}
      <button className="button button-primary" disabled={busy}>{busy ? 'Creando cuenta…' : 'Crear cuenta'}</button>
      <p className="entry-switch">¿Ya tenés cuenta? <Link to="/login">Ingresá</Link></p>
    </form>
    <p className="entry-legal">Proyecto académico. Se juega solo con créditos de práctica.</p>
  </main>
}
