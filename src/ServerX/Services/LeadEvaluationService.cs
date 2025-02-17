using System.Net.Http.Json;
using Polly;
using Polly.Retry;
using SharedMessaging;
using SharedModels;

namespace ServerX.Services;

public interface ILeadEvaluationService
{
    Task<LeadEvaluation> EvaluateLeadAsync(Lead lead);
}

public class LeadEvaluationService : ILeadEvaluationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LeadEvaluationService> _logger;
    private readonly IMessageBroker _messageBroker;
    private readonly AsyncRetryPolicy<LeadEvaluation> _retryPolicy;
    private const string EVALUATION_QUEUE = "lead-evaluation-queue";
    private readonly Dictionary<string, TaskCompletionSource<LeadEvaluation>> _pendingEvaluations;

    public LeadEvaluationService(
        HttpClient httpClient,
        ILogger<LeadEvaluationService> logger,
        IMessageBroker messageBroker)
    {
        _httpClient = httpClient;
        _logger = logger;
        _messageBroker = messageBroker;
        _pendingEvaluations = new Dictionary<string, TaskCompletionSource<LeadEvaluation>>();

        _retryPolicy = Policy<LeadEvaluation>
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, _, attempt, _) =>
                {
                    _logger.LogWarning(
                        eventId: new EventId(attempt),
                        message: "Error calling AI service. Attempt {AttemptNumber} of 3",
                        args: new object[] { attempt });
                });

        // Subscribe to evaluation results
        _messageBroker.Subscribe<LeadEvaluation>(EVALUATION_QUEUE + "-result", HandleEvaluationResult);
    }

    public async Task<LeadEvaluation> EvaluateLeadAsync(Lead lead)
    {
        var completionSource = new TaskCompletionSource<LeadEvaluation>();
        lock (_pendingEvaluations)
        {
            _pendingEvaluations[lead.Email] = completionSource;
        }

        try
        {
            // Publish lead to queue
            _messageBroker.PublishMessage(EVALUATION_QUEUE, lead);
            _logger.LogInformation("Published lead {Email} to evaluation queue", lead.Email);

            // Wait for response with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var evaluationTask = completionSource.Task;

            var completedTask = await Task.WhenAny(evaluationTask, Task.Delay(-1, cts.Token));
            if (completedTask == evaluationTask)
            {
                return await evaluationTask;
            }

            throw new TimeoutException("Lead evaluation timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating lead via queue: {Email}", lead.Email);

            // Fallback to direct HTTP call
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var response = await _httpClient.PostAsJsonAsync("/api/leads/evaluate", lead);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<LeadEvaluation>()
                       ?? throw new InvalidOperationException("Failed to deserialize response");
            });
        }
        finally
        {
            // Clean up the completion source
            lock (_pendingEvaluations)
            {
                _pendingEvaluations.Remove(lead.Email);
            }
        }
    }

    private Task HandleEvaluationResult(LeadEvaluation evaluation)
    {
        _logger.LogInformation("Received evaluation result for lead {Email}", evaluation.Lead.Email);

        lock (_pendingEvaluations)
        {
            if (_pendingEvaluations.TryGetValue(evaluation.Lead.Email, out var completionSource))
            {
                completionSource.TrySetResult(evaluation);
                _pendingEvaluations.Remove(evaluation.Lead.Email);
            }
        }

        return Task.CompletedTask;
    }
}