using System.Security.Claims;

using System.Security.Claims; 
namespace SGRVBackEnd.Helpers
{
    public static class ClaimsHelper
    {
        public static int ObtenerIdEmpresa(ClaimsPrincipal user)
        {
            var claim = user.FindFirst("IdEmpresa"); if (claim == null || !int.TryParse(claim.Value, out var idEmpresa))
            {
                throw new UnauthorizedAccessException("El token no contiene una empresa válida."
                );
            }
            return idEmpresa;
        }
        public static int ObtenerIdUsuario(ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier); if (claim == null || !int.TryParse(claim.Value, out var idUsuario))
            {
                throw new UnauthorizedAccessException("El token no contiene un usuario válido."
                );
            }
            return  idUsuario;
        }
    }
}