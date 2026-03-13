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
        // Allow the SignalR WebSocket upgrade to carry the key as a query param
        // so clients that cannot set headers on the upgrade handshake can still connect.
        if (context.Request.Headers.TryGetValue(ApiKeyHeader, out var headerKey))
        {
            if (!headerKey.Equals(_expectedKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid API key.");
                return;
            }
        }
        else if (context.Request.Query.TryGetValue("apiKey", out var queryKey))
        {
            // SignalR WebSocket upgrade path: ?apiKey=...
            if (!queryKey.Equals(_expectedKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid API key.");
                return;
            }
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing API key. Provide the X-Api-Key header.");
            return;
        }

        await _next(context);
    }
}
