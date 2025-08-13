using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EventManagement.API.Extensions
{
    public static class HealthCheckExtensions
    {
        public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy())
                .AddSqlServer(
                    configuration.GetConnectionString("DefaultConnection")!,
                    name: "sql-server",
                    tags: new[] { "db", "sql", "sqlserver" })
                .AddCheck<ApiHealthCheck>("api-health");

            return services;
        }

        public static IEndpointRouteBuilder MapHealthCheckEndpoints(this IEndpointRouteBuilder endpoints)
        {
            // Basic health check
            endpoints.MapHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    var result = new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(entry => new
                        {
                            name = entry.Key,
                            status = entry.Value.Status.ToString(),
                            exception = entry.Value.Exception?.Message,
                            duration = entry.Value.Duration.ToString()
                        })
                    };
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(result));
                }
            });

            // Readiness check (for Kubernetes)
            endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                    {
                        status = report.Status.ToString()
                    }));
                }
            });

            // Liveness check (for Kubernetes)
            endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false,
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                    {
                        status = "Healthy"
                    }));
                }
            });

            return endpoints;
        }
    }

    public class ApiHealthCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            // Add custom health check logic here
            // For example: check external dependencies, disk space, memory usage, etc.
            
            var isHealthy = true; // Your health check logic

            if (isHealthy)
            {
                return Task.FromResult(HealthCheckResult.Healthy("EventManagement API is healthy"));
            }

            return Task.FromResult(HealthCheckResult.Unhealthy("EventManagement API is unhealthy"));
        }
    }
}
