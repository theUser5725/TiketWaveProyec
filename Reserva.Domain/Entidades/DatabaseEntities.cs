// Este archivo existía anteriormente como un contenedor. Se mantiene vacío
// porque ahora cada entidad está en su propio archivo: Asiento, Reserva, Usuario,
// Estadio, AsientoEstado y ReservaEstado.
// Si quieres un punto único para registrar mappings EF Core (ModelBuilder),
// podemos crear aquí métodos de extensión para aplicar configuraciones.

namespace Reserva.Domain.Entidades
{
	internal static class DatabaseEntities
	{
		// Clase holder para futuras configuraciones de mapeo (opcional).
	}
}
