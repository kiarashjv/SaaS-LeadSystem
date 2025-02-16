namespace SharedModels;

public class Lead
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
}

public class LeadEvaluation
{
    public Lead Lead { get; set; } = new();
    public bool IsQualified { get; set; }
    public string? Reason { get; set; }
}