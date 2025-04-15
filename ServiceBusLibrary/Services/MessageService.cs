using Microsoft.Extensions.Logging;
using ServiceBusLibrary.Interfaces;
using System.Text;
using System.Text.Json;
using ServiceBusMessage = ServiceBusLibrary.Models.ServiceBusMessage;

namespace ServiceBusLibrary.Services;

public class MessageService : IMessageService
{
    private readonly ILogger<MessageService> _logger;
    private readonly IServiceBusClient _client;

    public MessageService(ILogger<MessageService> logger, IServiceBusClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task SendMessageToQueueAsync<T>(T serviceBusMessage, string queueName)
    {
        try
        {
            IServiceBusSender sender = _client.CreateSender(queueName);
            string messageBody = JsonSerializer.Serialize(serviceBusMessage);
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

    public async Task SendMessageToTopicAsync<T>(T serviceBusMessage, string topicName)
    {
        try
        {
            IServiceBusSender sender = _client.CreateSender(topicName, isTopic: true);
            string messageBody = JsonSerializer.Serialize(serviceBusMessage);
            ServiceBusMessage message = new()
            {
                Body = Encoding.UTF8.GetBytes(messageBody)
            };
            await sender.SendMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send message to topic: {Message}", ex.Message);
            throw;
        }
    }
}
