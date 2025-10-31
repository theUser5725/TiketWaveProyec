# Migración segura de PK UUID -> INT (PostgreSQL)

Este documento contiene pasos y scripts recomendados para migrar tablas con PK tipo `uuid` a `int` identity en PostgreSQL, aplicable a las tablas `reservas` y `asientos` del proyecto TiketWave.

IMPORTANTE: Hacer backup antes de ejecutar cualquier script en producción.

## Resumen de la estrategia
1. Hacer backup y crear una ventana de mantenimiento si hay tráfico en vivo.
2. Añadir columna nueva `id_int` con default que viene de una secuencia (nextval).
3. Poblar `id_int` para filas existentes usando la secuencia.
4. Para cada tabla que tenga FKs hacia la tabla principal, añadir columna FK int y poblarla.
5. En ventana de mantenimiento corta: dropear constraints que referencian el PK antiguo, renombrar columnas (o cambiar nombres a `id_old`), y establecer `id_int` como PK e identidad.
6. Recrear FKs apuntando a la nueva PK.
7. Probar y limpiar columnas antiguas.

---

## Scripts (ejemplo para `reservas` y `asientos`)

-- 0) Backup (ejecutar en shell con pg_dump):
-- pg_dump -h host -U user -d dbname -Fc -f backup_file.dump

-- 1) Crear secuencias para las tablas (si no existen)
CREATE SEQUENCE IF NOT EXISTS reservas_id_seq OWNED BY reservas.id_int;
CREATE SEQUENCE IF NOT EXISTS asientos_id_seq OWNED BY asientos.id_int;

-- 2) Añadir columna id_int con default nextval
ALTER TABLE reservas ADD COLUMN id_int integer;
ALTER TABLE asientos ADD COLUMN id_int integer;

-- 3) Poblar id_int para filas existentes
UPDATE reservas SET id_int = nextval('reservas_id_seq') WHERE id_int IS NULL;
UPDATE asientos SET id_int = nextval('asientos_id_seq') WHERE id_int IS NULL;

-- 4) Asegurar default para nuevas filas
ALTER TABLE reservas ALTER COLUMN id_int SET DEFAULT nextval('reservas_id_seq');
ALTER TABLE asientos ALTER COLUMN id_int SET DEFAULT nextval('asientos_id_seq');

-- 5) Para cada tabla que tenga FK->reservas(id) o FK->asientos(id):
-- Añadir columna fk_int y poblarla según JOIN
-- Ejemplo (si existe tabla pagos con reserva_id uuid):
-- ALTER TABLE pagos ADD COLUMN reserva_id_int integer;
-- UPDATE pagos p SET reserva_id_int = r.id_int FROM reservas r WHERE p.reserva_id = r.id;

-- 6) Ventana de mantenimiento: swap de constraints (ejecutar dentro de transacción)
BEGIN;

-- Dropear FK que referencian uuid PKs (ejemplo nombre fk_pagos_reserva)
-- ALTER TABLE pagos DROP CONSTRAINT fk_pagos_reserva;

-- Dropear PK antiguo
ALTER TABLE reservas DROP CONSTRAINT IF EXISTS reservas_pkey;
ALTER TABLE asientos DROP CONSTRAINT IF EXISTS asientos_pkey;

-- Renombrar columna id -> id_uuid (opcional conservarla)
ALTER TABLE reservas RENAME COLUMN id TO id_uuid;
ALTER TABLE asientos RENAME COLUMN id TO id_uuid;

-- Renombrar id_int -> id y establecer como PK
ALTER TABLE reservas RENAME COLUMN id_int TO id;
ALTER TABLE asientos RENAME COLUMN id_int TO id;

ALTER TABLE reservas ADD PRIMARY KEY (id);
ALTER TABLE asientos ADD PRIMARY KEY (id);

-- Recrear FKs apuntando al nuevo id (ejemplo)
-- ALTER TABLE pagos ADD CONSTRAINT fk_pagos_reserva FOREIGN KEY (reserva_id_int) REFERENCES reservas(id);

COMMIT;

-- 7) Limpieza final: si todo OK, eliminar columnas antiguas y secuencias opcionales
-- ALTER TABLE reservas DROP COLUMN id_uuid;
-- ALTER TABLE asientos DROP COLUMN id_uuid;
-- DROP SEQUENCE IF EXISTS reservas_id_seq;
-- DROP SEQUENCE IF EXISTS asientos_id_seq;

---

## Consideraciones
- Si hay triggers, vistas, funciones o código externo que usa UUIDs, planificar migración o mantener la columna `id_uuid` como referencia durante más tiempo.
- Si expones ids en APIs ya, deberás comunicar el cambio o mantener `id_uuid`/public_id para compatibilidad.
- Operación sensible en producción; probar en staging antes de producción.

---

## Ejecución desde PowerShell (ejemplo)

```powershell
# Exportar backup
pg_dump -h localhost -U postgres -d reserva_db -Fc -f reserva_backup.dump

# Ejecutar script SQL (suponiendo psql en PATH)
psql -h localhost -U postgres -d reserva_db -f ./docs/migration-uuid-to-int.sql
```

---

Si quieres, genero la migración EF Core equivalente (AddColumn, UpdateData, Drop PK, etc.) y un archivo SQL listo para ejecutar. Indica si prefieres que cree la migración en el proyecto `Reserva.Infrastructure` y si quieres que intente aplicar la migración automáticamente en tu base de datos de desarrollo.