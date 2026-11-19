using System.Security.Claims;
using TheaterApi.Logging;

namespace TheaterApi.Middleware;

public sealed class ExceptionLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILogSink logs)
    {
        try { await next(context); }
        catch (Exception exception)
        {
            int? personId = int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
            await logs.ErrorAsync(personId, context.Request.Path, exception);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await Results.Problem(statusCode: 500, title: "An unexpected error occurred.").ExecuteAsync(context);
        }
    }
}
