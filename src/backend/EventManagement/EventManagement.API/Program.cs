using EventManagement.API.Interfaces;
using EventManagement.API.Services;
using EventManagement.Core.Interfaces;
using EventManagement.Core.Services;
using EventManagement.Infrastructure.Data;
using EventManagement.Infrastructure.Repositories;
using EventManagement.API.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure Entity Framework
builder.Services.AddDbContext<EventDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories
builder.Services.AddScoped<IEventRepository, EventRepository>();

// Register services
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IEventDtoService, EventDtoService>();

// Add health checks
builder.Services.AddHealthChecks(builder.Configuration);

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",    // Angular dev server
                "http://localhost:3000",    // Potential React dev server
                "http://frontend:80",       // Docker container frontend
                "http://localhost:80"       // Docker frontend mapped port
            )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

// Add health check endpoints
app.MapHealthCheckEndpoints();

// Ensure database is created (async and non-blocking)
_ = Task.Run(async () =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EventDbContext>();
        await context.Database.EnsureCreatedAsync();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogError(ex, "Failed to initialize database");
    }
});

await app.RunAsync();

// Make the implicit Program class available to tests
public partial class Program 
{ 
    protected Program() { }
}
