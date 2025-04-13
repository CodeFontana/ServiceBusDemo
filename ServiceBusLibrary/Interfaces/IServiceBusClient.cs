namespace ServiceBusLibrary.Interfaces;

public interface IServiceBusClient
{
    IServiceBusSender CreateSender(string entityName, bool isTopic = false);
    IServiceBusProcessor CreateProcessor(string queueName, object? options = null);
    IServiceBusProcessor CreateProcessor(string topicName, string subscriptionName, object? options = null);
}
