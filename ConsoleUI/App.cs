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

    private IServiceBusProcessor? _queueProcessor;
    private IServiceBusProcessor? _topicProcessor;
    private readonly CancellationTokenSource _cts = new();

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
                await Task.Delay(250, cancellationToken);
                await ExecuteAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is triggered
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

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();

        if (_queueProcessor != null)
        {
            await _queueProcessor.StopProcessingAsync();
            await _queueProcessor.DisposeAsync();
        }

        if (_topicProcessor != null)
        {
            await _topicProcessor.StopProcessingAsync();
            await _topicProcessor.DisposeAsync();
        }

        _cts.Dispose();
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // Start both processors
        await Task.WhenAll(
            ReceiveMessageFromQueueAsync("personqueue", cancellationToken),
            ReceiveMessageFromTopicAsync("personTopic", "sub1", cancellationToken)
        );

        // Wait for user input
        _logger.LogInformation("Press ENTER to stop processing messages...");
        await Task.Run(() => Console.ReadLine(), cancellationToken);
    }

    public async Task ReceiveMessageFromQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        try
        {
            ServiceBusProcessorOptions messageHandlerOptions = new()
            {
                MaxConcurrentCalls = 1,
                AutoCompleteMessages = false
            };

            _queueProcessor = _client.CreateProcessor(queueName, messageHandlerOptions);
            _queueProcessor.ProcessMessageAsync += MessageHandler;
            _queueProcessor.ProcessErrorAsync += ErrorHandler;
            await _queueProcessor.StartProcessingAsync();

            // Keep the processor running until cancellation
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is triggered
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

    private async Task ReceiveMessageFromTopicAsync(string topicName, string subscriptionName, CancellationToken cancellationToken)
    {
        try
        {
            ServiceBusProcessorOptions options = new()
            {
                MaxConcurrentCalls = 1,
                AutoCompleteMessages = false
            };

            _topicProcessor = _client.CreateProcessor(topicName, subscriptionName, options);
            _topicProcessor.ProcessMessageAsync += async args =>
            {
                string messageBody = Encoding.UTF8.GetString(args.Message.Body);
                _logger.LogInformation($"Received message from topic {topicName} subscription {subscriptionName}: \n{messageBody}");
                await args.CompleteMessageAsync(args.Message);
            };
            _topicProcessor.ProcessErrorAsync += args =>
            {
                _logger.LogError($"Error processing message from topic {topicName} subscription {subscriptionName}: {args.Exception.Message}");
                return Task.CompletedTask;
            };

            await _topicProcessor.StartProcessingAsync();

            // Keep the processor running until cancellation
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is triggered
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to process subscription: {Message}", ex.Message);
        }
    }
}
