using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceBusLibrary.Models;
using static System.Net.Mime.MediaTypeNames;

namespace FunctionApp;

internal sealed class PersonFunctionTimer
{
    private readonly ILogger<PersonFunctionTimer> _logger;
    private readonly IConfiguration _config;
    private readonly string _localBasePath;

    public PersonFunctionTimer(ILogger<PersonFunctionTimer> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
        _localBasePath = config["ConnectionStrings:Local"] 
            ?? throw new ArgumentNullException(nameof(config), "ConnectionStrings:Local configuration is missing.");
    }

    [Function("PersonFunctionTimerTrigger")]
    public async Task Run([TimerTrigger("*/5 * * * * *")] TimerInfo timer)
    {
        string queuePath = Path.Combine(_localBasePath, "Queues", "personqueue");
        string[] localMessages = Directory.GetFiles(queuePath, "*.json");

        foreach (string file in localMessages)
        {
            try
            {
                // Read the JSON file content
                string fileContent = await File.ReadAllTextAsync(file);

                // Parse JSON to get MessageId and Body
                using JsonDocument doc = JsonDocument.Parse(fileContent);
                string? messageId = doc.RootElement.GetProperty("MessageId").GetString();
                string? base64Body = doc.RootElement.GetProperty("Body").GetString();

                if (string.IsNullOrWhiteSpace(messageId))
                {
                    throw new InvalidDataException("Unable to parse message - MessageId is missing or empty.");
                }
                else if (string.IsNullOrWhiteSpace(base64Body))
                {
                    throw new InvalidDataException("Unable to parse message - Body is missing or empty.");
                }

                // Decode the base64 Body to bytes
                byte[] bodyBytes = Convert.FromBase64String(base64Body);

                // Construct a ServiceBusReceivedMessage (arrive at same point as ServiceBusTrigger)
                ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                    body: new BinaryData(bodyBytes),
                    messageId: messageId,
                    contentType: Application.Json // Match ServiceBusTrigger content type
                );

                _logger.LogInformation("Message ID: {id}", message.MessageId);
                _logger.LogInformation("Message Body: {body}", message.Body);
                _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

                string json = Encoding.UTF8.GetString(message.Body);
                PersonModel? person = JsonSerializer.Deserialize<PersonModel>(json);

                if (person is not null)
                {
                    _logger.LogInformation("Received person: {firstName} {lastName}", person.FirstName, person.LastName);
                    File.Delete(file);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError("Failed to deserialize message: {message}", ex.Message);
            }
        }
    }   
}
