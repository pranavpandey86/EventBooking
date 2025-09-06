namespace EventManagement.API.Configuration;

public class KafkaConfiguration
{
    public const string SectionName = "Kafka";
    
    public required string BootstrapServers { get; set; }
    public required string ClientId { get; set; }
    public int MessageTimeoutMs { get; set; } = 5000;
    public int RequestTimeoutMs { get; set; } = 30000;
    public string CompressionType { get; set; } = "snappy";
    public int RetryBackoffMs { get; set; } = 100;
    public int MessageMaxBytes { get; set; } = 1000000;
    
    public KafkaTopics Topics { get; set; } = new();
}

public class KafkaTopics
{
    public string EventCreated { get; set; } = "event-created";
    public string EventUpdated { get; set; } = "event-updated";
    public string EventDeleted { get; set; } = "event-deleted";
}