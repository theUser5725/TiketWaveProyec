using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Reserva.Domain.Entidades;
using Reserva.Domain.Interfaces;

namespace Reserva.API.Controllers
{
    [ApiController]
    [Route("api/reserva")]
    public class ReservaApiController : ControllerBase
    {
        private readonly IReservaRepository _repo;

        public ReservaApiController(IReservaRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Endpoint para crear una reserva. Valida que el evento exista y esté activo,
        /// que el asiento pertenezca al estadio del evento y que esté disponible.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Models.ReservaRequestDTO dto)
        {
            if (dto == null) return BadRequest();

            // Validar que el evento exista
            var evento = await _repo.GetEventoAsync(dto.IdEvento);
            if (evento == null) return BadRequest("El evento indicado no existe.");

            // Consideramos 'activo' si la fecha de inicio es en el futuro
            if (evento.FechaInicio <= DateTime.UtcNow) return BadRequest("El evento no está activo.");

            // Validar que el asiento exista y pertenezca al estadio del evento
            var asiento = await _repo.GetAsientoAsync(dto.IdAsiento);
            if (asiento == null) return BadRequest("El asiento indicado no existe.");
            if (asiento.EstadioId != evento.EstadioId) return BadRequest("El asiento no pertenece al estadio del evento.");

            // Validar disponibilidad (EstadoId == 1 => Disponible)
            if (asiento.EstadoId != 1) return Conflict("El asiento no está disponible.");

            // Crear reserva y marcar asiento como reservado
            var reserva = new Domain.Entidades.Reserva
            {
                UsuarioId = dto.IdUsuario,
                AsientoId = dto.IdAsiento,
                EstadioId = dto.IdEstadio,
                EstadoId = 2, // 'espera' por defecto
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repo.CreateAsync(reserva);

            // Intentar marcar el asiento como reservado
            var locked = await _repo.TryReserveAsync(created.AsientoId, created.Id);
            if (!locked)
            {
                // No se pudo reservar el asiento: revertir la reserva o devolver conflicto
                return Conflict("No se pudo reservar el asiento (concurrencia).");
            }

            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var r = await _repo.GetAsync(id);
            if (r == null) return NotFound();
            return Ok(r);
        }

        /// <summary>
        /// Cancela una reserva: actualiza estado y libera asiento.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var ok = await _repo.CancelAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
