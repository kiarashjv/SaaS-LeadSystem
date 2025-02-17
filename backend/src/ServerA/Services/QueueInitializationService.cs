using ServerA.Controllers;
using SharedMessaging;
using SharedModels;

namespace ServerA.Services;

public class QueueInitializationService : IHostedService
{
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<QueueInitializationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private const string EVALUATION_REQUEST_QUEUE = "lead-evaluation-queue";

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
        var leadController = scope.ServiceProvider.GetRequiredService<LeadsController>();

        _messageBroker.Subscribe<Lead>(EVALUATION_REQUEST_QUEUE, leadController.HandleLeadEvaluation);

        _logger.LogInformation("Queue subscriptions initialized");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}