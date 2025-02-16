using System.Net.Http.Json;
using Polly;
using Polly.Retry;
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
    private readonly AsyncRetryPolicy<LeadEvaluation> _retryPolicy;

    public LeadEvaluationService(HttpClient httpClient, ILogger<LeadEvaluationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

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
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var response = await _httpClient.PostAsJsonAsync("/api/leads/evaluate", lead);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<LeadEvaluation>()
                   ?? throw new InvalidOperationException("Failed to deserialize response");
        });
    }
}