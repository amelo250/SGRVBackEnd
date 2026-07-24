using Microsoft.EntityFrameworkCore;
using SGRVBackEnd.Helpers;
using System.Net;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

using System.Net; 
using System.Text.Json; 
using SGRVBackEnd.Helpers; 

namespace SGRVBackEnd.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next; private readonly ILogger<ExceptionMiddleware> _logger; private readonly IWebHostEnvironment _environment; public ExceptionMiddleware(RequestDelegate next, ILogger < ExceptionMiddleware > logger, IWebHostEnvironment environment)
        {
            _next = next; _logger = logger; _environment = environment;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no controlado"); context.Response.ContentType = "application/json"; context.Response.StatusCode =
                    (int)HttpStatusCode.InternalServerError; var mensaje = _environment.IsDevelopment() ? ex.Message
                    : "Ocurrió un error interno en el servidor."; var respuesta = ApiResponse<object>.Fallido(mensaje); var json = JsonSerializer.Serialize(respuesta); await context   .Response.WriteAsync(json);
            }
        }
    }
}