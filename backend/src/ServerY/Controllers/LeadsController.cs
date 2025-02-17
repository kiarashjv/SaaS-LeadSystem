using Microsoft.AspNetCore.Mvc;
using ServerY.Services;
using SharedModels;
using SharedMessaging;

namespace ServerY.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeadsController : ControllerBase
{
    private readonly ILeadStorageService _storageService;
    private readonly ILogger<LeadsController> _logger;
    private readonly IMessageBroker _messageBroker;
    private const string STORAGE_REQUEST_QUEUE = "lead-storage-queue";
    private const string STORAGE_RESPONSE_QUEUE = "lead-storage-queue-result";

    public LeadsController(
        ILeadStorageService storageService,
        ILogger<LeadsController> logger,
        IMessageBroker messageBroker)
    {
        _storageService = storageService;
        _logger = logger;
        _messageBroker = messageBroker;
    }

    [NonAction]
    public async Task HandleLeadStorage(Lead lead)
    {
        try
        {
            _logger.LogInformation("Storing qualified lead from queue: {Email}", lead.Email);
            var storedLead = await _storageService.StoreLead(lead);

            // Publish storage result back to queue
            _messageBroker.PublishMessage(STORAGE_RESPONSE_QUEUE, storedLead);
            _logger.LogInformation("Lead storage result published for: {Email}", lead.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing lead from queue: {Email}", lead.Email);
            throw;
        }
    }

    // Keep HTTP endpoints as fallback
    [HttpPost]
    public async Task<IActionResult> StoreLead([FromBody] Lead lead)
    {
        try
        {
            _logger.LogInformation("Storing qualified lead via HTTP: {Email}", lead.Email);
            var storedLead = await _storageService.StoreLead(lead);
            return Ok(storedLead);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing lead: {Email}", lead.Email);
            return StatusCode(500, "An error occurred while storing the lead");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetQualifiedLeads()
    {
        try
        {
            var leads = await _storageService.GetQualifiedLeads();
            return Ok(leads);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving qualified leads");
            return StatusCode(500, "An error occurred while retrieving leads");
        }
    }

    [HttpGet("{email}")]
    public async Task<IActionResult> GetLeadByEmail(string email)
    {
        try
        {
            var lead = await _storageService.GetLeadByEmail(email);
            if (lead == null)
            {
                return NotFound($"No lead found with email: {email}");
            }
            return Ok(lead);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving lead: {Email}", email);
            return StatusCode(500, "An error occurred while retrieving the lead");
        }
    }
}