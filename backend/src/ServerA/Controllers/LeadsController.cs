using Microsoft.AspNetCore.Mvc;
using SharedModels;
using SharedMessaging;
using ServerA.Services;

namespace ServerA.Controllers;


[ApiController]
[Route("api/[controller]")]
public class LeadsController : ControllerBase
{
    private readonly ILogger<LeadsController> _logger;
    private readonly IMessageBroker _messageBroker;
    private readonly ILeadEvaluationService _evaluationService;
    private const string EVALUATION_REQUEST_QUEUE = "lead-evaluation-queue";
    private const string EVALUATION_RESPONSE_QUEUE = "lead-evaluation-queue-result";

    public LeadsController(
        ILogger<LeadsController> logger,
        IMessageBroker messageBroker,
        ILeadEvaluationService evaluationService)
    {
        _logger = logger;
        _messageBroker = messageBroker;
        _evaluationService = evaluationService;
    }

    [NonAction]
    public async Task HandleLeadEvaluation(Lead lead)
    {
        try
        {
            _logger.LogInformation("AI Service evaluating lead from queue: {Email}", lead.Email);
            var evaluation = await _evaluationService.EvaluateLead(lead);
            _messageBroker.PublishMessage(EVALUATION_RESPONSE_QUEUE, evaluation);
            _logger.LogInformation("Published evaluation result for: {Email}", lead.Email);
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
            var evaluation = await _evaluationService.EvaluateLead(lead);
            return Ok(evaluation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating lead: {Email}", lead.Email);
            return StatusCode(500, "An error occurred while evaluating the lead");
        }
    }

}