using System.Security.Claims;
using Accesia.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Accesia.API.Attributes;

public class ValidateCsrfAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var csrfService = context.HttpContext.RequestServices.GetService<ICsrfTokenService>();
        if (csrfService == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        // Obtener ID del usuario del token JWT
        var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                mensaje = "Usuario no autenticado",
                timestamp = DateTime.UtcNow
            });
            return;
        }

        // Extraer token CSRF de la solicitud
        var headers = context.HttpContext.Request.Headers
            .ToDictionary(h => h.Key, h => h.Value.ToString());
        var csrfToken = csrfService.ExtractTokenFromHeaders(headers);
        if (string.IsNullOrWhiteSpace(csrfToken))
        {
            context.Result = new BadRequestObjectResult(new
            {
                mensaje = "Token CSRF requerido. Incluye el header 'X-CSRF-Token'",
                timestamp = DateTime.UtcNow
            });
            return;
        }

        // Validar token CSRF
        if (!csrfService.ValidateToken(csrfToken, userId))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                mensaje = "Token CSRF inválido o expirado",
                timestamp = DateTime.UtcNow
            });
            return;
        }

        base.OnActionExecuting(context);
    }
}