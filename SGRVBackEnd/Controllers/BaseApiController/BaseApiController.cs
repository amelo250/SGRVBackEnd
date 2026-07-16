
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SGRVBackEnd.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected int GetEmpresaId()
    {
        var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;

        if (string.IsNullOrWhiteSpace(empresaIdClaim))
        {
            throw new UnauthorizedAccessException(
                "El token no contiene el claim EmpresaId."
            );
        }

        if (!int.TryParse(empresaIdClaim, out var idEmpresa))
        {
            throw new UnauthorizedAccessException(
                "El claim EmpresaId del token no es válido."
            );
        }

        return idEmpresa;
    }

    protected int GetUsuarioId()
    {
        var usuarioIdClaim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(usuarioIdClaim))
        {
            throw new UnauthorizedAccessException(
                "El token no contiene el identificador del usuario."
            );
        }

        if (!int.TryParse(usuarioIdClaim, out var idUsuario))
        {
            throw new UnauthorizedAccessException(
                "El identificador del usuario no es válido."
            );
        }

        return idUsuario;
    }
}

