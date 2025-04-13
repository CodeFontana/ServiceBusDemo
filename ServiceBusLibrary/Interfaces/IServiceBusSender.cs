using ServiceBusLibrary.Models;

namespace ServiceBusLibrary.Interfaces;

public interface IServiceBusSender
{
    Task SendMessageAsync(ServiceBusMessage message);
}
