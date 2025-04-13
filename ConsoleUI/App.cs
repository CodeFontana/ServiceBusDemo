using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceBusLibrary.Interfaces;
using ProcessErrorEventArgs = ServiceBusLibrary.Services.ProcessErrorEventArgs;
using ProcessMessageEventArgs = ServiceBusLibrary.Services.ProcessMessageEventArgs;

namespace ConsoleUI;
public class App : IHostedService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IConfiguration _config;
    private readonly ILogger<App> _logger;
    private readonly IServiceBusClient _client;

    public App(IHostApplicationLifetime hostApplicationLifetime,
               IConfiguration configuration,
               ILogger<App> logger,
               IServiceBusClient client)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _config = configuration;
        _logger = logger;
        _client = client;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _hostApplicationLifetime.ApplicationStarted.Register(async () =>
        {
            try
            {
                await Task.Yield(); // https://github.com/dotnet/runtime/issues/36063
                await Task.Delay(1000); // Additional delay for Microsoft.Hosting.Lifetime messages
                await ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception!");
            }
            finally
            {
                _hostApplicationLifetime.StopApplication();
            }
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task ExecuteAsync()
    {
        await ReceiveMessageAsync("personqueue");
    }

    public async Task ReceiveMessageAsync(string queueName)
    {
        try
        {
            ServiceBusProcessorOptions messageHandlerOptions = new()
            {
                MaxConcurrentCalls = 1,
                AutoCompleteMessages = false
            };

            await using IServiceBusProcessor processor = _client.CreateProcessor(queueName, messageHandlerOptions);
            processor.ProcessMessageAsync += MessageHandler;
            processor.ProcessErrorAsync += ErrorHandler;
            await processor.StartProcessingAsync();
            Console.ReadLine();
            await processor.StopProcessingAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create message processor");
        }
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        string json = Encoding.UTF8.GetString(args.Message.Body);
        _logger.LogInformation("Received message: \n{person}", json);
        await args.CompleteMessageAsync(args.Message);
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Message handler exception -- Source={source}, Namespace={space}, EntityPath={path}", args.ErrorSource, args.FullyQualifiedNamespace, args.EntityPath);
        return Task.CompletedTask;
    }
}
