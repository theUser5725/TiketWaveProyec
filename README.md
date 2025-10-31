# TiketWave - Documentación técnica

## Índice

1. Visión general
2. Estructura del repositorio
3. Descripción detallada por componente
	- `Reserva.API`
	- `Reserva.Domain`
	- `Reserva.Infrastructure`
4. Flujo completo: desde petición hasta persistencia
	- Secuencia paso a paso
	- Diagramas de alto nivel
5. Estructura de datos
	- Entidades C# (ejemplos)
	- Esquema SQL sugerido
6. Consideraciones operativas
7. Próximos pasos

## 1. Visión general

Este documento describe cómo funciona cada parte del microservicio de reservas, cómo se orquesta una reserva desde que llega una petición HTTP hasta que queda persistida en la base de datos, y la estructura de los datos. Está pensado para desarrolladores e ingenieros de plataforma que deban operar o extender el servicio.

## 2. Estructura del repositorio

Raíz del proyecto (resumen):

```
TiketWave/
├── Reserva.API/                 # Capa Web API (Controllers, Middleware, Extensions, Program.cs)
├── Reserva.Domain/              # Lógica de negocio: entidades, interfaces y patrones (State/Observer)
└── Reserva.Infrastructure/      # Implementaciones: EF Core, repositorios (DAO), servicios externos (Redis, RedLock)
```

## 3. Descripción detallada por componente

3.1 `Reserva.API` — capa HTTP / orquestación

- Program.cs: configura pipeline, logging (Serilog), Swagger y registra servicios mediante `AddReservaServices`.
- Controllers (por ejemplo `ReservaController`): reciben DTOs HTTP, validan entrada mínima y llaman a `IReservaRepository` para crear/consultar reservas.
- Middleware (`ProxyValidationMiddleware`): responsable de checks transversales (headers, rate limit, cabeceras de tracing) y puede prevenir peticiones antes del handler.
- Extensions (`DependencyInjection.cs`): registra `DbContext`, `IReservaRepository`, `ICacheService` y otras dependencias (MediatR, Polly, health checks).

3.2 `Reserva.Domain` — reglas de negocio y modelos puros

- Entidades: `Reserva`, `Asiento` (modelo del dominio con propiedades y tokens de concurrencia).
- Patrones: State (gestiona transiciones de estado de una reserva), Observer (eventos de dominio como `ReservaCreada` o `ReservaExpirada`).
- Interfaces: `IReservaRepository` (contrato para persistencia), `ICacheService` (caching), `INotificationService` (si aplica).

3.3 `Reserva.Infrastructure` — implementación y recursos externos

- `ContextoReserva` (DbContext): define `DbSet<Reserva>` y `DbSet<Asiento>` y configura tokens de concurrencia (RowVersion) y `UseIdentityColumn()` para PK ints.
- `ReservaRepository`: implementación del DAO. Aquí se deben aplicar transacciones, locking distribuido (RedLock) o estrategias `SELECT FOR UPDATE` para evitar oversell.
- `RedisCacheService` (placeholder): almacenamiento en cache para lecturas frecuentes.

## 4. Flujo completo: desde petición hasta persistencia

Resumen: el flujo sigue este esquema: cliente -> API -> middleware -> repositorio -> BD -> cache -> notificaciones.

4.1 Secuencia paso a paso (creación de reserva)

1. Cliente envía POST /api/reservas con payload: { eventId, userId, cantidad }
2. `ProxyValidationMiddleware` valida cabeceras y seguridad básica. Si falla, se devuelve error 4xx.
3. `ReservaController.Create` valida payload y construye una instancia `Reserva` (sin asignar Id: la BD se encargará).
4. El controller llama a `IReservaRepository.CreateAsync(reserva)`.
5. En `ReservaRepository.TryReserveAsync` o la lógica de reserva:
	- Intentar adquirir lock distribuido (RedLock) para el `asientoId`.
	- Dentro del lock: verificar disponibilidad leyendo `Asientos` con `SELECT ... FOR UPDATE` o mediante EF Core en una transacción.
	- Si hay disponibilidad: marcar `Asiento.Disponible = false` y crear `Reserva` (Add + SaveChanges).
	- Si no hay disponibilidad: devolver false / error y liberar lock.
6. `SaveChanges` envía la inserción a PostgreSQL; la columna `id` (int identity) será asignada por la BD.
7. Una vez confirmado el commit, el repositorio actualiza el cache Redis (Set reserva por id) y publica un evento `ReservaCreada` al bus/internamente (observer).
8. El controller devuelve 201 Created con la reserva creada (incluyendo `id`), o el error apropiado.

4.2 Diagrama simplificado (secuencia)

Cliente -> API (Controller) -> Middleware -> Repositorio -> BD
													  -> Cache
													  -> Observer/Event bus

## 5. Estructura de datos

5.1 Entidades en C# (ejemplos simplificados)

Reserva.cs

```csharp
public class Reserva
{
	 public int Id { get; set; }                // PK: identity generado por BD
	 public int AsientoId { get; set; }         // FK a Asiento.Id
	 public DateTime CreatedAt { get; set; }
	 public string Estado { get; set; } = "Temporal";
	 public byte[]? RowVersion { get; set; }    // Concurrency token
}
```

Asiento.cs

```csharp
public class Asiento
{
	 public int Id { get; set; }                // PK: identity generado por BD
	 public string Codigo { get; set; }
	 public bool Disponible { get; set; } = true;
	 public byte[]? RowVersion { get; set; }
}
```

5.2 Esquema SQL sugerido (PostgreSQL)

```sql
CREATE TABLE asientos (
  id serial PRIMARY KEY,
  codigo text NOT NULL,
  disponible boolean NOT NULL DEFAULT true,
  row_version bytea
);

CREATE TABLE reservas (
  id serial PRIMARY KEY,
  asiento_id integer NOT NULL REFERENCES asientos(id),
  created_at timestamptz NOT NULL DEFAULT now(),
  estado text NOT NULL DEFAULT 'Temporal',
  row_version bytea
);
```

## 6. Consideraciones operativas

- Backups: antes de modificaciones estructurales (migraciones de PK), ejecutar `pg_dump` o snapshot.
- Migraciones: para cambiar tipos de PK (uuid -> int) usar estrategia segura (añadir columna int, poblarla, swap en ventana de mantenimiento). Ver `docs/migration-uuid-to-int.md`.
- Concurrencia: favor usar locking (RedLock o SELECT FOR UPDATE) y `RowVersion` para detectar conflictos.

## 7. Próximos pasos

- Crear tests de integración que levanten Postgres y Redis (docker-compose) y prueben altas concurrencias en el endpoint de creación.
- Añadir migraciones EF Core que reflejen los cambios en el modelo (ya se agregó `UseIdentityColumn()` en `ContextoReserva`).
- Establecer un pipeline CI que ejecute migraciones en un entorno de staging y corra tests de contrato.

---

Si quieres, agrego:
- Un Postman collection con ejemplos concretos.
- Un `docker-compose.yml` para levantar Postgres + Redis localmente.
- Una migración EF Core de ejemplo que introduce las columnas identity y el swap (esto requiere revisión antes de aplicar en producción).

