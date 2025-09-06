using Confluent.Kafka;
using EventManagement.API.Configuration;
using EventManagement.API.Interfaces;
using EventManagement.API.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EventManagement.API.Services;

public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaConfiguration _kafkaConfig;
    private readonly ILogger<KafkaEventPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed = false;

    public KafkaEventPublisher(IOptions<KafkaConfiguration> kafkaConfig, ILogger<KafkaEventPublisher> logger)
    {
        _kafkaConfig = kafkaConfig.Value;
        _logger = logger;
        
        _logger.LogInformation("Initializing Kafka Event Publisher with BootstrapServers: {BootstrapServers}, ClientId: {ClientId}", 
            _kafkaConfig.BootstrapServers, _kafkaConfig.ClientId);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var config = new ProducerConfig
        {
            BootstrapServers = _kafkaConfig.BootstrapServers,
            ClientId = _kafkaConfig.ClientId,
            MessageTimeoutMs = _kafkaConfig.MessageTimeoutMs,
            RequestTimeoutMs = _kafkaConfig.RequestTimeoutMs,
            CompressionType = Enum.Parse<CompressionType>(_kafkaConfig.CompressionType, true),
            RetryBackoffMs = _kafkaConfig.RetryBackoffMs,
            MessageMaxBytes = _kafkaConfig.MessageMaxBytes,
            Acks = Acks.All,  // Required when EnableIdempotence = true
            EnableIdempotence = true,
            MaxInFlight = 1,
            LingerMs = 10
        };

        try
        {
            _producer = new ProducerBuilder<string, string>(config)
                .SetErrorHandler((_, e) => _logger.LogError("Kafka producer error: {Error}", e.Reason))
                .SetLogHandler((_, logMessage) => 
                {
                    var logLevel = logMessage.Level switch
                    {
                        SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical or SyslogLevel.Error => LogLevel.Error,
                        SyslogLevel.Warning => LogLevel.Warning,
                        SyslogLevel.Notice or SyslogLevel.Info => LogLevel.Information,
                        SyslogLevel.Debug => LogLevel.Debug,
                        _ => LogLevel.Information
                    };
                    _logger.Log(logLevel, "Kafka: {Message}", logMessage.Message);
                })
                .Build();

            _logger.LogInformation("Kafka Event Publisher successfully initialized and producer created");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Kafka Event Publisher");
            throw;
        }
    }

    public async Task<bool> PublishEventCreatedAsync(EventCreatedMessage message, CancellationToken cancellationToken = default)
    {
        return await PublishMessageAsync(_kafkaConfig.Topics.EventCreated, message.EventId.ToString(), message, cancellationToken);
    }

    public async Task<bool> PublishEventUpdatedAsync(EventUpdatedMessage message, CancellationToken cancellationToken = default)
    {
        return await PublishMessageAsync(_kafkaConfig.Topics.EventUpdated, message.EventId.ToString(), message, cancellationToken);
    }

    public async Task<bool> PublishEventDeletedAsync(EventDeletedMessage message, CancellationToken cancellationToken = default)
    {
        return await PublishMessageAsync(_kafkaConfig.Topics.EventDeleted, message.EventId.ToString(), message, cancellationToken);
    }

    private async Task<bool> PublishMessageAsync<T>(string topic, string key, T message, CancellationToken cancellationToken) where T : BaseEventMessage
    {
        try
        {
            _logger.LogDebug("Publishing message to topic {Topic} with key {Key}", topic, key);

            var messageJson = JsonSerializer.Serialize(message, _jsonOptions);
            
            var kafkaMessage = new Message<string, string>
            {
                Key = key,
                Value = messageJson,
                Timestamp = new Timestamp(message.Timestamp)
            };

            var deliveryReport = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
            
            if (deliveryReport.Status == PersistenceStatus.Persisted)
            {
                _logger.LogInformation(
                    "Successfully published message to topic {Topic}, partition {Partition}, offset {Offset}",
                    deliveryReport.Topic, deliveryReport.Partition.Value, deliveryReport.Offset.Value);
                return true;
            }
            else
            {
                _logger.LogWarning(
                    "Message was not persisted to topic {Topic}. Status: {Status}",
                    topic, deliveryReport.Status);
                return false;
            }
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, 
                "Failed to publish message to topic {Topic} with key {Key}. Error: {Error}",
                topic, key, ex.Error.Reason);
            return false;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Publishing to topic {Topic} was cancelled", topic);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Unexpected error while publishing message to topic {Topic} with key {Key}",
                topic, key);
            return false;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                _logger.LogInformation("Disposing Kafka Event Publisher");
                _producer?.Flush(TimeSpan.FromSeconds(10));
                _producer?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while disposing Kafka Event Publisher");
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}