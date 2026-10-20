using System.Security.Cryptography;
using System.Text;

namespace UserManagementAPI.Middleware;

public sealed class TokenAuthenticationMiddleware(
    RequestDelegate next,
    IConfiguration config,
    IHostEnvironment environment,
    ILogger<TokenAuthenticationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (!environment.IsDevelopment() && !context.Request.IsHttps)
        {
            logger.LogWarning("Rejected insecure request {Method} {Path}: HTTPS required", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "HTTPS is required." });
            return;
        }

        string? expectedToken = config["ApiToken"];
        string? authorization = context.Request.Headers.Authorization.ToString();
        string providedToken = authorization?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
            ? authorization[7..].Trim()
            : string.Empty;

        bool authorized = !string.IsNullOrWhiteSpace(expectedToken)
            && !string.IsNullOrEmpty(providedToken)
            && FixedTimeEqual(providedToken, expectedToken);

        if (!authorized)
        {
            logger.LogWarning("Rejected unauthorized request {Method} {Path}: 401", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized. Supply a valid Bearer token." });
            return;
        }

        await next(context);
    }

    private static bool FixedTimeEqual(string first, string second)
    {
        byte[] a = SHA256.HashData(Encoding.UTF8.GetBytes(first));
        byte[] b = SHA256.HashData(Encoding.UTF8.GetBytes(second));
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
