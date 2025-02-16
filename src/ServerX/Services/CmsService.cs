using SharedModels;

namespace ServerX.Services;

public interface ICmsService
{
    Task<Lead> StoreQualifiedLeadAsync(Lead lead);
}

public class CmsService : ICmsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CmsService> _logger;

    public CmsService(HttpClient httpClient, ILogger<CmsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Lead> StoreQualifiedLeadAsync(Lead lead)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/leads", lead);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<Lead>()
                   ?? throw new InvalidOperationException("Failed to deserialize response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing qualified lead in CMS: {Email}", lead.Email);
            throw;
        }
    }
}