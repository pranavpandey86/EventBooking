using EventSearch.Core.Interfaces;
using EventSearch.Core.Services;
using EventSearch.Infrastructure.Repositories;
using EventSearch.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using StackExchange.Redis;

namespace EventSearch.Infrastructure.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventSearchInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Elasticsearch
        services.AddSingleton<IElasticClient>(provider =>
        {
            var connectionString = configuration.GetConnectionString("Elasticsearch") ?? "http://localhost:9200";
            var settings = new ConnectionSettings(new Uri(connectionString))
                .DefaultIndex("events")
                .DisableDirectStreaming()
                .PrettyJson()
                .DefaultMappingFor<Core.Entities.SearchableEvent>(m => m
                    .IndexName("events")
                    .IdProperty(p => p.Id)
                );

            return new ElasticClient(settings);
        });

        // Register Redis
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var connectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
            var configurationOptions = ConfigurationOptions.Parse(connectionString);
            configurationOptions.AbortOnConnectFail = false;
            configurationOptions.ConnectRetry = 3;
            configurationOptions.ConnectTimeout = 5000;
            
            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        // Register repositories
        services.AddScoped<IElasticsearchRepository, ElasticsearchRepository>();

        // Register services
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IEventIndexService, EventIndexService>();

        return services;
    }

    public static async Task InitializeEventSearchInfrastructureAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        
        // Initialize Elasticsearch index
        var elasticsearchRepository = scope.ServiceProvider.GetRequiredService<IElasticsearchRepository>();
        await elasticsearchRepository.CreateIndexIfNotExistsAsync();
    }
}