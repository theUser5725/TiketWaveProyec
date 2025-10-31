using System;

namespace Reserva.Domain.Entidades
{
    /// <summary>
    /// Entidad Asiento: representa una unidad de inventario (asiento/entrada).
    /// </summary>
    public class Asiento
    {
        // La PK es int y la genera la base de datos
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty; // ej. Fila-Asiento
        public bool Disponible { get; set; } = true;
        public byte[]? RowVersion { get; set; }
    }
}
