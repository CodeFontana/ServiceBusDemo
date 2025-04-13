using ServiceBusLibrary.Models;

namespace ServiceBusLibrary.Services;

public sealed class ProcessMessageEventArgs : EventArgs
{
    public required ServiceBusMessage Message { get; set; }
    public required Func<ServiceBusMessage, Task> CompleteMessageAsync { get; set; }
}
