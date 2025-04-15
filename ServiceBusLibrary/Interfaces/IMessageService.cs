namespace ServiceBusLibrary.Interfaces;

public interface IMessageService
{
    Task SendMessageToQueueAsync<T>(T ServiceBusMessage, string queueName);
    Task SendMessageToTopicAsync<T>(T message, string topicName);
}