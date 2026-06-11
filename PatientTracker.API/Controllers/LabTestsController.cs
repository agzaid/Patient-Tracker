using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PatientTracker.Application.DTOs;
using PatientTracker.Application.Services;
using PatientTracker.Application.Resources;
using System.Security.Claims;

namespace PatientTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LabTestsController : ControllerBase
{
    private readonly ILabTestService _labTestService;
    private readonly IStringLocalizer<ErrorMessages> _localizer;

    public LabTestsController(ILabTestService labTestService, IStringLocalizer<ErrorMessages> localizer)
    {
        _labTestService = labTestService;
        _localizer = localizer;
    }

    /// <summary>
    /// Get all lab tests for the authenticated user (paginated)
    /// </summary>
    /// <param name="parameters">Query parameters for pagination and search</param>
    /// <returns>Paginated list of lab tests</returns>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<LabTestDto>>> GetLabTests([FromQuery] QueryParameters parameters)
    {
        var userId = GetUserId();
        var paginatedLabTests = await _labTestService.GetLabTestsPaginatedAsync(userId, parameters.Page, parameters.PageSize, parameters.Search);
        return Ok(paginatedLabTests);
    }

    /// <summary>
    /// Get a specific lab test by ID
    /// </summary>
    /// <param name="id">Lab test ID</param>
    /// <returns>Lab test details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<LabTestDto>> GetLabTest(int id)
    {
        var userId = GetUserId();
        var labTest = await _labTestService.GetLabTestAsync(id, userId);
        
        if (labTest == null)
        {
            return NotFound(new { error = _localizer["LabTestNotFound"] });
        }

        return Ok(labTest);
    }

    /// <summary>
    /// Create a new lab test
    /// </summary>
    /// <param name="request">Lab test creation request</param>
    /// <returns>Created lab test</returns>
    [HttpPost]
    public async Task<ActionResult<LabTestDto>> CreateLabTest([FromBody] CreateLabTestRequest request)
    {
        var userId = GetUserId();
        var labTest = await _labTestService.CreateLabTestAsync(userId, request);
        return CreatedAtAction(nameof(GetLabTest), new { id = labTest.Id }, labTest);
    }

    /// <summary>
    /// Update an existing lab test
    /// </summary>
    /// <param name="id">Lab test ID</param>
    /// <param name="request">Lab test update request</param>
    /// <returns>Updated lab test</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<LabTestDto>> UpdateLabTest(int id, [FromBody] UpdateLabTestRequest request)
    {
        var userId = GetUserId();
        var labTest = await _labTestService.UpdateLabTestAsync(id, userId, request);
        return Ok(labTest);
    }

    /// <summary>
    /// Delete a lab test
    /// </summary>
    /// <param name="id">Lab test ID</param>
    /// <returns>Delete result</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLabTest(int id)
    {
        var userId = GetUserId();
        var result = await _labTestService.DeleteLabTestAsync(id, userId);
        
        if (!result)
        {
            return NotFound(new { error = "Lab test not found" });
        }

        return Ok(new { message = _localizer["LabTestDeletedSuccessfully"] });
    }

    [HttpGet("documents")]
    public async Task<ActionResult<PaginatedResponse<LabTestDocumentDto>>> GetLabTestDocuments(
        [FromQuery] LabTestDocumentsQueryParameters parameters)
    {
        var userId = GetUserId();
        var extractionService = HttpContext.RequestServices.GetRequiredService<ILabTestExtractionService>();
        var documents = await extractionService.GetLabTestDocumentsAsync(userId, parameters.Page, parameters.PageSize, parameters.Search);
        
        return Ok(documents);
    }

    [HttpGet("documents/{documentId}")]
    public async Task<ActionResult<LabTestDocumentWithTestsDto>> GetLabTestDocumentWithTests(int documentId)
    {
        var userId = GetUserId();
        var extractionService = HttpContext.RequestServices.GetRequiredService<ILabTestExtractionService>();
        var document = await extractionService.GetLabTestDocumentWithTestsAsync(userId, documentId);
        
        if (document == null)
        {
            return NotFound(new { error = _localizer["LabTestNotFound"] });
        }

        return Ok(document);
    }

    /// <summary>
    /// Delete a lab test document and all related data
    /// </summary>
    /// <param name="documentId">Lab test document ID</param>
    /// <returns>Delete result</returns>
    
    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user identifier");
        }
        return userId;
    }
}
