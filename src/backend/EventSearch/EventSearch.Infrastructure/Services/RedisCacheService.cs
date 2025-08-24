using EventSearch.Core.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace EventSearch.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly IServer _server;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheService(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisCacheService> logger)
    {
        _database = connectionMultiplexer.GetDatabase();
        _server = connectionMultiplexer.GetServer(connectionMultiplexer.GetEndPoints().First());
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            
            if (!value.HasValue)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(value!, _jsonOptions);
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from cache for key: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            if (value == null)
            {
                _logger.LogWarning("Attempted to cache null value for key: {Key}", key);
                return;
            }

            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var success = await _database.StringSetAsync(key, json, expiration);
            
            if (success)
            {
                _logger.LogDebug("Successfully cached value for key: {Key} with expiration: {Expiration}", 
                    key, expiration?.ToString() ?? "none");
            }
            else
            {
                _logger.LogWarning("Failed to cache value for key: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var removed = await _database.KeyDeleteAsync(key);
            
            if (removed)
            {
                _logger.LogDebug("Successfully removed cache entry for key: {Key}", key);
            }
            else
            {
                _logger.LogDebug("Cache entry not found for key: {Key}", key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing value from cache for key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var keys = _server.Keys(pattern: pattern).ToArray();
            
            if (keys.Length == 0)
            {
                _logger.LogDebug("No cache entries found for pattern: {Pattern}", pattern);
                return;
            }

            var removed = await _database.KeyDeleteAsync(keys);
            _logger.LogDebug("Removed {RemovedCount} cache entries for pattern: {Pattern}", removed, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing values from cache for pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _database.KeyExistsAsync(key);
            _logger.LogDebug("Cache key exists check for {Key}: {Exists}", key, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if cache key exists: {Key}", key);
            return false;
        }
    }
}