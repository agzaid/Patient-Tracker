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
public class MedicationsController : ControllerBase
{
    private readonly IMedicationService _medicationService;
    private readonly IStringLocalizer<ErrorMessages> _localizer;

    public MedicationsController(IMedicationService medicationService, IStringLocalizer<ErrorMessages> localizer)
    {
        _medicationService = medicationService;
        _localizer = localizer;
    }

    /// <summary>
    /// Get all medications for the authenticated user (paginated)
    /// </summary>
    /// <param name="parameters">Query parameters for pagination and search</param>
    /// <returns>Paginated list of medications</returns>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<MedicationDto>>> GetMedications([FromQuery] QueryParameters parameters)
    {
        var userId = GetUserId();
        var paginatedMedications = await _medicationService.GetMedicationsPaginatedAsync(userId, parameters.Page, parameters.PageSize, parameters.Search);
        return Ok(paginatedMedications);
    }

    /// <summary>
    /// Get a specific medication by ID
    /// </summary>
    /// <param name="id">Medication ID</param>
    /// <returns>Medication details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<MedicationDto>> GetMedication(int id)
    {
        var userId = GetUserId();
        var medication = await _medicationService.GetMedicationAsync(id, userId);
        
        if (medication == null)
        {
            return NotFound(new { error = _localizer["MedicationNotFound"] });
        }

        return Ok(medication);
    }

    /// <summary>
    /// Create a new medication
    /// </summary>
    /// <param name="request">Medication creation request</param>
    /// <returns>Created medication</returns>
    [HttpPost]
    public async Task<ActionResult<MedicationDto>> CreateMedication([FromBody] CreateMedicationRequest request)
    {
        var userId = GetUserId();
        var medication = await _medicationService.CreateMedicationAsync(userId, request);
        return CreatedAtAction(nameof(GetMedication), new { id = medication.Id }, medication);
    }

    /// <summary>
    /// Update an existing medication
    /// </summary>
    /// <param name="id">Medication ID</param>
    /// <param name="request">Medication update request</param>
    /// <returns>Updated medication</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<MedicationDto>> UpdateMedication(int id, [FromBody] UpdateMedicationRequest request)
    {
        var userId = GetUserId();
        var medication = await _medicationService.UpdateMedicationAsync(id, userId, request);
        return Ok(medication);
    }

    /// <summary>
    /// Delete a medication
    /// </summary>
    /// <param name="id">Medication ID</param>
    /// <returns>Delete result</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMedication(int id)
    {
        var userId = GetUserId();
        var result = await _medicationService.DeleteMedicationAsync(id, userId);
        
        if (!result)
        {
            return NotFound(new { error = _localizer["MedicationNotFound"] });
        }

        return Ok(new { message = _localizer["MedicationDeletedSuccessfully"] });
    }

    /// <summary>
    /// Get a medication document with its extracted medications
    /// </summary>
    /// <param name="documentId">Document ID</param>
    /// <returns>Medication document with medications</returns>
    [HttpGet("documents/{documentId}")]
    public async Task<ActionResult<MedicationDocumentWithMedicationsDto>> GetMedicationDocumentWithMedications(int documentId)
    {
        var userId = GetUserId();
        var extractionService = HttpContext.RequestServices.GetRequiredService<IMedicationExtractionService>();
        var document = await extractionService.GetMedicationDocumentWithMedicationsAsync(userId, documentId);
        
        if (document == null)
        {
            return NotFound(new { error = _localizer["MedicationDocumentNotFound"] });
        }

        return Ok(document);
    }

    /// <summary>
    /// Get paginated medication documents for the authenticated user
    /// </summary>
    /// <param name="parameters">Query parameters for pagination and search</param>
    /// <returns>Paginated list of medication documents</returns>
    [HttpGet("documents")]
    public async Task<ActionResult<PaginatedResponse<MedicationDocumentDto>>> GetMedicationDocuments(
        [FromQuery] MedicationDocumentsQueryParameters parameters)
    {
        var userId = GetUserId();
        var extractionService = HttpContext.RequestServices.GetRequiredService<IMedicationExtractionService>();
        var documents = await extractionService.GetMedicationDocumentsAsync(userId, parameters.Page, parameters.PageSize, parameters.Search);
        
        return Ok(documents);
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException(_localizer["InvalidUserIdentifier"]);
        }
        return userId;
    }
}
