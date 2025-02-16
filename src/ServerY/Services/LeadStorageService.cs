using SharedModels;

namespace ServerY.Services;

public interface ILeadStorageService
{
    Task<Lead> StoreLead(Lead lead);
    Task<IEnumerable<Lead>> GetQualifiedLeads();
    Task<Lead?> GetLeadByEmail(string email);
}

public class LeadStorageService : ILeadStorageService
{
    private readonly ILogger<LeadStorageService> _logger;
    private static readonly List<Lead> _qualifiedLeads = new();
    private static readonly object _lock = new();

    public LeadStorageService(ILogger<LeadStorageService> logger)
    {
        _logger = logger;
    }

    public Task<Lead> StoreLead(Lead lead)
    {
        lock (_lock)
        {
            var existingLead = _qualifiedLeads.FirstOrDefault(l => l.Email == lead.Email);
            if (existingLead != null)
            {
                _logger.LogInformation("Updating existing lead: {Email}", lead.Email);
                _qualifiedLeads.Remove(existingLead);
            }

            _qualifiedLeads.Add(lead);
            _logger.LogInformation("Stored qualified lead: {Email}", lead.Email);
        }

        return Task.FromResult(lead);
    }

    public Task<IEnumerable<Lead>> GetQualifiedLeads()
    {
        return Task.FromResult(_qualifiedLeads.AsEnumerable());
    }

    public Task<Lead?> GetLeadByEmail(string email)
    {
        var lead = _qualifiedLeads.FirstOrDefault(l => l.Email == email);
        return Task.FromResult(lead);
    }
}