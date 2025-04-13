using ServiceBusLibrary.Services;

namespace ServiceBusLibrary.Interfaces;

public interface IServiceBusProcessor : IAsyncDisposable
{
    event Func<ProcessMessageEventArgs, Task> ProcessMessageAsync;
    event Func<ProcessErrorEventArgs, Task> ProcessErrorAsync;

    Task StartProcessingAsync();
    Task StopProcessingAsync();
}
