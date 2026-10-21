/** Encabezado de página: título en tipografía de marcador y, a la derecha, lo que la página necesite (estado, saldo). */
export function PageHeading({ title, description, children }) {
  return <div className="page-heading"><div><h1>{title}</h1>{description && <p>{description}</p>}</div>{children}</div>
}
