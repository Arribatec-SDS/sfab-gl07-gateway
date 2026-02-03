using SfabGl07Gateway.Api.Services;

namespace SfabGl07Gateway.Api.Middleware;

/// <summary>
/// Middleware that initializes the database schema on the first authenticated request.
/// Must be placed AFTER authentication middleware.
/// </summary>
public class DatabaseInitializationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DatabaseInitializationMiddleware> _logger;

    public DatabaseInitializationMiddleware(
        RequestDelegate next,
        ILogger<DatabaseInitializationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only initialize on authenticated requests
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Get the database initializer from DI (scoped service)
            var dbInitializer = context.RequestServices.GetRequiredService<IDatabaseInitializer>();
            await dbInitializer.InitializeAsync();
        }

        await _next(context);
    }
}

/// <summary>
/// Extension method for adding the database initialization middleware.
/// </summary>
public static class DatabaseInitializationMiddlewareExtensions
{
    public static IApplicationBuilder UseDatabaseInitialization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DatabaseInitializationMiddleware>();
    }
}
