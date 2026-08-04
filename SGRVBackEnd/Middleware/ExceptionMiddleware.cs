using System.Net;
using SGRVBackEnd.Shared;

namespace SGRVBackEnd.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(
                exception,
                "Solicitud no autorizada en {Path}.",
                context.Request.Path);

            await WriteResponse(
                context,
                HttpStatusCode.Unauthorized,
                exception.Message);
        }
        catch (OperationCanceledException)
            when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Solicitud cancelada por el cliente en {Path}.",
                context.Request.Path);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Error no controlado en {Path}.",
                context.Request.Path);

            var message = _environment.IsDevelopment()
                ? exception.Message
                : "Ocurrió un error interno en el servidor.";

            await WriteResponse(
                context,
                HttpStatusCode.InternalServerError,
                message);
        }
    }

    private static async Task WriteResponse(
        HttpContext context,
        HttpStatusCode statusCode,
        string message)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new ApiResponse<object>
        {
            Success = false,
            Message = message
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}