namespace ServiceBusLibrary.Models;

public sealed class ServiceBusMessage
{
    public required byte[] Body { get; set; }
    public string? MessageId { get; set; }
}
