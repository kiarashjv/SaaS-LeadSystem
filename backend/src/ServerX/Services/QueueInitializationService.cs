using SharedMessaging;
using SharedModels;

namespace ServerX.Services;

public class QueueInitializationService : IHostedService
{
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<QueueInitializationService> _logger;
    private readonly ILeadEvaluationService _evaluationService;
    private readonly ICmsService _cmsService;
    private const string EVALUATION_RESPONSE_QUEUE = "lead-evaluation-queue-result";
    private const string STORAGE_RESPONSE_QUEUE = "lead-storage-queue-result";

    public QueueInitializationService(
        IMessageBroker messageBroker,
        ILogger<QueueInitializationService> logger,
        ILeadEvaluationService evaluationService,
        ICmsService cmsService)
    {
        _messageBroker = messageBroker;
        _logger = logger;
        _evaluationService = evaluationService;
        _cmsService = cmsService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing queue subscriptions...");

        try
        {
            // Subscribe to evaluation results
            _messageBroker.Subscribe<LeadEvaluation>(
                EVALUATION_RESPONSE_QUEUE,
                async (evaluation) =>
                {
                    _logger.LogInformation("Received evaluation response for: {Email}", evaluation.Lead.Email);
                    await _evaluationService.HandleEvaluationResult(evaluation);
                });

            // Subscribe to storage results
            _messageBroker.Subscribe<Lead>(
                STORAGE_RESPONSE_QUEUE,
                async (lead) =>
                {
                    _logger.LogInformation("Received storage response for: {Email}", lead.Email);
                    await _cmsService.HandleStorageResult(lead);
                });

            _logger.LogInformation("Queue subscriptions initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize queue subscriptions");
            throw;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}