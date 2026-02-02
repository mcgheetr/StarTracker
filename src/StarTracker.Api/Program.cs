using StarTracker.Core;
using StarTracker.Api;
using StarTracker.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Register StarTracker services
builder.Services.AddStarTrackerServices(builder.Configuration);

var app = builder.Build();

// API key middleware - validates X-API-Key header for /api routes
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var key = context.Request.Headers[Constants.ApiKeyHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(key))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(Results.Problem(detail: $"Missing {Constants.ApiKeyHeader} header", statusCode: 401));
            return;
        }

        var required = builder.Configuration["ApiKey"];
        if (!string.IsNullOrWhiteSpace(required) && !string.Equals(required, key, StringComparison.Ordinal))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(Results.Problem(detail: "Invalid API key", statusCode: 401));
            return;
        }
    }

    await next();
});

// Map all endpoints
app.MapStarTrackerEndpoints();

app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }

