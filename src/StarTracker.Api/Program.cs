using StarTracker.Core;
using StarTracker.Api;
using StarTracker.Api.Endpoints;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Register StarTracker services
builder.Services.AddStarTrackerServices(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "StarTracker API",
        Version = "v1",
        Description = "API for tracking stars and celestial observations.",
        Contact = new OpenApiContact
        {
            Name = "StarTracker Support",
            Email = "groundcoffee3@gmail.com"
        }
    });
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        Name = Constants.ApiKeyHeader,
        In = ParameterLocation.Header,
        Description = "API Key needed to access the endpoints. Example: 'X-API-Key: {your key}'"
    });

    options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("ApiKey", doc, null),
            new List<string>()
        }
    });
});
builder.Services.AddProblemDetails();

var app = builder.Build();


app.UseSwagger(options =>
{
    // Swagger UI currently expects OpenAPI 3.0.x
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;
});

app.UseSwaggerUI();

// API key middleware - validates X-API-Key header for /api routes
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var key = context.Request.Headers[Constants.ApiKeyHeader].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(key))
            {
                var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails {
                    Status = 401,
                    Title = "Unauthorized",
                    Detail = $"Missing {Constants.ApiKeyHeader} header"
                };
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/problem+json";
                var json = System.Text.Json.JsonSerializer.Serialize(problem);
                await context.Response.WriteAsync(json);
                return;
            }

        var required = builder.Configuration["ApiKey"];
            if (!string.IsNullOrWhiteSpace(required) && !string.Equals(required, key, StringComparison.Ordinal))
            {
                var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails {
                    Status = 401,
                    Title = "Unauthorized",
                    Detail = "Invalid API key"
                };
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/problem+json";
                var json = System.Text.Json.JsonSerializer.Serialize(problem);
                await context.Response.WriteAsync(json);
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
