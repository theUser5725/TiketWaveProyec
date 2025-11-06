-- =============================================
-- SCRIPT DE BASE DE DATOS - TIKETWAVE
-- Estructura sincronizada con ContextoReserva.cs
-- =============================================

-- Tabla: Estados de Asiento
CREATE TABLE IF NOT EXISTS "AsientoEstados" (
    "idEstadoAsiento" SERIAL PRIMARY KEY,
    "nombre" VARCHAR(50) NOT NULL UNIQUE
);

-- Tabla: Estados de Reserva  
CREATE TABLE IF NOT EXISTS "ReservaEstados" (
    "idEstadoReserva" SERIAL PRIMARY KEY,
    "nombre" VARCHAR(50) NOT NULL UNIQUE
);

-- Tabla: Usuarios
CREATE TABLE IF NOT EXISTS "Usuarios" (
    "idUsuario" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(100) NOT NULL,
    "Apellido" VARCHAR(100) NOT NULL
);

-- Tabla: Estadios
CREATE TABLE IF NOT EXISTS "Estadios" (
    "idEstadio" SERIAL PRIMARY KEY,
    "cantidad_asientos" INTEGER NOT NULL
);

-- Tabla: Asientos
CREATE TABLE IF NOT EXISTS "Asientos" (
    "idAsiento" SERIAL PRIMARY KEY,
    "estado" INTEGER NOT NULL REFERENCES "AsientoEstados"("idEstadoAsiento"),
    "idEstadio" INTEGER NOT NULL REFERENCES "Estadios"("idEstadio")
);

-- Tabla: Reservas (PRINCIPAL)
CREATE TABLE IF NOT EXISTS "Reservas" (
    "idReserva" SERIAL PRIMARY KEY,
    "idUsuario" INTEGER NOT NULL REFERENCES "Usuarios"("idUsuario"),
    "idasiento" INTEGER NOT NULL REFERENCES "Asientos"("idAsiento"),
    "idestadio" INTEGER NOT NULL REFERENCES "Estadios"("idEstadio"),
    "estado" INTEGER NOT NULL REFERENCES "ReservaEstados"("idEstadoReserva"),
    "created_at" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Tabla: Eventos
CREATE TABLE IF NOT EXISTS "Eventos" (
    "idEvento" SERIAL PRIMARY KEY,
    "nombre" VARCHAR(255) NOT NULL,
    "fechaInicio" TIMESTAMP WITH TIME ZONE NOT NULL,
    "idEstadio" INTEGER NOT NULL REFERENCES "Estadios"("idEstadio")
);

-- Tabla: Notificaciones
CREATE TABLE IF NOT EXISTS "notificacion" (
    "idNotificacion" SERIAL PRIMARY KEY,
    "idReserva" INTEGER NOT NULL REFERENCES "Reservas"("idReserva"),
    "idUsuario" INTEGER NOT NULL REFERENCES "Usuarios"("idUsuario"),
    "tipo" VARCHAR(50) NOT NULL,
    "mensaje" TEXT NOT NULL,
    "created_at" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- =============================================
-- DATOS INICIALES
-- =============================================

-- Estados de Asiento
INSERT INTO "AsientoEstados" ("nombre") VALUES 
('Disponible'),
('No Disponible')
ON CONFLICT ("nombre") DO NOTHING;

-- Estados de Reserva (en minúsculas para coincidir con HasData)
INSERT INTO "ReservaEstados" ("nombre") VALUES 
('aprobado'),
('espera'),
('cancelada')
ON CONFLICT ("nombre") DO NOTHING;

-- =============================================
-- PERMISOS
-- =============================================

-- Permisos para el usuario de aplicación
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO tkwaver;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO tkwaver;

-- =============================================
-- VERIFICACIÓN (opcional para debugging)
-- =============================================

-- SELECT '✅ Base de datos TiketWave inicializada correctamente' as mensaje;
-- SELECT COUNT(*) as total_tablas FROM information_schema.tables WHERE table_schema = 'public';