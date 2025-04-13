using Azure.Messaging.ServiceBus;
using ServiceBusLibrary.Interfaces;
using ServiceBusMessage = ServiceBusLibrary.Models.ServiceBusMessage;

namespace ServiceBusLibrary.Services;

public sealed class AzureServiceBusProcessor : IServiceBusProcessor, IAsyncDisposable
{
    private readonly ServiceBusProcessor _processor;

    public AzureServiceBusProcessor(ServiceBusProcessor processor)
    {
        _processor = processor;
    }

    public event Func<ProcessMessageEventArgs, Task> ProcessMessageAsync
    {
        add
        {
            _processor.ProcessMessageAsync += args =>
            {
                ServiceBusMessage message = new()
                {
                    Body = args.Message.Body.ToArray(),
                    MessageId = args.Message.MessageId
                };
                
                ProcessMessageEventArgs eventArgs = new()
                {
                    Message = message,
                    CompleteMessageAsync = async (msg) => await args.CompleteMessageAsync(args.Message)
                };
                return value(eventArgs);
            };
        }
        remove { } 
    }

    public event Func<ProcessErrorEventArgs, Task> ProcessErrorAsync
    {
        add
        {
            _processor.ProcessErrorAsync += args => value(new ProcessErrorEventArgs
            {
                Exception = args.Exception,
                EntityPath = args.EntityPath,
                ErrorSource = args.ErrorSource.ToString(),
                FullyQualifiedNamespace = args.FullyQualifiedNamespace
            });
        }
        remove { }
    }

    public async Task StartProcessingAsync()
    {
        await _processor.StartProcessingAsync();
    }

    public async Task StopProcessingAsync()
    {
        await _processor.StopProcessingAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _processor.DisposeAsync();
    }
}
