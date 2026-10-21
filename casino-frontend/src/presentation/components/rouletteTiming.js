export const prefersReducedMotion = () => typeof window !== 'undefined' && Boolean(window.matchMedia?.('(prefers-reduced-motion: reduce)').matches)

const SPIN_MS = 6200
const REDUCED_SPIN_MS = 700

/** Duración del giro de la rueda; la página revela el resultado cuando termina. Respeta "reducir movimiento". */
export const getRouletteAnimationMs = () => prefersReducedMotion() ? REDUCED_SPIN_MS : SPIN_MS
