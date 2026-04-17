namespace XlentApi.API.Middleware;


public class ApiKeyMiddleware
{
    private const string ApiKeyHeader = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly string _expectedKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _expectedKey = configuration["ApiSecurity:ApiKey"]
            ?? throw new InvalidOperationException(
                "ApiSecurity:ApiKey is missing from configuration. " +
                "Add it to appsettings.Development.json (and keep that file out of git).");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string? providedKey = null;

        if (context.Request.Headers.TryGetValue(ApiKeyHeader, out var headerKey))
        {
            providedKey = headerKey.ToString();
        }
        else if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var authValue = authHeader.ToString();
            const string bearerPrefix = "Bearer ";
            if (authValue.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                providedKey = authValue[bearerPrefix.Length..].Trim();
            }
        }
        else if (context.Request.Query.TryGetValue("apiKey", out var queryKey))
        {
            // SignalR WebSocket upgrade fallback: ?apiKey=...
            providedKey = queryKey.ToString();
        }

        if (string.IsNullOrWhiteSpace(providedKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing API key. Provide X-Api-Key or Bearer token.");
            return;
        }

        if (!string.Equals(providedKey, _expectedKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid API key.");
            return;
        }

        await _next(context);
    }
}
