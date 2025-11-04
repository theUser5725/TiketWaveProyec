

-- Crear tablas con estructura compatible EF Core
CREATE TABLE "AsientoEstados" (
    "Id" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE "ReservaEstados" (
    "Id" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE "Usuarios" (
    "Id" SERIAL PRIMARY KEY,
    "Nombre" VARCHAR(100) NOT NULL,
    "Apellido" VARCHAR(100) NOT NULL
);

CREATE TABLE "Estadios" (
    "Id" SERIAL PRIMARY KEY,
    "CantidadAsientos" INTEGER NOT NULL
);

CREATE TABLE "Asientos" (
    "Id" SERIAL PRIMARY KEY,
    "EstadoId" INTEGER NOT NULL REFERENCES "AsientoEstados"("Id"),
    "EstadioId" INTEGER NOT NULL REFERENCES "Estadios"("Id")
);

CREATE TABLE "Reservas" (
    "Id" SERIAL PRIMARY KEY,
    "UsuarioId" INTEGER NOT NULL REFERENCES "Usuarios"("Id"),
    "AsientoId" INTEGER NOT NULL REFERENCES "Asientos"("Id"),
    "EstadioId" INTEGER NOT NULL REFERENCES "Estadios"("Id"),
    "EstadoId" INTEGER NOT NULL REFERENCES "ReservaEstados"("Id"),
    "FechaReserva" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Datos iniciales
INSERT INTO "AsientoEstados" ("Nombre") VALUES 
('Disponible'),
('No Disponible');

INSERT INTO "ReservaEstados" ("Nombre") VALUES 
('Aprobado'),
('Espera'),
('Cancelada');

-- Permisos para el usuario de aplicación
-- Grant privileges to the application user (created by the container env)
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO tkwaver;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO tkwaver;