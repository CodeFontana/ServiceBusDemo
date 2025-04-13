using System.Text.Json;
using ServiceBusLibrary.Interfaces;
using ServiceBusLibrary.Models;

namespace ServiceBusLibrary.Services;

public sealed class LocalServiceBusSender : IServiceBusSender
{
    private readonly string _basePath;
    private readonly string _entityName;
    private readonly bool _isTopic;

    public LocalServiceBusSender(string basePath, string entityName, bool isTopic)
    {
        _basePath = basePath;
        _entityName = entityName;
        _isTopic = isTopic;
    }

    public async Task SendMessageAsync(ServiceBusMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.MessageId))
        {
            message.MessageId = Guid.NewGuid().ToString();
        }

        string entityPath = _isTopic
            ? Path.Combine(_basePath, "Topics", _entityName)
            : Path.Combine(_basePath, "Queues", _entityName);
        
        Directory.CreateDirectory(entityPath);

        if (_isTopic)
        {
            string[] subscriptions = Directory.GetDirectories(entityPath);

            foreach (string subPath in subscriptions)
            {
                string filePath = Path.Combine(subPath, $"{message.MessageId}.json");
                await WriteMessageAsync(message, filePath);
            }
        }
        else
        {
            string filePath = Path.Combine(_basePath, "Queues", _entityName, $"{message.MessageId}.json");
            await WriteMessageAsync(message, filePath);
        }
    }

    private static async Task WriteMessageAsync(ServiceBusMessage message, string filePath)
    {
        var localMessage = new { message.MessageId, message.Body };
        string json = JsonSerializer.Serialize(localMessage);
        await File.WriteAllTextAsync(filePath, json);
    }
}
