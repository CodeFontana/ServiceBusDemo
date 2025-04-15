namespace ServiceBusLibrary.Interfaces;

public interface IMessageService
{
    Task SendMessageAsync<T>(T ServiceBusMessage, string queueName);
    Task SendMessageToTopicAsync<T>(T message, string topicName);
}