using Microsoft.AspNetCore.Mvc;
using ServerX.Services;
using SharedModels;

namespace ServerX.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeadsController : ControllerBase
{
    private readonly ILeadEvaluationService _evaluationService;
    private readonly ICmsService _cmsService;
    private readonly ILogger<LeadsController> _logger;

    public LeadsController(ILeadEvaluationService evaluationService, ICmsService cmsService, ILogger<LeadsController> logger)
    {
        _evaluationService = evaluationService;
        _cmsService = cmsService;
        _logger = logger;
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
                await _cmsService.StoreQualifiedLeadAsync(lead);
            }

            return Ok(evaluation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating lead for {Email}", lead.Email);
            return StatusCode(500, "An error occurred while processing your request");
        }
    }

    // Test endpoint
    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new { Status = "ServerX is running!" });
    }
}

