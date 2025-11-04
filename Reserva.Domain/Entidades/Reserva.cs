using System;

namespace Reserva.Domain.Entidades
{
    /// <summary>
    /// Entidad Reserva: mapea la tabla `reserva`.
    /// Columnas relevantes: idReserva (PK), idUsuario, idasiento, idestadio, estado (FK a reservaEstado)
    /// </summary>
    public class Reserva
    {
        // PK identity
        public int Id { get; set; }

        // Backwards-compatible alias

        // FK al usuario que hace la reserva
        public int UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        // FK al asiento reservado
        public int AsientoId { get; set; }
        public Asiento? Asiento { get; set; }

        // FK al estadio (redundancia en la tabla original)
        public int EstadioId { get; set; }
        public Estadio? Estadio { get; set; }

        // FK al estado de la reserva (reservaEstado.idEstadoReserva)
        public int EstadoId { get; set; }
        // Navegación renombrada para evitar colisión con propiedad string `Estado` usada por código legacy.
        public ReservaEstado? EstadoEntity { get; set; }

        // Propiedad legacy en texto: muchos lugares del código usan `Reserva.Estado` como string.
        // La mantenemos aquí para compatibilidad y la sincronización debe manejarse a nivel de repositorio/servicio.

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Concurrency token opcional
        public byte[]? RowVersion { get; set; }
        // NOTE: removed legacy aliases (IdReserva) and flexible Estado object.
        // Use EstadoId (int) and EstadoEntity (ReservaEstado) for state handling.
    }
}
