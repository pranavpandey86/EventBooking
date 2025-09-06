namespace EventSearch.Infrastructure.Configuration;

public class KafkaConsumerConfiguration
{
    public const string SectionName = "KafkaConsumer";
    
    public required string BootstrapServers { get; set; }
    public required string GroupId { get; set; }
    public string AutoOffsetReset { get; set; } = "earliest";
    public bool EnableAutoCommit { get; set; } = false;
    public int SessionTimeoutMs { get; set; } = 6000;
    public int HeartbeatIntervalMs { get; set; } = 3000;
    public int MaxPollIntervalMs { get; set; } = 300000;
    public int FetchMinBytes { get; set; } = 1024;
    public int FetchMaxWaitMs { get; set; } = 500;
    
    public KafkaTopics Topics { get; set; } = new();
}

public class KafkaTopics
{
    public string EventCreated { get; set; } = "event-created";
    public string EventUpdated { get; set; } = "event-updated";
    public string EventDeleted { get; set; } = "event-deleted";
}