using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Reserva.API.Middleware
{
    /// <summary>
    /// Middleware placeholder para validaciones/proxy antes de procesar una petición.
    /// Implementa el patrón Proxy para controles de sesión/ratelimit/etc.
    /// Actualmente pasa la petición al siguiente middleware.
    /// </summary>
    public class ProxyValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public ProxyValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Aquí se podrían validar headers, tokens, rate-limits, IP allowlist, etc.
            // Si la validación falla, responder con 401/403 según corresponda.

            await _next(context);
        }
    }
}
