using ServiceBusLibrary.Interfaces;
using InternalServiceBusMessage = ServiceBusLibrary.Models.ServiceBusMessage;
using AzureServiceBusMessage = Azure.Messaging.ServiceBus.ServiceBusMessage;
using Azure.Messaging.ServiceBus;

namespace ServiceBusLibrary.Services;

public sealed class AzureServiceBusSender : IServiceBusSender
{
    private readonly ServiceBusSender _sender;

    public AzureServiceBusSender(ServiceBusSender sender)
    {
        _sender = sender;
    }

    public async Task SendMessageAsync(InternalServiceBusMessage message)
    {
        AzureServiceBusMessage azureMessage = new(message.Body);
        await _sender.SendMessageAsync(azureMessage);
    }
}
