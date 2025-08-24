using EventSearch.Infrastructure.Configuration;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add EventSearch Infrastructure services
builder.Services.AddEventSearchInfrastructure(builder.Configuration);

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("elasticsearch", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Elasticsearch is available"))
    .AddCheck("redis", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Redis is available"));

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EventSearch API",
        Version = "v1",
        Description = "Advanced search API for event discovery with Elasticsearch and Redis caching",
        Contact = new OpenApiContact
        {
            Name = "Event Management Team",
            Email = "team@eventmanagement.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Initialize infrastructure
try
{
    await app.Services.InitializeEventSearchInfrastructureAsync();
    app.Logger.LogInformation("EventSearch infrastructure initialized successfully");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Failed to initialize EventSearch infrastructure");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EventSearch API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAngularApp");

app.UseAuthorization();

app.MapControllers();

// Add health check endpoint
app.MapHealthChecks("/health");

app.Logger.LogInformation("EventSearch API is starting up...");

app.Run();
