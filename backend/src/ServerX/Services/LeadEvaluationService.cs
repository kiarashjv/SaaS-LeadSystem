using System.Net.Http.Json;
using Polly;
using Polly.Retry;
using SharedMessaging;
using SharedModels;
using System.Text.Json;

namespace ServerX.Services;

public interface ILeadEvaluationService
{
    Task<LeadEvaluation> EvaluateLeadAsync(Lead lead);
    Task HandleEvaluationResult(LeadEvaluation evaluation);
}

public class LeadEvaluationService : ILeadEvaluationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LeadEvaluationService> _logger;
    private readonly IMessageBroker _messageBroker;
    private readonly AsyncRetryPolicy<LeadEvaluation> _retryPolicy;
    private const string EVALUATION_REQUEST_QUEUE = "lead-evaluation-queue";
    private const string EVALUATION_RESPONSE_QUEUE = "lead-evaluation-queue-result";
    private const int TIMEOUT_SECONDS = 30;
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
    }

    public async Task<LeadEvaluation> EvaluateLeadAsync(Lead lead)
    {
        _logger.LogInformation("Starting evaluation for {Email}", lead.Email);
        var completionSource = new TaskCompletionSource<LeadEvaluation>();

        lock (_pendingEvaluations)
        {
            _pendingEvaluations[lead.Email] = completionSource;
            _logger.LogDebug("Added pending evaluation for {Email}", lead.Email);
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            // Publish lead to queue
            _messageBroker.PublishMessage(EVALUATION_REQUEST_QUEUE, lead);
            _logger.LogInformation("Published lead {Email} to evaluation queue", lead.Email);

            try
            {
                var result = await completionSource.Task.WaitAsync(cts.Token);
                _logger.LogInformation("Evaluation completed successfully for {Email}", lead.Email);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Evaluation timed out for {Email}", lead.Email);
                throw new TimeoutException("Lead evaluation timed out");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating lead via queue: {Email}", lead.Email);
            return await FallbackToHttp(lead);
        }
        finally
        {
            lock (_pendingEvaluations)
            {
                _pendingEvaluations.Remove(lead.Email);
            }
        }
    }

    private async Task<LeadEvaluation> FallbackToHttp(Lead lead)
    {
        _logger.LogInformation("Attempting HTTP fallback for {Email}", lead.Email);
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var response = await _httpClient.PostAsJsonAsync("/api/leads/evaluate", lead);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<LeadEvaluation>()
                   ?? throw new InvalidOperationException("Failed to deserialize response");
        });
    }

    public Task HandleEvaluationResult(LeadEvaluation evaluation)
    {
        _logger.LogInformation("Received evaluation result for {Email}", evaluation.Lead.Email);
        _logger.LogDebug("Evaluation result: {@Evaluation}", evaluation);

        TaskCompletionSource<LeadEvaluation>? completionSource = null;
        lock (_pendingEvaluations)
        {
            if (_pendingEvaluations.TryGetValue(evaluation.Lead.Email, out completionSource))
            {
                _logger.LogInformation("Found pending evaluation for {Email}, setting result", evaluation.Lead.Email);
                _pendingEvaluations.Remove(evaluation.Lead.Email);
            }
            else
            {
                _logger.LogWarning("No pending evaluation found for {Email}", evaluation.Lead.Email);
            }
        }

        if (completionSource != null)
        {
            var success = completionSource.TrySetResult(evaluation);
            _logger.LogInformation("Result set {Success} for {Email}", success, evaluation.Lead.Email);
        }

        return Task.CompletedTask;
    }
}