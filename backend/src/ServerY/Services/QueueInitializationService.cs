using ServerY.Controllers;
using SharedMessaging;
using SharedModels;

namespace ServerY.Services;

public class QueueInitializationService : IHostedService
{
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<QueueInitializationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private const string STORAGE_REQUEST_QUEUE = "lead-storage-queue";
    private const string STORAGE_RESPONSE_QUEUE = "lead-storage-queue-result";

    public QueueInitializationService(
        IMessageBroker messageBroker,
        ILogger<QueueInitializationService> logger,
        IServiceProvider serviceProvider)
    {
        _messageBroker = messageBroker;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing queue subscriptions...");

        using var scope = _serviceProvider.CreateScope();
        var leadsController = scope.ServiceProvider.GetRequiredService<LeadsController>();

        _messageBroker.Subscribe<Lead>(STORAGE_REQUEST_QUEUE, leadsController.HandleLeadStorage);

        _logger.LogInformation("Queue subscriptions initialized");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}