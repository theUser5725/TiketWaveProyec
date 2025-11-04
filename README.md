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

## 5.3 Esquema de la base de datos (SQL proporcionado por el cliente)

La siguiente es la querry exactamente como se entregó. El proyecto fue adaptado
para mapear estas tablas mediante entidades en `Reserva.Domain/Entidades`.

```sql
-- Tabla para estados de asientos
CREATE TABLE asientoEstado (
	idEstadoAsiento INT PRIMARY KEY IDENTITY(1,1),
	nombre VARCHAR(50) NOT NULL UNIQUE
);
GO

-- Tabla para estados de reservas
CREATE TABLE reservaEstado (
	idEstadoReserva INT PRIMARY KEY IDENTITY(1,1),
	nombre VARCHAR(50) NOT NULL UNIQUE
);
GO

-- Tabla de usuarios
CREATE TABLE usuario (
	idUsuario INT PRIMARY KEY IDENTITY(1,1),
	nombre VARCHAR(100) NOT NULL,
	apellido VARCHAR(100) NOT NULL
);
GO

-- Tabla de estadios
CREATE TABLE estadio (
	idEstadio INT PRIMARY KEY IDENTITY(1,1),
	cantidad_asientos INT NOT NULL
);
GO

-- Tabla de asientos (CORREGIDA: ahora incluye idEstadio)
CREATE TABLE asiento (
	idAsiento INT PRIMARY KEY IDENTITY(1,1),
	estado INT NOT NULL,
	idEstadio INT NOT NULL,
	FOREIGN KEY (estado) REFERENCES asientoEstado(idEstadoAsiento),
	FOREIGN KEY (idEstadio) REFERENCES estadio(idEstadio)
);
GO

-- Tabla de reservas
CREATE TABLE reserva (
	idReserva INT PRIMARY KEY IDENTITY(1,1),
	idUsuario INT NOT NULL,
	idasiento INT NOT NULL,
	idestadio INT NOT NULL,
	estado INT NOT NULL,
	FOREIGN KEY (idUsuario) REFERENCES usuario(idUsuario),
	FOREIGN KEY (idasiento) REFERENCES asiento(idAsiento),
	FOREIGN KEY (idestadio) REFERENCES estadio(idEstadio),
	FOREIGN KEY (estado) REFERENCES reservaEstado(idEstadoReserva)
);
GO

-- Insertar datos básicos
INSERT INTO asientoEstado (nombre) VALUES 
('Disponible'),
('No Disponible');
GO

INSERT INTO reservaEstado (nombre) VALUES 
('aprobado'),
('espera'),
('cancelada');
GO
```

### Mapeo tablas -> entidades (archivos creados)

- `AsientoEstado.cs` -> tabla `asientoEstado` (Id, Nombre)
- `ReservaEstado.cs` -> tabla `reservaEstado` (Id, Nombre)
- `Usuario.cs` -> tabla `usuario` (Id, Nombre, Apellido)
- `Estadio.cs` -> tabla `estadio` (Id, CantidadAsientos, Asientos[] - navegación opcional)
- `Asiento.cs` -> tabla `asiento` (Id / IdAsiento alias, EstadoId, EstadoEntity, EstadioId, Disponible)
- `Reserva.cs` -> tabla `reserva` (Id / IdReserva alias, UsuarioId, AsientoId, EstadioId, EstadoId, EstadoEntity, Estado (legacy flexible), CreatedAt)

> Nota: Para evitar romper llamadas existentes, las entidades incluyen propiedades "legacy/alias"
> como `IdAsiento`, `IdReserva`, `Disponible` y una propiedad flexible `Reserva.Estado` que acepta
> valores legacy en texto o en número. Es recomendable migrar el código para usar `EstadoId` y
> `EstadoEntity` (navegación) en el dominio nuevo.

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
- Una migración EF Core de ejemplo que introduce las columnas identity y el swap (esto requiere revisión antes de aplicar en producción).

## Run local (Docker)

He añadido un archivo `docker-compose.yml` y `scripts/init.sql` que crean un contenedor Postgres con las tablas iniciales y Redis.

Para levantar los servicios:

```powershell
docker-compose up -d
```

Postgres escuchará en el puerto 5432 y Redis en 6379. El script `scripts/init.sql` crea las tablas y los datos
iniciales (asientoEstado y reservaEstado).

Si quieres puedo además añadir una migración EF Core y un pequeño proyecto de pruebas de integración que valide
la conexión y los mapeos.

## Docker: configuración concreta usada aquí (credenciales y propósito)

Se ha preparado un entorno Docker local para desarrollo y pruebas. Propósito: proporcionar una base reproducible
de PostgreSQL (con datos seed) y Redis para cache/local testing sin depender de servicios externos.

- Contenedor Postgres:
	- Imagen: `postgres:15`
	- Nombre del contenedor (en compose): `tiketwave-db`
	- Volumen de datos: `postgres_data` (persistencia local)
	- Script de inicialización montado: `./scripts/init.sql` -> `/docker-entrypoint-initdb.d/init.sql`

- Credenciales (tal como están guardadas en este repositorio):
	- Usuario: `tkwaver`
	- Contraseña: `tkdpsw987`
	- Base de datos: `tkwaver_db`

> Nota: las credenciales están registradas en `docker-compose.yml` y en `Reserva.API/appsettings.Development.json` para
> facilitar el arranque local. Si prefieres, puedo moverlas a un archivo `.env` y actualizar `docker-compose.yml` para
> que las lea desde allí (recomendado desde el punto de vista de seguridad operativa).

## Qué se hizo en los ficheros relevantes

- `docker-compose.yml`:
	- Definido servicio `postgres` con las variables `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB` apuntando a
		`tkwaver` / `tkdpsw987` / `tkwaver_db`.
	- Montado `./scripts/init.sql` para ejecutar la inicialización de esquema y datos cuando el volumen es nuevo.

- `scripts/init.sql`:
	- Crea las tablas base (`AsientoEstados`, `ReservaEstados`, `Usuarios`, `Estadios`, `Asientos`, `Reservas`).
	- Inserta valores seed para estados de asiento y reserva.
	- Otorga privilegios sobre tablas y secuencias al usuario `tkwaver` (GRANT ... TO tkwaver).

- `Reserva.API/appsettings.Development.json`:
	- Cadena de conexión de desarrollo actualizada para apuntar a `Host=localhost;Port=5432;Database=tkwaver_db;Username=tkwaver;Password=tkdpsw987`.

- `scripts/recreate-db.ps1` (nuevo):
	- Helper PowerShell que hace `docker-compose down -v` (borra volumen), `docker-compose up -d`, espera a que Postgres
		esté listo y lista las tablas en `tkwaver_db` usando el usuario `tkwaver`.

## Logs de Postgres — interpretación rápida

Cuando arranques el contenedor verás líneas como:

```
PostgreSQL init process complete; ready for start up.
LOG: starting PostgreSQL ...
LOG: listening on IPv4 address "0.0.0.0", port 5432
LOG: database system is ready to accept connections
```

- "init process complete" significa que `initdb` y los scripts en `/docker-entrypoint-initdb.d` se ejecutaron
	correctamente (esto sólo ocurre la primera vez que el volumen es creado).
- "listening on ..." y "ready to accept connections" significan que el servidor está arrancado y aceptando conexiones
	por TCP (puerto 5432) y/o socket Unix.

Si ves errores de tipo `role "reserva" does not exist` al ejecutar `init.sql`, es porque el script intentaba dar
permisos a un role que no se había creado; por eso actualizamos `init.sql` para otorgar permisos al role `tkwaver`.

## Comandos clave (PowerShell / Windows)

- Levantar todos los servicios (en background):
```powershell
docker-compose up -d
```

- Parar y eliminar contenedores (sin borrar volumenes):
```powershell
docker-compose down
```

- Parar y **eliminar volúmenes** (esto borra los datos y fuerza re-ejecución de `init.sql`):
```powershell
docker-compose down -v
```

- Ver estado de servicios:
```powershell
docker-compose ps
```

- Ver logs del contenedor Postgres (últimos 200 líneas):
```powershell
docker logs tiketwave-db --tail 200
```

- Ejecutar un comando psql dentro del contenedor (usando TCP loopback para evitar problemas de socket):
```powershell
docker exec -e PGPASSWORD=tkdpsw987 tiketwave-db psql -h 127.0.0.1 -U tkwaver -d tkwaver_db -c '\dt'
```

- Conectarte desde tu máquina si tienes `psql` instalado (host local):
```powershell
psql "host=localhost port=5432 dbname=tkwaver_db user=tkwaver password=tkdpsw987"
```

- Si no quieres borrar el volumen pero necesitas crear/actualizar el usuario y la DB en un servidor ya existente, usa
	estos comandos (ejecutarlos con un superuser o dentro del contenedor `tiketwave-db` como `postgres`):

```sql
-- Conectar como superuser dentro del contenedor:
-- docker exec -it tiketwave-db psql -U postgres

-- Crear role si no existe y asignar contraseña
DO $$ BEGIN
	 IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'tkwaver') THEN
			 CREATE ROLE tkwaver WITH LOGIN PASSWORD 'tkdpsw987';
	 END IF;
END $$;

-- Crear base si no existe y asignar propietario
DO $$ BEGIN
	 IF NOT EXISTS (SELECT FROM pg_database WHERE datname = 'tkwaver_db') THEN
			 CREATE DATABASE tkwaver_db OWNER tkwaver;
	 END IF;
END $$;

-- Dar permisos sobre tablas y secuencias (ejecutar dentro de la BD objetivo)
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO tkwaver;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO tkwaver;
```

## Backups / restores

- Para volcar la base desde el host (requiere `pg_dump`):
```powershell
docker exec -e PGPASSWORD=tkdpsw987 tiketwave-db pg_dump -U tkwaver -d tkwaver_db -F c -f /tmp/tkwaver_db.dump
docker cp tiketwave-db:/tmp/tkwaver_db.dump .\tkwaver_db.dump
```

- Para restaurar un backup (restaura en una BD vacía o nueva):
```powershell
docker cp .\tkwaver_db.dump tiketwave-db:/tmp/tkwaver_db.dump
docker exec -e PGPASSWORD=tkdpsw987 tiketwave-db pg_restore -U tkwaver -d tkwaver_db /tmp/tkwaver_db.dump
```

## Migraciones EF Core (opcional)

- Propósito: versionar cambios de esquema desde el modelo C# y aplicarlos automáticamente con `dotnet ef database update`.
- Generar migración (desde la raíz del repo):
```powershell
dotnet ef migrations add NombreDeLaMigracion --project "Reserva.Infrastructure/Reserva.Infrastructure.csproj" --startup-project "Reserva.API/Reserva.API.csproj" -o Migrations
```

- Aplicar migraciones a la BD:
```powershell
dotnet ef database update --project "Reserva.Infrastructure/Reserva.Infrastructure.csproj" --startup-project "Reserva.API/Reserva.API.csproj"
```

> Nota: las migraciones funcionan mejor si las versiones de los paquetes `Microsoft.EntityFrameworkCore.*` están alineadas
> entre proyectos. En este repositorio se han generado migraciones en `Reserva.Infrastructure/Migrations` durante la
> puesta a punto, pero puedes usar `scripts/init.sql` para inicialización rápida en desarrollo.

## Recomendaciones finales

- Para desarrollo local: puedes usar `scripts/recreate-db.ps1` para borrar el volumen y volver a crear la BD con los datos
	seed automáticamente.
- Para entornos compartidos o producción: no guardes contraseñas en el repo; usa un `.env` o un gestor de secretos.
- Si te interesa que añada pgAdmin al `docker-compose.yml` o que mueva las credenciales a `.env`, dime y lo implemento.

---

Si quieres que incorpore esto como una sección más visible en la wiki del proyecto o en un fichero separado
(`docs/docker-setup.md`), lo replico allí y dejo `README.md` más conciso.

