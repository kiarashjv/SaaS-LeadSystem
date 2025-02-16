using Microsoft.AspNetCore.Mvc;
using SharedModels;

namespace ServerA.Controllers;


[ApiController]
[Route("api/[controller]")]
public class LeadsController : ControllerBase
{
    private readonly ILogger<LeadsController> _logger;
    private static readonly Random _random = new();

    public LeadsController(ILogger<LeadsController> logger)
    {
        _logger = logger;
    }

    [HttpPost("evaluate")]
    public async Task<IActionResult> EvaluateLead([FromBody] Lead lead)
    {
        _logger.LogInformation("AI Service evaluating lead: {Email}", lead.Email);

        // Simulate AI processing delay
        await Task.Delay(500);

        // Simple random evaluation (for demo purposes)
        var isQualified = _random.NextDouble() > 0.5;
        var reason = isQualified
            ? "Lead matches our target customer profile"
            : "Lead does not match our current criteria";

        var evaluation = new LeadEvaluation
        {
            Lead = lead,
            IsQualified = isQualified,
            Reason = reason
        };

        _logger.LogInformation("Lead {Email} evaluation complete. Qualified: {IsQualified}",
            lead.Email, isQualified);

        return Ok(evaluation);
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new { Status = "ServerA (AI Service) is running!" });
    }

}