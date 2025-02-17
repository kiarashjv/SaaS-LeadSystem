using Microsoft.AspNetCore.Mvc;
using SharedModels;
using SharedMessaging;

namespace ServerA.Controllers;


[ApiController]
[Route("api/[controller]")]
public class LeadsController : ControllerBase
{
    private readonly ILogger<LeadsController> _logger;
    private readonly IMessageBroker _messageBroker;
    private static readonly Random _random = new();
    private const string EVALUATION_QUEUE = "lead-evaluation-queue";

    public LeadsController(
        ILogger<LeadsController> logger,
        IMessageBroker messageBroker)
    {
        _logger = logger;
        _messageBroker = messageBroker;

        // Subscribe to incoming leads
        _messageBroker.Subscribe<Lead>(EVALUATION_QUEUE, HandleLeadEvaluation);
    }

    private async Task HandleLeadEvaluation(Lead lead)
    {
        try
        {
            _logger.LogInformation("AI Service evaluating lead from queue: {Email}", lead.Email);

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

            // Publish evaluation result back to queue
            _messageBroker.PublishMessage(EVALUATION_QUEUE + "-result", evaluation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing lead from queue: {Email}", lead.Email);
            throw;
        }
    }

    [HttpPost("evaluate")]
    public async Task<IActionResult> EvaluateLead([FromBody] Lead lead)
    {
        try
        {
            _logger.LogInformation("AI Service evaluating lead via HTTP: {Email}", lead.Email);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating lead: {Email}", lead.Email);
            return StatusCode(500, "An error occurred while evaluating the lead");
        }
    }

}