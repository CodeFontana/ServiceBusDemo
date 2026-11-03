using CodeFoxtrot.CloudLocal.Messaging;
using ServiceBusLibrary.Interfaces;

namespace ServiceBusLibrary.Services;

public sealed class LocalServiceBusClient : IServiceBusClient
{
    private readonly FileQueueMessenger _queues;
    private readonly FileTopicMessenger _topics;

    public LocalServiceBusClient(string basePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePath);
        _queues = new FileQueueMessenger(basePath);
        _topics = new FileTopicMessenger(basePath);
    }

    public IServiceBusSender CreateSender(string entityName, bool isTopic = false)
    {
        return new LocalServiceBusSender(_queues, _topics, entityName, isTopic);
    }

    public IServiceBusProcessor CreateProcessor(string queueName, object? options = null)
    {
        return new LocalServiceBusProcessor(_queues, queueName);
    }

    public IServiceBusProcessor CreateProcessor(string topicName, string subscriptionName, object? options = null)
    {
        return new LocalServiceBusProcessor(_topics, topicName, subscriptionName);
    }

    public async Task EnsureTopicSubscriptionAsync(string topicName, string subscriptionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionName);
        await _topics.CreateTopicAsync(topicName);
        await _topics.CreateSubscriptionAsync(topicName, subscriptionName);
    }
}
