using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SGRVBackEnd.Helpers;

namespace SGRVBackEnd.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected int GetEmpresaId()
    {
        return GetRequiredIntegerClaim(CustomClaimTypes.EmpresaId);
    }

    protected int GetUsuarioId()
    {
        var claim =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst(CustomClaimTypes.UsuarioId)?.Value;

        if (!int.TryParse(claim, out var idUsuario) || idUsuario <= 0)
        {
            throw new UnauthorizedAccessException(
                "El token no contiene un usuario válido.");
        }

        return idUsuario;
    }

    private int GetRequiredIntegerClaim(string claimType)
    {
        var value = User.FindFirst(claimType)?.Value;

        if (!int.TryParse(value, out var result) || result <= 0)
        {
            throw new UnauthorizedAccessException(
                $"El token no contiene el claim {claimType} válido.");
        }

        return result;
    }
}