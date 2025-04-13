using ServiceBusLibrary.Interfaces;

namespace ServiceBusLibrary.Services;

public sealed class LocalServiceBusClient : IServiceBusClient
{
    private readonly string _basePath;

    public LocalServiceBusClient(string basePath)
    {
        _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        Directory.CreateDirectory(Path.Combine(_basePath, "Queues"));
        Directory.CreateDirectory(Path.Combine(_basePath, "Topics"));
    }

    public IServiceBusSender CreateSender(string entityName, bool isTopic = false)
    {
        return new LocalServiceBusSender(_basePath, entityName, isTopic);
    }

    public IServiceBusProcessor CreateProcessor(string queueName, object? options = null)
    {
        return new LocalServiceBusProcessor(_basePath, queueName);
    }

    public IServiceBusProcessor CreateProcessor(string topicName, string subscriptionName, object? options = null)
    {
        return new LocalServiceBusProcessor(_basePath, topicName, subscriptionName);
    }
}
