import { useEffect, useRef } from 'react'
import * as THREE from 'three'
import { getRouletteAnimationMs, prefersReducedMotion } from './rouletteTiming.js'

const wheelNumbers = [0,32,15,19,4,21,2,25,17,34,6,27,13,36,11,30,8,23,10,5,24,16,33,1,20,14,31,9,22,18,29,7,28,12,35,3,26]
// El 36 es rojo en la ruleta europea; antes faltaba y la rueda lo pintaba negro.
const redNumbers = new Set([1,3,5,7,9,12,14,16,18,19,21,23,25,27,30,32,34,36])
const GOLD = 0xc9a961

// Geometría compartida por la animación.
const RIM_RADIUS = 2.04
const RIM_HEIGHT = 0.48
const POCKET_RADIUS = 1.625
const POCKET_HEIGHT = 0.415
const OVERVIEW = { position: new THREE.Vector3(0, 5.3, 5.6), look: new THREE.Vector3(0, 0.05, 0), fov: 36 }

const clamp01 = value => Math.min(1, Math.max(0, value))
const easeOutCubic = t => 1 - Math.pow(1 - t, 3)
const easeInOutCubic = t => t < 0.5 ? 4 * t * t * t : 1 - Math.pow(-2 * t + 2, 3) / 2
const smoothstep = t => t * t * (3 - 2 * t)

function makeNumberTexture(number, color, renderer) {
  const canvas = document.createElement('canvas')
  canvas.width = 256
  canvas.height = 128
  const context = canvas.getContext('2d')
  context.font = '600 104px Georgia, "Times New Roman", serif'
  context.textAlign = 'center'
  context.textBaseline = 'middle'
  context.shadowColor = 'rgba(0,0,0,0.85)'
  context.shadowBlur = 8
  context.fillStyle = color
  context.fillText(String(number), 128, 68)
  const texture = new THREE.CanvasTexture(canvas)
  texture.colorSpace = THREE.SRGBColorSpace
  texture.anisotropy = renderer.capabilities.getMaxAnisotropy()
  return texture
}

function addRing(parent, radius, tube, height, material) {
  const ring = new THREE.Mesh(new THREE.TorusGeometry(radius, tube, 12, 160), material)
  ring.rotation.x = Math.PI / 2
  ring.position.y = height
  ring.castShadow = true
  ring.receiveShadow = true
  parent.add(ring)
  return ring
}

function createWheel(scene, renderer) {
  const gold = new THREE.MeshStandardMaterial({ color: GOLD, metalness: 0.85, roughness: 0.25 })
  const darkMetal = new THREE.MeshStandardMaterial({ color: 0x2a2317, metalness: 0.7, roughness: 0.38 })
  const wood = new THREE.MeshStandardMaterial({ color: 0x2f1c10, metalness: 0.1, roughness: 0.48 })
  const bowl = new THREE.MeshStandardMaterial({ color: 0x15281e, metalness: 0.2, roughness: 0.5 })

  // La madera y la pista exterior quedan fijas; solo gira el cabezal con los números.
  const table = new THREE.Mesh(new THREE.CylinderGeometry(2.3, 2.36, 0.32, 128), wood)
  table.position.y = -0.1
  table.receiveShadow = true
  scene.add(table)
  const track = new THREE.Mesh(new THREE.CylinderGeometry(2.12, 2.18, 0.2, 128), bowl)
  track.position.y = 0.2
  track.receiveShadow = true
  scene.add(track)
  addRing(scene, 2.2, 0.06, 0.24, gold)

  const wheel = new THREE.Group()
  scene.add(wheel)
  const body = new THREE.Mesh(new THREE.CylinderGeometry(1.96, 2.0, 0.23, 128), bowl)
  body.position.y = 0.2
  body.castShadow = true
  body.receiveShadow = true
  wheel.add(body)

  const pocketBed = new THREE.Mesh(new THREE.CylinderGeometry(1.82, 1.86, 0.055, 128), darkMetal)
  pocketBed.position.y = 0.33
  wheel.add(pocketBed)
  addRing(wheel, 1.85, 0.024, 0.37, gold)
  addRing(wheel, 1.42, 0.018, 0.37, gold)

  const slotMaterials = wheelNumbers.map(number => new THREE.MeshStandardMaterial({
    color: number === 0 ? 0x17774a : redNumbers.has(number) ? 0xa8313a : 0x141716,
    metalness: 0.15,
    roughness: 0.38,
  }))
  const step = (Math.PI * 2) / wheelNumbers.length
  const slotGeometry = new THREE.BoxGeometry(0.268, 0.043, 0.37)
  const dividerGeometry = new THREE.BoxGeometry(0.014, 0.05, 0.39)
  wheelNumbers.forEach((number, index) => {
    const angle = index * step
    const slot = new THREE.Mesh(slotGeometry, slotMaterials[index])
    slot.position.set(Math.sin(angle) * POCKET_RADIUS, 0.37, Math.cos(angle) * POCKET_RADIUS)
    slot.rotation.y = angle
    slot.receiveShadow = true
    wheel.add(slot)

    const divider = new THREE.Mesh(dividerGeometry, gold)
    divider.position.set(Math.sin(angle - step / 2) * POCKET_RADIUS, 0.392, Math.cos(angle - step / 2) * POCKET_RADIUS)
    divider.rotation.y = angle - step / 2
    divider.castShadow = true
    wheel.add(divider)

    const label = new THREE.Sprite(new THREE.SpriteMaterial({
      map: makeNumberTexture(number, number === 0 ? '#c9f5d2' : '#f8f1dc', renderer),
      transparent: true,
      depthWrite: false,
      toneMapped: false,
    }))
    label.position.set(Math.sin(angle) * 1.9, 0.42, Math.cos(angle) * 1.9)
    label.scale.set(0.25, 0.125, 1)
    wheel.add(label)
  })

  const hubBase = new THREE.Mesh(new THREE.CylinderGeometry(0.57, 0.63, 0.12, 64), darkMetal)
  hubBase.position.y = 0.4
  hubBase.castShadow = true
  wheel.add(hubBase)
  addRing(wheel, 0.55, 0.026, 0.465, gold)
  const hub = new THREE.Mesh(new THREE.SphereGeometry(0.46, 48, 24), new THREE.MeshStandardMaterial({ color: 0xb79b5c, metalness: 0.9, roughness: 0.22 }))
  hub.scale.y = 0.3
  hub.position.y = 0.47
  hub.castShadow = true
  wheel.add(hub)
  // Torreta central: da una referencia visual clara de que el cabezal gira.
  const spindle = new THREE.Mesh(new THREE.CylinderGeometry(0.05, 0.08, 0.32, 24), gold)
  spindle.position.y = 0.6
  wheel.add(spindle)
  const armGeometry = new THREE.CylinderGeometry(0.022, 0.022, 0.62, 12)
  for (let arm = 0; arm < 4; arm++) {
    const bar = new THREE.Mesh(armGeometry, gold)
    bar.rotation.z = Math.PI / 2
    bar.rotation.y = (arm * Math.PI) / 4
    bar.position.y = 0.66
    wheel.add(bar)
  }

  const ballPivot = new THREE.Group()
  const ball = new THREE.Mesh(new THREE.SphereGeometry(0.072, 32, 24), new THREE.MeshPhysicalMaterial({ color: 0xfaf6ec, metalness: 0.05, roughness: 0.12, clearcoat: 1 }))
  ball.castShadow = true
  ball.position.set(0, POCKET_HEIGHT, POCKET_RADIUS)
  ballPivot.add(ball)
  scene.add(ballPivot)

  return { wheel, ballPivot, ball, step }
}

export function Roulette3D({ spinning, winningNumber }) {
  const hostRef = useRef(null)
  const sceneState = useRef(null)

  useEffect(() => {
    const host = hostRef.current
    if (!host) return undefined

    let renderer
    try {
      renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true, powerPreference: 'high-performance' })
    } catch {
      host.dataset.unavailable = 'true'
      host.textContent = 'La vista 3D no está disponible en este navegador.'
      return undefined
    }
    renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2))
    renderer.outputColorSpace = THREE.SRGBColorSpace
    renderer.toneMapping = THREE.ACESFilmicToneMapping
    renderer.toneMappingExposure = 1.05
    renderer.shadowMap.enabled = true
    renderer.shadowMap.type = THREE.PCFSoftShadowMap
    host.appendChild(renderer.domElement)

    const scene = new THREE.Scene()
    const camera = new THREE.PerspectiveCamera(OVERVIEW.fov, 1, 0.1, 30)
    camera.position.copy(OVERVIEW.position)
    camera.lookAt(OVERVIEW.look)
    scene.add(new THREE.HemisphereLight(0xe6ecdc, 0x1d150e, 1.6))
    const keyLight = new THREE.DirectionalLight(0xffe4b0, 3.2)
    keyLight.position.set(-3.5, 7, 4)
    keyLight.castShadow = true
    keyLight.shadow.mapSize.set(1024, 1024)
    keyLight.shadow.camera.left = -3
    keyLight.shadow.camera.right = 3
    keyLight.shadow.camera.top = 3
    keyLight.shadow.camera.bottom = -3
    scene.add(keyLight)
    // Luz de "lámpara de mesa" sobre el centro: da brillo dorado sin quemar los colores.
    const lamp = new THREE.SpotLight(0xfff0cc, 30, 12, Math.PI / 5, 0.6, 1.6)
    lamp.position.set(0, 6, 0)
    scene.add(lamp)
    const rimLight = new THREE.PointLight(0x6fbf8f, 10, 10)
    rimLight.position.set(3, 2.2, -2.5)
    scene.add(rimLight)

    const wheelScene = createWheel(scene, renderer)
    const reduced = prefersReducedMotion()
    sceneState.current = {
      scene, camera, renderer, ...wheelScene,
      spin: null, cameraMove: null, look: OVERVIEW.look.clone(),
      lastFrame: performance.now(), visible: true, reduced,
      // La bola arranca apoyada en el cero y gira con el cabezal.
      ballInPocket: true, pocketOffset: 0, focused: false, baseFov: OVERVIEW.fov,
    }

    const resize = () => {
      const width = Math.max(host.clientWidth, 1)
      const height = Math.max(host.clientHeight, 1)
      renderer.setSize(width, height, false)
      camera.aspect = width / height
      // En contenedores angostos (celular) abrimos el campo para que la rueda entre completa.
      sceneState.current.baseFov = camera.aspect < 1.25 ? OVERVIEW.fov + 8 : OVERVIEW.fov
      if (!sceneState.current.spin && !sceneState.current.cameraMove && !sceneState.current.focused) camera.fov = sceneState.current.baseFov
      camera.updateProjectionMatrix()
    }
    const resizeObserver = new ResizeObserver(resize)
    resizeObserver.observe(host)
    resize()
    // Sin pantalla visible no renderizamos: ahorra batería y GPU al scrollear.
    const visibilityObserver = new IntersectionObserver(([entry]) => { if (sceneState.current) sceneState.current.visible = entry.isIntersecting })
    visibilityObserver.observe(host)

    renderer.setAnimationLoop(now => {
      const state = sceneState.current
      if (!state) return
      const delta = Math.min((now - state.lastFrame) / 1000, 0.1)
      state.lastFrame = now
      const spin = state.spin

      if (spin) {
        const p = clamp01((now - spin.startedAt) / spin.duration)
        // Cabezal: gira en sentido contrario a la bola y desacelera hasta detenerse.
        const wheelAngle = spin.wheelStart + spin.wheelTurns * easeOutCubic(p)
        state.wheel.rotation.y = wheelAngle

        if (p < spin.land) {
          const q = p / spin.land
          state.ballPivot.rotation.y = spin.ballStart + spin.ballTravel * easeOutCubic(q)
          // 1) sube a la pista, 2) rueda por el borde, 3) cae en espiral hacia los casilleros.
          const climb = smoothstep(clamp01(p / 0.08))
          const drop = easeInOutCubic(clamp01((p - spin.dropStart) / (spin.land - spin.dropStart)))
          const radius = THREE.MathUtils.lerp(THREE.MathUtils.lerp(spin.radiusStart, RIM_RADIUS, climb), POCKET_RADIUS, drop)
          const height = THREE.MathUtils.lerp(THREE.MathUtils.lerp(spin.heightStart, RIM_HEIGHT, climb), POCKET_HEIGHT, drop)
          state.ball.position.z = radius
          state.ball.position.y = height + Math.abs(Math.sin(drop * Math.PI * 3)) * 0.05 * (1 - drop)
        } else {
          // Ya dentro del casillero: la bola viaja con la rueda y da un último rebote corto.
          const settle = clamp01((p - spin.land) / (1 - spin.land))
          state.ballPivot.rotation.y = wheelAngle + spin.pocketOffset
          state.ball.position.z = POCKET_RADIUS
          state.ball.position.y = POCKET_HEIGHT + Math.abs(Math.sin(settle * Math.PI * 2)) * 0.022 * (1 - settle)
        }

        const focus = smoothstep(clamp01((p - 0.6) / 0.4))
        state.camera.position.lerpVectors(spin.cameraFrom, spin.cameraTo, focus)
        state.look.lerpVectors(spin.lookFrom, spin.lookTo(state.wheel.rotation.y), focus)
        state.camera.fov = THREE.MathUtils.lerp(spin.fovFrom, spin.fovTo, focus)
        state.camera.lookAt(state.look)
        state.camera.updateProjectionMatrix()

        if (p >= 1) {
          state.spin = null
          state.focused = true
          state.ballInPocket = true
          state.pocketOffset = spin.pocketOffset
          // Mantiene el primer plano del resultado y luego vuelve a la vista general.
          state.returnAt = now + (state.reduced ? 1200 : 3200)
        }
      } else {
        // Reposo: el cabezal gira lento, como en una mesa real entre rondas.
        if (!state.reduced) state.wheel.rotation.y -= delta * 0.12
        if (state.ballInPocket) state.ballPivot.rotation.y = state.wheel.rotation.y + state.pocketOffset

        if (state.focused && now >= state.returnAt) {
          state.focused = false
          state.cameraMove = { startedAt: now, duration: state.reduced ? 1 : 1400, from: state.camera.position.clone(), lookFrom: state.look.clone(), fovFrom: state.camera.fov }
        }
        if (state.focused) {
          // Sigue enfocando el casillero ganador mientras la rueda gira despacio.
          const pocket = pocketPosition(state.wheel.rotation.y + state.pocketOffset)
          state.look.copy(pocket)
          state.camera.position.copy(closeUpCamera(state.wheel.rotation.y + state.pocketOffset))
          state.camera.lookAt(state.look)
        }
        const move = state.cameraMove
        if (move) {
          const t = easeInOutCubic(clamp01((now - move.startedAt) / move.duration))
          state.camera.position.lerpVectors(move.from, OVERVIEW.position, t)
          state.look.lerpVectors(move.lookFrom, OVERVIEW.look, t)
          state.camera.fov = THREE.MathUtils.lerp(move.fovFrom, state.baseFov, t)
          state.camera.lookAt(state.look)
          state.camera.updateProjectionMatrix()
          if (t >= 1) state.cameraMove = null
        }
      }
      if (state.visible) renderer.render(state.scene, state.camera)
    })

    return () => {
      resizeObserver.disconnect()
      visibilityObserver.disconnect()
      renderer.setAnimationLoop(null)
      scene.traverse(object => {
        if (object.geometry) object.geometry.dispose()
        if (object.material) {
          const materials = Array.isArray(object.material) ? object.material : [object.material]
          materials.forEach(material => {
            if (material.map) material.map.dispose()
            material.dispose()
          })
        }
      })
      renderer.dispose()
      renderer.domElement.remove()
      sceneState.current = null
    }
  }, [])

  useEffect(() => {
    const state = sceneState.current
    if (!state || !spinning || !Number.isInteger(winningNumber) || winningNumber < 0 || winningNumber > 36) return
    const index = wheelNumbers.indexOf(winningNumber)
    const now = performance.now()
    const duration = getRouletteAnimationMs()
    const land = 0.84
    const wheelStart = state.wheel.rotation.y
    // La rueda gira en sentido negativo y la bola en positivo, como en una mesa real.
    const wheelTurns = -Math.PI * 2 * (state.reduced ? 0.25 : 3)
    const pocketOffset = index * state.step
    const wheelAtLanding = wheelStart + wheelTurns * easeOutCubic(land)
    const ballStart = state.ballPivot.rotation.y
    const normalize = angle => ((angle % (Math.PI * 2)) + Math.PI * 2) % (Math.PI * 2)
    const ballTravel = Math.PI * 2 * (state.reduced ? 0 : 7) + normalize(wheelAtLanding + pocketOffset - ballStart)

    state.ballInPocket = false
    state.focused = false
    state.cameraMove = null
    state.spin = {
      startedAt: now,
      duration,
      land,
      dropStart: 0.56,
      wheelStart,
      wheelTurns,
      pocketOffset,
      ballStart,
      ballTravel,
      radiusStart: state.ball.position.z,
      heightStart: state.ball.position.y,
      cameraFrom: state.camera.position.clone(),
      lookFrom: state.look.clone(),
      fovFrom: state.camera.fov,
      fovTo: 32,
      // El destino de la cámara se calcula sobre la posición final del casillero.
      cameraTo: closeUpCamera(wheelStart + wheelTurns + pocketOffset),
      lookTo: () => pocketPosition(wheelStart + wheelTurns + pocketOffset),
    }
  }, [spinning, winningNumber])

  return <div className="roulette-3d" ref={hostRef} role="img" aria-label={spinning ? 'Ruleta europea girando; resultado oculto' : winningNumber == null ? 'Ruleta europea tridimensional' : `Bola detenida en el número ${winningNumber}`}/>
}

function pocketPosition(angle) {
  return new THREE.Vector3(Math.sin(angle) * POCKET_RADIUS, POCKET_HEIGHT, Math.cos(angle) * POCKET_RADIUS)
}
/** Cámara en primer plano: algo más afuera del casillero, elevada, para leer el número y ver los vecinos. */
function closeUpCamera(angle) {
  return new THREE.Vector3(Math.sin(angle) * 3.55, 2.35, Math.cos(angle) * 3.55)
}
