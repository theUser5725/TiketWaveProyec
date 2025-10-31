using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Reserva.Domain.Entidades;
using Reserva.Domain.Interfaces;

namespace Reserva.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservaController : ControllerBase
    {
        private readonly IReservaRepository _repo;

        public ReservaController(IReservaRepository repo)
        {
            _repo = repo;
        }

        /// <summary>
        /// Endpoint de ejemplo para crear una reserva (esqueleto).
        /// Implementar validaciones, idempotencia y locking en la implementación real.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Domain.Entidades.Reserva reserva)
        {
            if (reserva == null) return BadRequest();

            // El Id lo genera la base de datos (identity). Solo fijamos CreatedAt.
            reserva.CreatedAt = DateTime.UtcNow;

            var created = await _repo.CreateAsync(reserva);

            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var r = await _repo.GetAsync(id);
            if (r == null) return NotFound();
            return Ok(r);
        }
    }
}
