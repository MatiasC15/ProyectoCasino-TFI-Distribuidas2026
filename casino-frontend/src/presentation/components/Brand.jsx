const wheelOrder = [0,32,15,19,4,21,2,25,17,34,6,27,13,36,11,30,8,23,10,5,24,16,33,1,20,14,31,9,22,18,29,7,28,12,35,3,26]
const red = new Set([1,3,5,7,9,12,14,16,18,19,21,23,25,27,30,32,34,36])

/** Logotipo: el rombo rojo es el mismo que marca "rojo" en el paño. */
export function Wordmark() {
  return <span className="wordmark"><i className="wordmark-diamond" aria-hidden="true"/>Casino Club</span>
}

/** Ícono de rueda para la barra inferior, con la misma API de tamaño que los íconos de lucide. */
export function WheelGlyph({ size = 20 }) {
  return <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" aria-hidden="true">
    <circle cx="12" cy="12" r="9"/><circle cx="12" cy="12" r="3"/>
    {[0,60,120,180,240,300].map(angle => <line key={angle} x1="12" y1="3" x2="12" y2="9" transform={`rotate(${angle} 12 12)`}/>)}
  </svg>
}

const point = (radius, degrees) => {
  const radians = (degrees - 90) * Math.PI / 180
  return [100 + radius * Math.cos(radians), 100 + radius * Math.sin(radians)]
}
function pocketPath(index, outer, inner) {
  const step = 360 / 37
  const start = index * step - step / 2, end = start + step
  const [ax, ay] = point(outer, start), [bx, by] = point(outer, end), [cx, cy] = point(inner, end), [dx, dy] = point(inner, start)
  return `M${ax} ${ay} A${outer} ${outer} 0 0 1 ${bx} ${by} L${cx} ${cy} A${inner} ${inner} 0 0 0 ${dx} ${dy}Z`
}

/** Rueda europea dibujada en SVG con el orden real de los casilleros. Decorativa. */
export function WheelArt({ className = '' }) {
  return <svg className={`wheel-art ${className}`} viewBox="0 0 200 200" aria-hidden="true">
    <circle cx="100" cy="100" r="99" fill="#2a1a10"/>
    <circle cx="100" cy="100" r="93" fill="none" stroke="#c9a463" strokeWidth="1.5"/>
    <g className="wheel-art-rotor">
      {wheelOrder.map((number, index) => <path key={number} d={pocketPath(index, 90, 62)} fill={number === 0 ? '#1d8a57' : red.has(number) ? '#b52f37' : '#0b1410'} stroke="#c9a463" strokeWidth="0.5"/>)}
      {wheelOrder.map((number, index) => {
        const [x, y] = point(81, index * 360 / 37)
        return <text key={number} x={x} y={y} transform={`rotate(${index * 360 / 37} ${x} ${y})`} textAnchor="middle" dominantBaseline="central" fontSize="7.5" fontWeight="700" fill="#efe6d2" fontFamily="var(--display)">{number}</text>
      })}
      <circle cx="100" cy="100" r="62" fill="#103a2b" stroke="#c9a463" strokeWidth="1"/>
      <circle cx="100" cy="100" r="40" fill="none" stroke="rgb(201 164 99 / 0.35)" strokeWidth="0.75"/>
      {[0,90,180,270].map(angle => <line key={angle} x1="100" y1="72" x2="100" y2="128" stroke="#c9a463" strokeWidth="2.5" strokeLinecap="round" transform={`rotate(${angle + 45} 100 100)`}/>)}
      <circle cx="100" cy="100" r="9" fill="#c9a463"/>
    </g>
  </svg>
}

/** Rueda grande y recortada que acompaña las pantallas de ingreso. */
export function EntryWheel() {
  return <div className="entry-wheel"><WheelArt/></div>
}
