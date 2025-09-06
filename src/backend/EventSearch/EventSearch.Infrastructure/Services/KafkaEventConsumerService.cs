using Confluent.Kafka;
using EventSearch.Core.Interfaces;
using EventSearch.Core.Models;
using EventSearch.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EventSearch.Infrastructure.Services;

public class KafkaEventConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaConsumerConfiguration _config;
    private readonly ILogger<KafkaEventConsumerService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private IConsumer<string, string>? _consumer;

    public KafkaEventConsumerService(
        IServiceProvider serviceProvider,
        IOptions<KafkaConsumerConfiguration> config,
        ILogger<KafkaEventConsumerService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Kafka Event Consumer Service");
        
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _config.BootstrapServers,
            GroupId = _config.GroupId,
            AutoOffsetReset = Enum.Parse<AutoOffsetReset>(_config.AutoOffsetReset, true),
            EnableAutoCommit = _config.EnableAutoCommit,
            SessionTimeoutMs = _config.SessionTimeoutMs,
            HeartbeatIntervalMs = _config.HeartbeatIntervalMs,
            MaxPollIntervalMs = _config.MaxPollIntervalMs,
            FetchMinBytes = _config.FetchMinBytes,
            FetchWaitMaxMs = _config.FetchMaxWaitMs,
            EnablePartitionEof = false,
            AllowAutoCreateTopics = false
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka consumer error: {Error}", e.Reason))
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
                _logger.Log(logLevel, "Kafka Consumer: {Message}", logMessage.Message);
            })
            .Build();

        // Subscribe to all event topics
        var topics = new[]
        {
            _config.Topics.EventCreated,
            _config.Topics.EventUpdated,
            _config.Topics.EventDeleted
        };

        _consumer.Subscribe(topics);
        _logger.LogInformation("Subscribed to Kafka topics: {Topics}", string.Join(", ", topics));

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka Event Consumer Service is running");

        try
        {
            while (!stoppingToken.IsCancellationRequested && _consumer != null)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(1000));
                    
                    if (consumeResult?.Message != null)
                    {
                        await ProcessMessageAsync(consumeResult, stoppingToken);
                        
                        // Manual commit after successful processing
                        if (!_config.EnableAutoCommit)
                        {
                            _consumer.Commit(consumeResult);
                        }
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming Kafka message");
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogInformation(ex, "Kafka consumer operation was cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in Kafka consumer");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Kafka Event Consumer Service");
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Processing message from topic {Topic}, partition {Partition}, offset {Offset}",
                consumeResult.Topic, consumeResult.Partition.Value, consumeResult.Offset.Value);

            using var scope = _serviceProvider.CreateScope();
            var eventProcessor = scope.ServiceProvider.GetRequiredService<IEventProcessor>();

            switch (consumeResult.Topic)
            {
                case var topic when topic == _config.Topics.EventCreated:
                    var createdMessage = JsonSerializer.Deserialize<EventCreatedMessage>(consumeResult.Message.Value, _jsonOptions);
                    if (createdMessage != null)
                    {
                        await eventProcessor.ProcessEventCreatedAsync(createdMessage, cancellationToken);
                    }
                    break;

                case var topic when topic == _config.Topics.EventUpdated:
                    var updatedMessage = JsonSerializer.Deserialize<EventUpdatedMessage>(consumeResult.Message.Value, _jsonOptions);
                    if (updatedMessage != null)
                    {
                        await eventProcessor.ProcessEventUpdatedAsync(updatedMessage, cancellationToken);
                    }
                    break;

                case var topic when topic == _config.Topics.EventDeleted:
                    var deletedMessage = JsonSerializer.Deserialize<EventDeletedMessage>(consumeResult.Message.Value, _jsonOptions);
                    if (deletedMessage != null)
                    {
                        await eventProcessor.ProcessEventDeletedAsync(deletedMessage, cancellationToken);
                    }
                    break;

                default:
                    _logger.LogWarning("Received message from unknown topic: {Topic}", consumeResult.Topic);
                    break;
            }

            _logger.LogDebug("Successfully processed message from topic {Topic}", consumeResult.Topic);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize message from topic {Topic}: {Message}",
                consumeResult.Topic, consumeResult.Message.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message from topic {Topic}", consumeResult.Topic);
            throw; // Re-throw to prevent commit if processing fails
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Kafka Event Consumer Service");
        
        _consumer?.Close();
        
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        try
        {
            _consumer?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Kafka consumer");
        }
        
        base.Dispose();
    }
}