using Microsoft.Extensions.Logging;
using ServiceBusLibrary.Interfaces;
using System.Text;
using System.Text.Json;
using ServiceBusMessage = ServiceBusLibrary.Models.ServiceBusMessage;

namespace ServiceBusLibrary.Services;

public class QueueService : IQueueService
{
    private readonly ILogger<QueueService> _logger;
    private readonly IServiceBusClient _client;

    public QueueService(ILogger<QueueService> logger, IServiceBusClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task SendMessageAsync<T>(T ServiceBusMessage, string queueName)
    {
        try
        {
            IServiceBusSender sender = _client.CreateSender(queueName);
            string messageBody = JsonSerializer.Serialize(ServiceBusMessage);
            ServiceBusMessage message = new()
            { 
                Body = Encoding.UTF8.GetBytes(messageBody) 
            };
            await sender.SendMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send message -- {ex.Message}", ex.Message);
        }
    }
}
