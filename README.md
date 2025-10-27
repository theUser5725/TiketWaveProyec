# TiketWave - Microservicio de Reservas

## Descripción General
Microservicio para la gestión de reservas de eventos, implementando patrones de diseño State y Observer, con soporte para caching distribuido y control de concurrencia.

## Arquitectura

### 1. Estructura del Proyecto
```
TiketWave/
├── Reserva.API/                 # Capa de presentación
│   ├── Controllers/            # Controladores REST
│   ├── Extensions/            # Extensiones de configuración
│   ├── Middleware/           # Middlewares personalizados
│   └── Program.cs           # Punto de entrada y configuración
├── Reserva.Domain/          # Capa de dominio
│   ├── Entidades/         # Modelos de dominio
│   ├── Interfaces/       # Contratos y abstracciones
│   └── Patrones/        # Implementaciones de patrones
└── Reserva.Infrastructure/  # Capa de infraestructura
    ├── DAO/              # Acceso a datos
    ├── Persistencia/    # Contexto EF Core
    └── Servicios/      # Servicios de infraestructura
```
# TiketWave - Microservicio de Reservas

Este documento describe en detalle cómo está organizado el microservicio de reservas, qué hace cada capa y cómo fluyen los datos entre componentes. Incluye ejemplos prácticos, configuraciones, diagramas y recomendaciones para pruebas y despliegue.

## Índice
- Descripción general
- Estructura del proyecto
- Detalle por capas (qué hacen y cómo lo hacen)
- Modelos y esquema de datos
- Flujo de datos (secuencias de creación y cambio de estado)
- Contratos y APIs públicas
- Configuración y variables importantes
- Cómo ejecutar y probar (unit y integración)
- Migraciones y base de datos
- Consideraciones de concurrencia y caching
- Troubleshooting y preguntas frecuentes
- Próximos pasos y mejoras

## 1. Descripción general
El servicio gestiona reservas de eventos con foco en: consistencia de inventario, control de concurrencia, trazabilidad y extensibilidad. Está pensado para integrarse en una arquitectura de microservicios donde otras piezas (pago, notificaciones) reaccionan a eventos del dominio.

## 2. Estructura del proyecto (resumen de carpetas)

TiketWave/
- Reserva.API/                 # Capa Web API (Controllers, Middleware, Extensions, Program.cs)
- Reserva.Domain/              # Lógica de negocio: entidades, interfaces y patrones (State/Observer)
- Reserva.Infrastructure/      # Implementaciones: EF Core, repositorios (DAO), servicios externos (Redis, RedLock)

Rutas y ficheros clave (ejemplos):
- `Reserva.API/Program.cs` — arranque, pipeline, Swagger, Serilog
- `Reserva.API/Extensions/DependencyInjection.cs` — registra DbContext, repositorios y servicios
- `Reserva.API/Middleware/ProxyValidationMiddleware.cs` — validaciones pre-request
- `Reserva.API/Controllers/ReservaController.cs` — endpoints REST
- `Reserva.Domain/Entidades/Reserva.cs` — entidad dominio
- `Reserva.Domain/Patrones/State/*` — estados y lógica de transición
- `Reserva.Infrastructure/Persistencia/ContextoReserva.cs` — DbContext EF Core
- `Reserva.Infrastructure/DAO/ReservaRepository.cs` — implementación de `IReservaRepository`

## 3. Detalle por capas: qué hacen y cómo lo hacen

3.1 Capa de presentación — `Reserva.API`
- Propósito: recibir peticiones HTTP, validar entrada, orquestar llamadas al dominio/infraestructura y devolver respuestas.
- Componentes principales:
  - Controllers: traducen DTOs HTTP a comandos del dominio y devuelven DTOs de respuesta.
  - Middleware (ej. `ProxyValidationMiddleware`): cheques transversales (headers, auth, rate limit). Se ejecuta antes del controller y puede abortar la petición si no pasa checks.
  - Extensions (`AddReservaServices`): encapsulan la configuración de dependencias (DbContext, repositorios, cache, MediatR, Polly).
  - Swagger & OpenAPI: documentación interactiva disponible en `/swagger`.

3.2 Capa de dominio — `Reserva.Domain`
- Propósito: contener las reglas de negocio y modelos puros (sin dependencias infra).
- Elementos destacados:
  - Entidades: `Reserva` (Id, EventId, UserId, Cantidad, Estado, FechaCreacion, FechaExpiracion, Meta)
  - Value Objects: si aplica (por ejemplo `Dinero`, `CantidadBoletos`).
  - Patrones:
    - State Pattern: los distintos estados de una reserva (Pendiente, Confirmada, Cancelada, Expirada) implementan la interfaz `IEstadoReserva` con métodos como `CanTransitionTo(...)` y `OnEnter(...)`.
    - Observer Pattern: el dominio publica eventos internos (ej. `ReservaCreada`, `ReservaExpirada`, `ReservaConfirmada`) para notificar subscribers (servicio de notificaciones, auditoría, etc.).
  - Interfaces: `IReservaRepository` (contrato para persistencia), `ICacheService` (abstracción de cache), `INotificationService` (para notificaciones externas si existen).

3.3 Capa de infraestructura — `Reserva.Infrastructure`
- Propósito: implementar los contratos del dominio y encapsular la interacción con recursos externos.
- Implementaciones típicas:
  - `ReservaRepository` (IReservaRepository): usa `ContextoReserva` (EF Core) y aplica patrones de concurrencia cuando intenta reservar stock.
  - `ContextoReserva` (DbContext): define DbSet<Reserva>, configuración de modelos y mappings.
  - `RedisCacheService` (ICacheService): caching de consultas frecuentes como `GetReservaById` o listados parciales.
  - `RedLock` (con RedLock.net): para obtener locks distribuidos cuando se realiza reserva sobre inventario compartido.

## 4. Modelos y esquema de datos (ejemplo)

Entidad `Reserva` (simplificada):
- Id (GUID) — PK
- EventId (string) — id del evento
- UserId (string) — id del usuario que reserva
- Cantidad (int)
- Estado (string / enum)
- FechaCreacion (datetime)
- FechaExpiracion (nullable datetime)
- Metadata (jsonb) — datos opcionales

SQL sugerido (esquema básico):

CREATE TABLE reservas (
  id uuid PRIMARY KEY,
  event_id text NOT NULL,
  user_id text NOT NULL,
  cantidad int NOT NULL,
  estado text NOT NULL,
  fecha_creacion timestamptz NOT NULL DEFAULT now(),
  fecha_expiracion timestamptz,
  metadata jsonb
);

Índices recomendados:
- index on (event_id)
- index on (user_id)
- index on (estado)

## 5. Flujo de datos: secuencias críticas

5.1 Creación de una reserva (secuencia)

mermaid
sequenceDiagram
  participant C as Cliente
  participant A as API (Controller)
  participant M as ProxyValidationMiddleware
  participant R as ReservaRepository
  participant DB as PostgreSQL
  participant RC as RedisCache
  participant O as Observers (notifs)

  C->>A: POST /api/reservas {eventId,userId,cantidad}
  A->>M: Pasa por middleware (headers, auth, rate limit)
  M-->>A: Validación OK
  A->>R: TryReserveAsync(command)
  R->>R: Obtener lock distribuido (RedLock)
  R->>DB: SELECT ... FOR UPDATE / verificar inventario
  DB-->>R: Disponibilidad
  R->>DB: INSERT reserva
  DB-->>R: OK (id)
  R->>RC: Actualizar cache (Set reserva)
  R->>O: Publicar evento ReservaCreada
  R-->>A: Resultado (Reserva creada)
  A-->>C: 201 Created {id,...}

5.2 Cambio de estado (ej. confirmación post-pago)
- Se recibe notificación o endpoint que cambia estado a Confirmada.
- El flujo valida transición permitida mediante State Pattern (`IEstadoReserva.CanTransitionTo`), persiste y publica evento `ReservaConfirmada`.

## 6. Contratos y signatures (ejemplos)

IReservaRepository (resumen):
- Task<Reserva?> GetByIdAsync(Guid id);
- Task<IEnumerable<Reserva>> ListByEventAsync(string eventId);
- Task<Guid> TryReserveAsync(ReservaCreateCommand command); // maneja locking y persistencia
- Task<bool> ChangeStatusAsync(Guid id, string nuevoEstado);

ICacheService (resumen):
- Task<T?> GetAsync<T>(string key);
- Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);
- Task RemoveAsync(string key);

IEstadoReserva (resumen):
- bool CanTransitionTo(EstadoDestino destino);
- void OnEnter(Reserva reserva);

Eventos de dominio (ejemplos):
- ReservaCreada { ReservaId, EventId, UserId }
- ReservaConfirmada { ReservaId }
- ReservaExpirada { ReservaId }

## 7. Configuración y variables importantes

appsettings.json (claves importantes):
- ConnectionStrings:ReservaPostgres — cadena de conexión a PostgreSQL
- Redis: Cache connection (host:port)
- RedLock: configuración (expiry, retryCount, retryDelay)
- Reservation:DefaultExpiryMinutes — tiempo por defecto para expiración de reservas

Ejemplo mínimo `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "ReservaPostgres": "Host=localhost;Database=ReservaDb;Username=postgres;Password=postgres"
  },
  "Redis": {
    "Connection": "localhost:6379"
  },
  "Reservation": {
    "DefaultExpiryMinutes": 15
  }
}
```

## 8. Cómo ejecutar y probar

8.1 Preparar entorno

Powershell (ejemplos copy/paste):

```powershell
# Restaurar y compilar
dotnet restore
dotnet build

# Crear y aplicar migraciones (si aún no existen)
dotnet ef migrations add InitialCreate --project Reserva.Infrastructure --startup-project Reserva.API
dotnet ef database update --project Reserva.Infrastructure --startup-project Reserva.API

# Ejecutar API
cd Reserva.API
dotnet run
```

8.2 Probar endpoints (ejemplos)

Usando curl / HTTPie / Postman:

POST crear reserva:

```http
POST http://localhost:5190/api/reservas
Content-Type: application/json

{
  "eventId": "ev-123",
  "userId": "user-45",
  "cantidad": 2
}
```

GET reserva por id:

```http
GET http://localhost:5190/api/reservas/{id}
```

8.3 Pruebas unitarias y de integración

- Unit: usar xUnit / Moq para probar lógica de `State` y validaciones del dominio.
- Integration: usar `WebApplicationFactory<TEntryPoint>` (Microsoft.AspNetCore.Mvc.Testing) para levantar la API en memoria y ejecutar flujos completos contra un Postgres/Redis de prueba (docker-compose ideal).

Ejemplo rápido (xUnit): probar que `Pendiente` -> `Expirada` ocurre después del tiempo configurado (ejecutar lógica de `TemporalState` simulando tiempo o inyectando reloj de prueba).

## 9. Migraciones y base de datos

- Las migraciones se mantienen en `Reserva.Infrastructure/Migrations`.
- Comandos:

```powershell
dotnet ef migrations add <Nombre> --project Reserva.Infrastructure --startup-project Reserva.API
dotnet ef database update --project Reserva.Infrastructure --startup-project Reserva.API
```

Si usas Docker Compose para local, añade servicios `postgres` y `redis` y configura las cadenas de conexión.

## 10. Concurrencia y caching (detalles)

- Locking: para evitar oversell, `TryReserveAsync` debe:
  1) Intentar adquirir lock (RedLock) con timeout corto.
  2) Dentro del lock, leer inventario con `SELECT ... FOR UPDATE` o leer y verificar contadores.
  3) Realizar INSERT/UPDATE y liberar lock.

- Cache: diseñar TTLs cortos para datos críticos (reserva por id) y usar strategies de invalidación en write-through (on write, invalidar o actualizar cache).

- Expiración de reservas: el `TemporalState` puede programar expiraciones (por ejemplo via background worker o utilizando TTLs en Redis con key expirations que publiquen eventos cuando caducan).

## 11. Troubleshooting y FAQs

- Error: "No se puede conectar a Postgres": verifica `ConnectionStrings:ReservaPostgres` y que el servicio de Postgres esté accesible.
- Error: "Lock not acquired": aumenta timeout del RedLock o revisa que Redis esté correctamente en cluster/replicado si usas RedLock.
- Swagger no aparece: asegúrate que `app.UseSwagger()` y `app.UseSwaggerUI()` estén llamados en `Program.cs` bajo entorno de desarrollo o configurados para producción si es necesario.

## 12. Buenas prácticas y próximos pasos

- Añadir pruebas de integración E2E con base de datos y redis en CI (usar containers Docker en pipeline).
- Implementar pruebas de carga para validar comportamiento bajo concurrencia.
- Implementar tracing distribuido (OpenTelemetry) para correlación de operaciones entre servicios.

---

Si quieres, puedo:
- Generar un Postman collection con ejemplos de endpoints.
- Añadir un `docker-compose.yml` para levantar Postgres+Redis y ejecutar pruebas de integración locales.
- Crear ejemplos de tests unitarios y de integración (xUnit) para las piezas críticas (State, TryReserveAsync).

Dime cuál de estas tareas prefieres que haga a continuación y la implemento.
