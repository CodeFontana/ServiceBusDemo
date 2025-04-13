using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ServiceBusLibrary.Models;

namespace FunctionApp;

public sealed class PersonFunction
{
    private readonly ILogger<PersonFunction> _logger;

    public PersonFunction(ILogger<PersonFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(PersonFunction))]
    public async Task Run(
        [ServiceBusTrigger("personQueue", Connection = "AzureServiceBus")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        try
        {
            PersonModel? person = JsonSerializer.Deserialize<PersonModel>(message.Body.ToString());

            if (person is not null)
            {
                _logger.LogInformation("Received person: {firstName} {lastName}", person.FirstName, person.LastName);
                await messageActions.CompleteMessageAsync(message);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError("Failed to deserialize message: {message}", ex.Message);
        }
    }
}
