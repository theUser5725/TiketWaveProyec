# TiketWave - Documentación técnica


Este documento resume la arquitectura, componentes y flujos del microservicio de reservas. Está pensado para
desarrolladores y operadores que necesiten entender cómo funciona el sistema, cómo ejecutarlo localmente y cómo
probar los flujos principales.

## Índice

1. Visión general del sistema
2. Estructura del repositorio
3. Tecnologías y stack
4. Componentes del sistema
	 - 4.1 Reserva.API
	 - 4.2 Reserva.Domain
	 - 4.3 Reserva.Infrastructure
5. Esquema de base de datos completo (SQL)
6. Entidades del dominio (mapa tabla → C#)
7. Flujos principales
	 - 7.1 Creación de reserva
	 - 7.2 Cancelación de reserva
	 - 7.3 Sistema de notificaciones
8. Componentes avanzados implementados
9. Endpoints de la API
10. Configuración y despliegue (Docker)
11. Guías de uso (ejecución local y pruebas)
12. Consideraciones operativas

---

## 1. Visión general del sistema

Objetivo: ofrecer un microservicio que gestione la reserva de asientos para eventos con garantías de
consistencia bajo concurrencia, notificaciones y un API HTTP sencillo.

Principios:
- Consistencia transaccional al reservar asientos (bloqueos DB + control optimista con RowVersion).
- Separación de responsabilidades: API, dominio puro y adaptadores/infra.
- Extensible para notificaciones y almacenamiento en cache.

## 2. Estructura del repositorio

Proyectos principales:
- `Reserva.API/` — capa WebApi (Controllers, DI, Program.cs)
- `Reserva.Domain/` — entidades, interfaces y patrones (State, Observer)
- `Reserva.Infrastructure/` — implementación concreta: EF Core, repositorios, servicios (Redis, Notification)

Archivos y carpetas relevantes:
- `scripts/init.sql` — script de inicialización de la BD (seed).
- `docker-compose.yml` — compose para Postgres y Redis.
- `docs/` — documentación adicional (migraciones, notas).

Dependencias entre proyectos:
- `Reserva.API` referencia `Reserva.Infrastructure` y `Reserva.Domain`.
- `Reserva.Infrastructure` referencia `Reserva.Domain`.

## 3. Tecnologías y stack

- .NET 9 (C#)
- EF Core con Npgsql (PostgreSQL)
- PostgreSQL 15 (desarrollo vía Docker)
- Redis (cache y posible lock distribuido)
- Patterns: Repository/DAO, Observer, (Singleton para secciones críticas in-process)

## 4. Componentes del sistema

4.1 Reserva.API (Capa Web)
- Controllers: exponen endpoints REST (ej. `ReservaController`) para crear, consultar y cancelar reservas.
- Extensions/DI: `AddReservaServices` registra DbContext, repositorios, cache y servicios singleton (ReservationSingleton, NotificationService).
- Middleware: validaciones transversales y trazabilidad.

4.2 Reserva.Domain (Lógica de negocio)
- Entidades: `Reserva`, `Asiento`, `Usuario`, `Estadio`, `ReservaEstado`, `AsientoEstado`, `Evento`, `Notificacion`.
- Interfaces: `IReservaRepository`, `ICacheService`, `INotificationService`, `IReservaSingletonService`.
- Patrones: State (para estados de reserva), Observer (eventos de reserva como `ReservaCreada`, `ReservaCancelada`).

4.3 Reserva.Infrastructure (Implementación)
- `ContextoReserva` — DbContext configurado con mappings y shadow properties `RowVersion`.
- `ReservaRepository` — implementación con transacciones y locking (SELECT ... FOR UPDATE para PostgreSQL).
- `RedisCacheService` — servicio de cache (placeholder).
- `NotificationService` — cola en memoria + worker que persiste notificaciones en BD (usa `IDbContextFactory`).
- `ReservaSingletonService` — singleton que serializa secciones críticas con `SemaphoreSlim` (in-process).

## 5. Esquema de base de datos completo (SQL)

El siguiente script contiene las tablas principales usadas por la aplicación. Está pensado para ejecutarse
en PostgreSQL (sintaxis compatible con Docker init script). Si usas SQL Server, algunos DDL y los hints de locking cambian.

-- SQL BEGIN

```sql
-- asientoEstado
CREATE TABLE IF NOT EXISTS asientoEstado (
	idEstadoAsiento serial PRIMARY KEY,
	nombre varchar(50) NOT NULL UNIQUE
);

-- reservaEstado
CREATE TABLE IF NOT EXISTS reservaEstado (
	idEstadoReserva serial PRIMARY KEY,
	nombre varchar(50) NOT NULL UNIQUE
);

-- usuario
CREATE TABLE IF NOT EXISTS usuario (
	idUsuario serial PRIMARY KEY,
	nombre varchar(100) NOT NULL,
	apellido varchar(100) NOT NULL
);

-- estadio
CREATE TABLE IF NOT EXISTS estadio (
	idEstadio serial PRIMARY KEY,
	cantidad_asientos int NOT NULL
);

-- asiento
CREATE TABLE IF NOT EXISTS asiento (
	idAsiento serial PRIMARY KEY,
	estado int NOT NULL,
	idEstadio int NOT NULL REFERENCES estadio(idEstadio),
	row_version bytea
);

-- reserva
CREATE TABLE IF NOT EXISTS reserva (
	idReserva serial PRIMARY KEY,
	idUsuario int NOT NULL REFERENCES usuario(idUsuario),
	idasiento int NOT NULL REFERENCES asiento(idAsiento),
	idestadio int NOT NULL REFERENCES estadio(idEstadio),
	estado int NOT NULL REFERENCES reservaEstado(idEstadoReserva),
	created_at timestamptz NOT NULL DEFAULT now(),
	row_version bytea
);

-- evento
CREATE TABLE IF NOT EXISTS evento (
	idEvento serial PRIMARY KEY,
	nombre varchar(200) NOT NULL,
	fechaInicio timestamptz NOT NULL,
	idEstadio int NOT NULL REFERENCES estadio(idEstadio)
);

-- notificacion
CREATE TABLE IF NOT EXISTS notificacion (
	idNotificacion serial PRIMARY KEY,
	idReserva int NOT NULL,
	idUsuario int NULL,
	tipo varchar(100) NOT NULL,
	mensaje text NOT NULL,
	created_at timestamptz NOT NULL DEFAULT now()
);

-- Seed básico
INSERT INTO asientoEstado (nombre) VALUES ('Disponible') ON CONFLICT DO NOTHING;
INSERT INTO asientoEstado (nombre) VALUES ('No Disponible') ON CONFLICT DO NOTHING;

INSERT INTO reservaEstado (nombre) VALUES ('aprobado') ON CONFLICT DO NOTHING;
INSERT INTO reservaEstado (nombre) VALUES ('espera') ON CONFLICT DO NOTHING;
INSERT INTO reservaEstado (nombre) VALUES ('cancelada') ON CONFLICT DO NOTHING;

-- SQL END
```

> Nota: `row_version` se usa para concurrencia optimista (tipo `bytea`) y es gestionado por EF Core como shadow property.

## 6. Entidades del dominio (mapeo tabla → C#)

- `Asiento` → `Reserva.Domain.Entidades.Asiento`
	- Id (idAsiento), EstadoId (estado), EstadioId (idEstadio), RowVersion (shadow)

- `Reserva` → `Reserva.Domain.Entidades.Reserva`
	- Id (idReserva), UsuarioId (idUsuario), AsientoId (idasiento), EstadioId (idestadio), EstadoId (estado), CreatedAt, RowVersion

- `Evento` → `Reserva.Domain.Entidades.Evento`
- `Notificacion` → `Reserva.Domain.Entidades.Notificacion`
- `Usuario`, `Estadio`, `AsientoEstado`, `ReservaEstado` también mapeadas en `ContextoReserva`.

Ejemplo (simplificado) — creación de una Reserva desde el controller:

```csharp
var reserva = new Reserva { UsuarioId = dto.IdUsuario, AsientoId = dto.IdAsiento, EstadioId = dto.IdEstadio, EstadoId = 2, CreatedAt = DateTime.UtcNow };
await _repo.CreateAsync(reserva);
// luego TryReserveAsync intenta bloquear y marcar asiento
```

## 7. Flujos principales

7.1 Creación de reserva
- Paso 1: El cliente POST /api/reserva con DTO (IdEvento, IdUsuario, IdAsiento, IdEstadio).
- Paso 2: El controller valida evento, existencia de asiento y pertenencia al estadio.
- Paso 3: Se crea la entidad `Reserva` (estado 'espera') y se persiste.
- Paso 4: `TryReserveAsync` se ejecuta dentro de `ReservaSingletonService` (serializa en proceso) y:
	- inicia una transacción EF Core
	- ejecuta `SELECT ... FOR UPDATE` sobre la fila de `asiento` para bloquearla (Postgres)
	- valida `EstadoId == 1` (Disponible) y `EstadioId` coincidente
	- actualiza `asiento.EstadoId = 2` (No Disponible), `SaveChanges()` y `Commit`
	- encola evento `ReservaCreada` en `NotificationService`

7.2 Cancelación de reserva
- Endpoint: DELETE /api/reserva/{id}
- `ReservaRepository.CancelAsync` hace:
	- serializar en el singleton
	- iniciar transacción
	- marcar `reserva.EstadoId = 3` (cancelada)
	- marcar `asiento.EstadoId = 1` (Disponible)
	- `SaveChanges()` y `Commit`
	- encolar `ReservaCancelada` en `NotificationService`

7.3 Sistema de notificaciones
- `NotificationService` recibe eventos (implementa `INotificationService.EnqueueAsync`) y los añade a una cola en memoria.
- Worker en background consume la cola y persiste filas en `notificacion` usando `IDbContextFactory<ContextoReserva>`.

## 8. Componentes avanzados implementados

8.1 Sistema de Notificaciones
- Entidad `Notificacion` + `NotificationService` (cola + worker) que guarda eventos en BD.

8.2 Control de Concurrencia
- `ReservaSingletonService` (in-process) para serializar secciones críticas.
- `SELECT ... FOR UPDATE` en `TryReserveAsync` para bloqueo a nivel DB (Postgres).
- Shadow properties `RowVersion` para concurrencia optimista.

8.3 Patrón Observer / Eventos
- Eventos definidos: `ReservaCreadaEvent`, `ReservaCanceladaEvent`, `ReservaExpiradaEvent`.
- El repositorio publica eventos al `INotificationService` cuando ocurren acciones relevantes.

8.4 Validaciones y Seguridad
- Validaciones de entrada en controllers (DTOs), verificación de pertenencia de asiento al estadio, y comprobación de disponibilidad.

## 9. Endpoints de la API

- POST /api/reserva
	- Crea una reserva (payload JSON: IdEvento, IdUsuario, IdAsiento, IdEstadio)
	- Respuestas: 201 Created (reserva), 400 BadRequest (datos/validaciones), 409 Conflict (concurrencia)

- GET /api/reserva/{id}
	- Devuelve la reserva por id (200 OK o 404 NotFound)

- DELETE /api/reserva/{id}
	- Cancela la reserva (204 No Content o 404 NotFound)

Ejemplo rápido (PowerShell):

```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:5000/api/reserva" -Body (@{ IdEvento=1; IdUsuario=1; IdAsiento=10; IdEstadio=1 } | ConvertTo-Json) -ContentType 'application/json'

Invoke-RestMethod -Method Delete -Uri "http://localhost:5000/api/reserva/123" -UseBasicParsing
```

## 10. Configuración y despliegue

10.1 Docker y contenedores
- `docker-compose.yml` levanta:
	- Postgres (`tiketwave-db`) con `scripts/init.sql` montado para seed
	- Redis (cache)

10.2 Variables de entorno
- Las credenciales están en `docker-compose.yml` y `appsettings.Development.json` (uso local):
	- Usuario: `tkwaver`
	- Password: `tkwpsw987`
	- DB: `tkwaver_db`

10.3 Configuración de desarrollo
- Ejecutar: `docker-compose up -d`
- Compilar solución: `dotnet build Reserva.sln`
- Ejecutar API: `dotnet run --project Reserva.API/Reserva.API.csproj`

## 11. Guías de uso

11.1 Ejecución local
1. Levanta servicios: `docker-compose up -d`
2. Compila: `dotnet build Reserva.sln`
3. Ejecuta la API: `dotnet run --project Reserva.API/Reserva.API.csproj`

11.2 Testing de endpoints
- Usa Postman, curl o `Invoke-RestMethod` para probar POST/GET/DELETE descritos en la sección 9.




