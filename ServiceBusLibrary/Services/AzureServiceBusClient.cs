using Azure.Messaging.ServiceBus;
using ServiceBusLibrary.Interfaces;

namespace ServiceBusLibrary.Services;

public sealed class AzureServiceBusClient : IServiceBusClient
{
    private readonly ServiceBusClient _client;

    public AzureServiceBusClient(string connectionString)
    {
        _client = new ServiceBusClient(connectionString);
    }

    public IServiceBusSender CreateSender(string entityName, bool isTopic = false)
    {
        return new AzureServiceBusSender(_client.CreateSender(entityName));
    }

    public IServiceBusProcessor CreateProcessor(string queueName, object? options = null)
    {
        ServiceBusProcessorOptions processorOptions = options as ServiceBusProcessorOptions ?? new ServiceBusProcessorOptions();
        return new AzureServiceBusProcessor(_client.CreateProcessor(queueName, processorOptions));
    }

    public IServiceBusProcessor CreateProcessor(string topicName, string subscriptionName, object? options = null)
    {
        ServiceBusProcessorOptions processorOptions = options as ServiceBusProcessorOptions ?? new ServiceBusProcessorOptions();
        return new AzureServiceBusProcessor(_client.CreateProcessor(topicName, subscriptionName, processorOptions));
    }
}
