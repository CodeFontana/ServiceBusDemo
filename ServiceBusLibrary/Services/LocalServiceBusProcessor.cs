using CodeFoxtrot.CloudLocal.Messaging;
using ServiceBusLibrary.Interfaces;
using ServiceBusLibrary.Models;

namespace ServiceBusLibrary.Services;

public sealed class LocalServiceBusProcessor : IServiceBusProcessor, IAsyncDisposable
{
    private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(30);

    private readonly FileQueueMessenger? _queues;
    private readonly FileTopicMessenger? _topics;
    private readonly string _entityName;
    private readonly string? _subscriptionName;
    private readonly CancellationTokenSource _cts = new();
    private Task? _processingTask;

    public event Func<ProcessMessageEventArgs, Task>? ProcessMessageAsync;
    public event Func<ProcessErrorEventArgs, Task>? ProcessErrorAsync;

    public LocalServiceBusProcessor(FileQueueMessenger queues, string queueName)
    {
        ArgumentNullException.ThrowIfNull(queues);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        _queues = queues;
        _entityName = queueName;
    }

    public LocalServiceBusProcessor(FileTopicMessenger topics, string topicName, string subscriptionName)
    {
        ArgumentNullException.ThrowIfNull(topics);
        ArgumentException.ThrowIfNullOrWhiteSpace(topicName);
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionName);
        _topics = topics;
        _entityName = topicName;
        _subscriptionName = subscriptionName;
    }

    public async Task StartProcessingAsync()
    {
        if (_subscriptionName is null)
        {
            await Queues.CreateQueueAsync(_entityName);
        }
        else
        {
            await Topics.CreateTopicAsync(_entityName);
            await Topics.CreateSubscriptionAsync(_entityName, _subscriptionName);
        }

        _processingTask = Task.Run(() => ProcessLoopAsync(_cts.Token));
    }

    public async Task StopProcessingAsync()
    {
        await _cts.CancelAsync();

        if (_processingTask is not null)
        {
            await _processingTask;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopProcessingAsync();
        _cts.Dispose();
    }

    private async Task ProcessLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                IReadOnlyList<MessageDelivery> deliveries = await ReceiveAsync(cancellationToken);

                if (deliveries.Count == 0)
                {
                    await Task.Delay(100, cancellationToken);
                    continue;
                }

                foreach (MessageDelivery delivery in deliveries)
                {
                    await DispatchAsync(delivery, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                await RaiseErrorAsync(ex);
            }
        }
    }

    private async Task DispatchAsync(MessageDelivery delivery, CancellationToken cancellationToken)
    {
        bool completed = false;
        ProcessMessageEventArgs args = new()
        {
            Message = new ServiceBusMessage
            {
                Body = delivery.Body,
                MessageId = delivery.MessageId
            },
            CompleteMessageAsync = async _ =>
            {
                await CompleteAsync(delivery, cancellationToken);
                completed = true;
            }
        };

        try
        {
            if (ProcessMessageAsync is not null)
            {
                await ProcessMessageAsync(args);
            }

            if (!completed)
            {
                await AbandonAsync(delivery, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            if (!completed)
            {
                try
                {
                    await AbandonAsync(delivery, CancellationToken.None);
                }
                catch (Exception abandonException)
                {
                    await RaiseErrorAsync(abandonException);
                }
            }

            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            await RaiseErrorAsync(ex);
        }
    }

    private Task<IReadOnlyList<MessageDelivery>> ReceiveAsync(CancellationToken cancellationToken)
    {
        if (_subscriptionName is null)
        {
            return Queues.ReceiveAsync(_entityName, LockDuration, 1, cancellationToken);
        }

        return Topics.ReceiveAsync(_entityName, _subscriptionName, LockDuration, 1, cancellationToken);
    }

    private Task CompleteAsync(MessageDelivery delivery, CancellationToken cancellationToken)
    {
        if (_subscriptionName is null)
        {
            return Queues.CompleteAsync(_entityName, delivery.MessageId, delivery.Receipt, cancellationToken);
        }

        return Topics.CompleteAsync(_entityName, _subscriptionName, delivery.MessageId, delivery.Receipt, cancellationToken);
    }

    private Task AbandonAsync(MessageDelivery delivery, CancellationToken cancellationToken)
    {
        if (_subscriptionName is null)
        {
            return Queues.AbandonAsync(_entityName, delivery.MessageId, delivery.Receipt, cancellationToken);
        }

        return Topics.AbandonAsync(_entityName, _subscriptionName, delivery.MessageId, delivery.Receipt, cancellationToken);
    }

    private async Task RaiseErrorAsync(Exception exception)
    {
        if (ProcessErrorAsync is null)
        {
            return;
        }

        string entityPath = _subscriptionName is null
            ? _entityName
            : $"{_entityName}/subscriptions/{_subscriptionName}";

        await ProcessErrorAsync(new ProcessErrorEventArgs
        {
            Exception = exception,
            EntityPath = entityPath,
            ErrorSource = "LocalProcessor",
            FullyQualifiedNamespace = "local"
        });
    }

    private FileQueueMessenger Queues => _queues ?? throw new InvalidOperationException("Queue messenger is not configured.");

    private FileTopicMessenger Topics => _topics ?? throw new InvalidOperationException("Topic messenger is not configured.");
}
