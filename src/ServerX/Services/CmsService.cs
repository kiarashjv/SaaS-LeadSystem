using System.Net.Http.Json;
using SharedModels;
using SharedMessaging;

namespace ServerX.Services;

public interface ICmsService
{
    Task<Lead> StoreQualifiedLeadAsync(Lead lead);
}

public class CmsService : ICmsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CmsService> _logger;
    private readonly IMessageBroker _messageBroker;
    private const string STORAGE_QUEUE = "lead-storage-queue";
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

        // Subscribe to storage results
        _messageBroker.Subscribe<Lead>(STORAGE_QUEUE + "-result", HandleStorageResult);
    }

    public async Task<Lead> StoreQualifiedLeadAsync(Lead lead)
    {
        var completionSource = new TaskCompletionSource<Lead>();
        lock (_pendingRequests)
        {
            _pendingRequests[lead.Email] = completionSource;
        }

        try
        {
            // Publish lead to storage queue
            _messageBroker.PublishMessage(STORAGE_QUEUE, lead);
            _logger.LogInformation("Published lead {Email} to storage queue", lead.Email);

            // Wait for response with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var storageTask = completionSource.Task;

            var completedTask = await Task.WhenAny(storageTask, Task.Delay(-1, cts.Token));
            if (completedTask == storageTask)
            {
                return await storageTask;
            }

            throw new TimeoutException("Lead storage timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing lead via queue: {Email}", lead.Email);

            // Fallback to direct HTTP call
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/leads", lead);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Lead>()
                       ?? throw new InvalidOperationException("Failed to deserialize response");
            }
            catch (Exception httpEx)
            {
                _logger.LogError(httpEx, "Error storing lead via HTTP: {Email}", lead.Email);
                throw;
            }
        }
        finally
        {
            // Clean up the completion source
            lock (_pendingRequests)
            {
                _pendingRequests.Remove(lead.Email);
            }
        }
    }

    private Task HandleStorageResult(Lead storedLead)
    {
        _logger.LogInformation("Received storage confirmation for lead {Email}", storedLead.Email);

        lock (_pendingRequests)
        {
            if (_pendingRequests.TryGetValue(storedLead.Email, out var completionSource))
            {
                completionSource.TrySetResult(storedLead);
                _pendingRequests.Remove(storedLead.Email);
            }
        }

        return Task.CompletedTask;
    }
}