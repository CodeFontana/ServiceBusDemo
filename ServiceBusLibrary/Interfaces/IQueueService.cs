namespace ServiceBusLibrary.Interfaces;

public interface IQueueService
{
    Task SendMessageAsync<T>(T ServiceBusMessage, string queueName);
    Task SendMessageToTopicAsync<T>(T message, string topicName);
}