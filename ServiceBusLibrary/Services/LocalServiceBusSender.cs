using CodeFoxtrot.CloudLocal.Messaging;
using ServiceBusLibrary.Interfaces;
using ServiceBusLibrary.Models;

namespace ServiceBusLibrary.Services;

public sealed class LocalServiceBusSender : IServiceBusSender
{
    private readonly FileQueueMessenger _queues;
    private readonly FileTopicMessenger _topics;
    private readonly string _entityName;
    private readonly bool _isTopic;

    public LocalServiceBusSender(FileQueueMessenger queues, FileTopicMessenger topics, string entityName, bool isTopic)
    {
        ArgumentNullException.ThrowIfNull(queues);
        ArgumentNullException.ThrowIfNull(topics);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        _queues = queues;
        _topics = topics;
        _entityName = entityName;
        _isTopic = isTopic;
    }

    public async Task SendMessageAsync(ServiceBusMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_isTopic)
        {
            await _topics.CreateTopicAsync(_entityName);
            await _topics.PublishAsync(_entityName, message.Body);
            return;
        }

        await _queues.CreateQueueAsync(_entityName);
        await _queues.SendAsync(_entityName, message.Body);
    }
}
