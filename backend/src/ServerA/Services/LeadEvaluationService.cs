using SharedModels;

namespace ServerA.Services;

public interface ILeadEvaluationService
{
    Task<LeadEvaluation> EvaluateLead(Lead lead);
}

public class LeadEvaluationService : ILeadEvaluationService
{
    private readonly ILogger<LeadEvaluationService> _logger;

    public LeadEvaluationService(ILogger<LeadEvaluationService> logger)
    {
        _logger = logger;
    }

    public async Task<LeadEvaluation> EvaluateLead(Lead lead)
    {
        // Simulate AI processing delay
        await Task.Delay(100);

        var (isQualified, reason) = EvaluateLeadCriteria(lead);

        _logger.LogInformation("Lead {Email} evaluation complete. Qualified: {IsQualified}",
            lead.Email, isQualified);

        return new LeadEvaluation
        {
            Lead = lead,
            IsQualified = isQualified,
            Reason = reason
        };
    }

    private (bool isQualified, string reason) EvaluateLeadCriteria(Lead lead)
    {
        // Company name criteria
        if (lead.CompanyName.Length < 3)
        {
            return (false, "Company name is too short");
        }

        // Email validation
        if (!lead.Email.Contains("@") || !lead.Email.Contains("."))
        {
            return (false, "Invalid email format");
        }

        // Phone number validation (must be at least 10 digits)
        var digitsOnly = new string(lead.PhoneNumber.Where(char.IsDigit).ToArray());
        if (digitsOnly.Length < 10)
        {
            return (false, "Phone number must have at least 10 digits");
        }

        // Name validation (must be at least 2 words)
        var nameWords = lead.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (nameWords.Length < 2)
        {
            return (false, "Full name is required (first and last name)");
        }

        // If all criteria pass, the lead is qualified
        return (true, "Lead matches all qualification criteria");
    }
}