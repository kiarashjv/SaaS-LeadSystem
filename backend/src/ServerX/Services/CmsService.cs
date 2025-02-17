using System.Net.Http.Json;
using Polly;
using Polly.Retry;
using SharedModels;
using SharedMessaging;

namespace ServerX.Services;

public interface ICmsService
{
    Task<Lead> StoreQualifiedLeadAsync(Lead lead);
    Task HandleStorageResult(Lead storedLead);
}

public class CmsService : ICmsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CmsService> _logger;
    private readonly IMessageBroker _messageBroker;
    private readonly AsyncRetryPolicy<Lead> _retryPolicy;
    private const string STORAGE_QUEUE = "lead-storage-queue";
    private const string STORAGE_RESPONSE_QUEUE = "lead-storage-queue-result";
    private readonly Dictionary<string, TaskCompletionSource<Lead>> _pendingRequests;

    public CmsService(
        HttpClient httpClient,
        ILogger<CmsService> logger,
        IMessageBroker messageBroker)
    {
        _httpClient = httpClient;
        _logger = logger;
        _messageBroker = messageBroker;
        _pendingRequests = new Dictionary<string, TaskCompletionSource<Lead>>();


        _retryPolicy = Policy<Lead>
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, _, attempt, _) =>
                {
                    _logger.LogWarning(
                        eventId: new EventId(attempt),
                        message: "Error calling Storage service. Attempt {AttemptNumber} of 3",
                        args: new object[] { attempt });
                });
    }

    public async Task<Lead> StoreQualifiedLeadAsync(Lead lead)
    {
        var completionSource = new TaskCompletionSource<Lead>();
        lock (_pendingRequests)
        {
            _pendingRequests[lead.Email] = completionSource;
            _logger.LogDebug("Added pending storage request for {Email}", lead.Email);
        }

        try
        {
            _messageBroker.PublishMessage(STORAGE_QUEUE, lead);
            _logger.LogInformation("Published lead {Email} to storage queue", lead.Email);

            // Add a small delay to ensure subscription is ready
            await Task.Delay(100);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                var result = await completionSource.Task.WaitAsync(cts.Token);
                _logger.LogInformation("Storage completed successfully for {Email}", lead.Email);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Storage timed out for {Email}", lead.Email);
                throw new TimeoutException("Lead storage timed out");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing lead via queue: {Email}", lead.Email);

            lock (_pendingRequests)
            {
                _pendingRequests.Remove(lead.Email);
            }

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                _logger.LogInformation("Attempting HTTP fallback for {Email}", lead.Email);
                var response = await _httpClient.PostAsJsonAsync("/api/leads", lead);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Lead>()
                       ?? throw new InvalidOperationException("Failed to deserialize response");
            });
        }
    }

    public Task HandleStorageResult(Lead storedLead)
    {
        _logger.LogInformation("Received storage confirmation for lead {Email}", storedLead.Email);

        TaskCompletionSource<Lead>? completionSource = null;
        lock (_pendingRequests)
        {
            if (_pendingRequests.TryGetValue(storedLead.Email, out completionSource))
            {
                _logger.LogInformation("Found pending request for {Email}, setting result", storedLead.Email);
                _pendingRequests.Remove(storedLead.Email);
            }
            else
            {
                _logger.LogWarning("No pending request found for {Email}", storedLead.Email);
            }
        }

        if (completionSource != null)
        {
            var success = completionSource.TrySetResult(storedLead);
            _logger.LogInformation("Result set {Success} for {Email}", success, storedLead.Email);
        }

        return Task.CompletedTask;
    }
}