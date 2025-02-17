using Microsoft.AspNetCore.Mvc;
using ServerX.Services;
using SharedMessaging;
using SharedModels;

namespace ServerX.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeadsController : ControllerBase
{
    private readonly ILeadEvaluationService _evaluationService;
    private readonly ICmsService _cmsService;
    private readonly ILogger<LeadsController> _logger;
    private readonly IMessageBroker _messageBroker;

    public LeadsController(
        ILeadEvaluationService evaluationService,
        ICmsService cmsService,
        ILogger<LeadsController> logger,
        IMessageBroker messageBroker)
    {
        _evaluationService = evaluationService;
        _cmsService = cmsService;
        _logger = logger;
        _messageBroker = messageBroker;
    }

    [HttpPost("evaluate")]
    public async Task<IActionResult> EvaluateLead([FromBody] Lead lead)
    {
        try
        {
            // Basic validation
            if (string.IsNullOrEmpty(lead.Email) || string.IsNullOrEmpty(lead.Name))
            {
                return BadRequest("Name and Email are required");
            }

            // Evaluate lead using AI service
            _logger.LogInformation("Evaluating lead for {Email}", lead.Email);
            var evaluation = await _evaluationService.EvaluateLeadAsync(lead);

            // If qualified, store in CMS
            if (evaluation.IsQualified)
            {
                _logger.LogInformation("Lead {Email} is qualified, storing in CMS", lead.Email);
                await _cmsService.StoreQualifiedLeadAsync(evaluation.Lead);
            }

            return Ok(evaluation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing lead: {Email}", lead.Email);
            return StatusCode(500, "An error occurred while processing your request");
        }
    }
}

