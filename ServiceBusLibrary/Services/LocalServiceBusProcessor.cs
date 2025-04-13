using System.Text.Json;
using ServiceBusLibrary.Interfaces;
using ServiceBusLibrary.Models;

namespace ServiceBusLibrary.Services;

public sealed class LocalServiceBusProcessor : IServiceBusProcessor, IAsyncDisposable
{
    private readonly string _entityPath;
    private readonly CancellationTokenSource _cts = new();
    private Task? _processingTask;

    public event Func<ProcessMessageEventArgs, Task>? ProcessMessageAsync;
    public event Func<ProcessErrorEventArgs, Task>? ProcessErrorAsync;

    public LocalServiceBusProcessor(string basePath, string queueName)
    {
        _entityPath = Path.Combine(basePath, "Queues", queueName);
        Directory.CreateDirectory(_entityPath);
    }

    public LocalServiceBusProcessor(string basePath, string topicName, string subscriptionName)
    {
        _entityPath = Path.Combine(basePath, "Topics", topicName, subscriptionName);
        Directory.CreateDirectory(_entityPath);
    }

    public Task StartProcessingAsync()
    {
        _processingTask = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    string[] files = Directory.GetFiles(_entityPath, "*.json");
                    
                    if (files.Length > 0)
                    {
                        // Process oldest message
                        string file = files[0];
                        string json = await File.ReadAllTextAsync(file);

                        ServiceBusMessage message = JsonSerializer.Deserialize<ServiceBusMessage>(json)!;

                        ProcessMessageEventArgs args = new()
                        {
                            Message = message,
                            CompleteMessageAsync = async (msg) => { File.Delete(file); await Task.CompletedTask; }
                        };

                        if (ProcessMessageAsync != null)
                        {
                            await ProcessMessageAsync(args);
                        }
                    }
                    else
                    {
                        // Poll if no messages
                        await Task.Delay(100);
                    }
                }
                catch (Exception ex)
                {
                    if (ProcessErrorAsync != null)
                    {
                        await ProcessErrorAsync(new ProcessErrorEventArgs 
                        { 
                            Exception = ex, 
                            EntityPath = _entityPath,
                            ErrorSource = "LocalProcessor",
                            FullyQualifiedNamespace = "local",
                        });
                    }
                }
            }
        });

        return Task.CompletedTask;
    }

    public async Task StopProcessingAsync()
    {
        _cts?.Cancel();

        if (_processingTask != null)
        {
            await _processingTask;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopProcessingAsync();
        _cts.Dispose();
    }
}
