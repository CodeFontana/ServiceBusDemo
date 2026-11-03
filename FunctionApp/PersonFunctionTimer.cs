using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using CodeFoxtrot.CloudLocal.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceBusLibrary.Models;
using static System.Net.Mime.MediaTypeNames;

namespace FunctionApp;

internal sealed class PersonFunctionTimer
{
    private const string QueueName = "personqueue";
    private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(30);

    private readonly ILogger<PersonFunctionTimer> _logger;
    private readonly string _localBasePath;

    public PersonFunctionTimer(ILogger<PersonFunctionTimer> logger, IConfiguration config)
    {
        _logger = logger;
        _localBasePath = config["ConnectionStrings:Local"]
            ?? throw new ArgumentNullException(nameof(config), "ConnectionStrings:Local configuration is missing.");
    }

    [Function("PersonFunctionTimerTrigger")]
    public async Task Run([TimerTrigger("*/5 * * * * *")] TimerInfo timer)
    {
        FileQueueMessenger messenger = new(_localBasePath);
        await messenger.CreateQueueAsync(QueueName);

        while (true)
        {
            IReadOnlyList<MessageDelivery> deliveries = await messenger.ReceiveAsync(QueueName, LockDuration, maxMessages: 32);

            if (deliveries.Count == 0)
            {
                break;
            }

            foreach (MessageDelivery delivery in deliveries)
            {
                try
                {
                    ServiceBusReceivedMessage message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                        body: new BinaryData(delivery.Body),
                        messageId: delivery.MessageId,
                        contentType: Application.Json
                    );

                    _logger.LogInformation("Message ID: {id}", message.MessageId);
                    _logger.LogInformation("Message Body: {body}", message.Body);
                    _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

                    string json = Encoding.UTF8.GetString(message.Body);
                    PersonModel? person = JsonSerializer.Deserialize<PersonModel>(json);

                    if (person is null)
                    {
                        await messenger.AbandonAsync(QueueName, delivery.MessageId, delivery.Receipt);
                        continue;
                    }

                    _logger.LogInformation("Received person: {firstName} {lastName}", person.FirstName, person.LastName);
                    await messenger.CompleteAsync(QueueName, delivery.MessageId, delivery.Receipt);
                }
                catch (JsonException ex)
                {
                    _logger.LogError("Failed to deserialize message: {message}", ex.Message);
                    await messenger.AbandonAsync(QueueName, delivery.MessageId, delivery.Receipt);
                }
            }
        }
    }
}
